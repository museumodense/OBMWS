using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Reflection;
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
    public class WSTableParam : WSParam
    {
        public WSTableParam(Type _WSEntityType, int _CODE, string _NAME, WSColumnRef _WSColumnRefName, Type _DataType, MetaFunctions _func)
            : base(_CODE, _NAME, _DataType, _func)
        {
            WSEntityType = _WSEntityType;
            WSColumnRef = _WSColumnRefName;
        }

        public WSColumnRef WSColumnRef { get; private set; }

        public string DEFAULT_VALUE { get; set; }

        public Type WSEntityType = null;

        public string _DBType = null;
        public bool DBTypeSet = false;
        public string DBType
        {
            get
            {
                if (!DBTypeSet && DataType != null && DataType.IsSimple())
                {
                    DBTypeSet = true;
                    PropertyInfo pInfo = WSEntityType.GetProperty(WSColumnRef.NAME);
                    if (pInfo != null)
                    {
                        ColumnAttribute cAttribute = pInfo.CustomAttribute<ColumnAttribute>(true);
                        if (cAttribute != null)
                        {
                            _DBType = cAttribute.DbType;
                        }
                    }
                }
                return _DBType;
            }
        }

        internal bool IsColumn;
        internal bool IsAssociation;

        public bool _IsPrimary = false;
        public bool IsPrimarySet = false;
        public bool IsPrimary
        {
            get
            {
                if (!IsPrimarySet)
                {
                    PropertyInfo prop = WSEntityType.GetProperty(WSColumnRef.NAME);
                    if (prop != null)
                    {
                        IEnumerable<ColumnAttribute> cAttributes = prop.GetCustomAttributes(typeof(ColumnAttribute), true).OfType<ColumnAttribute>();
                        if (cAttributes.Any())
                        {
                            _IsPrimary = cAttributes.FirstOrDefault().IsPrimaryKey;
                        }
                    }
                    IsPrimarySet = true;
                }
                return _IsPrimary;
            }
        }
        public bool IsRequired { get { return !string.IsNullOrEmpty(DBType) && DBType.Contains(" NOT NULL"); } }
        public bool IsEditable(bool isOwner, int ACCESS_LEVEL) {
            return 
                isAccessible(isOwner, ACCESS_LEVEL) && !IsPrimary && WRITE_ACCESS_MODE != null &&
                (
                    (isOwner && WRITE_ACCESS_MODE.OWNER_ACCESS_ALLOWED)
                    ||
                    (WRITE_ACCESS_MODE.ACCESS_LEVEL != WSConstants.ACCESS_LEVEL.LOCK && ACCESS_LEVEL >= WRITE_ACCESS_MODE.ACCESS_LEVEL)
                );
        }
        public bool IsSortable { get { return IsComparable; } }

        private bool _IsComparable = false;
        private bool IsComparableSet = false;
        public bool IsComparable { get { if (!IsComparableSet) { IsComparableSet = true; _IsComparable = DataType.IsSimple() && !string.IsNullOrEmpty(DBType) && !WSConstants.LONG_TEXT_DBTYPES.Any(x => DBType.StartsWith(x)); } return _IsComparable; } }

        public override bool isValid { get { return base.isValid && WSColumnRef != null && !string.IsNullOrEmpty(WSColumnRef.NAME); } }

        public new void ReadXml(XmlReader reader)
        {
            ReadXmlAttributes(reader);
            ReadXmlContent(reader);
        }
        public new void ReadXmlAttributes(System.Xml.XmlReader reader)
        {
            base.ReadXmlAttributes(reader);

            #region DEFAULT_VALUE
            string _DEFAULT_VALUE = reader["defaultValue"];
            if (!string.IsNullOrEmpty(_DEFAULT_VALUE))
            {
                DEFAULT_VALUE = _DEFAULT_VALUE;
            }
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
            
            writer.WriteAttributeString("defaultValue", DEFAULT_VALUE);
        }
        public new void WriteXmlContent(System.Xml.XmlWriter writer, int tabIndex = 0)
        {
            base.WriteXmlContent(writer, tabIndex);
        }

        internal void Merge(WSTableParam newPrimParam)
        {
            base.Merge(newPrimParam);
        }

        internal new WSTableParam Clone()
        {
            WSTableParam param = new WSTableParam(WSEntityType, CODE, NAME, WSColumnRef, DataType, func);
            param.DISPLAY_NAME = DISPLAY_NAME;
            param.DEFAULT_VALUE = DEFAULT_VALUE;
            param.IsColumn = IsColumn;
            param.IsAssociation = IsAssociation;
            param.READ_ACCESS_MODE = READ_ACCESS_MODE != null ? READ_ACCESS_MODE.Clone() : new WSAccessMode(WSConstants.ACCESS_LEVEL.READ);
            param.WRITE_ACCESS_MODE = WRITE_ACCESS_MODE != null ? WRITE_ACCESS_MODE.Clone() : new WSAccessMode(WSConstants.ACCESS_LEVEL.UPDATE);
            param.SkipEmpty = SkipEmpty;
            param.ALLOWED_VALUES = (ALLOWED_VALUES != null && ALLOWED_VALUES.Any()) ? ALLOWED_VALUES.Select(x => x.Clone()).ToList() : new List<WSValue>();
            param.ALIACES = (ALIACES != null && ALIACES.Any()) ? ALIACES.Select(x => x).ToList() : new List<string>();
            param.DESCRIPTION = DESCRIPTION;
            return param;
        }


        internal bool Match(WSTableParam param, IEnumerable<WSTableSource> role_sources = null, Func<Type, WSTableSource> getTSource = null)
        {
            try
            {
                return param != null && WSEntityType != null && WSEntityType == param.WSEntityType && base.Match(param.NAME, role_sources, getTSource);
            }
            catch (Exception) { }
            return false;
        }

        public override bool Match(string key, IEnumerable<WSTableSource> sources = null, Func<Type, WSTableSource> getTSource = null, bool TypeMatchAllowed = true)
        {
            return TypeMatchAllowed ? base.Match(key, sources, getTSource) : WSColumnRef.NAME.ToLower().Equals(key.ToLower());
        }

        private string _Json = null;
        public new string Json { get { if (string.IsNullOrEmpty(_Json)) { _Json = LoadJson(); } return _Json; } }
        private string LoadJson()
        {
            StringBuilder sb = new StringBuilder();
            
            sb.Append("\"IsPrimary\":" + IsPrimary.ToString().ToLower() + "");
            sb.Append(",\"IsAssociation\":" + IsAssociation.ToString().ToLower() + "");
            Type eType = DataType.GetEntityType();
            sb.Append(",\"Association\":{" + ((IsAssociation && eType != null) ? (
                "\"FullName\":\"" + eType.FullName + "\"" +
                ",\"Type\":\"" + eType.Name + "\"" +
                ",\"Namespace\":\"" + eType.Namespace + "\""
            ) : string.Empty) + "}");
            sb.Append(",\"IsRequired\":" + IsRequired.ToString().ToLower() + "");
            sb.Append(",\"IsSortable\":" + IsSortable.ToString().ToLower() + "");

            sb.Append("," + base.Json);

            return sb.ToString();
        }
    }
}