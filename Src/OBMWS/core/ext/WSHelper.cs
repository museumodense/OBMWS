using System;
using System.Collections.Generic;
using System.IO;
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
    public abstract class WSHelper
    {
        public WSStatus regStatistics(HttpContext context)
        {
            WSStatus status = WSStatus.NONE_Copy();
            try
            {
                string httpSession = string.Empty;
                string urlQuery = string.Empty;
                string postParams = string.Empty;
                string routeData = string.Empty;
                System.Text.StringBuilder request = new System.Text.StringBuilder();
                Guid errorKey = Guid.NewGuid();

                #region STANDARD_ASP_URL_PARAMS
                string[] STANDARD_ASP_URL_PARAMS = new string[]{
                    "__utma",
                    "__utmb",
                    "__utmc",
                    "__utmz",
                    //"ASP.NET_SessionId",
                    "fbm_876939902336614",
                    "fbm_881650921865512",
                    "fbsr_881650921865512",
                    "ALL_HTTP",
                    "ALL_RAW",
                    "APPL_MD_PATH",
                    //"APPL_PHYSICAL_PATH",
                    //"AUTH_TYPE",
                    //"AUTH_USER",
                    "AUTH_PASSWORD",
                    //"LOGON_USER",
                    //"REMOTE_USER",
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
                    //"CONTENT_TYPE",
                    "GATEWAY_INTERFACE",
                    "HTTPS",
                    "HTTPS_KEYSIZE",
                    "HTTPS_SECRETKEYSIZE",
                    "HTTPS_SERVER_ISSUER",
                    "HTTPS_SERVER_SUBJECT",
                    "INSTANCE_ID",
                    "INSTANCE_META_PATH",
                    //"LOCAL_ADDR",
                    //"PATH_INFO",
                    //"PATH_TRANSLATED",
                    //"QUERY_STRING",
                    //"REMOTE_ADDR",
                    //"REMOTE_HOST",
                    //"REMOTE_PORT",
                    //"REQUEST_METHOD",
                    "SCRIPT_NAME",
                    //"SERVER_NAME",
                    //"SERVER_PORT",
                    "SERVER_PORT_SECURE",
                    //"SERVER_PROTOCOL",
                    "SERVER_SOFTWARE",
                    //"URL",
                    "HTTP_CONNECTION",
                    "HTTP_CONTENT_LENGTH",
                    "HTTP_CONTENT_TYPE",
                    "HTTP_ACCEPT",
                    "HTTP_ACCEPT_ENCODING",
                    "HTTP_ACCEPT_LANGUAGE",
                    "HTTP_COOKIE",
                    //"HTTP_HOST",
                    //"HTTP_REFERER",
                    //"HTTP_USER_AGENT",
                    //"HTTP_ORIGIN",
                    "HTTP_X_REQUESTED_WITH",
                    "HTTP_DNT",
                    "AspSessionIDManagerInitializeRequestCalled",
                    //"AspSession"
                };
                #endregion

                string SecurityKey = string.Empty;
                string referrer = (context.Request.UrlReferrer == null ? "" : context.Request.UrlReferrer.AbsoluteUri);

                new OBMWS.WSConverter().ToMd5Hash("Sdk1RtmB" + DateTime.Now.ToString("yyyyMMddhhmm"), out SecurityKey);

                #region READ 'postParams' PARAMETERS
                try
                {
                    Dictionary<string, string> items = new Dictionary<string, string>();

                    string[] PKeys = context.Request.Params.AllKeys.Where(x => !STANDARD_ASP_URL_PARAMS.Select(p => p.ToLower()).Contains(x.ToLower()) && !x.StartsWith("_")).ToArray();
                    foreach (var PKey in PKeys)
                    {
                        if (PKey != null)
                        {
                            try { items.Add(PKey, context.Request.Params[PKey]); } catch (Exception) { }
                        }
                    }
                    postParams = (items != null && items.Any()) ? items.Select(i => "\"" + i.Key + "\":\"" + i.Value + "\"").Aggregate((a, b) => a + "," + b) : "";
                }
                catch (Exception) { }
                #endregion

                #region READ 'httpSession' PARAMETERS
                try
                {
                    Dictionary<string, string> items = new Dictionary<string, string>() { { "SessionID", context.Session.SessionID } };
                    System.Collections.Specialized.NameObjectCollectionBase.KeysCollection PKeys = context.Session.Keys;
                    foreach (var PKey in PKeys)
                    {
                        if (PKey != null)
                        {
                            try { items.Add(PKey.ToString(), context.Session[PKey.ToString()].ToString()); } catch (Exception) { }
                        }
                    }
                    httpSession = "{\"Session\":{" + ((items != null && items.Any()) ? items.Select(i => "\"" + i.Key + "\":\"" + i.Value + "\"").Aggregate((a, b) => a + "," + b) : "") + "}}";
                }
                catch (Exception) { }
                #endregion

                #region READ 'urlQuery' PARAMETERS
                try
                {
                    Dictionary<string, string> items = new Dictionary<string, string>();
                    foreach (var queryParam in context.Request.QueryString.Keys)
                    {
                        if (queryParam != null)
                        {
                            try { items.Add(queryParam.ToString(), context.Request.QueryString[queryParam.ToString()]); } catch (Exception) { }
                        }
                    }
                    urlQuery = (items != null && items.Any()) ? items.Select(i => "\"" + i.Key + "\":\"" + i.Value + "\"").Aggregate((a, b) => a + "," + b) : "";
                }
                catch (Exception) { }
                #endregion

                #region READ ROUTE-DATA PARAM
                try
                {
                    Dictionary<string, string> items = new Dictionary<string, string>();
                    foreach (var urlParam in context.Request.RequestContext.RouteData.Values)
                    {
                        try { items.Add(urlParam.Key, urlParam.Value != null ? urlParam.Value.ToString() : ""); } catch (Exception) { }
                    }
                    routeData = (items != null && items.Any()) ? items.Select(i => "\"" + i.Key + "\":\"" + i.Value + "\"").Aggregate((a, b) => a + "," + b) : "";
                }
                catch (Exception) { }
                #endregion

                #region Build 'Http_Request' object
                request.Append("{");
                request.Append("\"ApplicationPath\":\"" + context.Request.ApplicationPath + "\",");
                #region Browser
                request.Append("\"Browser\":{");
                if (context.Request.Browser != null)
                {
                    request.Append("\"Browser\":\"" + context.Request.Browser.Browser + "\",");
                    request.Append("\"Id\":\"" + context.Request.Browser.Id + "\",");
                    request.Append("\"Type\":\"" + context.Request.Browser.Type + "\",");
                    request.Append("\"Version\":\"" + context.Request.Browser.Version + "\",");
                    request.Append("\"IsMobileDevice\":\"" + context.Request.Browser.IsMobileDevice + "\",");
                    request.Append("\"Beta\":\"" + context.Request.Browser.Beta + "\",");
                    request.Append("\"Platform\":\"" + context.Request.Browser.Platform + "\",");
                    request.Append("\"ScreenPixelsHeight\":\"" + context.Request.Browser.ScreenPixelsHeight + "\",");
                    request.Append("\"ScreenPixelsWidth\":\"" + context.Request.Browser.ScreenPixelsWidth + "\",");
                    request.Append("\"SupportsCss\":\"" + context.Request.Browser.SupportsCss + "\",");
                    request.Append("\"Cookies\":\"" + context.Request.Browser.Cookies + "\",");
                    request.Append("\"SupportsCallback\":\"" + context.Request.Browser.SupportsCallback + "\",");
                    request.Append("\"W3CDomVersion\":\"" + context.Request.Browser.W3CDomVersion + "\",");
                    request.Append("\"Win16\":\"" + context.Request.Browser.Win16 + "\",");
                    request.Append("\"Win32\":\"" + context.Request.Browser.Win32 + "\",");

                    /*********************************************************
                    "\"Frames\":\"" + Request.Browser.Frames + "\"," +
                    "\"Tables\":\"" + Request.Browser.Tables + "\"," +
                    "\"UseOptimizedCacheKey\":\"" + Request.Browser.UseOptimizedCacheKey + "\"," +
                    "\"PreferredRequestEncoding\":\"" + Request.Browser.PreferredRequestEncoding + "\"," +
                    "\"PreferredResponseEncoding\":\"" + Request.Browser.PreferredResponseEncoding + "\"," +
                    "\"JScriptVersion\":\"" + (Request.Browser.JScriptVersion != null ? Request.Browser.JScriptVersion.Build : -1) + "\"," +
                    "\"VBScript\":\"" + Request.Browser.VBScript + "\"," +
                    "\"SupportsInputMode\":\"" + Request.Browser.SupportsInputMode + "\"," +
                    "\"SupportsItalic\":\"" + Request.Browser.SupportsItalic + "\"," +
                    "\"SupportsJPhoneMultiMediaAttributes\":\"" + Request.Browser.SupportsJPhoneMultiMediaAttributes + "\"," +
                    "\"SupportsJPhoneSymbols\":\"" + Request.Browser.SupportsJPhoneSymbols + "\"," +
                    "\"SupportsQueryStringInFormAction\":\"" + Request.Browser.SupportsQueryStringInFormAction + "\"," +
                    "\"SupportsRedirectWithCookie\":\"" + Request.Browser.SupportsRedirectWithCookie + "\"," +
                    "\"SupportsSelectMultiple\":\"" + Request.Browser.SupportsSelectMultiple + "\"," +
                    "\"SupportsUncheck\":\"" + Request.Browser.SupportsUncheck + "\"," +
                    "\"SupportsXmlHttp\":\"" + Request.Browser.SupportsXmlHttp + "\"," +
                    "\"SupportsDivAlign\":\"" + Request.Browser.SupportsDivAlign + "\"," +
                    "\"SupportsDivNoWrap\":\"" + Request.Browser.SupportsDivNoWrap + "\"," +
                    "\"SupportsEmptyStringInCookieValue\":\"" + Request.Browser.SupportsEmptyStringInCookieValue + "\"," +
                    "\"SupportsFontColor\":\"" + Request.Browser.SupportsFontColor + "\"," +
                    "\"SupportsFontName\":\"" + Request.Browser.SupportsFontName + "\"," +
                    "\"SupportsFontSize\":\"" + Request.Browser.SupportsFontSize + "\"," +
                    "\"SupportsInputIStyle\":\"" + Request.Browser.SupportsInputIStyle + "\"," +
                    "\"SupportsImageSubmit\":\"" + Request.Browser.SupportsImageSubmit + "\"," +
                    "\"SupportsIModeSymbols\":\"" + Request.Browser.SupportsIModeSymbols + "\"," +
                    "\"ActiveXControls\":\"" + Request.Browser.ActiveXControls + "\"," +
                    "\"AOL\":\"" + Request.Browser.AOL + "\"," +
                    "\"BackgroundSounds\":\"" + Request.Browser.BackgroundSounds + "\"," +
                    "\"CanCombineFormsInDeck\":\"" + Request.Browser.CanCombineFormsInDeck + "\"," +
                    "\"CanInitiateVoiceCall\":\"" + Request.Browser.CanInitiateVoiceCall + "\"," +
                    "\"CanRenderAfterInputOrSelectElement\":\"" + Request.Browser.CanRenderAfterInputOrSelectElement + "\"," +
                    "\"CanRenderEmptySelects\":\"" + Request.Browser.CanRenderEmptySelects + "\"," +
                    "\"CanRenderInputAndSelectElementsTogether\":\"" + Request.Browser.CanRenderInputAndSelectElementsTogether + "\"," +
                    "\"CanRenderMixedSelects\":\"" + Request.Browser.CanRenderMixedSelects + "\"," +
                    "\"CanRenderOneventAndPrevElementsTogether\":\"" + Request.Browser.CanRenderOneventAndPrevElementsTogether + "\"," +
                    "\"CanRenderPostBackCards\":\"" + Request.Browser.CanRenderPostBackCards + "\"," +
                    "\"CanRenderSetvarZeroWithMultiSelectionList\":\"" + Request.Browser.CanRenderSetvarZeroWithMultiSelectionList + "\"," +
                    "\"CanSendMail\":\"" + Request.Browser.CanSendMail + "\"," +
                    "\"CDF\":\"" + Request.Browser.CDF + "\"," +
                    "\"Crawler\":\"" + Request.Browser.Crawler + "\"," +
                    "\"DefaultSubmitButtonLimit\":\"" + Request.Browser.DefaultSubmitButtonLimit + "\"," +
                    "\"EcmaScriptVersion\":\"" + Request.Browser.EcmaScriptVersion + "\"," +
                    "\"GatewayMajorVersion\":\"" + Request.Browser.GatewayMajorVersion + "\"," +
                    "\"GatewayMinorVersion\":\"" + Request.Browser.GatewayMinorVersion + "\"," +
                    "\"GatewayVersion\":\"" + Request.Browser.GatewayVersion + "\"," +
                    "\"HasBackButton\":\"" + Request.Browser.HasBackButton + "\"," +
                    "\"HidesRightAlignedMultiselectScrollbars\":\"" + Request.Browser.HidesRightAlignedMultiselectScrollbars + "\"," +
                    "\"HtmlTextWriter\":\"" + Request.Browser.HtmlTextWriter + "\"," +
                    "\"InputType\":\"" + Request.Browser.InputType + "\"," +
                    "\"IsColor\":\"" + Request.Browser.IsColor + "\"," +
                    "\"JavaApplets\":\"" + Request.Browser.JavaApplets + "\"," +
                    "\"MajorVersion\":\"" + Request.Browser.MajorVersion + "\"," +
                    "\"MaximumHrefLength\":\"" + Request.Browser.MaximumHrefLength + "\"," +
                    "\"MaximumRenderedPageSize\":\"" + Request.Browser.MaximumRenderedPageSize + "\"," +
                    "\"MaximumSoftkeyLabelLength\":\"" + Request.Browser.MaximumSoftkeyLabelLength + "\"," +
                    "\"MinorVersion\":\"" + Request.Browser.MinorVersion + "\"," +
                    "\"MinorVersionString\":\"" + Request.Browser.MinorVersionString + "\"," +
                    "\"MobileDeviceManufacturer\":\"" + Request.Browser.MobileDeviceManufacturer + "\"," +
                    "\"MobileDeviceModel\":\"" + Request.Browser.MobileDeviceModel + "\"," +
                    "\"Browser\":\"" + (Request.Browser.MSDomVersion!=null?Request.Browser.MSDomVersion.Build:-1) + "\"," +
                    "\"NumberOfSoftkeys\":\"" + Request.Browser.NumberOfSoftkeys + "\"," +
                    "\"PreferredImageMime\":\"" + Request.Browser.PreferredImageMime + "\"," +
                    "\"PreferredRenderingMime\":\"" + Request.Browser.PreferredRenderingMime + "\"," +
                    "\"PreferredRenderingType\":\"" + Request.Browser.PreferredRenderingType + "\"," +
                    "\"RendersBreakBeforeWmlSelectAndInput\":\"" + Request.Browser.RendersBreakBeforeWmlSelectAndInput + "\"," +
                    "\"RendersBreaksAfterHtmlLists\":\"" + Request.Browser.RendersBreaksAfterHtmlLists + "\"," +
                    "\"RendersBreaksAfterWmlAnchor\":\"" + Request.Browser.RendersBreaksAfterWmlAnchor + "\"," +
                    "\"RendersWmlDoAcceptsInline\":\"" + Request.Browser.RendersWmlDoAcceptsInline + "\"," +
                    "\"RendersWmlSelectsAsMenuCards\":\"" + Request.Browser.RendersWmlSelectsAsMenuCards + "\"," +
                    "\"RequiredMetaTagNameValue\":\"" + Request.Browser.RequiredMetaTagNameValue + "\"," +
                    "\"RequiresAttributeColonSubstitution\":\"" + Request.Browser.RequiresAttributeColonSubstitution + "\"," +
                    "\"RequiresContentTypeMetaTag\":\"" + Request.Browser.RequiresContentTypeMetaTag + "\"," +
                    "\"RequiresControlStateInSession\":\"" + Request.Browser.RequiresControlStateInSession + "\"," +
                    "\"RequiresDBCSCharacter\":\"" + Request.Browser.RequiresDBCSCharacter + "\"," +
                    "\"RequiresHtmlAdaptiveErrorReporting\":\"" + Request.Browser.RequiresHtmlAdaptiveErrorReporting + "\"," +
                    "\"RequiresLeadingPageBreak\":\"" + Request.Browser.RequiresLeadingPageBreak + "\"," +
                    "\"RequiresNoBreakInFormatting\":\"" + Request.Browser.RequiresNoBreakInFormatting + "\"," +
                    "\"RequiresOutputOptimization\":\"" + Request.Browser.RequiresOutputOptimization + "\"," +
                    "\"RequiresPhoneNumbersAsPlainText\":\"" + Request.Browser.RequiresPhoneNumbersAsPlainText + "\"," +
                    "\"RequiresSpecialViewStateEncoding\":\"" + Request.Browser.RequiresSpecialViewStateEncoding + "\"," +
                    "\"RequiresUniqueFilePathSuffix\":\"" + Request.Browser.RequiresUniqueFilePathSuffix + "\"," +
                    "\"RequiresUniqueHtmlCheckboxNames\":\"" + Request.Browser.RequiresUniqueHtmlCheckboxNames + "\"," +
                    "\"RequiresUniqueHtmlInputNames\":\"" + Request.Browser.RequiresUniqueHtmlInputNames + "\"," +
                    "\"RequiresUrlEncodedPostfieldValues\":\"" + Request.Browser.RequiresUrlEncodedPostfieldValues + "\"," +
                    "\"ScreenBitDepth\":\"" + Request.Browser.ScreenBitDepth + "\"," +
                    "\"ScreenCharactersHeight\":\"" + Request.Browser.ScreenCharactersHeight + "\"," +
                    "\"ScreenCharactersWidth\":\"" + Request.Browser.ScreenCharactersWidth + "\"," +
                    "\"SupportsAccesskeyAttribute\":\"" + Request.Browser.SupportsAccesskeyAttribute + "\"," +
                    "\"SupportsBodyColor\":\"" + Request.Browser.SupportsBodyColor + "\"," +
                    "\"SupportsBold\":\"" + Request.Browser.SupportsBold + "\"," +
                    "\"SupportsCacheControlMetaTag\":\"" + Request.Browser.SupportsCacheControlMetaTag + "\"," +
                    * ****************************************************************/

                    request.Append("\"Browser\":\"" + context.Request.Browser.ToString() + "\"");
                }
                request.Append("},");
                #endregion
                request.Append("\"HttpMethod\":\"" + context.Request.HttpMethod + "\",");
                request.Append("\"IsSecureConnection\":\"" + context.Request.IsSecureConnection + "\",");
                request.Append("\"RequestType\":\"" + context.Request.RequestType + "\",");
                request.Append("\"UserAgent\":\"" + context.Request.UserAgent + "\",");
                request.Append("\"UserHostAddress\":\"" + context.Request.UserHostAddress + "\",");
                request.Append("\"UserHostName\":\"" + context.Request.UserHostName + "\",");
                request.Append("\"UserLanguages\":[");
                if (context.Request.UserLanguages != null && context.Request.UserLanguages.Any()) { request.Append(context.Request.UserLanguages.Select(l => "\"" + l + "\"").Aggregate((a, b) => a + "," + b)); }
                request.Append("],");
                request.Append("\"UrlReferrer\":\"" + referrer + "\"");                
                request.Append("}");
                #endregion

                status = WSStatus.SUCCESS_Copy();
                status.AddNote(registerHttpActivity(context.Request.Url.AbsoluteUri, context.Request.UserHostAddress, request.ToString(), httpSession, urlQuery, postParams, referrer, null, true));
            }
            catch (Exception) { status = WSStatus.ERROR_Copy(); }
            return status;
        }
        public abstract string registerError(Guid key, string ip, string source, string title, string exception);
        public abstract string registerHttpActivity(string url, string uip, string http_request, string httpSession, string urlQuery, string postParams, string referrer, string _Notes = null, bool save = false);
        
        public string getNewFileName(string dir, string _name)
        {
            string name = string.Empty;
            if (Directory.Exists(dir))
            {
                string tempName = _name;
                FileInfo file = new FileInfo(dir + tempName);
                string ext = file.Extension;
                int i = 1;
                int maxCount = 100;
                while (file != null && file.Exists && i < maxCount)
                {
                    tempName = _name.Replace(ext, "(" + i + ")" + ext);
                    file = new FileInfo(dir + tempName);
                    i++;
                }
                name = file.Exists ? string.Empty : file.Name;
            }
            return name;
        }
    }
}