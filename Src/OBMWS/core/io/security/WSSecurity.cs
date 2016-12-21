using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

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

namespace OBMWS.security
{
    public abstract class WSSecurity
    {
        public WSStatus status = WSStatus.NONE_Copy();
        private string UPass = null;
        private MetaFunctions CFunc = null;
        public WSSecurity(WSDynamicEntity _Session, string _SessionID, MetaFunctions _CFunc)
        {
            Session = _Session;
            SessionID = _SessionID;
            CFunc = _CFunc;
            try
            {
                AuthToken.User = new WSUserToken();
                if (_Session != null)
                {
                    SessionID = _Session.readPropertyValue("SessionID").ToString();
                    object userObj = _Session.readPropertyValue("User");
                    if (userObj != null)
                    {
                        WSDynamicEntity user = (WSDynamicEntity)userObj;

                        object id = user.readPropertyValue("UserID");
                        object email = user.readPropertyValue("Email");
                        object login = user.readPropertyValue("Login");
                        object firstname = user.readPropertyValue("FirstName");
                        object lastname = user.readPropertyValue("LastName");
                        object role = user.readPropertyValue("RoleID");
                        object isactive = user.readPropertyValue("IsActive");
                        object password = user.readPropertyValue("Password");

                        AuthToken.User = new WSUserToken()
                        {
                            id = int.Parse(id.ToString()),
                            email = email == null ? "" : email.ToString(),
                            login = login == null ? "" : login.ToString(),
                            firstname = firstname == null ? "" : firstname.ToString(),
                            lastname = lastname == null ? "" : lastname.ToString(),
                            role = role == null ? (byte)0 : byte.Parse(role.ToString()),
                            isactive = isactive == null ? false : isactive.ToString().IsTrue()
                        };
                        WSCurrentUser = new WSCurrentUser(user);
                        UPass = password == null ? "" : password.ToString();
                    }
                }
            }
            catch (Exception e) { CFunc.RegError(GetType(), e, ref status); }
        }

        public bool IsLogged { get { return AuthToken.User != null && AuthToken.User.id > 0; } }

        public string SessionID { get; private set; }

        private WSDynamicEntity Session = null;
        
        #region AuthToken
        private WSAuthEntity _AuthToken = null;
        public WSAuthEntity AuthToken { get { if (_AuthToken == null) { _AuthToken = new WSAuthEntity() { access_token = SessionID }; } return _AuthToken; } }
        public void setAuthToken(WSAuthEntity _token, string _1MinTicket) { if (ValidateOneMinTicket(_1MinTicket)) { _AuthToken = _token; } }
        #endregion

        public WSCurrentUser WSCurrentUser { get; private set; }
        
        public bool ExitPrivateSession<S>(WSDataContext db, ClientFunctions CFunc, WSTableSource SessionSrc, ref WSStatus statusLines) where S : WSDynamicEntity
        {
            bool DEAUTHORIZED = false;
            try
            {
                AuthToken.issued = DateTime.MinValue;
                AuthToken.expires = DateTime.MinValue;
                AuthToken.expires_in = 0;
                AuthToken.User = null;

                if (Session == null) { DEAUTHORIZED = true; }
                else
                {
                    if (db != null)
                    {
                        //TODO@ANDVO:2016-11-09 : instead of looking for Primary Key value, - look for 'SessionID' field to make sure ALL related records will be removed

                        string idName = SessionSrc.PrimParams.Any() && SessionSrc.PrimParams.Count() == 1 ? SessionSrc.PrimParams.Single().WSColumnRef.NAME : null;
                        object idValue = null;

                        if (Session.TryReadPropertyValue(idName, out idValue))
                        {
                            ParameterExpression paramExp = Expression.Parameter(SessionSrc.ReturnType, "x");

                            Expression<Func<S, bool>> expr = new WSJValue(idValue.ToString()).GetFieldFilter(CFunc, (WSTableParam)SessionSrc.GetXParam(idName), paramExp, 0).ToLambda<S>(paramExp);

                            S delItem = db.GetTable<S>().FirstOrDefault(expr);

                            db.GetTable<S>().DeleteOnSubmit(delItem);
                            db.SubmitChanges();
                            DEAUTHORIZED = true;
                        }
                    }
                }
                if (DEAUTHORIZED) {
                    AuthToken.status = WSConstants.AUTH_STATES.DEAUTHORIZED;
                }
            }
            catch (Exception e)
            {
                CFunc.RegError(GetType(), e, ref statusLines);
                AuthToken.status = WSConstants.AUTH_STATES.FAILED_DEAUTHORIZE;
            }
            finally { WSServerMeta.ClearCache(SessionID); }
            return DEAUTHORIZED;
        }
        public void EnterPrivateSession<S>(WSDataContext db, ClientFunctions CFunc, WSTableSource SessionSrc, WSUserToken _user, string _1MinTicket, ref WSStatus statusLines, bool renew = false) where S : WSDynamicEntity
        {
            try
            {
                if(db!=null)
                {
                    if (ExitPrivateSession<S>(db, CFunc, SessionSrc, ref statusLines) && renew && _user.IsValid && ValidateOneMinTicket(_1MinTicket))
                    {
                        try
                        {
                            AuthToken.User = _user;

                            S _session = (S)Activator.CreateInstance(typeof(S), new object[] { });

                            setSession(DateTime.Now, SessionID, AuthToken.User.id, ref _session);

                            db.GetTable<S>().InsertOnSubmit(_session);

                            db.SubmitChanges();

                            Func<S, bool> func = s => s.readPropertyValue(WSConstants.PARAMS.SESSIONID.NAME, "").ToString().ToLower().Equals(SessionID.ToLower());

                            Session = db.GetTable<S>().FirstOrDefault(func);

                        }
                        catch (Exception e) { CFunc.RegError(GetType(), e, ref statusLines); Session = null; }
                    }
                }

                if (Session != null)
                {
                    setAuthToken(Session, ref _AuthToken);
                }
            }
            catch (Exception e) { CFunc.RegError(GetType(), e, ref statusLines); }
        }

