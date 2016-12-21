using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

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
    public abstract class WSFormat : IXmlSerializable
    {
        private WSFormat() { }
        public WSFormat(int _code, string _name, bool _isStandard, bool _isFixed = false)
        {
            if (_code < 0) { throw new ArgumentNullException("_code"); }
            if (string.IsNullOrEmpty(_name)) { throw new ArgumentNullException("_name"); } 
            code = _code;
            name = _name.ToLower();
            isStandard = _isStandard;
            isFixed = _isFixed;
        }
        private bool isFixed { get; set; }

        private int _code = -1;
        public int code { get { return _code; } set { _code = isFixed ? _code : value; } }

        private string _name = string.Empty;
        public string name { get { return _name; } set { _name = isFixed ? _name : value; } }

        public bool isStandard { get; private set; }

        public bool Match(WSFormat value)
        {
            try
            {
                return value!=null &&
                (
                    value.code == code
                );
            }
            catch (Exception) { return false; }
        }

        public override bool Equals(object obj)
        {
            if (obj == null) { return false; }
            WSFormat p = obj as WSFormat;
            if ((System.Object)p == null) { return false; }
            return GetHashCode() == p.GetHashCode();
        }
        public override string ToString() { return "{" + code + ":" + name + "}"; }
        public override int GetHashCode() { return ToString().GetHashCode(); }

        #region IComparer
        public class PComparer : IEqualityComparer<WSFormat>
        {
            public bool Equals(WSFormat x, WSFormat y) { return x.GetHashCode() == y.GetHashCode(); }
            public int GetHashCode(WSFormat obj) { return obj.GetHashCode(); }
        }
        #endregion

        public System.Xml.Schema.XmlSchema GetSchema() { return null; }

        public void ReadXml(System.Xml.XmlReader reader)
        {
            ReadXmlAttributes(reader);
            ReadXmlContent(reader);
        }
        public void ReadXmlAttributes(System.Xml.XmlReader reader)
        {
            #region Name
            string _Name = reader["name"];
            if (!string.IsNullOrEmpty(_Name)) {
                WSFormat _format = WSConstants.FORMAT.FORMATS.FirstOrDefault(x=>x.name.Equals(_Name.ToLower()));
                if (_format != null)
                {
                    code = _format.code;
                    name = _format.name;
                    isStandard = _format.isStandard;
                    isFixed = _format.isFixed;
                }
            }
            #endregion
        }
        public void ReadXmlContent(System.Xml.XmlReader reader) { }
        
        public void WriteXml(System.Xml.XmlWriter writer)
        {
            WriteXmlAttributes(writer);
            WriteXmlContent(writer);
        }
        public void WriteXmlAttributes(System.Xml.XmlWriter writer)
        {
            writer.WriteAttributeString("name", "" + name);
        }
        public void WriteXmlContent(System.Xml.XmlWriter writer, int tabIndex = 0) { }
        
        internal object convert(object text)
        {
            try
            {
                if (text != null && WSConstants.FORMAT.FORMATS.Contains(this))
                {
                    if (this.Equals(WSConstants.FORMAT.XML_FORMAT))
                    {
                        //TODO: convert to XML
                    }
                    else if (this.Equals(WSConstants.FORMAT.JSON_FORMAT))
                    {
                        text = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(text);
                    }
                    else if (this.Equals(WSConstants.FORMAT.JSONP_FORMAT))
                    {
                        text = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(text);
                    }
                    else if (this.Equals(WSConstants.FORMAT.PDF_FORMAT))
                    {
                        //TODO: convert to PDF
                    }
                    else if (this.Equals(WSConstants.FORMAT.IMAGE_FORMAT))
                    {
                        //TODO: convert to IMAGE
                    }
                    else if (this.Equals(WSConstants.FORMAT.TEXT_FORMAT))
                    {
                        text = text == null ? string.Empty : text;
                    }
                    else if (this.Equals(WSConstants.FORMAT.NONE_FORMAT))
                    {
                        //do nothing
                    }
                }
            }
            catch (Exception) { }
            return text;
        }

        internal abstract WSFormat Clone();

        private string _Json = null;
        public string Json
        {
            get
            {
                if (string.IsNullOrEmpty(_Json))
                {
                    StringBuilder sb = new StringBuilder();

                    sb.Append("\"Name\":\"" + name + "\"");
                    sb.Append(",\"IsStandard\":" + isStandard.ToString().ToLower() + "");

                    _Json = sb.ToString();
                }
                return _Json;
            }
        }
    }
}