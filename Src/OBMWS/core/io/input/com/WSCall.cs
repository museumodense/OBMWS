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
    public class WSCall
    {
        public WSStatus status = WSStatus.NONE_Copy();
        public WSSecurityMeta Meta = null;

        public WSCall(HttpContext _InContext)
        {
            try
            {
                if (_InContext != null)
                {
                    INPUT = new Dictionary<string, string>();

                    #region READ POST PARAMETERS
                    if (_InContext.Request.HttpMethod.Equals("POST"))
                    {

                        #region READ FORM PARAMETERS
                        foreach (string fKey in _InContext.Request.Form.AllKeys)
                        {
                            if (!string.IsNullOrEmpty(fKey))
                            {
                                string fValue = _InContext.Request.Form[fKey];
                                INPUT.Save(fKey, fValue);
                            }
                        }
                        #endregion


                        string[] allKeys = _InContext.Request.Params.AllKeys;
                        IEnumerable<string> actualKeys = allKeys.Where(x => x != null && !WSConstants.STANDARD_ASP_URL_PARAMS.Select(p => p.ToLower()).Contains(x.ToLower()));
                        string[] PKeys = actualKeys.ToArray();

                        foreach (string PKey in _InContext.Request.Params)
                        {
                            if (!string.IsNullOrEmpty(PKey))
                            {
                                if (!WSConstants.STANDARD_ASP_URL_PARAMS.Any(x => x.Equals(PKey)))
                                {
                                    bool isValid = true;
                                    string PVal = _InContext.Request.Params[PKey];
                                    if (PKey.ToLower().Equals("url")) {
                                        string RawUrl = _InContext.Request.RawUrl;
                                        if (PVal.Equals(RawUrl)) { isValid = false; }
                                        else { PVal = PVal.Split(new char[] { ',' }).FirstOrDefault(x => !x.ToLower().Equals(RawUrl.ToLower())); isValid = !string.IsNullOrEmpty(PVal); }
                                    }
                                    if (isValid) { INPUT.Save(PKey, PVal, false); }
                                }
                            }
                            else
                            {
                                string jValue = _InContext.Request.Params[PKey];
                                status.AddNote("POST: try saving empty key {" + PKey + ":" + jValue + "}", WSConstants.ACCESS_LEVEL.READ);
                                if (!string.IsNullOrEmpty(jValue))
                                {
                                    WSJson json = jValue.ToJson();
                                    if (json == null)
                                    {
                                        status.AddNote("POST:failed convert json {" + jValue + "}. Try autoresolve.", WSConstants.ACCESS_LEVEL.READ);
                                        json = ("{data:" + jValue + "}").ToJson();
                                        json = json != null && (json is WSJObject) ? ((WSJObject)json).Value[0].Value : null;
                                    }

                                    if (json == null) { status.AddNote("POST:failed convert json {" + jValue + "}", WSConstants.ACCESS_LEVEL.READ); }
                                    else
                                    {
                                        if (json is WSJArray)
                                        {
                                            WSJArray jArray = (WSJArray)json;
                                            foreach (WSJson innerJson in jArray.Value)
                                            {
                                                if (innerJson is WSJProperty)
                                                {
                                                    WSJProperty jProp = (WSJProperty)innerJson;
                                                    INPUT.Save(jProp.Key, jProp.Value.ToString(), false);
                                                }
                                            }
                                        }
                                        else if (json is WSJObject)
                                        {
                                            foreach (WSJProperty jProp in ((WSJObject)json).Value)
                                            {
                                                string jVal = Newtonsoft.Json.JsonConvert.SerializeObject(jProp.Value, new WSFilterConverter());
                                                INPUT.Save(jProp.Key, jVal, false);
                                            }
                                        }
                                        else if (json is WSJProperty)
                                        {
                                            WSJProperty jProp = (WSJProperty)json;
                                            string jVal = Newtonsoft.Json.JsonConvert.SerializeObject(jProp.Value, new WSFilterConverter());
                                            INPUT.Save(jProp.Key, jVal, false);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    #endregion

                    #region READ QUERY-STRING PARAMETERS
                    foreach (var queryParam in _InContext.Request.QueryString.Keys)
                    {
                        if (queryParam != null)
                        {
                            string qKey = queryParam.ToString();
                            string qValue = _InContext.Request.QueryString[qKey];
                            INPUT.Save(qKey, qValue);
                        }
                    }
                    #endregion

                    #region READ ROUTE-DATA PARAM
                    foreach (var urlParam in _InContext.Request.RequestContext.RouteData.Values)
                    {
                        if (!WSConstants.STANDARD_ASP_URL_PARAMS.Select(p => p.ToLower()).Contains(urlParam.Key.ToLower()))
                        {
                            string uKey = urlParam.Key;
                            string uValue = urlParam.Value.ToString();
                            INPUT.Save(uKey, uValue);
                        }
                    }
                    #endregion

                    SessionID = INPUT.Any(x => WSConstants.PARAMS.SESSIONID.Match(x.Key)) ? INPUT.FirstOrDefault(x => WSConstants.PARAMS.SESSIONID.Match(x.Key)).Value : string.Empty;
                    if (string.IsNullOrEmpty(SessionID))
                    {
                        if (_InContext.Session != null) { SessionID = _InContext.Session.SessionID; }
                        else { SessionID = _InContext.Request.Params["ASP.NET_SessionId"]; }
                    }

                    IsLocal = _InContext.Request == null || _InContext.Request.IsLocal;

                    UserHostAddress = _InContext.Request.UserHostAddress;

                    HttpMethod = _InContext.Request.HttpMethod;

                    Url = _InContext.Request.Url;

                    Files = _InContext.Request.Files;
                }
            }
            catch (Exception) { }
        }

        public bool IsLocal { get; private set; } = false;
        public string SessionID { get; private set; } = "undefined";
        public string HttpMethod { get; private set; } = "GET";
        public string UserHostAddress { get; private set; } = "0.0.0.0";
        public Uri Url { get; private set; } = null;
        public HttpFileCollection Files { get; private set; } = null;
        public Dictionary<string, string> INPUT { get; private set; } = new Dictionary<string, string>();

    }
}
