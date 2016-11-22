using OBMWS.security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

#region license
//	GNU General Public License (GNU GPLv3)
 
//	Copyright © 2016 Odense Bys Museer

//	Author: Andriy Volkov

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
    public class WSRequest : WSCall
    {
        private ClientFunctions CFunc = null;
        public WSRequest(HttpContext _InContext, Dictionary<string, WSSecurityMeta> _SecurityMap, ClientFunctions _CFunc)
            : base(_InContext)
        {
            Meta = _SecurityMap.Keys.FirstOrDefault(x => x.Equals(DB)) != null ? _SecurityMap[DB] : null;

            CFunc = _CFunc;
        }
        public WSRequestID ID { get { if (_ID == null) {
                    _ID = new WSRequestID(Url, SessionID);
                }
                return _ID;
            }
            private set { _ID = value; }
        }
        private WSRequestID _ID = null;

        #region Request base parameters
        #region FORMAT
        public WSFormat FORMAT
        {
            get
            {
                if (_FORMAT == null)
                {
                    _FORMAT = WSConstants.FORMAT.DEFAULT_FORMAT;
                    try
                    {
                        if (INPUT.ContainsKey(WSConstants.FORMAT.KEYNAME))
                        {
                            WSFormat temp_FORMAT = WSConstants.FORMAT.FORMATS.FirstOrDefault(p => p.name.Equals(INPUT[WSConstants.FORMAT.KEYNAME].ToLower()));
                            if (temp_FORMAT != null)
                            {
                                _FORMAT = temp_FORMAT;
                            }
                        }
                        else if (INPUT.ContainsKey("id"))
                        {
                            if (SOURCE.ALIACES.Contains("pdf"))
                            {
                                _FORMAT = WSConstants.FORMAT.PDF_FORMAT;
                            }
                            else if (SOURCE.ALIACES.Contains("image"))
                            {
                                _FORMAT = WSConstants.FORMAT.IMAGE_FORMAT;
                            }
                        }
                    }
                    catch (Exception e) { CFunc.RegError(GetType(), e, ref status); }
                }
                return _FORMAT;
            }
            set { _FORMAT = value; }
        }
        private WSFormat _FORMAT = null;
        #endregion
        #region CALLBACK
        public string CALLBACK
        {
            get
            {
                if (string.IsNullOrEmpty(_CALLBACK))
                {
                    try
                    {
                        if ((WSConstants.FORMAT.JSONP_FORMAT.Match(FORMAT) || WSConstants.FORMAT.JSONP_FORMAT.Match(FORMAT)) && INPUT.ContainsKey("callback"))
                        {
                            _CALLBACK = INPUT["callback"];
                        }
                    }
                    catch (Exception e) { CFunc.RegError(GetType(), e, ref status); }
                }
                return _CALLBACK;
            }
        }
        private string _CALLBACK = string.Empty;
        #endregion
        #region Action
        public WSValue ACTION
        {
            get
            {
                if (_ACTION == null) {
                    _ACTION = FORMAT.Match(WSConstants.FORMAT.IMAGE_FORMAT) ? WSConstants.PARAMS.IMG.ACTION.ReadWSValue(INPUT) : new WSConstants.PARAMS.ACTION().ReadWSValue(INPUT);
                }
                return _ACTION;
            }
        }
        private WSValue _ACTION = null;
        #endregion
        #region Verison
        private WSValue _VERSION = null;
        public WSValue VERSION
        {
            get
            {
                if (_VERSION == null) { _VERSION = WSConstants.PARAMS.VERSION.ReadWSValue(INPUT, WSConstants.PARAMS.IOVERSION.DEAULT); }
                return _VERSION;
            }
        }
        #endregion
        #region RecordID
        private WSValue _RECORD_ID = null;
        public WSValue RECORD_ID
        {
            get
            {
                if (_RECORD_ID == null) { _RECORD_ID = WSConstants.PARAMS.RECORD_ID.ReadWSValue(INPUT, null); }
                return _RECORD_ID;
            }
        }
        #endregion
        #region Count
        private int _COUNT = 0;
        public int COUNT
        {
            get
            {
                if (_COUNT == 0)
                {
                    try
                    {
                        string countVal = INPUT.ReadValue(WSConstants.PARAMS.COUNT, out countVal) ? countVal : "";
                        if (string.IsNullOrEmpty(countVal)) { _COUNT = WSConstants.DEFAULT_ITEMS_PER_REQUEST; }
                        else if (WSConstants.ALIACES.COUNT_ALL.Match(countVal)) { _COUNT = WSConstants.MAX_ITEMS_PER_REQUEST; }
                        else
                        {
                            _COUNT = int.TryParse(countVal, out _COUNT) ? _COUNT : WSConstants.DEFAULT_ITEMS_PER_REQUEST;
                        }
                    }
                    catch (Exception e) { CFunc.RegError(GetType(), e, ref status); _COUNT = WSConstants.DEFAULT_ITEMS_PER_REQUEST; }
                    if (Security.AuthToken.User.role <= WSConstants.ACCESS_LEVEL.READ && _COUNT > WSConstants.CONFIG.MaxItemsPerRequestForGuests) { _COUNT = WSConstants.CONFIG.MaxItemsPerRequestForGuests; }
                }
                return _COUNT;
            }
        }
        #endregion
        #region Offset
        private int _OFFSET = 0;
        public int OFFSET
        {
            get
            {
                if (_OFFSET == 0)
                {
                    try {
                        object o = WSConstants.PARAMS.OFFSET.ReadValue(INPUT);
                        if (o == null) { _OFFSET = WSConstants.DEFAULT_OFFSET; }
                        else { _OFFSET = (int)o; }
                    } catch (Exception e) { CFunc.RegError(GetType(), e, ref status); _OFFSET = WSConstants.DEFAULT_OFFSET; }
                }
                return _OFFSET;
            }
        }
        #endregion
        #region Override
        private bool _OVERRIDE = false;
        private bool _OVERRIDE_SET = false;
        public bool OVERRIDE
        {
            get
            {
                if (!_OVERRIDE_SET)
                {
                    try {
                        object o = null;
                        if (typeof(bool).Read(WSConstants.PARAMS.OVERRIDE.ReadValue(INPUT), out o))
                        {
                            _OVERRIDE = o != null ? (bool)o : false;
                            _OVERRIDE_SET = true;
                        }
                    } catch (Exception e) { CFunc.RegError(GetType(), e, ref status); _OVERRIDE = false; }
                }
                return _OVERRIDE;
            }
        }
        #endregion
        #region OutFields
        public WSParamList OUT_FIELDS
        {
            get
            {
                if (_OUT_FIELDS == null && SOURCE != null)
                {
                    _OUT_FIELDS = new WSParamList();
                    try {
                        string of = INPUT.Any(i => WSConstants.PARAMS.OUTPUT.Match(i.Key)) ?
                            INPUT.FirstOrDefault(i => WSConstants.PARAMS.OUTPUT.Match(i.Key)).Value : 
                            null;
                        of = string.IsNullOrEmpty(of) ? string.Empty : of.Trim(WSConstants.TRIM_CHARS);
                        string[] ofs = string.IsNullOrEmpty(of) ? new string[0] : of.Split(WSConstants.LIST_SEPARATORS, StringSplitOptions.RemoveEmptyEntries);
                        IEnumerable<WSParam> ioparams = 
                            ofs.Any()?
                            ofs.Select(k => SOURCE.GetXParam(k)).Where(x => x != null).Distinct(new WSParam.PComparer()):
                            new List<WSParam>();
                        if (ioparams.Any()) { _OUT_FIELDS.AddRange(ioparams); }
                    } catch (Exception e) { CFunc.RegError(GetType(), e, ref status); }
                    if (!_OUT_FIELDS.Any()) _OUT_FIELDS = DISTINCT_FIELDS;
                }
                return _OUT_FIELDS;
            }
        }
        private WSParamList _OUT_FIELDS = null;
        #endregion
        #region DistinctFields
        private WSParamList _DISTINCT_FIELDS = null;
        public WSParamList DISTINCT_FIELDS
        {
            get
            {
                if (_DISTINCT_FIELDS == null && SOURCE != null)
                {
                    _DISTINCT_FIELDS = new WSParamList();
                    try
                    {
                        string of = INPUT.Any(i => WSConstants.PARAMS.DISTINCT.Match(i.Key)) ?
                            INPUT.FirstOrDefault(i => WSConstants.PARAMS.DISTINCT.Match(i.Key)).Value :
                            null;
                        of = string.IsNullOrEmpty(of) ? string.Empty : of.Trim(WSConstants.TRIM_CHARS);
                        string[] ofs = string.IsNullOrEmpty(of) ? new string[0] : of.Split(WSConstants.LIST_SEPARATORS, StringSplitOptions.RemoveEmptyEntries);
                        IEnumerable<WSParam> ioparams =
                            ofs.Any() ?
                            ofs.Select(k => SOURCE.GetXParam(k)).Where(x => x != null).Distinct(new WSParam.PComparer()) :
                            new List<WSParam>();

                        if (ioparams.Any()) {_DISTINCT_FIELDS.AddRange(ioparams);}
                    }
                    catch (Exception e) { CFunc.RegError(GetType(), e, ref status); }
                }
                return _DISTINCT_FIELDS;
            }
        }
        #endregion
        #region Mode
        private WSValue _MODE = null;
        public WSValue MODE
        {
            get
            {
                if (_MODE == null)
                {
                    #region READ MODE WSParam
                    try
                    {
                        WSParam mode = new WSConstants.PARAMS.MODE();
                        Func<KeyValuePair<string, string>, bool> expr = (x => mode.Match(x.Key));
                        _MODE = mode.ALLOWED_VALUES.FirstOrDefault(x => x.ALIACES.Contains(INPUT.FirstOrDefault(expr).Value));
                    }
                    catch (Exception e) { CFunc.RegError(GetType(), e, ref status); }
                    if (_MODE == null) { _MODE = WSConstants.PARAMS.MODE.DEFAULT; }
                    #endregion
                }
                return _MODE;
            }
        }
        #endregion
        #endregion

        #region SOURCE
        public WSSource SOURCE
        {
            get
            {
                if (_SOURCE == null)
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(SrcName))
                        {
                            _SOURCE = CFunc.GetSource(SrcName, DB);

                            if (_SOURCE is WSTableSource)
                            {
                                object jValue = null;
                                bool IsValidSchema = WSConstants.PARAMS.SCHEMA.TryReadValue(INPUT, out jValue);
                                WSJson json = IsValidSchema ? jValue.ToString().ToJson() : null;


                                WSEntitySchema _DynamicSchema = null;
                                if (json != null)
                                {
                                    if (json is WSJProperty && _SOURCE.Match(((WSJProperty)json).Key))
                                    {
                                        _DynamicSchema = new WSEntitySchema((WSTableSource)_SOURCE, (WSJProperty)json, CFunc, null, IsValidSchema);
                                    }
                                    else if (json is WSJObject)
                                    {
                                        WSJProperty jProp = ((WSJObject)json).Value.FirstOrDefault(x => SOURCE.Match(x.Key));
                                        if (jProp != null && jProp.IsValid)
                                        {
                                            _DynamicSchema = new WSEntitySchema((WSTableSource)_SOURCE, jProp, CFunc, null, IsValidSchema);
                                        }
                                    }
                                }

                                ((WSTableSource)_SOURCE).DynamicSchema = _DynamicSchema == null || !_DynamicSchema.IsValid ?
                                    new WSEntitySchema(
                                        (WSTableSource)_SOURCE,
                                        new WSJProperty(
                                            _SOURCE.NAME.ToLower(),
                                            new WSJArray() { Value = new List<WSJson> { new WSJValue(WSConstants.ALIACES.ALL_PRIMITIVE_FIELDS.NAME) } }
                                        ),
                                        CFunc,
                                        null,
                                        false) :
                                    _DynamicSchema;
                            }
                        }
                    }
                    catch (Exception e) { CFunc.RegError(GetType(), e, ref status); }
                }
                return _SOURCE;
            }
        }
        private WSSource _SOURCE = null;
        public Type SrcType
        {
            get
            {
                if (_SrcType == null) { try { _SrcType = string.IsNullOrEmpty(SrcName) ? null : CFunc.GetSourceType(SrcName, DB); } catch (Exception e) { CFunc.RegError(GetType(), e, ref status); } }
                return _SrcType;
            }
        }
        private Type _SrcType = null;
        public string SrcName
        {
            get
            {
                if (_SrcName == null)
                {
                    _SrcName = "";
                    try
                    {
                        Func<KeyValuePair<string, string>, bool> func = s => WSConstants.PARAMS.KEYS.SOURCE.Equals(s.Key.ToLower());
                        _SrcName = INPUT.Any(func) ? INPUT.Single(func).Value : null;
                    }
                    catch (Exception e) { CFunc.RegError(GetType(), e, ref status); }
                }
                return _SrcName;
            }
        }
        private string _SrcName = null;
        #endregion
        
        #region DB Name
        public string DB
        {
            get
            {
                if (string.IsNullOrEmpty(_DB))
                {
                    _DB = !INPUT.ContainsKey(WSConstants.PARAMS.KEYS.DB) ? string.Empty : INPUT.Single(s => WSConstants.PARAMS.KEYS.DB.Equals(s.Key.ToLower())).Value;

                    _DB = string.IsNullOrEmpty(_DB) ? WSConstants.CONFIG.DefaultDB : _DB;
                }
                return _DB;
            }
        }
        private string _DB = null;
        #endregion

        #region Session
        private WSDynamicEntity ReadSession(/*WSDataContext ZoneContext*/)
        {
            WSDynamicEntity entity = null;
            if (Meta != null && !string.IsNullOrEmpty(SessionID) && ZoneContext != null)
            {
                long init_ticks = DateTime.Now.Ticks;
                try
                {
                    status.AddNote("Zone:" + Meta.Zone, WSConstants.ACCESS_LEVEL.READ);

                    System.Reflection.MethodInfo mInfo = ZoneContext.GetType().GetMethod("GetTable", new Type[] { });

                    var tObj = mInfo.MakeGenericMethod(new Type[] { Meta.SessionType }).Invoke(ZoneContext, new object[] { });

                    Func<WSDynamicEntity, bool> func = s => s.readPropertyValue(WSConstants.PARAMS.SESSIONID.NAME, "").ToString().ToLower().Equals(SessionID.ToLower());

                    System.Reflection.MethodInfo[] methods = typeof(Enumerable).GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);

                    var method = methods.FirstOrDefault(m => m.Name == "FirstOrDefault" && m.GetParameters().Count() == 2).MakeGenericMethod(typeof(WSDynamicEntity));

                    entity = (WSDynamicEntity)method.Invoke(null, new object[] { tObj, func });
                }
                catch (Exception e) { CFunc.RegError(GetType(), e, ref status); }

                //if (ZoneContext != null) ZoneContext.Close(/*SessionID*/);

                TimeSpan ticks1 = new TimeSpan(DateTime.Now.Ticks - init_ticks); init_ticks = DateTime.Now.Ticks;
            }
            return entity;
        }
        #endregion

        #region Security
        public WSSecurity Security
        {
            get
            {
                if (_Security == null && Meta != null)
                {
                    long init_ticks = DateTime.Now.Ticks;
                    try
                    {
                        //using (WSDataContext ZoneContext = WSServerMeta.GetInternalContext(Meta.Zone, ID))
                        //{
                            _Security = (WSSecurity)Activator.CreateInstance(Meta.SecurityType, new object[] { ReadSession(/*ZoneContext*/), SessionID, CFunc });

                            if (_Security.IsValid)
                            {
                                ID = new WSRequestID(Url, SessionID, _Security.AuthToken.User.role);
                            }
                            TimeSpan ticks1 = new TimeSpan(DateTime.Now.Ticks - init_ticks); init_ticks = DateTime.Now.Ticks;

                            status.AddNote("Security:" + _Security);
                        //}
                    }
                    catch (Exception e) { CFunc.RegError(GetType(), e, ref status); }
                }
                return _Security;
            }
        }
        private WSSecurity _Security = null;
        #endregion

        public WSDataContext DBContext { get { if (_DBContext==null) {
                    _DBContext = WSServerMeta.GetInternalContext(Meta.DB, ID, $"{GetType().Name}.DBContext ('{Url.EscapedUrl()}')");
                } return _DBContext;
            }
        } private WSDataContext _DBContext = null;

        public WSDataContext ZoneContext { get { if (_ZoneContext == null) {
                    _ZoneContext = WSServerMeta.GetInternalContext(Meta.Zone, ID, $"{GetType().Name}.ZoneContext ('{Url.EscapedUrl()}')");
                } return _ZoneContext;
            }
        } private WSDataContext _ZoneContext = null;
                
        public void Dispose()
        {
            try { if (DBContext != null)    { DBContext.Dispose();      _DBContext = null;      } } catch (Exception) { }
            try { if (ZoneContext != null)  { ZoneContext.Dispose();    _ZoneContext = null;    } } catch (Exception) { }
        }
    }
    public class WSRequestID
    {
        public WSRequestID(Uri _Url = null, string _SessionID = null, byte _URole = 0)
        {
            SessionID = string.IsNullOrEmpty(_SessionID) ? SessionID : _SessionID;
            URL = _Url != null ? _Url.PathAndQuery : URL;
            URole = _URole > 0 ? _URole : URole;
            ID = new WSConverter().ToMd5Hash(URL + URole + SessionID);
            LastModified = DateTime.Now;
        }
        public readonly DateTime Created = DateTime.Now;
        public DateTime LastModified { get; private set; } = DateTime.Now;
        public string SessionID { get; private set; } = "undefined";
        public string URL { get; private set; } = string.Empty;
        public byte URole { get; private set; } = 0;
        public string ID { get; private set; } = "undefined";

        public override string ToString() { return ID; }
        public override int GetHashCode() { return ID.GetHashCode(); }
        public override bool Equals(object obj) {
            return obj != null && obj.GetType() == GetType() && ID.Equals(((WSRequestID)obj).ID);
        }
    }
}