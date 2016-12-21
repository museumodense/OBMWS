using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;
using System.Web;
using System.Xml;
using OBMWS.security;

#region license
//	GNU General Public License (GNU GPLv3)

//	Copyright © 2016 Odense Bys Museer

//	Author: Andriy Volkov

//	Source URL:	https://github.com/odensebysmuseer/OBMWS

//	This program is free software: you can redistribute it and/or modify
//	it under the terms of the GNU General Public License as published by
//	the Free Software Foundation, either version 3 of the License, or
//	(at your option) any later version.

//	This program is distributed in the hope that it will be useful,
//	but WITHOUT ANY WARRANTY; without even the implied warranty of
//	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
//	See the GNU General Public License for more details.

//	You should have received a copy of the GNU General Public License
//	along with this program.  If not, see <http://www.gnu.org/licenses/>.
#endregion

namespace OBMWS
{
    public abstract class WSClientMeta : WSServerMeta
    {
        public WSStatus LoadStatus = WSStatus.NONE_Copy();
        
        #region DYNAMIC AREA
        public WSRequest Request { get; private set; }
        public WSClientMeta(HttpContext _context)
        {
            Request = new WSRequest(_context, SecurityMap, ClientFunctions);

            preloadStaticSources();

            if (ReloadRAMRequired) { reloadCoreSources(); }
        }
        public override bool ValidateOneMinTicket(string _1MinTicket)
        {
            return Request.Security.ValidateOneMinTicket(_1MinTicket);
        }
        protected void preloadStaticSources()
        {
            try
            {
                #region SYSTEM
                if (Request.Security != null)
                {
                    SaveStaticSource(new WSSource(typeof(WSSystemEntity), ClientFunctions, WSConstants.ALIACES.ACTION_SYSTEM.ALIACES, "System", Request.Security.AuthToken.User.role, true, WSConstants.ACCESS_LEVEL.ADMIN)
                    {
                        Params = new List<WSParam>() { },
                        ShowMessageInaccessible = true
                    });
                }
                #endregion

                loadStaticSources();
            }
            catch (Exception e) { RegError(GetType(), e, ref LoadStatus); }
        }
        public abstract void loadStaticSources();
        public abstract void RegError(Type caller, Exception e, ref WSStatus status, string errorMsg = null);

        public readonly WSSources<WSSource> STATIC_SOURCES = new WSSources<WSSource>();
        public void SaveStaticSource(WSSource src)
        {
            try
            {
                if (!STATIC_SOURCES.Any(s => s.Match(src))) { STATIC_SOURCES.Add(src); }
                else { STATIC_SOURCES.FirstOrDefault(x => x.Match(src)).Merge(src); }
            }
            catch (Exception e) { RegError(GetType(), e, ref LoadStatus); }
        }

