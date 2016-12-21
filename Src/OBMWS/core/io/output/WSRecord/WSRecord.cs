using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;
using Newtonsoft.Json;

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
    public class WSRecord : IXmlSerializable
    {
        public WSRecord(MetaFunctions _CFunc, WSEntity _entity, byte _role, WSSchema _schema, WSParamList _outFields = null, string _Name = null)
        {
            CFunc = _CFunc;
            Name = string.IsNullOrEmpty(_Name) ? GetType().Name : _Name;
            schema = _schema;
            outFields = _outFields;
            entity = _entity;
            role = _role;
        }

        private string Name = "unknown";
        private WSParamList outFields = null;
        private WSSchema schema = null;

        public WSValue mode { get; set; }

        private byte role { get; set; }
        public WSEntity entity { get; private set; }

        private WSSource _xSource = null;
        private MetaFunctions CFunc;

        private WSSource xSource
        {
            get
            {
                if (_xSource == null)
                {
                    try
                    {
                        _xSource = CFunc.GetSourceByType(entity.GetType());
                    }
                    catch (Exception e) { WSStatus status = WSStatus.NONE.clone(); CFunc.RegError(GetType(), e, ref status); }
                }
                return _xSource;
            }
        }

        private WSParam GetParam(string name, Type DefaultFieldType = null)
        {
            WSParam xParam = null;
            if (entity != null)
            {
                if (entity is WSDynamicEntity && xSource != null) { xParam = xSource.GetXParam(name, DefaultFieldType); }
                else if (entity is WSStaticEntity || entity is WSV1ResponseEntity) { xParam = new WSParam(0, name, DefaultFieldType, null); }
            }
            return xParam;
        }

        internal static bool IsValid<X>(X rec) where X : class { return rec != null; }

        #region PROCEED XML READ/WRITE
        public System.Xml.Schema.XmlSchema GetSchema() { return null; }
        public void ReadXml(XmlReader reader) { }
        public void WriteXml(XmlWriter writer)
        {
            try
            {
                writer.WriteStartElement(Name);
                Type type = GetType();
                bool writeAllFields = WSParamList.IsEmpty(outFields);

                foreach (PropertyInfo x in type.GetProperties())
                {
                    string xName = x.Name.ToLower();

                    WSParam xParam = GetParam(x.Name, x.PropertyType);
                    if (xParam != null && role >= xParam.READ_ACCESS_MODE.ACCESS_LEVEL)
                    {
                        if (writeAllFields)
                        {
                            writer.WriteStartElement(xName);
                            WriteXmlValue(x.GetValue(this, null), null, x.PropertyType, writer);
                            writer.WriteEndElement();
                        }
                        else
                        {
                            WSParam outputParam = outFields.FirstOrDefault(a => a.Match(xName));
                            if (outputParam != null)
                            {
                                writer.WriteStartElement(xName);
                                WriteXmlValue(x.GetValue(this, null), outputParam, x.PropertyType, writer);
                                writer.WriteEndElement();
                            }
                        }
                    }
                }
                foreach (FieldInfo x in type.GetFields())
                {
                    string xName = x.Name.ToLower();

                    WSParam xParam = GetParam(x.Name, x.FieldType);
                    if (xParam != null && role >= xParam.READ_ACCESS_MODE.ACCESS_LEVEL)
                    {
                        if (writeAllFields)
                        {
                            writer.WriteStartElement(xName);
                            WriteXmlValue(x.GetValue(this), null, x.FieldType, writer);
                            writer.WriteEndElement();
                        }
                        else
                        {
                            WSParam outputParam = outFields.FirstOrDefault(a => a.Match(xName));
                            if (outputParam != null)
                            {
                                writer.WriteStartElement(xName);
                                WriteXmlValue(x.GetValue(this), outputParam, x.FieldType, writer);
                                writer.WriteEndElement();
                            }
                        }
                    }
                }
                writer.WriteEndElement();
            }
            catch (Exception e) { WSStatus status = WSStatus.NONE.clone(); CFunc.RegError(GetType(), e, ref status); }
        }
        private void WriteXmlValue(object obj, WSParam outputParam, Type type, XmlWriter writer)
        {
            WSStatus status = WSStatus.NONE.clone();
            try
            {
                object _temp = null;
                if (obj == null)
                {
                    writer.WriteValue(string.Empty);
                }
                else if (obj.GetType().IsPrimitive || obj.GetType().Equals(typeof(string)))
                {
                    writer.WriteValue(outputParam.TryReadPrimitiveWithDefault(obj, string.Empty, out _temp) ? _temp : string.Empty);
                }
                else if (type.IsCollection())
                {
                    if (obj is IEnumerable)
                    {
                        IEnumerable enu = obj as IEnumerable;

                        if (enu == null || !enu.GetEnumerator().MoveNext()) { writer.WriteValue(string.Empty); }
                        else
                        {
                            if (obj is IList)
                            {
                                IList list = obj as IList;
                                Type lt = list[0].GetType();
                                if (lt.IsPrimitive) { writer.WriteValue(outputParam.TryReadPrimitiveWithDefault(obj, string.Empty, out _temp) ? _temp : string.Empty); }
                                else
                                {
                                    foreach (object item in list)
                                    {
                                        if (item is WSRecord) { ((WSRecord)item).WriteXml(writer); }
                                        else { object _item = null; writer.WriteValue(outputParam.TryReadPrimitiveWithDefault(item, string.Empty, out _item)? _item : string.Empty); }
                                    }
                                }
                            }
                            else if (obj is IDictionary)
                            {
                                IDictionary list = obj as IDictionary;

                                foreach (object item in list)
                                {
                                    DictionaryEntry entry = (DictionaryEntry)item;
                                    writer.WriteStartElement(entry.Key != null ? entry.Key.ToString() : "undefined");
                                    if (entry.Value is WSRecord) { ((WSRecord)entry.Value).WriteXml(writer); }
                                    else { object _item = null; writer.WriteValue(outputParam.TryReadPrimitiveWithDefault(entry.Value, string.Empty, out _item)? _item : string.Empty); }
                                    writer.WriteEndElement();
                                }
                            }
                        }
                    }
                }
                else if (obj is WSRecord)
                {
                    ((WSRecord)obj).WriteXml(writer);
                }
                else
                {
                    try { new XmlSerializer(obj.GetType()).Serialize(writer, obj); }
                    catch (Exception e) {
                        CFunc.RegError(GetType(), e, ref status);
                        writer.WriteValue(string.Empty);
                    }
                }
            }
            catch (Exception e) { CFunc.RegError(GetType(), e, ref status); }
        }
        #endregion

        #region PROCEED JSON WRITE
        public WSStatus WriteJson(JsonWriter writer, JsonSerializer serializer, List<Type> printedTypes, WSRequest Request, MetaFunctions CFunc, WSDataContext DBContext)
        {
            WSStatus status = WSStatus.NONE_Copy();
            try
            {
                if (entity == null)
                {
                    status.CODE = WSStatus.ERROR.CODE;
                    status.AddNote("WSEntity not known", WSConstants.ACCESS_LEVEL.READ);
                }
                else
                {
                    List<Type> postPrintedTypes = printedTypes.Select(x => x).ToList();
                    postPrintedTypes.Add(entity.GetType());

                    if (entity != null) { status.childs.Add(entity.WriteJson(writer, serializer, schema, outFields, postPrintedTypes, Request, CFunc, DBContext)); }
                }
            }
            catch (Exception e) {
                CFunc.RegError(GetType(), e, ref status);
                status.CODE = WSStatus.ERROR.CODE;
                status.AddNote("Error(line" + e.LineNumber() + "- " + e.Message + ")");
            }
            return status;
        }
        #endregion
        
    }
}
