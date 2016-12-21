using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

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
    public class WSSource : WSValue
    {
        internal WSStatus log = WSStatus.NONE_Copy();
        public Type ReturnType = null;
        public string SecurityZone { get; } = WSConstants.CONFIG.DefaultDB;
        public bool PrintStatus = false;
        public bool ShowMessageInaccessible = false;
        internal byte UserRole = WSConstants.USER_ROLE.READER;
        internal bool LoadAllowed = false;
        public WSSource ME { get; private set; }
        internal bool IsBase { get; set; }
        protected MetaFunctions CFunc = null;

        private WSSource() { }

        private WSSource(Type _ReturnType, MetaFunctions _CFunc, string _Name, byte _UserRole = WSConstants.ACCESS_LEVEL.READ, bool _IsBase = true, byte _AccessLevel = WSConstants.ACCESS_LEVEL.ADMIN)
            : base(string.IsNullOrEmpty(_Name) ? _ReturnType == null ? string.Empty : _ReturnType.Name : _Name) 
        { 
            ReturnType = _ReturnType;
            CFunc = _CFunc;
            UserRole = _UserRole;
            ME = this;
            IsBase = _IsBase;
            AccessLevel_ = WSConstants.ACCESS_LEVEL.LEVELS.Any(x => x == _AccessLevel) ? _AccessLevel : WSConstants.ACCESS_LEVEL.ADMIN;
        }

        public WSSource(Type _ReturnType, MetaFunctions _CFunc, List<string> _allowed_values, string _Name, byte _UserRole, bool _IsBase = true, byte _AccessLevel = WSConstants.ACCESS_LEVEL.ADMIN)
            : base(string.IsNullOrEmpty(_Name) ? _ReturnType == null ? string.Empty : _ReturnType.Name : _Name, _allowed_values)
        {
            UserRole = _UserRole;
            CFunc = _CFunc;
            ME = this;
            IsBase = _IsBase;
            ReturnType = _ReturnType;
            AccessLevel_ = WSConstants.ACCESS_LEVEL.LEVELS.Any(x => x == _AccessLevel) ? _AccessLevel : WSConstants.ACCESS_LEVEL.ADMIN;
        }
        
        public WSParam GetXParam(string xName, Type DefaultFieldType = null, Func<Type, WSTableSource> getTSource = null)
        {
            WSParam param = null;
            try
            {
                if (!string.IsNullOrEmpty(xName))
                {
                    xName = xName.ToLower();
                    param = Params.FirstOrDefault(p => p is WSTableParam ? ((WSTableParam)p).Match(xName, null, getTSource) : p.Match(xName, null, getTSource));
                    if (param == null)
                    {
                        if (DefaultFieldType != null)
                        {
                            param = new WSTableParam(ReturnType, 0, xName.ToLower(), new WSColumnRef(xName), DefaultFieldType, CFunc);
                        }
                    }
                }
            }
            catch (Exception e) { WSStatus status = WSStatus.NONE.clone(); CFunc.RegError(GetType(), e, ref status); }
            return param;
        }

        private IEnumerable<WSParam> _Params = null;
        public IEnumerable<WSParam> Params
        {
            get { if (_Params == null) _Params = new List<WSParam>(); return _Params; }
            set { _Params = value; }
        }
        internal void AddParam(WSParam _param) { List<WSParam> _Params = Params.ToList(); _Params.Add(_param); Params = _Params; }
        internal void AddParams(IEnumerable<WSParam> _params) { List<WSParam> _Params = Params.ToList(); _Params.AddRange(_params); Params = _Params; }

        private byte AccessLevel_ = WSConstants.ACCESS_LEVEL.ADMIN;
        public byte AccessLevel
        {
            get { 
                if (!WSConstants.ACCESS_LEVEL.LEVELS.Any(x => x == AccessLevel_)) {
                    AccessLevel_ = WSConstants.ACCESS_LEVEL.ADMIN;
                } return AccessLevel_; 
            }
        }

        private byte _DefaultSchemaDeepness = WSConstants.DEFAULT_DEEPNESS;
        public byte DefaultSchemaDeepness
        {
            get { return _DefaultSchemaDeepness; }
            set {
                _DefaultSchemaDeepness = value;
            }
        }

        public bool IsInitiated { get { return ReturnType != null; } }

        public bool Match(WSSource match)
        {
            return match != null && Match(match.ReturnType);
        }
        public bool Match(Type _ReturnType)
        {
            return _ReturnType != null && ReturnType == _ReturnType;
        }

        public new bool isValid { get { return base.isValid; } }

        #region READ XML

        public new void ReadXml(System.Xml.XmlReader reader)
        {
            ReadXmlAttributes(reader);
            ReadXmlContent(reader);
        }
        public new void ReadXmlAttributes(System.Xml.XmlReader reader)
        {
            base.ReadXmlAttributes(reader);

            #region AccessLevel
            string _AccessLevel = reader["accessLevel"];
            dynamic _SchemaAcsLevel = WSConstants.ACCESS_LEVEL.ADMIN;
            if (!string.IsNullOrEmpty(_AccessLevel) && typeof(byte).Read(_AccessLevel, out _SchemaAcsLevel) && WSConstants.ACCESS_LEVEL.LEVELS.Any(x => x == _SchemaAcsLevel))
            {
                AccessLevel_ = _SchemaAcsLevel;
            }
            #endregion

            #region DefaultSchemaDeepness
            string _DefaultSchemaDeepness = reader["defaultSchemaDeepness"];
            dynamic _DfltSchemaDeepness = WSConstants.DEFAULT_DEEPNESS;
            if (!string.IsNullOrEmpty(_DefaultSchemaDeepness) && typeof(byte).Read(_DefaultSchemaDeepness, out _DfltSchemaDeepness)) { DefaultSchemaDeepness = _DfltSchemaDeepness; }
            #endregion

            #region PrintStatus
            string _PrintStatus = reader["printStatus"];
            dynamic _pStatus = false;
            if (!string.IsNullOrEmpty(_PrintStatus) && typeof(bool).Read(_PrintStatus, out _pStatus)) { PrintStatus = _pStatus; }
            #endregion

            #region ShowMessageInaccessible
            string _ShowMessageInaccessible = reader["showMessageInaccessible"];
            dynamic _ShMsgInaccessible = false;
            if (!string.IsNullOrEmpty(_ShowMessageInaccessible) && typeof(bool).Read(_ShowMessageInaccessible, out _ShMsgInaccessible)) { ShowMessageInaccessible = _ShMsgInaccessible; }
            #endregion

        }
        public void ReadXmlContent(System.Xml.XmlReader reader, Func<Type, WSTableSource> getTSource)
        {
            base.ReadXmlContent(reader);

            bool done = false;
            while (reader.MoveToContent() == XmlNodeType.Element)
            {
                switch (reader.Name)
                {
                    case "params":
                        {
                            if (reader.ReadToDescendant("param"))
                            {
                                while (reader.MoveToContent() == XmlNodeType.Element)
                                {
                                    string pName = reader.GetAttribute("name");
                                    WSParam param = GetXParam(pName, null, getTSource);
                                    if (param == null)
                                    {
                                        reader.Skip();
                                        continue;
                                    }
                                    else
                                    {
                                        if (param is WSTableParam) { ((WSTableParam)param).ReadXml(reader); }
                                        else { param.ReadXml(reader); }
                                    }

                                    reader.MoveToContent();
                                    if (!reader.Read()) break;
                                }
                            }
                            break;
                        }
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

        public new void WriteXml(System.Xml.XmlWriter writer)
        {
            WriteXmlAttributes(writer);
            WriteXmlContent(writer);
        }
        public new void WriteXmlAttributes(System.Xml.XmlWriter writer)
        {
            base.WriteXmlAttributes(writer);

            writer.WriteAttributeString("returnType", ReturnType.FullName);
            writer.WriteAttributeString("sourceType", "" + GetType());
            writer.WriteAttributeString("accessLevel", "" + AccessLevel);
            writer.WriteAttributeString("defaultSchemaDeepness", "" + DefaultSchemaDeepness);
            writer.WriteAttributeString("printStatus", PrintStatus ? "1" : "0");
            writer.WriteAttributeString("showMessageInaccessible", ShowMessageInaccessible ? "1" : "0");
        }
        public new void WriteXmlContent(System.Xml.XmlWriter writer, int tabIndex = 0)
        {
            base.WriteXmlContent(writer, tabIndex);
            
            writer.WriteStartElement("params");
            foreach (WSParam param in Params)
            {
                writer.WriteStartElement("param");
                if (param is WSTableParam) { ((WSTableParam)param).WriteXml(writer); }
                else { param.WriteXml(writer); }
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }
        #endregion

        public void Merge(WSSource ext)
        {
            base.Merge(ext);

            PrintStatus = ext.PrintStatus;
            ShowMessageInaccessible = ext.ShowMessageInaccessible;
            DefaultSchemaDeepness = ext.DefaultSchemaDeepness;
            if (WSConstants.ACCESS_LEVEL.LEVELS.Any(x => x == ext.AccessLevel)) { AccessLevel_ = ext.AccessLevel; }

            foreach (WSParam orgParam in Params.OfType<WSParam>())
            {
                WSParam extParam = ext.Params.FirstOrDefault(p => p.Match(orgParam.NAME));
                if (extParam != null) orgParam.Merge(extParam);
            }
        }
        internal WSSource Clone(MetaFunctions _CFunc, byte? _UserRole = null, bool _IsBase = false)
        {
            WSSource src = new WSSource(ReturnType, _CFunc == null ? CFunc : _CFunc, ALIACES, NAME, _UserRole == null ? UserRole : (byte)_UserRole, _IsBase, AccessLevel)
            {
                PrintStatus = PrintStatus,
                ShowMessageInaccessible = ShowMessageInaccessible,
                DefaultSchemaDeepness = DefaultSchemaDeepness,
                Params = Params.Where(x => x != null).Select(x => x is WSTableParam ? ((WSTableParam)x).Clone() : x.Clone())
            };
            return src;
        }

        private string _Json = null;
        public new string Json { get { if (string.IsNullOrEmpty(_Json)) { _Json = LoadJson(); } return _Json; } private set { _Json = value; } }
        public new void ClearJson() { _Json = null; base.ClearJson(); }
        private string LoadJson()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("\"Name\":\"" + ReturnType.Name + "\"");
            sb.Append(",\"ReturnType\":\"" + ReturnType.FullName + "\"");
            sb.Append(",\"SecurityZone\":\"" + SecurityZone + "\"");

            sb.Append("," + base.Json);

            sb.Append(",\"Params\":[");
            Func<WSParam, bool> isAccessibleParam = x => x.READ_ACCESS_MODE.ACCESS_LEVEL <= UserRole;
            Func<WSParam, string> jFunc = (x => "{" + (x is WSTableParam ? ((WSTableParam)x).Json : x.Json) + "}");
            if (Params != null && Params.Any(isAccessibleParam))
            {
                List<string> paramList = new List<string>();
                foreach (WSParam param in Params.Where(isAccessibleParam))
                {
                    if (param is WSTableParam) {
                        WSTableParam tParam = (WSTableParam)param;
                        if (tParam.IsAssociation)
                        {
                            WSSource src = CFunc.GetSourceByType(tParam.DataType.GetEntityType());
                            if (src != null && src.AccessLevel <= UserRole)
                            {
                                paramList.Add("{" + tParam.Json + "}");
                            }
                        }
                        else { paramList.Add("{" + tParam.Json + "}"); }
                    }
                    else { paramList.Add("{" + param.Json + "}"); }
                    
                }
                sb.Append(paramList.Aggregate((a, b) => a + "," + b));
            }
            sb.Append("]");

            return sb.ToString();
        }

        #region INTERNAL FUNCTIONS
        internal void ClearDublicatedAliaces()
        {
            List<string> uniqueAliaces = new List<string>();
            foreach (WSParam param in Params)
            {
                IEnumerable<string> unique = param.ALIACES.Where(x => !uniqueAliaces.Any(a => a.Equals(x)));
                unique = unique != null && unique.Any() ? unique : new List<string>();
                param.ALIACES = unique.ToList();//TODO@ANDVO : avoid using '.ToList()' to speed up ececution 
                uniqueAliaces.AddRange(unique);
            }
        }
        #endregion

        #region OVERRITED
        public override bool Equals(object obj) { return obj != null && obj.GetType() == GetType() && ToString().Equals(obj.ToString()); }
        public override int GetHashCode() { return string.IsNullOrEmpty(ToString()) ? base.ToString().GetHashCode() : ToString().GetHashCode(); }
        public override string ToString() { return NAME + " [" + AccessLevel + ":" + UserRole + ":" + IsBase + ":"+SecurityZone+"]"; }
        #endregion
    }
}