        internal WSAccessKeyEntity GenerateWSAccessKey()
        {
            WSAccessKeyEntity key = null;
            if (Request.Security.IsLogged && Request.Security.AuthToken.User.role >= WSConstants.ACCESS_LEVEL.ADMIN && Request.INPUT.Any(x => WSConstants.ALIACES.USER_ID.Match(x.Key)))
            {
                using (WSDataContext DBContext = GetInternalContext(Request.Meta.DB, Request.ID, $"{GetType().Name}.GenerateWSAccessKey()"))
                {
                    string UserID = Request.INPUT.ReadValue(WSConstants.ALIACES.USER_ID, out UserID) ? UserID : null;
                    Func<Type, bool> userFunc = a => a.Name.Equals("User");
                    PropertyInfo sessionProp = /*Request.*/DBContext
                            .GetType()
                            .GetProperties()
                            .FirstOrDefault(x => x.PropertyType.GetGenericTypeArguments().Any(userFunc));

                    Type userType = sessionProp == null ? null : sessionProp
                            .PropertyType
                            .GetGenericTypeArguments()
                            .FirstOrDefault(userFunc);

                    if (userType != null)
                    {
                        System.Reflection.MethodInfo mInfo = /*Request.*/DBContext.GetType().GetMethod("GetTable", new Type[] { });

                        var UObj = mInfo.MakeGenericMethod(new Type[] { userType }).Invoke(/*Request.*/DBContext, new object[] { });

                        Func<WSDynamicEntity, bool> func = s => s.getIdentities(ClientFunctions).FirstOrDefault(i => i.Key.ToLower().Equals("userid")).Value.ToString().ToLower().Equals(UserID.ToLower());

                        System.Reflection.MethodInfo[] methods = typeof(Enumerable).GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);

                        var method = methods.FirstOrDefault(m => m.Name == "FirstOrDefault" && m.GetParameters().Count() == 2).MakeGenericMethod(typeof(WSDynamicEntity));

                        WSDynamicEntity user = (WSDynamicEntity)method.Invoke(null, new object[] { UObj, func });

                        if (user != null)
                        {
                            object pass = null;
                            if (user.TryReadPropertyValue("Password", out pass))
                            {
                                key = new WSAccessKeyEntity(UserID.ToLower(), Request.Security.generateKey(new string[] { UserID.ToLower(), pass.ToString() }));
                            }
                        }
                    }
                }
            }
            return key;
        }
        
        public WSUserSet DYNAMIC_SOURCES
        {
            get
            {
                if (_DYNAMIC_SOURCES == null && Request!=null && !string.IsNullOrEmpty(Request.SessionID))
                {
                    DynamicSourcesCache cache = Cache.Read(Request.SessionID);

                    bool SessionCacheEmpty = DynamicSourcesCache.IsEmpty(cache);
                    bool SessionCacheIsOld = !SessionCacheEmpty && cache.timestapt < DateTime.Now.AddHours(-1);

                    if (ReloadRAMRequired || SessionCacheEmpty || SessionCacheIsOld)
                    {
                        WSLogRecord Log = new WSLogRecord("LOAD DYNAMIC SCHEMA");
                            
                        string SID = Request.SessionID;

                        DynamicSourcesCache userCache = new DynamicSourcesCache()
                        {
                            SessionID = SID,
                            timestapt = DateTime.Now,
                            userset = new WSUserSet(RoleSet, ReadWSSession, ClientFunctions)
                        }; 

                        Cache.Save(Request.SessionID, userCache, 10000);

                        Log.Save();
                    }
                    cache = Cache.Read(Request.SessionID);
                    _DYNAMIC_SOURCES = cache != null ? cache.userset : null;
                }
                return _DYNAMIC_SOURCES;
            }
        }
        public WSUserSet _DYNAMIC_SOURCES = null;
        
