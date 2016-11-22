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
    public abstract class WSParamValidatable : WSAllocable
    {
        public abstract bool isValid { get; }

        #region XML
        public new System.Xml.Schema.XmlSchema GetSchema(){ return null; }

        #region READ XML
        public new void ReadXml(System.Xml.XmlReader reader)
        {
            ReadXmlAttributes(reader);
            ReadXmlContent(reader);
        }
        public new void ReadXmlAttributes(System.Xml.XmlReader reader){ base.ReadXmlAttributes(reader); }
        public new void ReadXmlContent(System.Xml.XmlReader reader){ base.ReadXmlContent(reader); }
        #endregion

        #region WRITE XML
        public new void WriteXml(System.Xml.XmlWriter writer)
        {
            WriteXmlAttributes(writer);
            WriteXmlContent(writer);
        }
        public new void WriteXmlAttributes(System.Xml.XmlWriter writer){ base.WriteXmlAttributes(writer); }
        public new void WriteXmlContent(System.Xml.XmlWriter writer, int tabIndex = 0){ base.WriteXmlContent(writer, tabIndex); }
        #endregion
        #endregion

        internal void Merge(WSParamValidatable obj) { base.Merge(obj); }
    }
}