        protected abstract void setAuthToken<S>(S session, ref WSAuthEntity AuthToken) where S : WSDynamicEntity;
        protected abstract void setSession<S>(DateTime now, string sessionID, int id, ref S session) where S : WSDynamicEntity;
        protected abstract string OneMinTicket(DateTime? _initTime = default(DateTime?));

        public bool ValidateOneMinTicket(string _1MinTicket)
        {
            try
            {
                string key1 = OneMinTicket(DateTime.Now.AddMinutes(-1));

                string key2 = OneMinTicket(DateTime.Now);

                string key3 = OneMinTicket(DateTime.Now.AddMinutes(1));

                return new string[] { key1, key2, key3 }.Contains(_1MinTicket);
            }
            catch (Exception e) { CFunc.RegError(GetType(), e, ref status); }
            return false;
        }

        public bool ValidateMD5Key(WSActivationKey _key, string _UPass)
        {
            bool isValid = false;
            try
            {
                if (_key == null) { }
                else if (string.IsNullOrEmpty(_key.key) || _key.key.Length < 8)
                {
                    isValid = false;
                }
                else if (string.IsNullOrEmpty(_UPass))
                {
                    isValid = false;
                }
                else if (string.IsNullOrEmpty(WSConstants.CONFIG.XPass))
                {
                    isValid = false;
                }
                else
                {
                    string checkKey = generateKey(_key.userid, _UPass);
                    isValid = checkKey.StartsWith(_key.key);
                }
            }
            catch (Exception e) { CFunc.RegError(GetType(), e, ref status); }
            return isValid;
        }
        internal bool IsValidUser(WSJson jUser)
        {
            bool _isValid = false;
            try
            {
                _isValid = AuthToken.User.Match(jUser);
            }
            catch (Exception e) { CFunc.RegError(GetType(), e, ref status); }
            return _isValid;
        }
        public string CreateMD5Key(string _UID = "", string _UPass = "")
        {
            return generateKey(!string.IsNullOrEmpty(_UID) ? _UID : AuthToken.User.id + "", !string.IsNullOrEmpty(_UPass) ? _UPass : UPass);
        }
        internal string generateKey(params string[] values)
        {
            string key = string.Empty;
            new WSConverter().ToMd5Hash(CFunc.SrcBinaryKey + (values.Any() ? values.Aggregate((a, b) => a + "" + b) : "") + WSConstants.CONFIG.XPass, out key);
            return key;
        }

        #region SharedKey handling
        public WSShareKey generateWSShareKeyByType(Type eType) {
            return new WSShareKey(eType, WSConstants.CONFIG.XPass, AuthToken.User.id.ToString());
        }
        internal bool isValidWSShareKey(WSJson shareKeys, Type eType)
        {
            bool _isValid = false;
            try
            {
                _isValid = generateWSShareKeyByType(eType).Match(shareKeys);
            }
            catch (Exception e) { CFunc.RegError(GetType(), e, ref status); }
            return _isValid;
        }
        #endregion

        #region AccessKey handling
        internal bool validateWSAccessKey(string hostKey, WSJson accessKey, out WSAccessKey key)
        {
            key = null;
            try
            {
                string _MD5Key = null;
                DateTime? _startDate = null;
                DateTime? _endDate = null;
                #region read 'accessKey'
                if (accessKey is WSJValue)
                {
                    _MD5Key = ((WSJValue)accessKey).Value;
                }
                else if (accessKey is WSJObject)
                {

                    List<WSJProperty> props = ((WSJObject)accessKey).Value;
                    foreach (WSJProperty prop in props)
                    {
                        string val = ((WSJValue)prop.Value).Value;
                        DateTime tempDate = DateTime.MinValue;
                        switch (prop.Key)
                        {
                            case "MD5Key":
                                _MD5Key = val;
                                break;
                            case "startDate":
                                tempDate = DateTime.MinValue;
                                if (new WSConverter().ToDate(val, out tempDate)) { _startDate = tempDate; }
                                else { _MD5Key = null; }
                                break;
                            case "endDate":
                                tempDate = DateTime.MaxValue;
                                if (new WSConverter().ToDate(val, out tempDate)) { _endDate = tempDate; }
                                else { _MD5Key = null; }
                                break;
                        }
                    }
                }
                #endregion
                if (!string.IsNullOrEmpty(_MD5Key))
                {
                    #region validate 'accessKey'
                    key = new WSAccessKey(this, hostKey, _MD5Key, _startDate, _endDate);
                    WSAccessKey tempKey = new WSAccessKey(
                        this, 
                        hostKey,
                        generateKey(
                            hostKey,
                            _startDate == null ? null : ((DateTime)_startDate).ToString(WSConstants.DATE_FORMAT_MIN),
                            _endDate == null ? null : ((DateTime)_endDate).ToString(WSConstants.DATE_FORMAT_MIN)
                        ),
                        _startDate,
                        _endDate
                    );
                    if (key.Match(tempKey)) { return true; }
                    else { key = null; }
                    #endregion
                }
            }
            catch (Exception e) { key = null; CFunc.RegError(GetType(), e, ref status); }
            return false;
        }
        #endregion

        public bool IsValid { get { return AuthToken != null && AuthToken.User != null; } }
        
        public override string ToString() { return AuthToken.User.id + ":" + AuthToken.User.role; }
    }
}