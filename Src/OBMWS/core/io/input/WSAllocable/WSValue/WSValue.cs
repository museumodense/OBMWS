using System;
using System.Collections.Generic;
using System.Linq;

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
    public class WSValue : WSAllocable
    {
        public WSValue() { }
        public WSValue(string _NAME) { init(_NAME, new List<string> { }); }
        public WSValue(string _NAME, string _VALUE) { init(_NAME, new List<string> { _VALUE }); }
        public WSValue(string _NAME, IEnumerable<string> _ALIACES) { init(_NAME, _ALIACES); }
        private void init(string _NAME, IEnumerable<string> _ALIACES)
        {
            if (string.IsNullOrEmpty(_NAME)) { throw new ArgumentNullException("'NAME' can not be empty"); }
            this.NAME = _NAME;
            if (_ALIACES != null&& _ALIACES.Any())
            {
                _ALIACES = _ALIACES.Select(x => x.ToLower());
                ALIACES.AddRange(_ALIACES);
                ALIACES = ALIACES.Distinct().ToList();
            }
        }

        public string NAME { get; private set; }
        public string DESCRIPTION { get; set; }

        public static bool IsValid(WSValue root) { try { return root != null && root.isValid; } catch (Exception) { } return false; }
        public bool isValid { get { try { return !string.IsNullOrEmpty(NAME) && ALIACES != null; } catch (Exception) { } return false; } }

        public bool Match(string value) { try {
            value = value == null ? null : value.ToLower();
            return isValid && value != null && (ALIACES.Any(x => value.Equals(x)) || NAME.ToLower().Equals(value));
        } catch (Exception) { return false; } }
        
        public bool Match(WSValue value) { try { 
            return value!=null && (ALIACES.Any(x => value.ALIACES.Any(v=>x.Equals(v))) || value.NAME.ToLower().Equals(NAME.ToLower())); 
        } catch (Exception) { } return false; }

        public override string ToString() { return "{" + NAME + ":[" + (ALIACES == null || ALIACES.Count == 0 ? string.Empty : ALIACES.Aggregate((a, b) => a + "," + b)) + "]}"; }
        public override bool Equals(object obj)
        {
            if (obj == null) { return false; }
            return obj is WSValue && this.NAME.ToLower().Equals((obj as WSValue).NAME.ToLower());
        }
        public override int GetHashCode() { return NAME.GetHashCode(); }

        public class XComparer : IEqualityComparer<WSValue>
        {
            public bool Equals(WSValue x, WSValue y) { return x.Equals(y); }
            public int GetHashCode(WSValue obj) { return obj.GetHashCode();/* ^ obj.name.GetHashCode(); */ }
        }

        #region XML SERIALIZING
        public new System.Xml.Schema.XmlSchema GetSchema(){ return null; }

        public new void ReadXml(System.Xml.XmlReader reader)
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

            #region DESCRIPTION
            string _DESCRIPTION = reader["description"];
            if (!string.IsNullOrEmpty(_DESCRIPTION)) { DESCRIPTION = _DESCRIPTION; }
            #endregion
        }
        public new void ReadXmlContent(System.Xml.XmlReader reader)
        {
            base.ReadXmlContent(reader);
        }
        public new void WriteXml(System.Xml.XmlWriter writer)
        {
            WriteXmlAttributes(writer);
            WriteXmlContent(writer);
        }
        public new void WriteXmlAttributes(System.Xml.XmlWriter writer)
        {
            base.WriteXmlAttributes(writer);

            writer.WriteAttributeString("name", NAME);
            writer.WriteAttributeString("description", DESCRIPTION == null ? "" : DESCRIPTION);
        }
        public new void WriteXmlContent(System.Xml.XmlWriter writer, int tabIndex = 0)
        {
            base.WriteXmlContent(writer, tabIndex);
        }
        #endregion

        public void Merge(WSValue src)
        {
            base.Merge(src);

            NAME = string.IsNullOrEmpty(src.NAME) ? NAME : src.NAME;
            DESCRIPTION = string.IsNullOrEmpty(src.DESCRIPTION) ? DESCRIPTION : src.DESCRIPTION;
        }
        
        internal WSValue Clone()
        {
            return new WSValue()
            {
                NAME = NAME,
                DESCRIPTION = DESCRIPTION,
                ALIACES = ALIACES.Select(x=>x).ToList()
            };
        }
    }
}