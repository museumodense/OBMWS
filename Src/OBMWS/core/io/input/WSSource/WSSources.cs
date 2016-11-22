using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml.Serialization;
using System.Xml;
using System.Text;
using OBMWS.security;

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
    [XmlRoot("sources")]
    public class WSSources<T> : List<T>, IXmlSerializable where T : WSSource
    {
        public WSSources() { }
        public WSSources(IEnumerable<T> items) {
            foreach (T item in items)
            {
                try
                {
                    Add(item);
                }
                catch (Exception) { }
            }
        }

        public System.Xml.Schema.XmlSchema GetSchema() { return null; }
        public void ReadXml(System.Xml.XmlReader reader)
        {
            //try
            //{
            //    if (reader.MoveToContent() == XmlNodeType.Element && reader.LocalName == "Sources" && reader.ReadToDescendant("Source"))
            //    {
            //        IEnumerable<WSTableSource> orgSources = WSConstants.ORG_SOURCES.OfType<WSTableSource>();
            //        while (reader.MoveToContent() == XmlNodeType.Element && reader.LocalName == "Source")
            //        {
            //            WSSource source = null;

            //            string TName = reader.GetAttribute("NAME");
            //            Type SourceType = Type.GetType(reader.GetAttribute("SourceType"));

            //            if (SourceType != null && SourceType == typeof(WSTableSource))
            //            {
            //                string DBName = reader.GetAttribute("DBName");
            //                source = orgSources.FirstOrDefault(s => s.Match(TName, DBName));
            //                if (source != null) { 
            //                    ((WSTableSource)source).ReadXml(reader);
            //                }
            //            }
            //            if (!this.Any(x => x.Match(source))) { this.Add(source); }

            //            reader.MoveToContent();
            //            if (!reader.Read()) break;
            //        }
            //    }
            //}
            //catch (Exception) { }
        }
        public void WriteXml(System.Xml.XmlWriter writer)
        {
            foreach (T src in this)
            {
                writer.WriteStartElement("source");
                
                if (src is WSTableSource) { (src as WSTableSource).WriteXml(writer); }
                else { src.WriteXml(writer); }

                writer.WriteEndElement();
            }
        }

        public WSSources<T> Clone(ref WSUserDBSet DBSet, MetaFunctions Func)
        {
            WSSources<T> srcs = new WSSources<T>();
            try
            {
                foreach (T src in this)
                {
                    if (src is WSTableSource)
                    {
                        WSSource tSrc = DBSet == null ? (src as WSTableSource).Clone(Func) : (src as WSTableSource).Clone(Func, DBSet.DBSession.user.role);

                        srcs.Add((T)tSrc);
                    }
                    else
                    {
                        srcs.Add((src as WSSource).Clone(Func, DBSet.DBSession.user.role, false) as T);
                    }
                }
            }
            catch (Exception) { }
            return srcs;
        }
        internal WSSources<T> Merge(List<T> extenders)
        {
            WSSources<T> srcs = new WSSources<T>();
            foreach (T src in this.OfType<T>())
            {
                try
                {
                    T extender = extenders.Single(x => x.Match(src));
                    if (extender != null)
                    {
                        if (src is WSTableSource) { (src as WSTableSource).Merge(extender); } 
                        else { src.Merge(extender); }
                        srcs.Add(src);
                    }
                }
                catch (Exception) { }
            }
            return srcs;
        }
        internal bool Configure(FileInfo file, out bool ForceReload)
        {
            ForceReload = false;
            bool configured = false;
            try
            {
                if (file != null && file.Exists)
                {
                    using (XmlReader reader = XmlReader.Create(file.FullName))
                    {
                        XmlNodeType cType = reader.MoveToContent();
                        if (cType != XmlNodeType.Element && reader.LocalName.Equals("sources")) { }
                        else if (!reader.ReadToDescendant("source")) { }
                        else
                        {
                            List<string> foundSources = new List<string>();
                            do {
                                if (reader.GetAttribute("sourceType").Equals(typeof(WSTableSource).FullName))
                                {
                                    WSTableSource src = GetSource<WSTableSource>(reader);
                                    if (src != null)
                                    {
                                        src.ReadXml(reader, getTSource);
                                        foundSources.Add(src.NAME);
                                    }
                                    else {
                                        reader.MoveToElement();
                                        ForceReload = true;
                                    }
                                }
                            } while (reader.ReadToNextSibling("source"));

                            ForceReload = ForceReload || this.Any(x => !foundSources.Contains(x.NAME));
                            
                            configured = reader.NodeType == XmlNodeType.EndElement && reader.LocalName.Equals("sources");
                        }
                    }
                }
            }
            catch (Exception) { configured = false; }
            return configured;
        }
        
        private A GetSource<A>(XmlReader reader) where A : WSSource { return this.OfType<A>().FirstOrDefault(x => x.ReturnType.FullName.Equals(reader.GetAttribute("returnType"))); }
        private WSTableSource getTSource(Type type) { return this.OfType<WSTableSource>().FirstOrDefault(x => x.ReturnType==type); }

        private WSSourceSet _SourceSet = null;
        public WSSourceSet SourceSet
        {
            get
            {
                if (_SourceSet == null)
                {
                    _SourceSet = new WSSourceSet(this as WSSources<WSTableSource>);
                }
                return _SourceSet;
            }
        }

        private string _Json = null;

        public string Json { get { if (string.IsNullOrEmpty(_Json)) { _Json = LoadJson(); } return _Json; } private set { _Json = value; } }
        private string LoadJson()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("{\"Sources\":[");
            if (this.Any())
            {
                sb.Append(this.OrderBy(x => x.NAME).Select(x => "{" + (x.Json) + "}").Aggregate((a, b) => a + "," + b));
            }
            else { sb.Append("NULL"); }
            sb.Append("]}");

            return sb.ToString();
        }
        
        public bool IsValid { get { if (_IsValid == null) { _IsValid = !this.Any(x => x == null || !x.isValid); } return _IsValid != null && (bool)_IsValid; } }
        private bool? _IsValid = null;

        public bool IsReady { get { if (_IsReady == null) { _IsReady = IsValid && ((typeof(T) == typeof(WSTableSource)) ? !this.Any(x => !(x as WSTableSource).IsReady) : true); } return _IsReady != null && (bool)_IsReady; } }
        private bool? _IsReady = null;
        
        internal bool Load(MetaFunctions Func)
        {
            if (typeof(T) == typeof(WSTableSource))
            {
                foreach (T item in this)
                {
                    if (!(item as WSTableSource).Load(Func, this.Select(x=>x as WSTableSource))) return false;
                }
            }
            return true;
        }
        
        public override string ToString() { return string.Format("{{{0}:{1}}}", this.Count, typeof(T).Name); }
    }
}