        private bool WriteToFile(object obj, string path, bool _override=false)
        {
            try
            {
                if (!File.Exists(path) || _override)
                {
                    using (TextWriter writer = new StreamWriter(path))
                    {
                        new XmlSerializer(obj.GetType()).Serialize(writer, obj);
                        return true;
                    }
                }
            }
            catch (Exception e) { RegError(GetType(), e, ref LoadStatus); }
            return false;
        }
        private WSSources<WSTableSource> ReadFile(string path)
        {
            WSSources<WSTableSource> sources = null;
            try
            {
                if (File.Exists(path))
                {
                    using (XmlReader reader = XmlReader.Create(path))
                    {
                        sources = (WSSources<WSTableSource>)new XmlSerializer(typeof(WSSources<WSTableSource>)).Deserialize(reader);
                    }
                }
            }
            catch (Exception e) { RegError(GetType(), e, ref LoadStatus); }
            return sources;
        }
        private WSSession ReadWSSession(string dbName)
        {
            WSSession session = null;
            if (!string.IsNullOrEmpty(dbName))
            {
                try
                {
                    WSSecurityMeta meta = SecurityMap[dbName];
                    session = new WSSession(Request.SessionID, meta);
                    /**********************
                     * ANDVO@NOTE: 
                     * DO NOT do [ZoneContext = Request.ZoneContext;] 
                     * because it will use Request's zone, when it MUST BE zone for the argument:'dbName' !!!
                     * */
                    using (WSDataContext ZoneContext = GetInternalContext(meta.Zone, Request.ID, $"{GetType().Name}.ReadWSSession('{dbName}')"))
                    {
                        if (ZoneContext != null && !ZoneContext.IsDisposed && ZoneContext.Connection.State==System.Data.ConnectionState.Open)
                        {
                            if (meta.SessionType != null)
                            {
                                MethodInfo mInfo = ZoneContext.GetType().GetMethod("GetTable", new Type[] { });

                                var tObj = mInfo.MakeGenericMethod(new Type[] { meta.SessionType }).Invoke(ZoneContext, new object[] { });

                                Func<WSDynamicEntity, bool> func = s => s.readPropertyValue(WSConstants.PARAMS.SESSIONID.NAME, "").ToString().ToLower().Equals(Request.SessionID.ToLower());

                                MethodInfo[] methods = typeof(Enumerable).GetMethods(BindingFlags.Static | BindingFlags.Public);

                                var method = methods.FirstOrDefault(m => m.Name == "FirstOrDefault" && m.GetParameters().Count() == 2).MakeGenericMethod(typeof(WSDynamicEntity));

                                WSDynamicEntity sessionEntity = (WSDynamicEntity)method.Invoke(null, new object[] { tObj, func });

                                if (sessionEntity != null)
                                {
                                    WSDynamicEntity userEntity = (WSDynamicEntity)sessionEntity.GetType().GetProperties().Single(x => x.PropertyType == meta.UserType).GetValue(sessionEntity, null);

                                    if (userEntity != null)
                                    {
                                        object _id = userEntity.TryReadPropertyValue("UserID", out _id) ? _id : null;
                                        object _email = userEntity.TryReadPropertyValue("Email", out _email) ? _email : null;
                                        object _firstname = userEntity.TryReadPropertyValue("FirstName", out _firstname) ? _firstname : null;
                                        object _lastname = userEntity.TryReadPropertyValue("LastName", out _lastname) ? _lastname : null;
                                        object _isactive = userEntity.TryReadPropertyValue("IsActive", out _isactive) ? _isactive : null;
                                        object _login = userEntity.TryReadPropertyValue("Login", out _login) ? _login : null;

                                        WSDynamicEntity roleEntity = (WSDynamicEntity)userEntity.GetType().GetProperties().FirstOrDefault(x => x.PropertyType == meta.RoleType).GetValue(userEntity, null);

                                        if (roleEntity != null)
                                        {
                                            object _role = roleEntity.TryReadPropertyValue("ID", out _role) ? _role : null;
                                            object _roleName = roleEntity.TryReadPropertyValue("Name", out _roleName) ? _roleName : null;

                                            int id = int.TryParse(_id.ToString(), out id) ? id : -1;
                                            string email = _email != null ? _email.ToString() : string.Empty;
                                            string login = _login != null ? _login.ToString() : string.Empty;
                                            string firstname = _firstname != null ? _firstname.ToString() : string.Empty;
                                            string lastname = _lastname != null ? _lastname.ToString() : string.Empty;
                                            bool isactive = bool.TryParse(_isactive.ToString(), out isactive) ? isactive : false;
                                            byte role = byte.TryParse(_role == null ? null : _role.ToString(), out role) ? role : WSConstants.DEFAULT_USER_ROLE;
                                            string roleName = _roleName != null ? _roleName.ToString() : string.Empty;

                                            session.user = new WSUserToken()
                                            {
                                                id = id,
                                                email = email,
                                                login = login,
                                                firstname = firstname,
                                                lastname = lastname,
                                                isactive = isactive,
                                                role = role,
                                                roleName = roleName
                                            };
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception e) { RegError(GetType(), e, ref LoadStatus); }
            }
            return session;
        }
        
        public void SaveDynamicSource(WSTableSource src)
        {
            try { if (src!=null && !string.IsNullOrEmpty(src.DBName) && CoreSources.ContainsKey(src.DBName) && CoreSources[src.DBName].Any(x => x.Match(src))) {
                    CoreSources[src.DBName].Single(x => x.Match(src)).Merge(src);
            } } catch (Exception e) { RegError(GetType(), e, ref LoadStatus); }
        }
        public bool IsAccessible(byte ACCESS_LEVEL) { return Request.Security != null && Request.Security.AuthToken.User.role >= ACCESS_LEVEL; } 
        private WSSource GetSource(string SRCName, string DBName = null)
        {
            WSSource src = null;
            try
            {
                Func<WSSource, bool> sFunc = s => s.Match(SRCName);
                src = STATIC_SOURCES.Any(sFunc) ? STATIC_SOURCES.Single(sFunc) : null;

                if (src == null && !string.IsNullOrEmpty(DBName) && DYNAMIC_SOURCES != null && DYNAMIC_SOURCES.ContainsKey(DBName))
                {
                    Func<WSTableSource, bool> dFunc = x => x.Match(SRCName, DBName);
                    src = DYNAMIC_SOURCES[DBName].Any(dFunc) ? DYNAMIC_SOURCES[DBName].Single(dFunc) : null;
                }
                if (src == null)
                {
                    //...throw error
                }
            }
            catch (Exception e) { RegError(GetType(), e, ref LoadStatus); }
            return src;
        }
        private Type GetSourceType(string SRCName, string DBName)
        {
            Type src = null;
            try
            {
                if (STATIC_SOURCES.Any(s => s.Match(SRCName)))
                {
                    src = STATIC_SOURCES.Single(s => s.Match(SRCName)).ReturnType;
                }
                else if (!string.IsNullOrEmpty(DBName) && SYSTEM_SOURCES != null && SYSTEM_SOURCES.ContainsKey(DBName))
                {
                    src = SYSTEM_SOURCES[DBName].Single(x => x.Match(SRCName, DBName)).ReturnType;
                }
            }
            catch (Exception e) { RegError(GetType(), e, ref LoadStatus, $"[{SRCName}] not found"); }
            return src;
        }
        public WSSource GetSourceByType(Type type, string _1MinTicket = null) {
            try
            {
                if (type != null)
                {
                    if (type.IsValidDynamicEntity())
                    {
                        bool isValidTicket = _1MinTicket != null && Request.Security != null && Request.Security.ValidateOneMinTicket(_1MinTicket);
                        WSUserSet set = isValidTicket ? SYSTEM_SOURCES : DYNAMIC_SOURCES;
                        return set == null ? null : set.GetSourceByType(type);
                    }
                    else if (STATIC_SOURCES.Any(s => s.ReturnType == type))
                    {
                        return STATIC_SOURCES.Single(s => s.ReturnType == type);
                    }
                }
            }
            catch (Exception e) { RegError(GetType(), e, ref LoadStatus); }
            return null;
        }
        #endregion

        public new void CleanUp()
        {
            Request.Dispose();
            InnerDispose();
            WSServerMeta.CleanUp();
        }
        public abstract void InnerDispose();
        public abstract string SrcBinaryKey { get; }

        public ClientFunctions ClientFunctions
        {
            get
            {
                if (_ClientFunctions == null) { _ClientFunctions = new ClientFunctions(IsAccessible, GetSource, GetSourceType, GetSourceByType, RegError, SrcBinaryKey); }
                return _ClientFunctions;
            }
        }
        private ClientFunctions _ClientFunctions = null;
    }
    public class DynamicSourcesCache
    {
        public string SessionID = string.Empty;
        public DateTime timestapt = DateTime.MinValue;
        public WSUserSet userset = null;

        internal void Flush()
        {
            throw new NotImplementedException();
        }

        internal static bool IsEmpty(DynamicSourcesCache cache)
        {
            return cache==null || cache.userset == null || !cache.userset.Any() || !cache.userset.SelectMany(x => x.Value.Select(a => a)).Any();
        }
    }
}