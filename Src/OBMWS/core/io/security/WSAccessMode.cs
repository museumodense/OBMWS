using System;
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
    public class WSAccessMode : IXmlSerializable
    {
        public static readonly WSAccessMode READ = new WSAccessMode(WSConstants.ACCESS_LEVEL.READ);
        public static readonly WSAccessMode INSERT = new WSAccessMode(WSConstants.ACCESS_LEVEL.INSERT);
        public static readonly WSAccessMode UPDATE = new WSAccessMode(WSConstants.ACCESS_LEVEL.UPDATE);
        public static readonly WSAccessMode DELETE = new WSAccessMode(WSConstants.ACCESS_LEVEL.DELETE);
        public static readonly WSAccessMode DEV = new WSAccessMode(WSConstants.ACCESS_LEVEL.DEV, false);
        public static readonly WSAccessMode ADMIN = new WSAccessMode(WSConstants.ACCESS_LEVEL.ADMIN, false);
        public static readonly WSAccessMode LOCK = new WSAccessMode(WSConstants.ACCESS_LEVEL.LOCK, false);

        public WSAccessMode(byte _ACCESS_LEVEL, bool _OWNER_ACCESS_ALLOWED = true)
        {
            OWNER_ACCESS_ALLOWED = _OWNER_ACCESS_ALLOWED;
            ACCESS_LEVEL = _ACCESS_LEVEL;
        }

        public bool OWNER_ACCESS_ALLOWED { get; set; }

        public byte ACCESS_LEVEL = WSConstants.ACCESS_LEVEL.LOCK;

        public override string ToString() { return "{" + ACCESS_LEVEL + ":" + OWNER_ACCESS_ALLOWED + "}"; }

        public System.Xml.Schema.XmlSchema GetSchema() { return null; }

        public void ReadXml(System.Xml.XmlReader reader)
        {
            ReadXmlAttributes(reader);
            ReadXmlContent(reader);
        }
        public void ReadXmlAttributes(System.Xml.XmlReader reader)
        {
            #region ACCESS_LEVEL
            string _ACCESS_LEVEL_Value = reader["accessLevel"];
            byte _ACCESS_LEVEL = ACCESS_LEVEL;
            if (!string.IsNullOrEmpty(_ACCESS_LEVEL_Value) && byte.TryParse(_ACCESS_LEVEL_Value, out _ACCESS_LEVEL)) { ACCESS_LEVEL = _ACCESS_LEVEL; }
            #endregion

            #region OWNER_ACCESS_ALLOWED
            string _OWNER_ACCESS_ALLOWED_Value = reader["ownerAccessAllowed"];
            bool _OWNER_ACCESS_ALLOWED = OWNER_ACCESS_ALLOWED;
            if (!string.IsNullOrEmpty(_OWNER_ACCESS_ALLOWED_Value) && bool.TryParse(_OWNER_ACCESS_ALLOWED_Value, out _OWNER_ACCESS_ALLOWED)) { OWNER_ACCESS_ALLOWED = _OWNER_ACCESS_ALLOWED; }
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
            writer.WriteAttributeString("accessLevel", "" + ACCESS_LEVEL);
            writer.WriteAttributeString("ownerAccessAllowed", "" + OWNER_ACCESS_ALLOWED);
        }
        public void WriteXmlContent(System.Xml.XmlWriter writer, int tabIndex = 0) { }

        internal void Merge(WSAccessMode src)
        {
            try
            {
                //allow to lock it
                OWNER_ACCESS_ALLOWED = OWNER_ACCESS_ALLOWED ? src.OWNER_ACCESS_ALLOWED : OWNER_ACCESS_ALLOWED;

                //allow to make it less accessible
                ACCESS_LEVEL = src.ACCESS_LEVEL > ACCESS_LEVEL ? src.ACCESS_LEVEL : ACCESS_LEVEL;
            }
            catch (Exception) { }
        }

        internal WSAccessMode Clone()
        {
            return new WSAccessMode(ACCESS_LEVEL, OWNER_ACCESS_ALLOWED);
        }

        private string _Json = null;
        public string Json
        {
            get
            {
                if (string.IsNullOrEmpty(_Json))
                {
                    StringBuilder sb = new StringBuilder();
                    
                    sb.Append("{");
                    sb.Append("\"ACCESS_LEVEL\":" + ACCESS_LEVEL + "");
                    sb.Append(",\"OWNER_ACCESS_ALLOWED\":" + OWNER_ACCESS_ALLOWED.ToString().ToLower() + "");
                    sb.Append("}");

                    _Json = sb.ToString();
                }
                return _Json;
            }
        }
    }
}