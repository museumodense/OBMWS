using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

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
    public abstract class WSAllocable : IXmlSerializable
    {
        private List<string> _ALIACES = null;
        public List<string> ALIACES
        {
            get { if (_ALIACES == null) { _ALIACES = new List<string>(); } return _ALIACES; }
            set {
                _ALIACES = 
                    (value != null && value.Any(x => !string.IsNullOrEmpty(x))) ? 
                    value.Where(x => !string.IsNullOrEmpty(x)).Select(v => v.ToLower()).Distinct().ToList() : 
                    null; 
            }
        }

        #region XML 
        public System.Xml.Schema.XmlSchema GetSchema() { return null; }

        #region READ XML
        public void ReadXml(System.Xml.XmlReader reader)
        {
            ReadXmlAttributes(reader);
            ReadXmlContent(reader);
        }
        public void ReadXmlAttributes(System.Xml.XmlReader reader) { }
        public void ReadXmlContent(System.Xml.XmlReader reader)
        {
            reader.MoveToContent();
            reader.Read();

            bool done = false;
            while (reader.MoveToContent() == XmlNodeType.Element)
            {
                switch (reader.Name)
                {
                    case "aliaces":
                        if (reader.ReadToDescendant("aliace"))
                        {
                            List<string> aList = new List<string>();
                            while (reader.MoveToContent() == XmlNodeType.Element)
                            {
                                if (!reader.IsEmptyElement)
                                {
                                    string aliace = reader.ReadElementContentAsString();
                                    if (!string.IsNullOrEmpty(aliace)) { if (!aList.Contains(aliace)) { aList.Add(aliace); } }
                                }
                                if (!reader.Read()) break;
                            }
                            //MergeAliaces(aList);
                            ALIACES = aList;
                        }
                        break;
                    default:
                        {
                            done = true;
                            break;
                        }
                }
                reader.MoveToContent();
                if (done || !reader.Read()) break;
            }
        }
        #endregion

        #region WRITE XML
        public void WriteXml(System.Xml.XmlWriter writer)
        {
            WriteXmlAttributes(writer);
            WriteXmlContent(writer);
        }
        public void WriteXmlAttributes(System.Xml.XmlWriter writer) { }
        public void WriteXmlContent(System.Xml.XmlWriter writer, int tabIndex = 0)
        {
            #region ALIACES
            writer.WriteStartElement("aliaces");
            if (ALIACES != null)
            {
                tabIndex++;
                foreach (string aliace in ALIACES)
                {
                    if (!string.IsNullOrEmpty(aliace))
                    {
                        writer.WriteStartElement("aliace");
                        writer.WriteValue(aliace);
                        writer.WriteEndElement();
                    }
                }
                tabIndex++;
            }
            writer.WriteEndElement();
            #endregion
        }
        #endregion

        #endregion

        #region JSON
        private string _Json = null;
        public string Json { get { if (string.IsNullOrEmpty(_Json)) { _Json = LoadJson(); } return _Json; } private set { _Json = value; } }
        public void ClearJson() { _Json = null; }
        private string LoadJson()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("\"ALIACES\":[");
            if (ALIACES != null && ALIACES.Any())
            {
                sb.Append(ALIACES.Select(x => "\"" + x + "\"").Aggregate((a, b) => a + "," + b));
            }
            sb.Append("]");
            return sb.ToString();
        }
        #endregion

        #region MERGE
        internal void Merge(WSAllocable obj)
        {
            MergeAliaces(obj.ALIACES);
        }
        private void MergeAliaces(IEnumerable<string> _ALIACES)
        {
            try
            {
                if (_ALIACES != null && _ALIACES.Any())
                {
                    _ALIACES = _ALIACES.Select(x => x.ToLower());
                    ALIACES.AddRange(_ALIACES);
                    ALIACES = ALIACES.Distinct().ToList();
                }
            }
            catch (Exception) { }
        }
        #endregion
    }
}