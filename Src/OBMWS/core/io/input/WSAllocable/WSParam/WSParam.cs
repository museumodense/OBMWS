using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;

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
    public class WSParam : WSParamValidatable
    {
        private WSParam() { }
        public WSParam(int _CODE, string _NAME, Type _DataType, MetaFunctions _func)
        {
            func = _func;
            if (_CODE < 0) { throw new ArgumentNullException("_CODE"); }
            if (string.IsNullOrEmpty(_NAME)) { throw new ArgumentNullException("_NAME"); }
            CODE = _CODE;
            NAME = _NAME;
            DISPLAY_NAME = _NAME;
            if (!ALIACES.Any(a => a.Equals(NAME.ToLower()))) { ALIACES.Add(NAME.ToLower()); }
            DataType = _DataType;
            if (_DataType != null)
            {
                if (!_DataType.IsSimple() && !_DataType.IsSimpleCollection())
                {
                    Type eType = _DataType.GetEntityType();
                    if (eType.IsValidDynamicEntity() && !ALIACES.Any(a => a.Equals(eType.Name.ToLower())))
                    {
                        ALIACES.Add(eType.Name.ToLower());
                    }
                }
            }
        }

        protected MetaFunctions func = null;

        private WSAccessMode _READ_ACCESS_MODE = null;
        public WSAccessMode READ_ACCESS_MODE { 
            get { if (_READ_ACCESS_MODE == null) { _READ_ACCESS_MODE = new WSAccessMode(WSConstants.ACCESS_LEVEL.READ); } return _READ_ACCESS_MODE; } 
            set { 
                _READ_ACCESS_MODE = value;
                if (_READ_ACCESS_MODE != null && _READ_ACCESS_MODE.ACCESS_LEVEL > WRITE_ACCESS_MODE.ACCESS_LEVEL) { WRITE_ACCESS_MODE = new WSAccessMode(_READ_ACCESS_MODE.ACCESS_LEVEL); }
            } 
        }

        private WSAccessMode _WRITE_ACCESS_MODE = null;
        public WSAccessMode WRITE_ACCESS_MODE { 
            get {
                if (_WRITE_ACCESS_MODE == null) { _WRITE_ACCESS_MODE = new WSAccessMode(READ_ACCESS_MODE.ACCESS_LEVEL > WSConstants.ACCESS_LEVEL.UPDATE ? READ_ACCESS_MODE.ACCESS_LEVEL : WSConstants.ACCESS_LEVEL.UPDATE); } 
                return _WRITE_ACCESS_MODE;
            }
            set
            {
                _WRITE_ACCESS_MODE = value;
                if (value == null) { _WRITE_ACCESS_MODE = value; }
                else if (value.ACCESS_LEVEL < READ_ACCESS_MODE.ACCESS_LEVEL) { _WRITE_ACCESS_MODE = new WSAccessMode(READ_ACCESS_MODE.ACCESS_LEVEL); }
            }
        }

        public bool SkipEmpty = WSConstants.DEFAULT_SKIP_EMPTY_FIELD;
        
        public List<WSValue> ALLOWED_VALUES = new List<WSValue>();
        
        private int _CODE = -1;
        public int CODE {
            get {
                return _CODE;
            }
            set {
                _CODE = value;
            }
        }

        private string _NAME = null;
        public string NAME { get { return _NAME; } set { _NAME = value.ToLower(); } }

        private string _DISPLAY_NAME = null;
        public string DISPLAY_NAME { get { return _DISPLAY_NAME; } set { _DISPLAY_NAME = value.ToLower(); } }

        public string DESCRIPTION { get; set; }

        public Type DataType { get; private set; }

        #region MAIN FUNCTIONS

        public static bool IsValid(WSParam p) { try { return (p != null && p.isValid); } catch (Exception) { } return false; }
        public override bool isValid { get { return CODE >= 0 && !string.IsNullOrEmpty(NAME) && DataType != null; } }

        public bool IsPrimitive { get { return !DataType.IsCollectionOf<WSDynamicEntity>() && !DataType.IsValidDynamicEntity(); } }

        //TODO:2016-03-09 : turn 'isAccessible(...)' to the property, and use func....(User data) to calculate it 
        public bool isAccessible(bool isOwner, int ACCESS_LEVEL)
        {
            return
            (isOwner && READ_ACCESS_MODE.OWNER_ACCESS_ALLOWED)
            ||
            (READ_ACCESS_MODE.ACCESS_LEVEL != WSConstants.ACCESS_LEVEL.LOCK && ACCESS_LEVEL >= READ_ACCESS_MODE.ACCESS_LEVEL);
        }
        
        public virtual bool Match(string key, IEnumerable<WSTableSource> sources=null, Func<Type, WSTableSource> getTSource = null, bool TypeMatchAllowed = true)
        {
            try
            {
                if (!string.IsNullOrEmpty(NAME) && !string.IsNullOrEmpty(key))
                {
                    key = key.ToLower();
                    int _CODE = -1;
                    if (CODE >= 0)
                    {
                        if (NAME.Equals(key)) { return true; }
                        else if (DISPLAY_NAME.Equals(key)) { return true; }
                        else if (ALIACES != null && ALIACES.Contains(key)) { return true; }
                        else if (this is WSTableParam)
                        {
                            WSTableParam p = (WSTableParam)this;
                            if (p.WSColumnRef.NAME.ToLower().Equals(key)) {
                                return true;
                            } else if(TypeMatchAllowed && p.DataType.IsValidDynamicEntity()){

                                WSTableSource src = null;
                                if (getTSource != null) { src = getTSource(p.DataType.GetEntityType()); }

                                else if (sources != null) { src = sources.FirstOrDefault(s => s.ReturnType == p.DataType.GetEntityType()); }

                                else if (func != null) { src = (WSTableSource)func.GetSourceByType(p.DataType.GetEntityType()); }
                                    
                                if (src != null && src.Match(key))
                                {
                                    return true;
                                }
                            }
                        }
                        else if (int.TryParse(key, out _CODE) && _CODE == CODE) { return true; }
                    };
                }
            }
            catch (Exception) { }
            return false;
        }
        
        public WSValue ReadWSValue(Dictionary<string, string> dict, WSValue DEFAULT = null)
        {
            WSValue v = null;
            return TryReadWSValue(dict, out v, DEFAULT) ? v : DEFAULT;
        }
        public bool TryReadWSValue(Dictionary<string, string> dict, out WSValue value, WSValue DEFAULT = null)
        {
            WSValue v = null;
            if (dict != null && dict.Any())
            {
                try
                {
                    Func<KeyValuePair<string, string>, bool> expr = (i => Match(i.Key));
                    if (ALLOWED_VALUES != null && ALLOWED_VALUES.Any())
                    {
                        v = ALLOWED_VALUES.FirstOrDefault(x => x.Match(dict.FirstOrDefault(expr).Value));
                    }
                    else if (dict.Any(expr))
                    {
                        KeyValuePair<string,string> pair = dict.FirstOrDefault(expr);
                        v = new WSValue(pair.Key, pair.Value);
                    }
                }
                catch (Exception) { }
            }
            value = v == null ? DEFAULT : v;
            return value != null;
        }

        public object ReadValue(Dictionary<string, string> dict, string DEFAULT = null)
        {
            object v = null;
            return TryReadValue(dict, out v, DEFAULT) ? v : DEFAULT;
        }
        public bool TryReadValue(Dictionary<string, string> dict, out object v, string DEFAULT = null)
        {
            v = null;
            if (dict != null && dict.Any(d => Match(d.Key)) && DataType.Read(dict.FirstOrDefault(d => Match(d.Key)).Value, out v))
            {
                bool succeeded = v != null;
                v = succeeded ? v : DEFAULT;
                return succeeded;
            }
            return false;
        }

        public bool TryReadPrimitiveWithDefault(object _obj, object _default, out object obj)
        {
            obj = _obj == null ? _default : _obj.GetType().IsSameOrSubclassOf(typeof(string)) ? System.Web.HttpUtility.HtmlDecode(_obj.ToString()) : _obj;
            return obj != null || (_obj == null && _default == null);
        }

        public bool ToWSJson(string invalue, out WSJson outvalue, dynamic DEFAULT = null)
        {
            outvalue = null;
            bool ok = false;
            try
            {
                if (!string.IsNullOrEmpty(invalue))
                {
                    try
                    {
                        outvalue = invalue.ToJson();
                        ok = outvalue != null;
                    }
                    catch (Exception) { }

                    if (!ok)
                    {
                        outvalue = (DataType.IsSimple() || WSConstants.SPECIAL_CASES.Any(c => c.Match(invalue))) ? new WSJValue(invalue)
                            : DataType.IsSameOrSubclassOf(typeof(WSEntity)) && (WSEntityFFilter.OPERATIONS.STATE_OPERATIONS.Any(c => c.Match(invalue))) ? new WSJValue(invalue)
                            : DataType.IsCollectionOf<WSEntity>() && WSEntityListFFilter.OPERATIONS.STATE_OPERATIONS.Any(c => c.Match(invalue)) ? new WSJValue(invalue)
                            : null;
                    }
                    ok = outvalue != null;
                }
            }
            catch (Exception) { }
            outvalue = ok ? outvalue : DEFAULT;
            return ok;
        }

        #endregion

        #region OVERRIDE METHODS
        public override string ToString() { return NAME; }
        public override bool Equals(object obj)
        {
            var item = obj as WSParam;
            if (item == null) { return false; }
            return this.NAME.Equals(item.NAME);
        }
        public override int GetHashCode() { return this.NAME.GetHashCode(); }
        #endregion

        #region IComparer
        public class PComparer : IEqualityComparer<WSParam>
        {
            public bool Equals(WSParam x, WSParam y) { return (x.NAME.Equals(y.NAME)); }
            public int GetHashCode(WSParam obj) { return obj.NAME.GetHashCode(); }
        }
        #endregion

        public new System.Xml.Schema.XmlSchema GetSchema()
        { return null; }

        public void ReadXml(System.Xml.XmlReader reader, Func<XmlReader, WSTableSource> getSource)
        {
            ReadXmlAttributes(reader);
            ReadXmlContent(reader);
        }
        public new void ReadXmlAttributes(System.Xml.XmlReader reader)
        {
            base.ReadXmlAttributes(reader);

            #region NAME
            string _NAME = reader["name"];
            if (!string.IsNullOrEmpty(_NAME)) { NAME = _NAME; }
            #endregion

            #region DISPLAY_NAME
            string _DISPLAY_NAME = reader["displayName"];
            if (!string.IsNullOrEmpty(_DISPLAY_NAME)) { DISPLAY_NAME = _DISPLAY_NAME; }
            #endregion

            #region DESCRIPTION
            string _DESCRIPTION = reader["description"];
            if (!string.IsNullOrEmpty(_DESCRIPTION)) { DESCRIPTION = _DESCRIPTION; }
            #endregion

            #region SkipEmpty
            string _SkipEmptyValue = reader["skipEmpty"];
            bool _SkipEmpty = SkipEmpty;
            if (!string.IsNullOrEmpty(_SkipEmptyValue) && bool.TryParse(_SkipEmptyValue, out _SkipEmpty)) { SkipEmpty = _SkipEmpty; }
            #endregion
        }
        public new void ReadXmlContent(System.Xml.XmlReader reader)
        {
            base.ReadXmlContent(reader);

            bool done = false;
            while (reader.MoveToContent() == XmlNodeType.Element)
            {
                switch (reader.Name)
                {
                    case "readAccessMode":
                        READ_ACCESS_MODE.ReadXml(reader);
                        break;
                    case "writeAccessMode":
                        WRITE_ACCESS_MODE.ReadXml(reader);
                        break;
                    case "allowedValues":
                        if (reader.ReadToDescendant("allowedValue"))
                        {
                            List<WSValue> values = new List<WSValue>();
                            while (reader.MoveToContent() == XmlNodeType.Element)
                            {
                                if (!reader.IsEmptyElement)
                                {
                                    string aName = reader.GetAttribute("name");
                                    if (string.IsNullOrEmpty(aName)) { reader.Skip(); }
                                    else {
                                        WSValue value = new WSValue(aName);
                                        value.ReadXml(reader);
                                        values.Add(value);
                                    }
                                }
                                reader.MoveToContent();
                                if (!reader.Read()) break;
                            }
                            ALLOWED_VALUES = values;
                        }
                        break;
                    default:
                        {
                            reader.Skip();
                            done = true;
                            break;
                        }
                }
                reader.MoveToContent();
                if (done || !reader.Read()) break;
            }
        }

        public new void WriteXml(System.Xml.XmlWriter writer)
        {
            WriteXmlAttributes(writer);
            WriteXmlContent(writer);
        }
        public new void WriteXmlAttributes(System.Xml.XmlWriter writer)
        {
            base.WriteXmlAttributes(writer);
            
            writer.WriteAttributeString("name", "" + NAME);
            writer.WriteAttributeString("displayName", "" + DISPLAY_NAME);
            writer.WriteAttributeString("description", DESCRIPTION == null ? "" : DESCRIPTION);
            writer.WriteAttributeString("skipEmpty", "" + SkipEmpty);
            writer.WriteAttributeString("paramType", "" + GetType().Name);
        }
        public new void WriteXmlContent(System.Xml.XmlWriter writer, int tabIndex = 0)
        {
            base.WriteXmlContent(writer);

            #region READ_ACCESS_MODE
            writer.WriteStartElement("readAccessMode");
            if (READ_ACCESS_MODE != null) READ_ACCESS_MODE.WriteXml(writer);
            writer.WriteEndElement();
            #endregion

            #region WRITE_ACCESS_MODE
            writer.WriteStartElement("writeAccessMode");
            if (WRITE_ACCESS_MODE != null) WRITE_ACCESS_MODE.WriteXml(writer);
            writer.WriteEndElement();
            #endregion
            
            #region ALLOWED_VALUES
            writer.WriteStartElement("allowedValues");
            if (ALLOWED_VALUES != null)
            {
                foreach (WSValue val in ALLOWED_VALUES)
                {
                    if (val != null)
                    {
                        writer.WriteStartElement("allowedValue");
                        val.WriteXml(writer);
                        writer.WriteEndElement();
                    }
                }
            }
            writer.WriteEndElement();
            #endregion
        }

        internal void Merge(WSParam param)
        {
            try
            {
                base.Merge(param);

                if (param.isValid)
                {
                    NAME = string.IsNullOrEmpty(param.NAME) ? NAME : param.NAME;
                    DISPLAY_NAME = string.IsNullOrEmpty(param.DISPLAY_NAME) ? DISPLAY_NAME : param.DISPLAY_NAME;
                    DESCRIPTION = string.IsNullOrEmpty(param.DESCRIPTION) ? DESCRIPTION : param.DESCRIPTION;
                    SkipEmpty = param.SkipEmpty;

                    READ_ACCESS_MODE.Merge(param.READ_ACCESS_MODE);
                    WRITE_ACCESS_MODE.Merge(param.WRITE_ACCESS_MODE);
                    if (param.ALLOWED_VALUES != null && param.ALLOWED_VALUES.Any(v => v.isValid)) { ALLOWED_VALUES = param.ALLOWED_VALUES.Where(v => v.isValid).ToList(); }
                }
            }
            catch (Exception) { }
        }

        internal WSParam Clone()
        {
            WSParam param = new WSParam(CODE, NAME, DataType, func);

            param.DISPLAY_NAME = DISPLAY_NAME;
            param.READ_ACCESS_MODE = READ_ACCESS_MODE != null ? READ_ACCESS_MODE.Clone() : new WSAccessMode(WSConstants.ACCESS_LEVEL.READ);
            param.WRITE_ACCESS_MODE = WRITE_ACCESS_MODE != null ? WRITE_ACCESS_MODE.Clone() : new WSAccessMode(WSConstants.ACCESS_LEVEL.UPDATE);
            param.SkipEmpty = SkipEmpty;
            param.ALLOWED_VALUES = (ALLOWED_VALUES != null && ALLOWED_VALUES.Any()) ? ALLOWED_VALUES.Select(x => x.Clone()).ToList() : new List<WSValue>();
            param.ALIACES = (ALIACES != null && ALIACES.Any()) ? ALIACES.Select(x => x).ToList() : new List<string>();
            param.DESCRIPTION = DESCRIPTION;
            
            return param;
        }

        internal void Configure(WSSource Source, WSJson json)
        {
            try
            {
                if (Source != null && json != null)
                {
                    if (json is WSJProperty && ((WSJProperty)json).Value is WSJValue)
                    {
                        WSJValue vJson = (WSJValue)((WSJProperty)json).Value;
                        switch (((WSJProperty)json).Key)
                        {
                            case "readaccess":
                                byte readAccessValue = byte.TryParse(vJson.Value, out readAccessValue) ? readAccessValue : READ_ACCESS_MODE.ACCESS_LEVEL;
                                READ_ACCESS_MODE = new WSAccessMode(readAccessValue, READ_ACCESS_MODE.OWNER_ACCESS_ALLOWED);
                                break;
                            case "writeaccess":
                                byte writeAccessValue = byte.TryParse(vJson.Value, out writeAccessValue) ? writeAccessValue : WRITE_ACCESS_MODE.ACCESS_LEVEL;
                                WRITE_ACCESS_MODE = new WSAccessMode(writeAccessValue, WRITE_ACCESS_MODE.OWNER_ACCESS_ALLOWED);
                                break;
                            case "skipempty":
                                bool skipempty = vJson.Value.IsTrue() ? true : vJson.Value.IsFalse() ? false : SkipEmpty;
                                SkipEmpty = skipempty;
                                break;
                            default: break;
                        }
                    }
                }
            }
            catch (Exception) { }
        }

        private string _Json = null;
        public new string Json { get { if (string.IsNullOrEmpty(_Json)) { _Json = LoadJson(); } return _Json; } }
        private string LoadJson()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("\"NAME\":\"" + NAME + "\"");
            sb.Append(",\"DISPLAY_NAME\":\"" + DISPLAY_NAME + "\"");
            sb.Append(",\"DESCRIPTION\":\"" + (DESCRIPTION == null ? "" : DESCRIPTION) + "\"");
            sb.Append(",\"DataType\":\"" + (DataType.Name.Equals("Nullable`1") ? (DataType.GetGenericTypeArguments()[0].Name + " (Nullable)") : DataType.IsCollectionOf<WSEntity>() ? (DataType.GetEntityType().Name + "[]") : DataType.Name) + "\"");
            sb.Append(",\"IsPrimitive\":" + (IsPrimitive).ToString().ToLower() + "");
            sb.Append(",\"IsEditable\":" + (IsPrimitive && func!=null && func.IsAccessible(WRITE_ACCESS_MODE.ACCESS_LEVEL) && WRITE_ACCESS_MODE.ACCESS_LEVEL >= WSConstants.ACCESS_LEVEL.UPDATE).ToString().ToLower() + "");

            sb.Append("," + base.Json);

            sb.Append(",\"READ_ACCESS_MODE\":");
            sb.Append(READ_ACCESS_MODE.Json);            
            sb.Append(",\"ALLOWED_VALUES\":[");
            if (ALLOWED_VALUES != null && ALLOWED_VALUES.Any())
            {
                sb.Append(ALLOWED_VALUES.Select(x => x.Json).Where(x => !string.IsNullOrEmpty(x)).Select(x => "{" + x + "}").Aggregate((a, b) => a + "," + b));
            }
            sb.Append("]");
            return sb.ToString();
        }
    }
}
