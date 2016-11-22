using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Configuration;
using System.Web;
using System.Drawing;
using System.IO;

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
    public class WSConstants
    {
        public WSConstants() { }
        private static int next_code { get { return _next_code++; } }
        private static int _next_code = 0;

        #region STATIC/CONST VALUES
        public const long SessionExpiresInSeconds = 3600;
        public const int MAX_DEEPNESS = byte.MaxValue;

        public const byte DEFAULT_DEEPNESS = 0;
        public const string DATE_FORMAT = "yyyy-MM-dd HH:mm:ss.fff";

        public const string DATE_FORMAT_MIN = "yyyy-MM-dd";
        public const string TIME_FORMAT = "hh:mm:ss.fff";
        public const string TIMESPAN_FORMAT = @"dd\.hh\:mm\:ss\.fff";
        public const string TIMESPAN_FORMAT_SIMPLE = @"hh\:mm\:ss";

        public static readonly char[] SQL_ARRAY_SEPARATOR = new char[] { ',' };
        public static readonly char[] JSON_ARRAY_SEPARATOR = new char[] { ',' };
        public static readonly char[] JSON_KEYVALUE_SEPARATOR = new char[] { ':' };
        public static readonly char[] JSON_ARRAY_TRIM_CHARS = new char[] { '[', ']' };
        public static readonly char[] JSON_OBJECT_TRIM_CHARS = new char[] { '{', '}' };
        public static readonly char[] TRIM_CHARS = new char[] { '{', '}', '[', ']', '(', ')', };

        public static readonly char[] DATE_TRIM_CHARS = new char[] { '.', '-', ':', '_', '/', '\\' };
        public static readonly char[] SIZE_SEPARATORS = new char[] { '.', ',', ':', ';', '/', '|' };
        public static readonly char[] LIST_SEPARATORS = new char[] { ',', ';', ' ', '+', '-' };
        public static readonly char[] GUID_LIST_SEPARATORS = new char[] { ',', ';', ' ', '+' };
        public static readonly char[] DATE_LIST_SEPARATORS = new char[] { ',', ';', '+' };
        public static readonly char[] NUMERIC_SEPARATORS = new char[] { ',', ';', ' ', '+' };
        public static readonly string[] LONG_TEXT_DBTYPES = new string[] { "NText", "Text", "Xml", "Image" };
        public const string TIMESPAN_REGEX_PATTERN_SIMPLE = @"(0[0-9]|1[0-9]|2[0-3])([\:])([0-5][0-9])([\:])([0-5][0-9])";//@"(\d?)(\.?)(\d{1,2})([\:])(\d{1,2})([\:])(\d{1,2})(\.?)(\d{0,3})";
        public const string TIMESPAN_REGEX_PATTERN = @"(\d?)(\.?)(0[0-9]|1[0-9]|2[0-3])([\:])([0-5][0-9])([\:])([0-5][0-9])(\.?)(\d{0,3})";//@"(\d?)(\.?)(\d{1,2})([\:])(\d{1,2})([\:])(\d{1,2})(\.?)(\d{0,3})";

        public const int MAX_ITEMS_ON_FULL_MODE = 1;
        public const int MAX_ITEMS_PER_REQUEST = short.MaxValue;
        public const int DEFAULT_OFFSET = 0;
        public const byte DEFAULT_USER_ROLE = ACCESS_LEVEL.READ;

        public const bool DEFAULT_SKIP_EMPTY_FIELD = false;

        private static int _DEFAULT_ITEMS_PER_REQUEST = 0;
        public static int DEFAULT_ITEMS_PER_REQUEST
        {
            get
            {
                if (_DEFAULT_ITEMS_PER_REQUEST <= 0)
                {
                    _DEFAULT_ITEMS_PER_REQUEST = 3;
                    try
                    {
                        _DEFAULT_ITEMS_PER_REQUEST = CONFIG.DefaultItemsPerRequest;
                    }
                    catch (Exception) { }
                }
                return _DEFAULT_ITEMS_PER_REQUEST;
            }
        }

        public static readonly WSValue[] SPECIAL_CASES = new WSValue[] { ALIACES.EMPTY, ALIACES.EXIST, ALIACES.IS_OWN };

        public static readonly Type[] NUMERIC_TYPES = new Type[]{
            typeof(short),
            typeof(int),
            typeof(double),
            typeof(float),
            typeof(decimal),
            typeof(byte)
        };

        public static readonly MethodInfo stringEqualsMethod = typeof(string).GetMethod("Equals", new Type[] { typeof(string) });
        public static readonly MethodInfo toStringMethod = typeof(object).GetMethod("ToString");
        public static readonly MethodInfo toFormattedStringMethod = typeof(DateTime).GetMethod("ToString", new Type[] { typeof(string) });
        public static readonly MethodInfo stringContainsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });
        public static readonly MethodInfo stringEndsWithMethod = typeof(string).GetMethod("EndsWith", new[] { typeof(string) });
        public static readonly MethodInfo stringStartsWithMethod = typeof(string).GetMethod("StartsWith", new[] { typeof(string) });
        public static readonly MethodInfo entitySetAnyMI = typeof(Enumerable).GetMethods().FirstOrDefault(x => x.Name.Equals("Any") && x.GetParameters().Count() == 1);
        public static readonly MethodInfo entitySetAnyMethodWithExpression = typeof(Enumerable).GetMethods().FirstOrDefault(x => x.Name.Equals("Any") && x.GetParameters().Count() == 2);

        public static Type[] SIMPLE_DATATYPES = new Type[] {
            typeof(int),
            typeof(short),
            typeof(float),
            typeof(double),
            typeof(long),
            typeof(decimal),
            typeof(byte),
            typeof(bool),
            typeof(string),
            typeof(TimeSpan),
            typeof(DateTime),
            typeof(Guid)
        };

        public const string EVENT_TYPE_READ = "Read";
        public const string EVENT_TYPE_INSERT = "Insert";
        public const string EVENT_TYPE_UPDATE = "Update";
        public const string EVENT_TYPE_DELETE = "Delete";

        #region STANDARD_ASP_URL_PARAMS
        public static readonly string[] STANDARD_ASP_URL_PARAMS = new string[]{
            "__utma",
            "__utmc",
            "__utmz",
            "ASP.NET_SessionId",
            "fbm_876939902336614",
            "fbm_881650921865512",
            "fbsr_881650921865512",
            "ALL_HTTP",
            "ALL_RAW",
            "APPL_MD_PATH",
            "APPL_PHYSICAL_PATH",
            "AUTH_TYPE",
            "AUTH_USER",
            "AUTH_PASSWORD",
            "LOGON_USER",
            "REMOTE_USER",
            "CERT_COOKIE",
            "CERT_FLAGS",
            "CERT_ISSUER",
            "CERT_KEYSIZE",
            "CERT_SECRETKEYSIZE",
            "CERT_SERIALNUMBER",
            "CERT_SERVER_ISSUER",
            "CERT_SERVER_SUBJECT",
            "CERT_SUBJECT",
            "CONTENT_LENGTH",
            "CONTENT_TYPE",
            "GATEWAY_INTERFACE",
            "HTTPS",
            "HTTPS_KEYSIZE",
            "HTTPS_SECRETKEYSIZE",
            "HTTPS_SERVER_ISSUER",
            "HTTPS_SERVER_SUBJECT",
            "INSTANCE_ID",
            "INSTANCE_META_PATH",
            //"LOCAL_ADDR",
            "PATH_INFO",
            "PATH_TRANSLATED",
            "QUERY_STRING",
            //"REMOTE_ADDR",
            //"REMOTE_HOST",
            //"REMOTE_PORT",
            //"REQUEST_METHOD",
            "SCRIPT_NAME",
            "SERVER_NAME",
            "SERVER_PORT",
            "SERVER_PORT_SECURE",
            "SERVER_PROTOCOL",
            "SERVER_SOFTWARE",
            "URL",
            "HTTP_CONNECTION",
            "HTTP_CONTENT_LENGTH",
            "HTTP_CONTENT_TYPE",
            "HTTP_ACCEPT",
            "HTTP_ACCEPT_ENCODING",
            "HTTP_ACCEPT_LANGUAGE",
            "HTTP_COOKIE",
            "HTTP_HOST",
            "HTTP_REFERER",
            "HTTP_USER_AGENT",
            "HTTP_ORIGIN",
            "HTTP_X_REQUESTED_WITH",
            "HTTP_DNT",
            "AspSessionIDManagerInitializeRequestCalled",
            "AspSession"
        };
        #endregion

        #region PROVIDERS
        public static readonly WSValue PROVIDER_FB = new WSValue("Facebook", new List<string> { "fb", "facebook" });
        public static readonly WSValue PROVIDER_GP = new WSValue("Google plus", new List<string> { "gp", "google-plus" });
        public static readonly WSValue PROVIDER_LI = new WSValue("LinkedIn", new List<string> { "li", "linkedin" });
        public static readonly WSValue PROVIDER_TR = new WSValue("Twitter", new List<string> { "tr", "twtr", "twitter" });
        public static readonly WSValue PROVIDER_HA = new WSValue("Historiskatlas", new List<string> { "ha", "historiskatlas" });
        public static readonly WSValue PROVIDER_WEB = new WSValue("Web", new List<string> { "web", "base" });
        #endregion

        #region AUTHENTICATION
        public static readonly WSConstParam AUTH_KEY = new WSConstParam(next_code, "key", typeof(int)) { ALIACES = ALIACES.AUTH_KEY.ALIACES };
        public static readonly WSConstParam AUTH_SECRET = new WSConstParam(next_code, "secret", typeof(string)) { ALIACES = ALIACES.AUTH_SECRET.ALIACES };
        public static readonly WSConstParam AUTH_UTOKEN = new WSConstParam(next_code, "token", typeof(string)) { ALIACES = ALIACES.AUTH_UTOKEN.ALIACES };
        public static readonly WSConstParam AUTH_PROVIDER = new WSConstParam(next_code, "provider", typeof(string))
        {
            ALIACES = ALIACES.AUTH_PROVIDER.ALIACES,
            ALLOWED_VALUES = new List<WSValue>() { PROVIDER_FB, PROVIDER_GP, PROVIDER_LI, PROVIDER_TR, PROVIDER_HA }
        };
        #endregion

        private static Dictionary<DayOfWeek, WSValue> _WeekDays = null;

        public static Dictionary<DayOfWeek, WSValue> WeekDays
        {
            get
            {
                if (_WeekDays == null)
                {
                    _WeekDays = new Dictionary<DayOfWeek, WSValue>{
                        {DayOfWeek.Monday,      new WSValue(DayOfWeek.Monday.ToString(),    getWeekDayAliaces(DayOfWeek.Monday))},
                        {DayOfWeek.Tuesday,     new WSValue(DayOfWeek.Tuesday.ToString(),   getWeekDayAliaces(DayOfWeek.Tuesday))},
                        {DayOfWeek.Wednesday,   new WSValue(DayOfWeek.Wednesday.ToString(), getWeekDayAliaces(DayOfWeek.Wednesday))},
                        {DayOfWeek.Thursday,    new WSValue(DayOfWeek.Thursday.ToString(),  getWeekDayAliaces(DayOfWeek.Thursday))},
                        {DayOfWeek.Friday,      new WSValue(DayOfWeek.Friday.ToString(),    getWeekDayAliaces(DayOfWeek.Friday))},
                        {DayOfWeek.Saturday,    new WSValue(DayOfWeek.Saturday.ToString(),  getWeekDayAliaces(DayOfWeek.Saturday))},
                        {DayOfWeek.Sunday,      new WSValue(DayOfWeek.Sunday.ToString(),    getWeekDayAliaces(DayOfWeek.Sunday))}
                    };
                    foreach (DayOfWeek wdKey in _WeekDays.Keys)
                    {
                        try
                        {
                            IEnumerable<DayOfWeek> wdInnerKeys = _WeekDays.Where(x => x.Key > wdKey).Select(x => x.Key);
                            if (wdInnerKeys != null && wdInnerKeys.Any())
                            {
                                foreach (DayOfWeek wdInnerKey in wdInnerKeys)
                                {
                                    IEnumerable<string> dublets = _WeekDays[wdKey].ALIACES.Intersect(_WeekDays[wdInnerKey].ALIACES);
                                    if (dublets != null && dublets.Any()) {
                                        foreach (string s in dublets) { _WeekDays[wdKey].ALIACES.Remove(s); _WeekDays[wdInnerKey].ALIACES.Remove(s); }
                                    }
                                }
                            }
                        }
                        catch (Exception) { }
                    }
                } return _WeekDays;
            }
        }

        private static List<string> getWeekDayAliaces(DayOfWeek day)
        {
            List<string> list = new List<string> { "" + ((int)day) };
            if (day == DayOfWeek.Sunday) list.Add("7");
            CultureInfo[] cultures = CultureInfo.GetCultures(CultureTypes.NeutralCultures);
            foreach (CultureInfo ci in cultures)
            {
                list.Add(ci.DateTimeFormat.GetDayName(day));
                list.Add(ci.DateTimeFormat.GetAbbreviatedDayName(day));
            }
            list = list.Distinct().ToList();
            return list;
        }

        #endregion


        //internal class CONNECTION
        //{
        //    internal static readonly System.Data.ConnectionState[] VALID_STATES = new System.Data.ConnectionState[] {
        //        System.Data.ConnectionState.Open,
        //        System.Data.ConnectionState.Connecting,
        //        System.Data.ConnectionState.Executing,
        //        System.Data.ConnectionState.Fetching
        //    };
        //    internal static readonly System.Data.ConnectionState[] INVALID_STATES = new System.Data.ConnectionState[] {
        //        System.Data.ConnectionState.Broken,
        //        System.Data.ConnectionState.Closed
        //    };
        //}
        public class AUTH_STATES
        {
            public static WSAuthState ACTIVATION_SUCCESS { get { return ACTIVATION_SUCCESS.Clone(); } }
            private static readonly WSAuthState _ACTIVATION_SUCCESS = new WSAuthState(5, "ACTIVATION SUCCEEDED");
            public static WSAuthState ACTIVATION_EMAIL_SEND_SUCCESS { get { return _ACTIVATION_EMAIL_SEND_SUCCESS.Clone(); } }
            private static WSAuthState _ACTIVATION_EMAIL_SEND_SUCCESS = new WSAuthState(4, "ACTIVATION EMAIL SENDING SUCCEEDED");
            public static WSAuthState CREATED { get { return CREATED_.Clone(); } }
            private static WSAuthState CREATED_ = new WSAuthState(3, "NEW USER RECORD CREATED");
            public static WSAuthState DEAUTHORIZED { get { return DEAUTHORIZED_.Clone(); } }
            private static WSAuthState DEAUTHORIZED_ = new WSAuthState(2, "DEAUTHORIZED");
            public static WSAuthState AUTHORIZED { get { return AUTHORIZED_.Clone(); } }
            private static WSAuthState AUTHORIZED_ = new WSAuthState(1, "AUTHORIZED");
            public static WSAuthState NOT_AUTHORIZED { get { return NOT_AUTHORIZED_.Clone(); } }
            private static WSAuthState NOT_AUTHORIZED_ = new WSAuthState(0, "NOT AUTHORIZED");
            public static WSAuthState PROVIDER_MISSING { get { return PROVIDER_MISSING_.Clone(); } }
            private static WSAuthState PROVIDER_MISSING_ = new WSAuthState(-1, "NO AUTH PROVIDER SPECIFIED");
            public static WSAuthState PROVIDER_INVALID { get { return PROVIDER_INVALID_.Clone(); } }
            private static WSAuthState PROVIDER_INVALID_ = new WSAuthState(-2, "AUTH PROVIDER NOT VALID");
            public static WSAuthState TOKEN_MISSING { get { return TOKEN_MISSING_.Clone(); } }
            private static WSAuthState TOKEN_MISSING_ = new WSAuthState(-3, "NO AUTH TOKEN SPECIFIED");
            public static WSAuthState TOKEN_INVALID { get { return TOKEN_INVALID_.Clone(); } }
            private static WSAuthState TOKEN_INVALID_ = new WSAuthState(-4, "AUTH TOKEN NOT VALID");
            public static WSAuthState OAUTH_RECORD_DO_NOT_EXISTS { get { return OAUTH_RECORD_DO_NOT_EXISTS_.Clone(); } }
            private static WSAuthState OAUTH_RECORD_DO_NOT_EXISTS_ = new WSAuthState(-5, "OAUTH RECORD DO NOT EXIST (AUTOCREATE INITIATED)");
            public static WSAuthState OAUTH_RECORD_FAILED_CREATE_EMAIL_MISSING { get { return OAUTH_RECORD_FAILED_CREATE_EMAIL_MISSING_.Clone(); } }
            private static WSAuthState OAUTH_RECORD_FAILED_CREATE_EMAIL_MISSING_ = new WSAuthState(-6, "OAUTH RECORD DO NOT EXIST AND FAILED TO CREATE (EMAIL MISSING)");
            public static WSAuthState USER_RECORD_FAILED_CREATE_LOGIN_IN_USE { get { return USER_RECORD_FAILED_CREATE_LOGIN_IN_USE_.Clone(); } }
            private static WSAuthState USER_RECORD_FAILED_CREATE_LOGIN_IN_USE_ = new WSAuthState(-7, "OAUTH RECORD DO NOT EXIST AND FAILED TO CREATE (LOGIN ALREADY IN USE)");
            public static WSAuthState USER_RECORD_FAILED_CREATE { get { return USER_RECORD_FAILED_CREATE_.Clone(); } }
            private static WSAuthState USER_RECORD_FAILED_CREATE_ = new WSAuthState(-8, "USER RECORD DO NOT EXIST OR FAILED TO CREATE (REASON UNKNOWN)");
            public static WSAuthState OAUTH_RECORD_FAILED_CREATE_REASON_UNKNOWN { get { return OAUTH_RECORD_FAILED_CREATE_REASON_UNKNOWN_.Clone(); } }
            private static WSAuthState OAUTH_RECORD_FAILED_CREATE_REASON_UNKNOWN_ = new WSAuthState(-9, "OAUTH RECORD DO NOT EXIST OR FAILED TO CREATE (REASON UNKNOWN)");
            public static WSAuthState USERNAME_REQUIRED { get { return USERNAME_REQUIRED_.Clone(); } }
            private static WSAuthState USERNAME_REQUIRED_ = new WSAuthState(-10, "USERNAME REQUIRED");
            public static WSAuthState PASSWORD_REQUIRED { get { return PASSWORD_REQUIRED_.Clone(); } }
            private static WSAuthState PASSWORD_REQUIRED_ = new WSAuthState(-11, "PASSWORD REQUIRED");
            public static WSAuthState NO_MATCH_FOUND { get { return NO_MATCH_FOUND_.Clone(); } }
            private static WSAuthState NO_MATCH_FOUND_ = new WSAuthState(-12, "NO MATCH FOUND");
            public static WSAuthState FAILED_DEAUTHORIZE { get { return FAILED_DEAUTHORIZE_.Clone(); } }
            private static WSAuthState FAILED_DEAUTHORIZE_ = new WSAuthState(-13, "FAILED DEAUTHORIZE");
            public static WSAuthState ACCESS_DENIED { get { return ACCESS_DENIED_.Clone(); } }
            private static WSAuthState ACCESS_DENIED_ = new WSAuthState(-14, "ACCESS DENIED");
            public static WSAuthState ACTIVATION_EMAIL_SEND_FAILURE { get { return ACTIVATION_EMAIL_SEND_FAILURE_.Clone(); } }
            private static WSAuthState ACTIVATION_EMAIL_SEND_FAILURE_ = new WSAuthState(-15, "ACTIVATION EMAIL SENDING FAILURE");
            public static WSAuthState USER_RECORD_DELETED { get { return USER_RECORD_DELETED_.Clone(); } }
            private static WSAuthState USER_RECORD_DELETED_ = new WSAuthState(-16, "USER RECORD DELETED");
            public static WSAuthState USER_RECORD_FAILED_DELETE { get { return USER_RECORD_FAILED_DELETE_.Clone(); } }
            private static WSAuthState USER_RECORD_FAILED_DELETE_ = new WSAuthState(-17, "USER RECORD FAILED DELETE");
            public static WSAuthState ACTIVATION_FAILURE { get { return ACTIVATION_FAILURE_.Clone(); } }
            private static WSAuthState ACTIVATION_FAILURE_ = new WSAuthState(-18, "USER ACTIVATION FAILED");
            public static WSAuthState SIMPLE_AUTHENTICATION_FAILURE { get { return SIMPLE_AUTHENTICATION_FAILURE_.Clone(); } }
            private static WSAuthState SIMPLE_AUTHENTICATION_FAILURE_ = new WSAuthState(-19, "SIMPLE AUTHENTICATION FAILED");
            public static WSAuthState ACTIVATION_KEY_INVALID { get { return ACTIVATION_KEY_INVALID_.Clone(); } }
            private static WSAuthState ACTIVATION_KEY_INVALID_ = new WSAuthState(-20, "ACTIVATION KEY NOT VALID");
            public static WSAuthState TOKEN_AUTHENTICATION_FAILURE { get { return TOKEN_AUTHENTICATION_FAILURE_.Clone(); } }
            private static WSAuthState TOKEN_AUTHENTICATION_FAILURE_ = new WSAuthState(-21, "TOKEN AUTHENTICATION FAILED");
            public static WSAuthState AUTH_RECORD_DO_NOT_EXISTS { get { return AUTH_RECORD_DO_NOT_EXISTS_.Clone(); } }
            private static WSAuthState AUTH_RECORD_DO_NOT_EXISTS_ = new WSAuthState(-22, "AUTH RECORD DO NOT EXIST (CREATION INITIATED)");

            public static List<WSAuthState> ALL_STATES { get { return new List<WSAuthState>() { DEAUTHORIZED, AUTHORIZED, NOT_AUTHORIZED, PROVIDER_MISSING, PROVIDER_INVALID, TOKEN_MISSING, TOKEN_INVALID, OAUTH_RECORD_DO_NOT_EXISTS, USER_RECORD_FAILED_CREATE, OAUTH_RECORD_FAILED_CREATE_EMAIL_MISSING, OAUTH_RECORD_FAILED_CREATE_REASON_UNKNOWN, USERNAME_REQUIRED, PASSWORD_REQUIRED, NO_MATCH_FOUND, FAILED_DEAUTHORIZE }; } }
        }
        public class CONFIG
        {
            public class KEYS
            {                
                public const string DefaultDB =                     "DefaultDB";
                public const string DefaultDBLocal =                "DefaultDBLocal";
                public const string DefaultItemsPerRequest =        "DefaultItemsPerRequest"; 
                public const string DefaultImageSize =              "DefaultImageSize";
                public const string ServiceRoot = "ServiceRoot";
                public const string WebRoot =                       "WebRoot";
                public const string AllowLocalhostSendEmais =       "AllowLocalhostSendEmais";
                public const string ImageSubPath =                  "ImageSubPath";
                public const string VideoSubPath =                  "VideoSubPath";
                public const string PDFSubPath =                    "PDFSubPath";
                public const string SharedRoot =                    "SharedRoot";
                public const string SharedMediaPath =               "SharedMediaPath";
                public const string welcome =                       "welcome";
                public const string XPass =                         "XPass";
                public const string MaxItemsPerRequestForGuests =   "MaxItemsPerRequestForGuests";
                public const string ConnectionTimeout =             "ConnectionTimeout";            
            }

            #region PUBLIC VALUES

            #region DefaultDB (typeof(string))
            private static string DefaultDB_ = null;
            public static string DefaultDB {
                get {
                    if (string.IsNullOrEmpty(DefaultDB_))
                    {
                        if (HttpContext.Current.Request.IsLocal) {
                            if (ConfigurationManager.AppSettings.AllKeys.Any(x => x.Equals(KEYS.DefaultDBLocal))) { DefaultDB_ = read(KEYS.DefaultDBLocal, ConfigurationManager.AppSettings[KEYS.DefaultDB]); }
                        } else {
                            if (ConfigurationManager.AppSettings.AllKeys.Any(x => x.Equals(KEYS.DefaultDB))) { DefaultDB_ = read(KEYS.DefaultDB, null); }
                        }
                        if (string.IsNullOrEmpty(DefaultDB_)) { throw new KeyNotFoundException(); }
                    }
                    return DefaultDB_;
                }
            }
            #endregion

            #region MaxItemsPerRequestForGuests (typeof(int))
            private static int DefaultItemsPerRequest_ = 0;
            internal static int DefaultItemsPerRequest
            {
                get
                {
                    if (DefaultItemsPerRequest_ == 0)
                    {
                        DefaultItemsPerRequest_ = 3;
                        string DefaultItemsPerRequestValue = read(KEYS.DefaultItemsPerRequest, "3");
                        if (!string.IsNullOrEmpty(DefaultItemsPerRequestValue))
                        {
                            DefaultItemsPerRequest_ = (int.TryParse(DefaultItemsPerRequestValue, out DefaultItemsPerRequest_) && DefaultItemsPerRequest_>0)
                            ? DefaultItemsPerRequest_
                            : 3;
                        }
                    }
                    return DefaultItemsPerRequest_ > 0 ? DefaultItemsPerRequest_ : 3;
                }
            }
            #endregion

            #region DefaultImageSize (typeof(Size))
            private static Size DefaultImageSize_ = Size.Empty;
            public static Size DefaultImageSize
            {
                get
                {
                    if (DefaultImageSize_ == Size.Empty)
                    {
                        DefaultImageSize_ = new Size(1000, 1000);
                        string DefaultImageSize_Value = read(KEYS.DefaultImageSize, "{1000:1000}");
                        if (!string.IsNullOrEmpty(DefaultImageSize_Value))
                        {
                            string[] sizeValues = DefaultImageSize_Value.Trim(WSConstants.TRIM_CHARS).Split(WSConstants.SIZE_SEPARATORS, StringSplitOptions.RemoveEmptyEntries);
                            if (sizeValues.Length == 2)
                            {
                                DefaultImageSize_ = new Size(
                                    int.Parse(sizeValues[0]),
                                    int.Parse(sizeValues[1])
                                );
                            }
                        }
                    }
                    return DefaultImageSize_;
                }
            }
            #endregion

            #region ServiceRoot (typeof(string))
            private static string ServiceRoot_ = null;
            public static string ServiceRoot { get { if (string.IsNullOrEmpty(ServiceRoot_)) { ServiceRoot_ = read(KEYS.ServiceRoot, null); } return ServiceRoot_; } }
            #endregion

            #region WebRoot (typeof(string))
            private static string WebRoot_ = null;
            public static string WebRoot { get { if (string.IsNullOrEmpty(WebRoot_)) { WebRoot_ = read(KEYS.WebRoot); } return WebRoot_; } }
            #endregion

            #region AllowLocalhostSendEmais (typeof(bool?))
            private static bool? AllowLocalhostSendEmais_ = null;
            public static bool AllowLocalhostSendEmais { get { if (AllowLocalhostSendEmais_ == null) { string AllowLocalhostSendEmaisValue = read(KEYS.AllowLocalhostSendEmais); AllowLocalhostSendEmais_ = AllowLocalhostSendEmaisValue!=null && AllowLocalhostSendEmaisValue.IsTrue(); } return (bool)AllowLocalhostSendEmais_; } }
            #endregion

            #region ImageSubPath (typeof(string))
            private static string ImageSubPath_ = null;
            public static string ImageSubPath { get { if (string.IsNullOrEmpty(ImageSubPath_)) { ImageSubPath_ = read(KEYS.ImageSubPath); } return ImageSubPath_; } }
            #endregion

            #region VideoSubPath (typeof(string))
            private static string VideoSubPath_ = null;
            public static string VideoSubPath { get { if (string.IsNullOrEmpty(VideoSubPath_)) { VideoSubPath_ = read(KEYS.VideoSubPath); } return VideoSubPath_; } }
            #endregion

            #region PDFSubPath (typeof(string))
            private static string PDFSubPath_ = null;
            public static string PDFSubPath { get { if (string.IsNullOrEmpty(PDFSubPath_)) { PDFSubPath_ = read(KEYS.PDFSubPath); } return PDFSubPath_; } }
            #endregion

            #region SharedRoot (typeof(string))
            private static string SharedRoot_ = null;
            public static string SharedRoot { get { if (string.IsNullOrEmpty(SharedRoot_)) { SharedRoot_ = read(KEYS.SharedRoot); } return SharedRoot_; } }
            #endregion

            #region SharedMediaPath (typeof(string))
            private static string SharedMediaPath_ = null;
            public static string SharedMediaPath { get { if (string.IsNullOrEmpty(SharedMediaPath_)) { SharedMediaPath_ = read(KEYS.SharedMediaPath); } return SharedMediaPath_; } }
            #endregion

            #region welcome (typeof(string))
            private static string welcome_ = null;
            public static string welcome { get { if (string.IsNullOrEmpty(welcome_)) { welcome_ = read(KEYS.welcome, "welcome"); } return welcome_; } }
            #endregion

            #endregion

            #region INTERNAL VALUES

            #region XPass (typeof(string))
            private static string XPass_ = null;
            internal static string XPass { get { if (string.IsNullOrEmpty(XPass_)) { XPass_ = read(KEYS.XPass, new WSConverter().ToMd5Hash(DateTime.Now.ToString()).Substring(0,8)); } return XPass_; } }
            #endregion
            
            #region MaxItemsPerRequestForGuests (typeof(int))
            private static int MaxItemsPerRequestForGuests_ = 0;
            internal static int MaxItemsPerRequestForGuests { get {
                string MaxItemsPerRequestForGuestsValue = read(KEYS.MaxItemsPerRequestForGuests, ""+ 100);
                if (MaxItemsPerRequestForGuests_ == 0) {
                    MaxItemsPerRequestForGuests_ = 
                    int.TryParse(MaxItemsPerRequestForGuestsValue, out MaxItemsPerRequestForGuests_) 
                    ? MaxItemsPerRequestForGuests_ 
                    : 100;
                }
                return MaxItemsPerRequestForGuests_ == 0 ? 100 : MaxItemsPerRequestForGuests_;
            } }
            #endregion

            #region ConnectionTimeout (typeof(int))
            private static int ConnectionTimeout_ = 0;
            internal static int ConnectionTimeout
            {
                get
                {
                    int defaultValue = 300;//seconds
                    string ConnectionTimeoutValue = read(KEYS.ConnectionTimeout, "" + defaultValue);
                    if (ConnectionTimeout_ == 0)
                    {
                        ConnectionTimeout_ =
                        (int.TryParse(ConnectionTimeoutValue, out ConnectionTimeout_) && ConnectionTimeout_ > 0)
                        ? ConnectionTimeout_
                        : defaultValue;
                    }
                    return ConnectionTimeout_ <= 0 ? defaultValue : ConnectionTimeout_;
                }
            }
            #endregion

            #endregion
            
            private static string read(string key, string default_value = null)
            {
                string value = default_value;
                try
                {
                    if (!string.IsNullOrEmpty(key)) {
                        if (ConfigurationManager.AppSettings.AllKeys.Any(x => x.Equals(key)))
                        {
                            value = ConfigurationManager.AppSettings[key];
                        }
                        else if (!string.IsNullOrEmpty(default_value))
                        {
                            string configPath1 = WSServerMeta.MapPath();
                            string configPath = "~";//configPath1;
                            Configuration config = System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration(configPath) as System.Configuration.Configuration;
                            config.AppSettings.Settings.Remove(key);
                            config.AppSettings.Settings.Add(key, default_value);

                            FileAttributes attributes = File.GetAttributes(config.FilePath);

                            if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                            {
                                attributes = RemoveAttribute(attributes, FileAttributes.ReadOnly);
                                File.SetAttributes(config.FilePath, attributes);
                            }

                            config.Save();

                            if ((attributes & FileAttributes.ReadOnly) != FileAttributes.ReadOnly)
                            {
                                File.SetAttributes(config.FilePath, File.GetAttributes(config.FilePath) | FileAttributes.ReadOnly);
                            }
                        }
                    }
                }
                catch (Exception e) { }
                return value;
            }
        }
        private static FileAttributes RemoveAttribute(FileAttributes attributes, FileAttributes attributesToRemove)
        {
            return attributes & ~attributesToRemove;
        }
        public class COMMAND_VALUES
        {
            public static readonly WSCommandValue USER_ID =                     new WSCommandValue(typeof(string),      ALIACES.USER_ID.NAME, ALIACES.USER_ID.ALIACES);
            public static readonly WSCommandValue SQL_COMMAND_VALUE_GETDATE =   new WSCommandValue(typeof(DateTime),    ALIACES.SQL_COMMAND_VALUE_GETDATE.NAME, ALIACES.SQL_COMMAND_VALUE_GETDATE.ALIACES);
            public static readonly WSCommandValue SQL_COMMAND_VALUE_GETTIME =   new WSCommandValue(typeof(TimeSpan),    ALIACES.SQL_COMMAND_VALUE_GETTIME.NAME, ALIACES.SQL_COMMAND_VALUE_GETTIME.ALIACES);

            public static IEnumerable<WSCommandValue> Items
            {
                get
                {
                    return typeof(COMMAND_VALUES).GetFields(BindingFlags.Static | BindingFlags.Public).Select(f => (WSCommandValue)f.GetValue(typeof(COMMAND_VALUES)));
                }
            }
        }
        public class COMMAND_KEYS
        {
            public static readonly WSCommandKey MATCH =                 new WSCommandKey(ALIACES.MATCH);
            public static readonly WSCommandKey READ =                  new WSCommandKey(ALIACES.READ);
            public static readonly WSCommandKey CURRENT_USER =          new WSCommandKey(ALIACES.CURRENT_USER);
            public static readonly WSCommandKey EXPLICIT =              new WSCommandKey(ALIACES.EXPLICIT); 
            public static readonly WSCommandKey SHARED_KEY =            new WSCommandKey(ALIACES.SHARED_KEY);
            public static readonly WSCommandKey ALL_PRIMITIVE_FIELDS =  new WSCommandKey(ALIACES.ALL_PRIMITIVE_FIELDS);

            public static IEnumerable<WSCommandKey> Items { get { return typeof(COMMAND_KEYS).GetFields(BindingFlags.Static | BindingFlags.Public).Select(f => (WSCommandKey)f.GetValue(typeof(COMMAND_KEYS))); } }
        }
        public class ALIACES
        {
            public static readonly WSValue ACCESS_KEY =                 new WSValue("ACCESS_KEY",   new List<string> { "accesskey", "securitykey", "access_key", "security_key" });
            public static readonly WSValue USER_ID =                    new WSValue("USERID",       new List<string> { "userid", "user_id", "user-id", "user id", "uid" });
            public static readonly WSValue SQL_COMMAND_VALUE_GETDATE =  new WSValue("GETDATE",      new List<string> { "currentdate", "current_date", "current date", "serverdate", "server_date", "server date", "getdate", "get_date", "get date", "newdate", "new_date", "new date", "date" });
            public static readonly WSValue SQL_COMMAND_VALUE_GETTIME =  new WSValue("GETTIME",      new List<string> { "currenttime", "current_time", "current time", "servertime", "server_time", "server time", "gettime", "get_time", "get time", "newtime", "new_time", "new time", "time" });
            
            public static readonly WSValue MATCH =                      new WSValue("match",        new List<string> { });
            public static readonly WSValue READ =                       new WSValue("read",         new List<string> { });
            public static readonly WSValue CURRENT_USER =               new WSValue("currentuser",  new List<string> { "current user", "current-user", "current_user"});
            public static readonly WSValue EXPLICIT =                   new WSValue("explicit",     new List<string> { });
            public static readonly WSValue SHARED_KEY =                 new WSValue("share",        new List<string> { "shk", "shkey", "sh_key", "sh-key", "shkeys", "sh_keys", "sh-keys", "shared", "sharedkey", "shared_key", "shared-key", "sharedkeys", "shared_keys", "shared-keys" });
            public static readonly WSValue ALL_PRIMITIVE_FIELDS =       new WSValue("primitive",    new List<string> { "all primitive", "simple", "all simple", "*" });
            public static readonly WSValue ANY =                        new WSValue("any",          new List<string> { });
            public static readonly WSValue NOT =                        new WSValue("not",          new List<string> { "ikke" });

            public static readonly WSValue TRUE =       new WSValue("true",     new List<string> { "1", "true", "yes", "y", "ok", "ja" });
            public static readonly WSValue FALSE =      new WSValue("false",    new List<string> { "0", "false", "n", "no", "nej" });
            public static readonly WSValue EMPTY =      new WSValue("empty",    new List<string> { "", "null", "empty", "none" });
            public static readonly WSValue EXIST =      new WSValue("exist",    new List<string> { "exists", "exist", "present", "notnull", "notempty", "filled" });
            public static readonly WSValue IS_OWN =     new WSValue("isown",    new List<string> { "is_own", "is-own", "own" }); 
            public static readonly WSValue COUNT =      new WSValue("count",    new List<string> { "count", "antal", "amount", "total", "size" });
            public static readonly WSValue COUNT_ALL =  new WSValue("all",      new List<string> { "max", "all", "*" });
            public static readonly WSValue ALL_META =   new WSValue("all",      new List<string> { "all", "*" });
            public static readonly WSValue OUTPUT =     new WSValue("output",   new List<string> { "out", "output", "fields" });
            public static readonly WSValue USERNAME =   new WSValue("username", new List<string> { "user", "name", "username", "uname", "login", "log-in", "key" });
            public static readonly WSValue SORT =       new WSValue("sort",     new List<string> { "sort", "sortby", "orderby" } );
            public static readonly WSValue SCHEMA =     new WSValue("schema",   new List<string> { "filter", "filters", "schemas" });
            public static readonly WSValue ONSUCCESS =  new WSValue("onsuccess",new List<string> { "onsuccess" });
            public static readonly WSValue ONERROR =    new WSValue("onerror",  new List<string> { "onerror" });
            public static readonly WSValue SESSIONID =  new WSValue("sessionid",new List<string> { "session", "sessionid", "sid" });
            public static readonly WSValue OVERRIDE =   new WSValue("override", new List<string> { "override" }); 
            public static readonly WSValue OFFSET =     new WSValue("offset",   new List<string> { "offset", "skip" });
            public static readonly WSValue DISTINCT =   new WSValue("distinct", new List<string> { "group", "unique" });
            public static readonly WSValue PASSWORD =   new WSValue("password", new List<string> { "password", "pass" });
            public static readonly WSValue EMAIL =      new WSValue("email",    new List<string> { "email", "mail", "e-mail", "e_mail" });
            public static readonly WSValue OPTIONS =    new WSValue("options",  new List<string> { "settings" });
            public static readonly WSValue NONE =       new WSValue("none",     new List<string> { });

            public static readonly WSValue MODE =       new WSValue("mode",     new List<string> { "m" });
            public static readonly WSValue MODE_LITE =  new WSValue("lite",     new List<string> { "lite", "0" });
            public static readonly WSValue MODE_REF =   new WSValue("reference",new List<string> { "ref", "1" });
            public static readonly WSValue MODE_FULL =  new WSValue("full",     new List<string> { "full", "2" });

            public static readonly WSValue ACTION_READ =    new WSValue(EVENT_TYPE_READ.ToLower(),  new List<string> { "get", "hent", "read" });
            public static readonly WSValue ACTION_WRITE =   new WSValue(EVENT_TYPE_UPDATE.ToLower(),new List<string> { "set", "saet", "write" });
            public static readonly WSValue ACTION_INSERT =  new WSValue(EVENT_TYPE_INSERT.ToLower(),new List<string> { "add", "tilfoj", "insert" });
            public static readonly WSValue ACTION_UPLOAD =  new WSValue("upload",                   new List<string> { "upload" });
            public static readonly WSValue ACTION_DELETE =  new WSValue(EVENT_TYPE_DELETE.ToLower(),new List<string> { "del", "delete", "remove" });
            public static readonly WSValue ACTION_EXECUTE = new WSValue("exec",                     new List<string> { "exec", "run", "execute" });
            public static readonly WSValue ACTION_SYNC =    new WSValue("sync",                     new List<string> { "sync", "synchronize" });
            public static readonly WSValue ACTION_DESTROY = new WSValue("clear",                    new List<string> { "clear", "remove", "destroy", "kill" });
            public static readonly WSValue ACTION_NONE =    new WSValue("none",                     new List<string> { "none", "non", "nothing", "empty", "" });
            public static readonly WSValue ACTION_AUTH =    new WSValue("auth",                     new List<string> { "auth", "login", "enter" });
            public static readonly WSValue ACTION_DEAUTH =  new WSValue("deauth",                   new List<string> { "deauth", "unauth", "logout", "exit" });
            public static readonly WSValue ACTION_TEST =    new WSValue("test",                     new List<string> { "test" });
            public static readonly WSValue ACTION_SYSTEM =  new WSValue("system",                   new List<string> { "sys", "system", "core" });
            public static readonly WSValue ACTION_FLUSH =   new WSValue("flush",                    new List<string> { "reset", "reload" });
            public static readonly WSValue ACTION_CREATE =  new WSValue("create",                   new List<string> { "opret" });
            

            public static readonly WSValue OPTION_COLLECT = new WSValue("collect",     new List<string> { "collapse", "take" });
            
            public static readonly WSValue OPERATOR_OR =    new WSValue("or", new List<string> { "eller" });
            public static readonly WSValue OPERATOR_AND =   new WSValue("and", new List<string> { "og" });

            public static readonly WSValue AUTH_KEY =       new WSValue("key",      new List<string> { "id", "authid", "oauthid", "authkey", "oauthkey" });
            public static readonly WSValue AUTH_SECRET =    new WSValue("secret",   new List<string> { "secretkey" });
            public static readonly WSValue AUTH_UTOKEN =    new WSValue("token",    new List<string> { "utoken" });
            public static readonly WSValue AUTH_PROVIDER =  new WSValue("provider", new List<string> { "mode", "authmode", "authprovider", "oauthprovider" });

            public static readonly WSValue PDF =            new WSValue("pdf",      new List<string> { });
            public static readonly WSValue VIDEO =          new WSValue("video",    new List<string> { });
            public static readonly WSValue IMG =            new WSValue("image",    new List<string> { "img" });
            public static readonly WSValue CONTENT_TYPE =   new WSValue("contenttype",  new List<string> { "content_type", "ctype", "filetype", "file_type" });
            public static readonly WSValue ENTITY =         new WSValue("entity",   new List<string> { "item", "object", "obj" }); 
        }
        public class USER_ROLE
        {
            public const byte READER = ACCESS_LEVEL.READ;
            public const byte CREATOR = ACCESS_LEVEL.INSERT;
            public const byte EDITOR = ACCESS_LEVEL.UPDATE;
            public const byte ERAZER = ACCESS_LEVEL.DELETE;
            public const byte ADMIN = ACCESS_LEVEL.ADMIN;
            public const byte DEVELOPER = ACCESS_LEVEL.DEV;

            public static readonly byte[] ROLES = new byte[] { READER, CREATOR, EDITOR, ERAZER, ADMIN, DEVELOPER };
        }
        public class ACCESS_LEVEL
        {
            public const byte LOCK = byte.MaxValue;
            public const byte READ = 0;
            public const byte INSERT = 1;
            public const byte UPDATE = 2;
            public const byte DELETE = 3;
            public const byte ADMIN = 4;
            public const byte DEV = 5;

            public static readonly byte[] LEVELS = new byte[] { LOCK, READ, INSERT, UPDATE, DELETE, ADMIN, DEV };
        }
        public class HTTPMETHOD
        {
            public const string GET =   "get";
            public const string PUT =   "put";
            public const string OPTIONS="options";
            public const string POST =  "post";
        }
        public class FORMAT
        {
            public const string KEYNAME = "format";

            public static readonly WSFormat NONE_FORMAT = new WSMetaFormat(nextCode, "*", false, true);
            public static readonly WSFormat TEXT_FORMAT = new WSMetaFormat(nextCode, "txt", false, true);
            public static readonly WSFormat XML_FORMAT = new WSMetaFormat(nextCode, "xml", false, true);
            public static readonly WSFormat JSON_FORMAT = new WSMetaFormat(nextCode, "json", true, true);
            public static readonly WSFormat JSONP_FORMAT = new WSMetaFormat(nextCode, "jsonp", true, true);
            public static readonly WSFormat PDF_FORMAT = new WSBinaryFormat(nextCode, "pdf", false, true);
            public static readonly WSFormat IMAGE_FORMAT = new WSBinaryFormat(nextCode, "img", false, true);
            public static readonly WSFormat DYNAMIC_IMAGE_FORMAT = new WSBinaryFormat(nextCode, "d_img", false, true);
            public static readonly WSFormat STATIC_IMAGE_FORMAT = new WSBinaryFormat(nextCode, "s_img", false, true);
            public static readonly WSFormat REF_FORMAT = new WSMetaFormat(nextCode, "ref", false, true); 

            public static readonly WSFormat[] FORMATS = new WSFormat[] {
                TEXT_FORMAT,
                XML_FORMAT,
                JSON_FORMAT,
                JSONP_FORMAT,
                PDF_FORMAT,
                IMAGE_FORMAT,
                DYNAMIC_IMAGE_FORMAT,
                STATIC_IMAGE_FORMAT,
                REF_FORMAT
            };
            public static readonly WSFormat[] META_FORMATS = new WSFormat[] {
                TEXT_FORMAT,
                XML_FORMAT,
                JSON_FORMAT,
                JSONP_FORMAT
            };
            public static readonly WSFormat[] BINARY_FORMATS = new WSFormat[] {
                PDF_FORMAT,
                IMAGE_FORMAT,
                DYNAMIC_IMAGE_FORMAT,
                STATIC_IMAGE_FORMAT
            };

            public static readonly WSFormat DEFAULT_FORMAT = JSON_FORMAT;

            private static int _nextCode = 0;
            public static int nextCode { get { _nextCode++; return _nextCode; } }
        }
        public class PARAMS
        {
            public class KEYS
            {
                public const string RECORD_ID =     "id";
                public const string SOURCE =        "xsource";
                public const string DB =            "xdb";
                public const string ACTION =        "action";
                public const string VERSION =       "version";
                public const string MODE =          "mode";
                public const string SCALE_MODE =    "scalemode";
                public const string LOAD_MODE =     "load_mode";
                public const string EXPANDABLE =    "expandable"; 
                public const string SORT =          "sort";
                public const string ONSUCCESS =     "onsuccess";
                public const string ONERROR =       "onerror";
                public const string SESSIONID =     "sessionid";
                public const string DISTINCT =      "distinct";
                public const string COUNT =         "count";
                public const string OFFSET =        "offset";
                public const string OUTPUT =        "output";
                public const string USERNAME =      "username";
                public const string PASSWORD =      "password";
                public const string EMAIL =         "email";
                public const string OVERRIDE =      "override";
                public const string DELETE_MODE=    "deletemode"; 
                public const string DELETED =       "deleted";
                public const string SCHEMA =        "schema";
                public const string CONTENT_TYPE =  "ctype";
                public const string ACTIVATION_KEY ="akey";
            }

            public class MODE : WSConstParam
            {

                public MODE() : base(next_code, KEYS.MODE, typeof(string))
                {
                    ALLOWED_VALUES = new List<WSValue>() {
                        WSConstants.ALIACES.MODE_LITE,
                        WSConstants.ALIACES.MODE_REF,
                        WSConstants.ALIACES.MODE_FULL
                    };
                    ALIACES = WSConstants.ALIACES.MODE.ALIACES;
                }
                public static readonly WSValue DEFAULT = WSConstants.ALIACES.MODE_LITE;
            }

            public static WSConstParam RECORD_ID = new WSConstParam(next_code, KEYS.RECORD_ID, typeof(int));

            public static WSConstParam ACTIVATION_KEY = new WSConstParam(next_code, KEYS.ACTIVATION_KEY, typeof(string));

            public class ACTION : WSConstParam
            {
                public ACTION() : base(next_code, KEYS.ACTION, typeof(string))
                {
                    Type aliaces = typeof(ALIACES);
                    ALLOWED_VALUES = aliaces.GetFields().Where(x => x.FieldType == typeof(WSValue) && x.Name.ToLower().StartsWith("action")).Select(x => (WSValue)x.GetValue(ALIACES)).ToList();
                }
            }

            public class IMG
            {
                private static IOACTION _ACTION = null;
                public static IOACTION ACTION { get { if (_ACTION == null) { _ACTION = new IOACTION(); } return _ACTION; } }
                public class IOACTION : WSConstParam
                {
                    public static readonly WSValue SCALE = new WSValue("scale", new List<string> { });
                    public static readonly WSValue CROP = new WSValue("crop", new List<string> { });
                    public static readonly WSValue GET = new WSValue("get", new List<string> { });
                    public IOACTION() : base(next_code, KEYS.ACTION, typeof(string))
                    {
                        ALLOWED_VALUES = new List<WSValue>() {
                            GET,
                            SCALE,
                            CROP
                        };
                    }
                }
                
                private static IOEXPANDABLE _EXPANDABLE = null;
                public static IOEXPANDABLE EXPANDABLE { get { if (_EXPANDABLE == null) { _EXPANDABLE = new IOEXPANDABLE(); } return _EXPANDABLE; } }
                public class IOEXPANDABLE : WSConstParam
                {
                    public IOEXPANDABLE() : base(next_code, KEYS.EXPANDABLE, typeof(bool))
                    {
                        ALIACES = new List<string>() { "expandable", "expanding" };
                    }
                }

                private static IOSCALE_MODE _SCALE_MODE = null;
                public static IOSCALE_MODE SCALE_MODE { get { if (_SCALE_MODE == null) { _SCALE_MODE = new IOSCALE_MODE(); } return _SCALE_MODE; } }
                public class IOSCALE_MODE : WSConstParam
                {
                    public static readonly WSValue EXACT = new WSValue("exact", new List<string> { });
                    public static readonly WSValue INNERPROPORTIONAL = new WSValue("inner", new List<string> { });
                    public static readonly WSValue OUTERPROPORTIONAL = new WSValue("outer", new List<string> { });
                    public static readonly WSValue WIDE = new WSValue("wide", new List<string> { });
                    public static readonly WSValue HIGH = new WSValue("high", new List<string> { });
                    public IOSCALE_MODE() : base(next_code, KEYS.SCALE_MODE, typeof(string))
                    {
                        ALIACES = new List<string>() { "scalemode", "scale_mode", "scale-mode" };
                        ALLOWED_VALUES = new List<WSValue>() {
                            EXACT,
                            INNERPROPORTIONAL,
                            OUTERPROPORTIONAL,
                            WIDE,
                            HIGH
                        };
                    }
                }

                #region LOAD MODE
                private static IOLOAD_MODE _LOAD_MODE = null;
                public static IOLOAD_MODE LOAD_MODE { get { if (_LOAD_MODE == null) { _LOAD_MODE = new IOLOAD_MODE(); } return _LOAD_MODE; } }
                public class IOLOAD_MODE : WSConstParam
                {
                    public static readonly WSValue INLINE = new WSValue("inline", new List<string> { });
                    public static readonly WSValue DOWNLOAD = new WSValue("download", new List<string> { });
                    public IOLOAD_MODE() : base(next_code, KEYS.LOAD_MODE, typeof(string)){ALLOWED_VALUES = new List<WSValue>() {
                        INLINE,
                        DOWNLOAD
                    };}
                }
                #endregion

                #region WIDTH
                public static WSConstParam WIDTH{get{if (_WIDTH == null) { _WIDTH = new WSConstParam(next_code, "width", typeof(int)); }return _WIDTH;}}
                private static WSConstParam _WIDTH = null;
                #endregion

                #region HEIGHT
                public static WSConstParam HEIGHT{get{if (_HEIGHT == null) { _HEIGHT = new WSConstParam(next_code, "height", typeof(int)); }return _HEIGHT;}}
                private static WSConstParam _HEIGHT = null;
                #endregion

                #region SIZE
                public static WSConstParam SIZE{get{if (_SIZE == null) { _SIZE = new WSConstParam(next_code, "size", typeof(string)); }return _SIZE;}}
                private static WSConstParam _SIZE = null;
                #endregion

                #region VIEWPORT
                public static WSConstParam VIEWPORT{get{if (_VIEWPORT == null) { _VIEWPORT = new WSConstParam(next_code, "viewport", typeof(string)); }return _VIEWPORT;}}
                private static WSConstParam _VIEWPORT = null;
                #endregion

                #region SRC
                public static WSConstParam SRC{get{if (_SRC == null) { _SRC = new WSConstParam(next_code, "src", typeof(string)); }return _SRC;}}
                private static WSConstParam _SRC = null;
                #endregion

            }

            private static IOVERSION _VERSION = null;
            public static IOVERSION VERSION {get{if(_VERSION==null){_VERSION = new IOVERSION();}return _VERSION;}}
            public class IOVERSION : WSConstParam
            {
                public IOVERSION() : base(next_code, KEYS.VERSION, typeof(string))
                {
                    ALLOWED_VALUES = new List<WSValue>() { VERSION0, VERSION1 };
                    ALIACES = new List<string> { "v", "version" };
                }

                public static readonly WSValue VERSION0 = new WSValue("0", new List<string> { "v0", "0" });
                public static readonly WSValue VERSION1 = new WSValue("1", new List<string> { "v1", "1" });

                public static readonly WSValue DEAULT = VERSION0;
            }
            
            private static DELETE_MODE_PARAM _DELETE_MODE = null;
            public static DELETE_MODE_PARAM DELETE_MODE { get { if (_DELETE_MODE == null) { _DELETE_MODE = new DELETE_MODE_PARAM(); } return _DELETE_MODE; } }
            public class DELETE_MODE_PARAM : WSConstParam
            {
                public DELETE_MODE_PARAM() : base(next_code, KEYS.DELETE_MODE, typeof(string))
                {
                    ALIACES = new List<string> { "mode", "delete_mode" };
                    ALLOWED_VALUES = new List<WSValue>() { SOFT, PERMANENT };
                }

                public static readonly WSValue SOFT = new WSValue("soft", new List<string> { "0" });
                public static readonly WSValue PERMANENT = new WSValue("permanent", new List<string> { "1" });

                public static readonly WSValue DEAULT = SOFT;
            }

            public static readonly WSConstParam SORT =                   new WSConstParam(next_code, KEYS.SORT,       typeof(string)) { ALIACES = ALIACES.SORT.ALIACES };
            public static readonly WSConstParam SCHEMA =                 new WSConstParam(next_code, KEYS.SCHEMA,     typeof(string)) { ALIACES = ALIACES.SCHEMA.ALIACES };
            public static readonly WSConstParam ONSUCCESS =              new WSConstParam(next_code, KEYS.ONSUCCESS,  typeof(string)) { ALIACES = ALIACES.ONSUCCESS.ALIACES };
            public static readonly WSConstParam ONSERROR =               new WSConstParam(next_code, KEYS.ONERROR,    typeof(string)) { ALIACES = ALIACES.ONERROR.ALIACES };
            public static readonly WSConstParam SESSIONID =              new WSConstParam(next_code, KEYS.SESSIONID,  typeof(string)) { ALIACES = ALIACES.SESSIONID.ALIACES };
            public static readonly WSConstParam OVERRIDE =               new WSConstParam(next_code, KEYS.OVERRIDE,   typeof(bool))   { ALIACES = ALIACES.OVERRIDE.ALIACES };
            public static readonly WSConstParam OFFSET =                 new WSConstParam(next_code, KEYS.OFFSET,     typeof(int))    { ALIACES = ALIACES.OFFSET.ALIACES };
            public static readonly WSConstParam DISTINCT =               new WSConstParam(next_code, KEYS.DISTINCT,   typeof(string)) { ALIACES = ALIACES.DISTINCT.ALIACES };
            public static readonly WSConstParam PASSWORD =               new WSConstParam(next_code, KEYS.PASSWORD,   typeof(string)) { ALIACES = ALIACES.PASSWORD.ALIACES };
            public static readonly WSConstParam EMAIL =                  new WSConstParam(next_code, KEYS.EMAIL,      typeof(string)) { ALIACES = ALIACES.EMAIL.ALIACES };
            public static readonly WSConstParam COUNT =                  new WSConstParam(next_code, KEYS.COUNT,      typeof(int))    { ALIACES = ALIACES.COUNT.ALIACES, ALLOWED_VALUES = new List<WSValue> { ALIACES.COUNT_ALL } };
            public static readonly WSConstParam OUTPUT =                 new WSConstParam(next_code, KEYS.OUTPUT,     typeof(string)) { ALIACES = ALIACES.OUTPUT.ALIACES };
            public static readonly WSConstParam USERNAME =               new WSConstParam(next_code, KEYS.USERNAME,   typeof(string)) { ALIACES = ALIACES.USERNAME.ALIACES };
            public static readonly WSConstParam CONTENT_TYPE =           new WSConstParam(next_code, KEYS.CONTENT_TYPE,typeof(string)){ ALIACES = ALIACES.CONTENT_TYPE.ALIACES };
        }
        public class LINKS
        {
            public const string RootPath = "~";
            public static string BinPath = RootPath+"/bin";
            public const string CoreSchemaPath = RootPath+"/config";
            public const string LogPath= RootPath + "/log";
        }        
        public class SETTINGS
        {
            public class CONNECTION_STRINGS{

            }
        }
    }
}
