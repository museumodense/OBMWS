using System;
using System.Collections.Generic;
using System.Data.Linq;
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
    public class WSTableSource : WSSource
    {
        public WSTableSource(Type _ReturnType, string _SecurityZone, string _Name, MetaFunctions _CFunc, byte _UserRole = WSConstants.ACCESS_LEVEL.READ, bool _IsBase = true, byte _AccessLevel = WSConstants.ACCESS_LEVEL.ADMIN, WSFormat[] _AvailableFormats = null, List<string> _AllowedValues = null)
            : base(_ReturnType, _CFunc, _AllowedValues == null ? new List<string>() : _AllowedValues, _Name, _UserRole, _IsBase, _AccessLevel)
        {
            SecurityZone = _SecurityZone;
            if (_AvailableFormats != null) { AVAILABLE_FORMATS = _AvailableFormats; }
        }

        public bool AllowOwnerAccess = true;
        
        public WSJson BaseFilter = null;

        public new string SecurityZone = string.Empty;

        public string DBName {
            get
            {
                if (string.IsNullOrEmpty(_DBName) && ReturnType!=null)
                {
                    try
                    {
                        using (WSDataContext db = WSServerMeta.GetServerContext(ReturnType, null, $"{GetType().Name}.DBName"))
                        {
                            Type dbType = db.GetType();
                            if (dbType != null)
                            {
                                System.Data.Linq.Mapping.DatabaseAttribute dbNameAttr = dbType.CustomAttribute<System.Data.Linq.Mapping.DatabaseAttribute>(true);
                                _DBName = dbNameAttr == null ? null : dbNameAttr.Name;
                            }
                        }
                    }
                    catch (Exception e) { WSStatus status = WSStatusBase.NONE.clone(); CFunc.RegError(GetType(), e, ref status); }
                }
                return _DBName;
            }
        }
        private string _DBName = null;
        
        public string dbTableName
        {
            get
            {
                if (_dbTableName == null && IsInitiated){
                    try { _dbTableName = ReturnType.CustomAttribute<System.Data.Linq.Mapping.TableAttribute>(true).Name; } catch (Exception e) { WSStatus status = WSStatusBase.NONE.clone(); CFunc.RegError(GetType(), e, ref status); }              
                }
                return _dbTableName;
            }
        }
        private string _dbTableName = null;

        public IEnumerable<WSTableParam> PrimParams
        {
            get
            {
                if (!PrimParamsRead && _PrimParams == null && Params != null)
                {
                    try
                    {
                        PrimParamsRead = true;
                        IEnumerable<WSTableParam> _TParams = Params.OfType<WSTableParam>();
                        _PrimParams = _TParams == null ? new List<WSTableParam>() : _TParams.Where(p => p.IsPrimary);
                    }
                    catch (Exception e) { WSStatus status = WSStatusBase.NONE.clone(); CFunc.RegError(GetType(), e, ref status); }
                    _PrimParams = _PrimParams == null ? new List<WSTableParam>() : _PrimParams;
                }
                return _PrimParams;
            }
            private set { _PrimParams = value != null && !value.Any(x=>!x.isValid) ? value : _PrimParams; }
        }
        private IEnumerable<WSTableParam> _PrimParams = null;
        private bool PrimParamsRead = false;

        public IEnumerable<WSTableParam> DBParams {
            get
            {
                if (_DBParams == null) { _DBParams = Params != null ? Params.OfType<WSTableParam>() : new List<WSTableParam>(); }
                return _DBParams;
            }
        }
        private IEnumerable<WSTableParam> _DBParams = null;

        public IEnumerable<WSTableParam> DBPrimitiveParams {
            get
            {
                if (_DBPrimitiveParams == null)
                {
                    _DBPrimitiveParams = DBParams != null ? DBParams.Where(p => (DBAssociationParams == null || !DBAssociationParams.Any(p1 => p1.Match(p.NAME))) && (DBChildParams == null || !DBChildParams.Any(p1 => p1.Match(p.NAME)))) : new List<WSTableParam>();
                }
                return _DBPrimitiveParams;
            }
        }
        private IEnumerable<WSTableParam> _DBPrimitiveParams = null;

        public IEnumerable<WSTableParam> DBAssociationParams {
            get
            {
                if (_DBAssociationParams == null)
                {
                    try
                    {
                        _DBAssociationParams = DBParams.Where(x => x.DataType.GetEntityType().IsValidDynamicEntity());
                    }
                    catch (Exception e) { WSStatus status = WSStatusBase.NONE.clone(); CFunc.RegError(GetType(), e, ref status); }
                }
                return _DBAssociationParams;
            }
        }
        private IEnumerable<WSTableParam> _DBAssociationParams = null;

        public IEnumerable<WSTableParam> DBChildParams {
            get
            {
                if (_DBChildParams == null)
                {
                    try
                    {
                        _DBChildParams = DBParams.Where(x => x.DataType.IsValidDynamicEntity());
                    }
                    catch (Exception e) { WSStatus status = WSStatusBase.NONE.clone(); CFunc.RegError(GetType(), e, ref status); }
                }
                return _DBChildParams;
            }
        }
        private IEnumerable<WSTableParam> _DBChildParams = null;

        public WSFormat[] AVAILABLE_FORMATS { 
            get { return _AVAILABLE_FORMATS; } 
            private set { if (value != null && value.Any()) { _AVAILABLE_FORMATS = value; } } 
        }
        private WSFormat[] _AVAILABLE_FORMATS = WSConstants.FORMAT.FORMATS.Where(f=>f.isStandard).ToArray();

        #region HasRecords
        private bool? _HasRecords = null;
        public bool HasRecords
        {
            get
            {
                if (_HasRecords==null)
                {
                    _HasRecords = FirstEntity!=null && FirstEntity.Any();
                }
                return _HasRecords == null ? true : (bool)_HasRecords ;
            }
        }
        #endregion
        
        #region IsReadable
        private bool? _IsReadableBySchema = null;
        private bool IsReadableBySchema { get { return _IsReadableBySchema == null ? IsReadableByRole : (bool)_IsReadableBySchema; } }

        private bool _IsReadableByRole = false;
        private bool IsReadableByRoleRead = false;
        private bool IsReadableByRole { get { if (!IsReadableByRoleRead) { _IsReadableByRole = isValid && UserRole >= WSConstants.ACCESS_LEVEL.READ; IsReadableByRoleRead = true; } return _IsReadableByRole; } }

        private bool _IsReadable = false;
        private bool IsReadableRead = false;
        public bool IsReadable { get { if (!IsReadableRead) { _IsReadable = IsReadableBySchema /*&&*/|| IsReadableByRole; IsReadableRead = true; } return _IsReadable; } }
        #endregion

        #region IsCreatable
        private bool? _IsCreatableBySchema = null;
        private bool IsCreatableBySchema { get { return _IsCreatableBySchema == null ? IsCreatableByRole : (bool)_IsCreatableBySchema; } }

        private bool _IsCreatableByRole = false;
        private bool IsCreatableByRoleRead = false;
        private bool IsCreatableByRole { get { if (!IsCreatableByRoleRead) { _IsCreatableByRole = IsReadableByRole && UserRole >= WSConstants.ACCESS_LEVEL.INSERT; IsCreatableByRoleRead = true; } return _IsCreatableByRole; } }

        private bool _IsCreatable = false;
        private bool IsCreatableRead = false;
        public bool IsCreatable { get { if (!IsCreatableRead) { _IsCreatable = IsCreatableBySchema /*&&*/|| IsCreatableByRole; IsCreatableRead = true; } return _IsCreatable;} }
        #endregion

        #region IsEditable
        private bool? _IsEditableBySchema = null;
        private bool IsEditableBySchema { get { return _IsEditableBySchema == null ? IsEditableByRole : (bool)_IsEditableBySchema; } }

        private bool _IsEditableByRole = false;
        private bool IsEditableByRoleRead = false;
        private bool IsEditableByRole { get { if (!IsEditableByRoleRead) { _IsEditableByRole = IsCreatableByRole && UserRole >= WSConstants.ACCESS_LEVEL.UPDATE && HasPrimary && HasRecords; IsEditableByRoleRead = true; } return _IsEditableByRole; } }

        private bool _IsEditable = false;
        private bool IsEditableRead = false;
        public bool IsEditable { get { if (!IsEditableRead) { _IsEditable = IsEditableBySchema /*&&*/|| IsEditableByRole; IsEditableRead = true; } return _IsEditable; } }
        #endregion

        #region IsDeletable
        private bool? _IsDeletableBySchema = null;
        private bool IsDeletableBySchema { get { return _IsDeletableBySchema == null ? IsDeletableByRole : (bool)_IsDeletableBySchema; } }

        private bool _IsDeletableByRole = false;
        private bool IsDeletableByRoleRead = false;
        private bool IsDeletableByRole { get { if (!IsDeletableByRoleRead) { _IsDeletableByRole = IsEditableByRole && UserRole >= WSConstants.ACCESS_LEVEL.DELETE; IsDeletableByRoleRead = true; } return _IsDeletableByRole; } }

        private bool _IsDeletable = false;
        private bool IsDeletableRead = false;
        public bool IsDeletable { get { if (!IsDeletableRead) { _IsDeletable = IsDeletableBySchema /*&&*/|| IsDeletableByRole; IsDeletableRead = true; } return _IsDeletable; } }

        //private bool _IsSoftDeletable = false;
        //private bool IsSoftDeletableRead = false;
        //public bool IsSoftDeletable { get { if (!IsSoftDeletableRead) { _IsSoftDeletable = IsDeletableByRole && DeleteFlag != null; IsSoftDeletableRead = true; } return _IsSoftDeletable; } }

        private WSTableParam _DeleteFlag = null;
        private bool DeleteFlagRead = false;
        public WSTableParam DeleteFlag {
            get {
                try {
                    if (!DeleteFlagRead && _DeleteFlag == null && Params != null) {
                        DeleteFlagRead = true;
                        _DeleteFlag = Params.OfType<WSTableParam>().FirstOrDefault(p => p.DataType.IsSimple() && p.Match("deleted"));
                    }
                } catch (Exception e) { WSStatus status = WSStatusBase.NONE.clone(); CFunc.RegError(GetType(), e, ref status); }
                return _DeleteFlag;
            }
            private set { _DeleteFlag = value; }
        }
        public WSJson DeletableFilter { get; private set; }
        public WSJson EditableFilter { get; private set; }
        public WSJson CreatableFilter { get; private set; }
        public WSJson ReadableFilter { get; private set; }
        #endregion

        #region HasPrimary
        public bool IsCompositeID { get { return IsInitiated && PrimParams.Count() > 1;/* ReturnType.IsCompositeID();*/ } }

        public bool IsSinglePrimaryID { get { return IsInitiated && PrimParams.Count() == 1;/* !string.IsNullOrEmpty(ReturnType.getPrimaryProperty());*/ } }

        public bool HasPrimary { get { return IsSinglePrimaryID || IsCompositeID; } }

        #endregion
        
        #region FirstEntity
        //TODO@ANDVO-2016-08-30:FirstEntity not working on tables without primary keys !!! Make it possible to indicate/warn user about missing primary key/identifier
        public Dictionary<string, string> FirstEntity
        {
            get
            {
                if (_FirstEntity == null)
                {
                    _FirstEntity = new Dictionary<string, string>();
                    WSStatus status = WSStatus.NONE_Copy();
                    using (WSDataContext db = WSServerMeta.GetServerContext(ReturnType, null, $"{typeof(WSServerMeta).Name}.FirstEntity"))
                    {
                        try
                        {
                            status.AddNote($@"Database:{db.Connection.Database}");
                            status.AddNote($@"DataSource:{db.Connection.DataSource}");
                            status.AddNote($@"State:{(db.Connection.State.ToString())}");
                            status.AddNote($@"Site:{(db.Connection.Site == null ? string.Empty : db.Connection.Site.Name)}");
                            status.AddNote($@"ConnectionString:{db.Connection.ConnectionString}");
                            status.AddNote($@"ConnectionTimeout:{db.Connection.ConnectionTimeout}");
                            string sql = "select top 1 * from " + dbTableName;
                            status.AddNote($"sql:{sql}");
                            IEnumerable<OBMWS.WSDynamicEntity> entities = db.ExecuteQuery(ReturnType, sql, new object[] { }).OfType<WSDynamicEntity>();
                            WSDynamicEntity entity = entities.FirstOrDefault();
                            status.AddNote($"entity:[{(entity == null ? "NONE" : "exists")}]");
                            Dictionary<string, string> allParams = null;
                            _FirstEntity = (entity == null || !entity.TryReadRecordToDictionary(db, out allParams, ref log) || !allParams.Any()) ?
                                new Dictionary<string, string>() :
                                allParams.ToDictionary(x => x.Key, x => x.Value);
                        }
                        catch (Exception e)
                        {
                            status.CODE = WSStatus.ERROR.CODE;
                            CFunc.RegError(GetType(), e, ref status, $"{{{ReturnType}-FirstEntity read status:{status.ToString()}}}");
                        }
                    }
                }
                return _FirstEntity;
            }
        }
        private Dictionary<string, string> _FirstEntity = null;

        public Dictionary<string, string> FirstEntityKeys
        {
            get
            {
                if (_FirstEntityKeys == null)
                {
                    _FirstEntityKeys = new Dictionary<string, string>();
                    if (HasRecords) try {
                        _FirstEntityKeys = (PrimParams.Any() ? FirstEntity.Where(x => PrimParams.Any(p => p.Match(x.Key))) : FirstEntity.Where(x => DBParams.Any(p => p.DataType!=typeof(string) && p.Match(x.Key)))).ToDictionary(x => x.Key, x => x.Value);
                    }
                    catch (Exception e) { WSStatus status = WSStatusBase.NONE.clone(); CFunc.RegError(GetType(), e, ref status, "FirstEntityKeys failed to read"); }
                }
                return _FirstEntityKeys;
            }
        }
        private Dictionary<string, string> _FirstEntityKeys = null;
        #endregion

        #region READ XML
        public void ReadXml(XmlReader reader, Func<Type, WSTableSource> getTSource)
        {
            ReadXmlAttributes(reader);
            ReadXmlContent(reader, getTSource);
        }
        public new void ReadXmlAttributes(XmlReader reader)
        {
            base.ReadXmlAttributes(reader);

            #region DeleteFlag
            string _DeleteFlag = reader["deleteFlag"];
            if (!string.IsNullOrEmpty(_DeleteFlag) && GetXParam(_DeleteFlag) != null) { DeleteFlag = (WSTableParam)GetXParam(_DeleteFlag); }
            #endregion

            #region IsReadableBySchema
            string _AttrIsReadableBySchema = reader["readable"];
            if (!string.IsNullOrEmpty(_AttrIsReadableBySchema))
            {
                if (_AttrIsReadableBySchema.IsTrue() || _AttrIsReadableBySchema.IsFalse())
                {
                    _IsReadableBySchema = _AttrIsReadableBySchema.IsTrue();
                }
                else {
                    _IsReadableBySchema = true;
                    ReadableFilter = _AttrIsReadableBySchema.ToJson();
                }
            }
            #endregion

            #region IsCreatableBySchema
            string _AttrIsCreatableBySchema = reader["creatable"];
            if (!string.IsNullOrEmpty(_AttrIsCreatableBySchema))
            {
                if (_AttrIsCreatableBySchema.IsTrue() || _AttrIsCreatableBySchema.IsFalse())
                {
                    _IsCreatableBySchema = _AttrIsCreatableBySchema.IsTrue();
                }
                else {
                    _IsCreatableBySchema = true;
                    CreatableFilter = _AttrIsCreatableBySchema.ToJson();
                }
            }
            #endregion

            #region IsEditableBySchema
            string _AttrIsEditableBySchema = reader["editable"];
            if (!string.IsNullOrEmpty(_AttrIsEditableBySchema))
            {
                if (_AttrIsEditableBySchema.IsTrue() || _AttrIsEditableBySchema.IsFalse())
                {
                    _IsEditableBySchema = _AttrIsEditableBySchema.IsTrue();
                }
                else {
                    _IsEditableBySchema = true;
                    EditableFilter = _AttrIsEditableBySchema.ToJson();
                }
            }
            #endregion

            #region IsDeletableBySchema
            string _AttrIsDeletableBySchema = reader["deletable"];
            if (!string.IsNullOrEmpty(_AttrIsDeletableBySchema))
            {
                if (_AttrIsDeletableBySchema.IsTrue() || _AttrIsDeletableBySchema.IsFalse())
                {
                    _IsDeletableBySchema = _AttrIsDeletableBySchema.IsTrue();
                }
                else {
                    _IsDeletableBySchema = true;
                    DeletableFilter = _AttrIsDeletableBySchema.ToJson();
                }
            }
            #endregion

            #region AllowOwnerAccess
            string _AllowOwnerAccess = reader["allowOwnerAccess"];
            dynamic _dAllowOwnerAccess = AllowOwnerAccess;
            if (!string.IsNullOrEmpty(_AllowOwnerAccess) && typeof(bool).Read(_AllowOwnerAccess, out _dAllowOwnerAccess)) { AllowOwnerAccess = _dAllowOwnerAccess; }
            #endregion

            #region BaseFilter
            try
            {
                string _BaseFilterValue = reader["baseFilter"];
                if (!string.IsNullOrEmpty(_BaseFilterValue))
                {
                    BaseFilter = _BaseFilterValue.ToJson();
                }
            }
            catch (Exception e) { WSStatus status = WSStatusBase.NONE.clone(); CFunc.RegError(GetType(), e, ref status); }

            #endregion

        }
        public new void ReadXmlContent(XmlReader reader, Func<Type, WSTableSource> getTSource)
        {
            base.ReadXmlContent(reader, getTSource);

            bool done = false;
            while (reader.MoveToContent() == XmlNodeType.Element)
            {
                switch (reader.Name)
                {
                    case "availableFormats":
                        {
                            if (reader.ReadToDescendant("format"))
                            {
                                List<WSFormat> _AVAILABLE_FORMATS = new List<WSFormat>();
                                while (reader.MoveToContent() == XmlNodeType.Element && reader.LocalName == "format")
                                {
                                    string fName = reader.GetAttribute("name");
                                    int fCode = int.TryParse(reader.GetAttribute("code"), out fCode) ? fCode : 0;
                                    bool fIsStandard = bool.TryParse(reader.GetAttribute("isStandard"), out fIsStandard) ? fIsStandard : false;
                                    string fType = reader.GetAttribute("formatType");
                                    if (fType != null)
                                    {
                                        if (fType.Equals(typeof(WSMetaFormat).FullName)) { _AVAILABLE_FORMATS.Add(new WSMetaFormat(fCode, fName, fIsStandard)); }
                                        else if (fType.Equals(typeof(WSBinaryFormat).FullName)) { _AVAILABLE_FORMATS.Add(new WSBinaryFormat(fCode, fName, fIsStandard)); }
                                    }
                                    reader.MoveToContent();
                                    if (!reader.Read()) break;
                                }
                                AVAILABLE_FORMATS = _AVAILABLE_FORMATS.ToArray();
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
            
            writer.WriteAttributeString("deleteFlag", DeleteFlag == null ? "" : DeleteFlag.NAME);
            writer.WriteAttributeString("readable",  ReadableFilter != null ? ReadableFilter.NiceUrlString : _IsReadableBySchema == null ? "" : (IsReadableBySchema ? "1" : "0"));
            writer.WriteAttributeString("creatable", CreatableFilter != null ? CreatableFilter.NiceUrlString : _IsCreatableBySchema == null ? "" : (IsCreatableBySchema ? "1" : "0"));
            writer.WriteAttributeString("deletable", DeletableFilter != null ? DeletableFilter.NiceUrlString : _IsDeletableBySchema == null ? "" : (IsDeletableBySchema ? "1" : "0"));
            writer.WriteAttributeString("editable", EditableFilter != null ? EditableFilter.NiceUrlString : _IsEditableBySchema == null ? "" : (IsEditableBySchema ? "1" : "0")); 
            writer.WriteAttributeString("allowOwnerAccess", AllowOwnerAccess ? "1" : "0");
            writer.WriteAttributeString("baseFilter", BaseFilter == null ? "" : BaseFilter.NiceUrlString);
        }
        public new void WriteXmlContent(System.Xml.XmlWriter writer, int tabIndex = 0)
        {
            base.WriteXmlContent(writer, tabIndex);

            writer.WriteStartElement("availableFormats");
            foreach (WSFormat format in AVAILABLE_FORMATS)
            {
                writer.WriteStartElement("format");
                format.WriteXml(writer);
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }
        #endregion

        #region CONFIGURATION
        public void Merge(WSTableSource ext)
        {
            base.Merge(ext);

            AllowOwnerAccess = ext.AllowOwnerAccess;
            BaseFilter = ext.BaseFilter;
            DeletableFilter = ext.DeletableFilter;
            EditableFilter = ext.EditableFilter;
            CreatableFilter = ext.CreatableFilter;
            ReadableFilter = ext.ReadableFilter;

            foreach (WSTableParam orgParam in Params.OfType<WSTableParam>())
            {
                WSTableParam extParam = ext.DBParams.FirstOrDefault(p => p.WSColumnRef.NAME.Equals(orgParam.WSColumnRef.NAME));
                if (extParam != null) orgParam.Merge(extParam);
            }
        }
        
        internal WSTableSource Clone(MetaFunctions _Func, byte? _UserRole = null)
        {
            WSTableSource src = new WSTableSource(ReturnType, SecurityZone, NAME, _Func, _UserRole == null ? UserRole : (byte)_UserRole, false, AccessLevel, AVAILABLE_FORMATS, ALIACES);

            src.PrintStatus = PrintStatus;
            src.ShowMessageInaccessible = ShowMessageInaccessible;
            src.AllowOwnerAccess = AllowOwnerAccess;
            src.BaseFilter = BaseFilter == null ? null : BaseFilter.JString.ToJson();
            src.DeletableFilter = DeletableFilter == null ? null : DeletableFilter.JString.ToJson();
            src.EditableFilter = EditableFilter == null ? null : EditableFilter.JString.ToJson();
            src.CreatableFilter = CreatableFilter == null ? null : CreatableFilter.JString.ToJson();
            src.ReadableFilter = ReadableFilter == null ? null : ReadableFilter.JString.ToJson();
            src.DefaultSchemaDeepness = DefaultSchemaDeepness;
            src._IsReadableBySchema = _IsReadableBySchema;
            src._IsCreatableBySchema = _IsCreatableBySchema;
            src._IsEditableBySchema = _IsEditableBySchema;
            src._IsDeletableBySchema = _IsDeletableBySchema;
            src.Params = Params.Select(x => x is WSTableParam ? ((WSTableParam)x).Clone() : x.Clone());
            //PrimParams = PrimParams == null ? null : PrimParams.Select(c => c.Clone());
            src.DeleteFlag = DeleteFlag == null ? null : DeleteFlag.Clone();
            //Json = Json;
            //CoreSchema = CoreSchema;
            src./*_*/SecurityZone = SecurityZone;
            src.BaseSchema = BaseSchema==null?null:(WSEntitySchema)BaseSchema.Clone(src);
            //src._IsReady = null;
            return src;
        }
        
        public bool Load(MetaFunctions CFunc, IEnumerable<WSTableSource> sources)
        {
            BaseSchema = BaseSchema == null ? new WSEntitySchema(CFunc, this, sources) : BaseSchema;
            return BaseSchema.Load(sources);
        }

        private string _Json = null;
        public new string Json { get { if (string.IsNullOrEmpty(_Json)) { _Json = LoadJson(); } return _Json; } private set { _Json = value; } }
        private string LoadJson()
        {
            try
            {
                StringBuilder sb = new StringBuilder();

                System.Web.Script.Serialization.JavaScriptSerializer js = new System.Web.Script.Serialization.JavaScriptSerializer();
                
                sb.Append("\"DeleteFlag\":\"" + (DeleteFlag == null ? "" : DeleteFlag.NAME) + "\"");
                sb.Append(",");
                sb.Append("\"IsReadable\":" + IsReadable.ToString().ToLower() + "");
                sb.Append(",");
                sb.Append("\"IsCreatable\":" + IsCreatable.ToString().ToLower() + "");
                sb.Append(",");
                sb.Append("\"IsEditable\":" + IsEditable.ToString().ToLower() + "");
                sb.Append(",");
                sb.Append("\"IsDeletable\":" + IsDeletable.ToString().ToLower() + "");
                sb.Append(",");
                string fEntityKeys = "{" + (FirstEntityKeys.Any() ? FirstEntityKeys.Select(x => "\"" + x.Key + "\":" + js.Serialize(x.Value)).Aggregate((a, b) => a + "," + b) : "") + "}";
                sb.Append("\"FirstEntity\":" + fEntityKeys);
                
                string baseJson = base.Json; 

                baseJson = baseJson.Replace(("\"SecurityZone\":\"" + base.SecurityZone + "\""), ("\"SecurityZone\":\"" + SecurityZone + "\""));

                sb.Append(string.IsNullOrEmpty(baseJson) ? "" : ("," + baseJson));

                sb.Append(",\"AVAILABLE_FORMATS\":[");
                if (AVAILABLE_FORMATS != null && AVAILABLE_FORMATS.Any())
                {
                    sb.Append(AVAILABLE_FORMATS.Select(x => "{" + x.Json + "}").Aggregate((a, b) => a + "," + b));
                }
                sb.Append("]");
                return sb.ToString();
            }
            catch (Exception e) { WSStatus status = WSStatusBase.NONE.clone(); CFunc.RegError(GetType(), e, ref status); }

            return string.Empty;
        }
        public new void ClearJson() { _Json = null; base.ClearJson(); }
        internal WSEntitySchema BaseSchema { get; private set; }
        public WSEntitySchema DynamicSchema { get; internal set; }
        #endregion

        #region extended/overriten
        public bool Match(string tableName, string dbName) { 
            bool baseMatch = base.Match(tableName);
            bool DBMatch = (DBName!=null && DBName.Equals(dbName));
            return baseMatch && DBMatch;
        }
        private bool _isValid = false;
        public new bool isValid { get { if(!_isValid) { _isValid = base.isValid && PrimParams != null && !PrimParams.Any(p => !p.isValid); } return _isValid; } }
        public bool IsReady { get { return isValid && BaseSchema != null && BaseSchema.IsValid; } }
        public override bool Equals(object obj) { return obj!=null && obj.GetType()==GetType() && ToString().Equals(obj.ToString()); }
        public override int GetHashCode() { return string.IsNullOrEmpty(ToString())?base.ToString().GetHashCode():ToString().GetHashCode(); }
        public override string ToString() { return DBName + "." + dbTableName + ":" + base.ToString(); }
        #endregion
    }
}