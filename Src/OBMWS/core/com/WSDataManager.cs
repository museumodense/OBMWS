using Newtonsoft.Json;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Web;
using System.Xml.Serialization;
using System.Xml;
using System.Text;

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
    public abstract class WSDataManager<T> where T : WSClientMeta
    {
        public WSClientMeta Meta = null;
        public WSDataManager()
        {
            //Meta = (WSClientMeta)Activator.CreateInstance(typeof(T), new object[] { context });
        }

        public bool sysInit(ref object response)
        {
            try
            {
                if (Meta.Request.SOURCE != null && Meta.Request.SOURCE.ReturnType == typeof(WSSystemEntity))
                {
                    response = new WSSystemResponse<WSSystemEntity>(Meta).response;
                    return true;
                }
            }
            catch (Exception e) { Meta.RegError(GetType(), e, ref Meta.LoadStatus); }
            return false;
        }

        public void proceed(HttpContext context)
        {
            List<string> statuses = new List<string>();
            try
            {
                object response = null;
                DateTime now = DateTime.Now;
                long millis = 0;

                Meta = (WSClientMeta)Activator.CreateInstance(typeof(T), new object[] { context });
                
                init(ref response);

                statuses.Add($"{{URL1:[{Meta.Request.Url.PathAndQuery}],INPUT1:{{{Meta.Request.INPUT.Select(x => x.Key + ":" + x.Value).Aggregate((a, b) => a + "," + b)}}}");

                millis = (DateTime.Now.Ticks - now.Ticks) / 10000; now = DateTime.Now;
                statuses.Add("2. Init : " + millis + " millis");

                if (response != null)
                {
                    if (Meta.Request.FORMAT.Equals(WSConstants.FORMAT.JSON_FORMAT) || response is V1SystemResponseRecord)
                    {
                        #region JSON RESPONSE
                        context.Response.ContentType = "application/json; charset=UTF-8";
                        string responseJson = GET_JSON(Meta, response);
                        context.Response.Write(responseJson);
                        #endregion
                    }
                    else if (Meta.Request.FORMAT.Equals(WSConstants.FORMAT.JSONP_FORMAT))
                    {
                        #region JSONP RESPONSE
                        context.Response.ContentType = "application/javascript; charset=UTF-8";
                        context.Response.Write(Meta.Request.CALLBACK + "(" + GET_JSON(Meta, response) + ")");
                        #endregion
                    }
                    else if (Meta.Request.FORMAT.Equals(WSConstants.FORMAT.XML_FORMAT))
                    {
                        #region XML RESPONSE
                        context.Response.ContentType = "application/xml; charset=UTF-8";
                        using (XmlWriter writer = new XmlTextWriter(context.Response.OutputStream, Encoding.Unicode))
                        {
                            if (response is IXmlSerializable)
                            {
                                ((IXmlSerializable)response).WriteXml(writer);
                            }
                            else
                            {
                                XmlSerializer serializer = new XmlSerializer(response.GetType());
                                serializer.Serialize(writer, response);
                            }
                        }
                        #endregion
                    }
                    else if (Meta.Request.FORMAT.Equals(WSConstants.FORMAT.PDF_FORMAT))
                    {
                        #region PDF RESPONSE
                        BinaryPDFResponse BResponse = (BinaryPDFResponse)response;
                        if (string.IsNullOrEmpty(BResponse.URL))
                        {
                            context.Response.ContentType = "text/plain";
                            context.Response.Write("Error! Could not find file" + (BResponse.ID < 0 ? "" : string.Format(" with ID:{0}", BResponse.ID)) + ".");
                        }
                        else
                        {
                            context.Response.Redirect(BResponse.URL, true);
                        }
                        #endregion
                    }
                    else if (Meta.Request.FORMAT.Equals(WSConstants.FORMAT.IMAGE_FORMAT))
                    {
                        #region IMAGE RESPONSE
                        BinaryImageResponse IResponse = (BinaryImageResponse)response;
                        if (IResponse.image == null)
                        {
                            context.Response.ContentType = "text/plain";
                            context.Response.Write(string.IsNullOrEmpty(IResponse.status) ? ("Error! Could not find image" + (IResponse.ID < 0 ? "" : string.Format(" with ID:{0}", IResponse.ID)) + ".") : IResponse.status);
                        }
                        else
                        {
                            context.Response.ContentType = "image/jpeg";
                            context.Response.AddHeader("Content-disposition", IResponse.load_mode.NAME + "; filename=HA" + IResponse.ID + ".jpg");
                            try { IResponse.image.Save(context.Response.OutputStream, System.Drawing.Imaging.ImageFormat.Jpeg); }
                            catch (Exception) { IResponse.image.Save(context.Response.OutputStream, System.Drawing.Imaging.ImageFormat.Png); }
                        }
                        #endregion
                    }
                }
                millis = (DateTime.Now.Ticks - now.Ticks) / 10000; now = DateTime.Now;
                statuses.Add("2. Print (serialize) : " + millis + " millis");
            }
            catch (ArgumentException ex)
            {
                #region THROW EXCEPTION (as short description text)                
                context.Response.ContentType = "text/plain";
                context.Response.Write("[ARGUMENT EXCEPTION:[" + ex.Message + (":" + ex.StackTrace) + "]");
                #endregion
            }
            catch (Exception ex)
            {
                #region THROW EXCEPTION (as short description text)
                context.Response.ContentType = "text/plain";
                context.Response.Write($"[GENERAL EXCEPTION:[{ex.Message}:{ex.StackTrace}][{statuses.Aggregate((a, b) => a + "," + b)}]");
                #endregion
            }
            finally
            {
                #region CLOSE RESPONSE
                Meta.CleanUp();

                context.Response.OutputStream.Flush();
                context.Response.End();
                #endregion
            }
        }

        protected abstract void init(ref object response);

        private string GET_JSON(WSClientMeta meta, object response)
        {
            try
            {
                JsonSerializerSettings settings = new JsonSerializerSettings();
                settings.Converters.Add(new WSRecordJsonConverter(meta.Request.OUT_FIELDS, meta.Request, meta.ClientFunctions, meta.Request.DBContext, response.GetType()));
                return JsonConvert.SerializeObject(response, Newtonsoft.Json.Formatting.Indented, settings);
            }
            catch (Exception) { }
            return string.Empty;
        }


        public delegate void DelWriteResponse<X>(OBMWS.WSDataManager<X> mgr, HttpContext context, List<string> statuses, ref object response) where X : WSClientMeta;
        public DelWriteResponse<T> WriteResponse = null;
    }
}