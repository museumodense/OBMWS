using System;
using System.Linq;
using System.Linq.Expressions;
using System.Data.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Web;
using System.Data;
using System.IO;

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
    public abstract class WSDynamicResponse<A> : WSResponse<A> where A : WSDynamicEntity
    {
        public WSDynamicResponse(WSClientMeta _meta) : base(_meta) {}
        
        public abstract bool AllowOwnerAccess(A item);
        
        public abstract bool SaveHistory(Type entityType, Dictionary<string, string> values, string eventName, string notes = "");

        #region ACTION CALLS
        public override void callAction()
        {
            List<A> items = null;
            string just4fun = meta.Request.IsLocal ? "╭∩╮（︶︿︶）╭∩╮" : string.Empty;
            string accessDeniedMsg = "Access denied. User not permitted to access the source[" + meta.Request.SOURCE.NAME + "].";// + just4fun;

            addMetaData();

            if (meta.Request.SOURCE.AccessLevel > meta.Request.Security.AuthToken.User.role)
            {
                iostatus.AddNote(accessDeniedMsg, WSConstants.ACCESS_LEVEL.READ, WSStatus.ACCESS_DENIED.CODE);
            }
            else
            {
                ((WSTableSource)meta.Request.SOURCE).DynamicSchema.apply(this);

                if (meta.Request.FORMAT.Match(WSConstants.FORMAT.IMAGE_FORMAT))
                {
                    items = getItems();
                }
                else
                {
                    if (WSConstants.ALIACES.ACTION_READ.Match(meta.Request.ACTION)) items = getItems();
                    else
                    {
                        if (WSConstants.ALIACES.ACTION_INSERT.Match(meta.Request.ACTION)) items = new List<A>() { initInsertRecord() };
                        else if (WSConstants.ALIACES.ACTION_WRITE.Match(meta.Request.ACTION)) items = new List<A>() { initUpdateRecord() };
                        else if (WSConstants.ALIACES.ACTION_DELETE.Match(meta.Request.ACTION)) items = new List<A>() { initDeleteRecord() };
                        else if (WSConstants.ALIACES.ACTION_UPLOAD.Match(meta.Request.ACTION)) items = new List<A>() { initUploadRecord() };
                    }
                }
            }
            initPackResponse(items);
        }

        private void addMetaData()
        {
            try
            {
                iostatus.AddNote($"{{RequestURL:'{meta.Request.Url.EscapedUrl()}',RequestID:'{meta.Request.ID}',SessionID:'{meta.Request.SessionID}',HttpMethod:{meta.Request.HttpMethod}");
            }
            catch (Exception) { }
        }

        private void initPackResponse(List<A> items)
        {
            if (iostatus.Equals(WSStatus.ACCESS_DENIED))
            {
                response = new WSStaticRecord(meta.ClientFunctions, new WSStatusEntity() { status = iostatus }, meta.Request.Security.AuthToken.User.role) { mode = meta.Request.MODE };
            }
            #region PROCEED META FORMAT
            else if (WSConstants.FORMAT.META_FORMATS.Any(f => f.Match(meta.Request.FORMAT)))
            {
                #region VERSION 0
                if (WSConstants.PARAMS.IOVERSION.VERSION0.Match(meta.Request.VERSION))
                {
                    try
                    {
                        List<WSDynamicRecord> GResponse0 = new WSV0ResponseRecord();
                        if (items != null && items.Any())
                        {
                            foreach (A entity in items as List<A>)
                            {
                                try
                                {
                                    if (meta.Request.OUT_FIELDS != null)
                                    {
                                        GResponse0.Add(new WSDynamicRecord(
                                            meta.ClientFunctions,
                                            entity,
                                            meta.Request.Security.AuthToken.User.role,
                                            ((WSTableSource)meta.Request.SOURCE).DynamicSchema,
                                            meta.Request.OUT_FIELDS
                                        )
                                        { mode = meta.Request.MODE });
                                    }
                                }
                                catch (Exception e)
                                {
                                    meta.RegError(GetType(), e, ref iostatus);
                                }
                            }
                        }
                        //else
                        //{
                        //    WSStatusEntity entity = new WSStatusEntity() { };
                        //    response = new WSStaticRecord(meta.ClientFunctions, entity, meta.Request.Security.AuthToken.User.role);
                        //    entity.status = iostatus;
                        //}
                        response = GResponse0;
                    }
                    catch (Exception e) { meta.RegError(GetType(), e, ref iostatus); }
                }
                #endregion

                #region VERSION 1
                else if (WSConstants.PARAMS.IOVERSION.VERSION1.Match(meta.Request.VERSION))
                {
                    WSV1ResponseRecord GResponse1 = null;
                    try
                    {
                        GResponse1 = new WSV1ResponseRecord(
                            meta.ClientFunctions,
                            new WSV1ResponseEntity()
                            {
                                status = items == null ? WSStatus.ERROR.NAME : iostatus.NAME,
                                data = items == null ? null : items.Select(x => x as WSEntity).Select(entity =>
                                    new WSDynamicRecord(
                                        meta.ClientFunctions,
                                        entity,
                                        meta.Request.Security.AuthToken.User.role,
                                        ((WSTableSource)meta.Request.SOURCE).DynamicSchema,
                                        meta.Request.OUT_FIELDS
                                    )
                                    { mode = meta.Request.MODE }).ToList()
                            }, meta.Request.Security.AuthToken.User.role
                        );
                    }
                    catch (Exception e)
                    {
                        meta.RegError(GetType(), e, ref iostatus);
                        GResponse1 = new WSV1ResponseRecord(meta.ClientFunctions, new WSV1ResponseEntity() { status = WSStatus.ERROR.NAME }, meta.Request.Security.AuthToken.User.role);
                        iostatus.AddNote("An error occured while retrieving the records.", WSConstants.ACCESS_LEVEL.READ);
                    }

                    ((WSV1ResponseEntity)GResponse1.entity).message = iostatus.DeepNotes;
                    response = GResponse1;
                }
                #endregion
            }
            #endregion

            #region PROCEED BINARY FORMAT
            else if (meta.Request.FORMAT.Equals(WSConstants.FORMAT.IMAGE_FORMAT))
            {
                initBinaryPackResponse(items);
            }
            #endregion
        }
        public abstract void initBinaryPackResponse(List<A> items);
        
        private A initUploadRecord()
        {
            try
            {
                if (!verifyAccess(WSConstants.ACCESS_LEVEL.INSERT)) { iostatus.AddNote("Insert access denied.", WSConstants.ACCESS_LEVEL.READ, WSStatus.ATTANTION.CODE); }
                else
                {
                    WSTableSource table = (WSTableSource)meta.Request.SOURCE;

                    string subDir = typeof(A).Name;
                    string root = meta.Request.IsLocal ? (WSServerMeta.MapPath(WSConstants.LINKS.RootPath) + "\\shared\\Media\\") : WSConstants.CONFIG.SharedMediaPath;
                    string dir = root + subDir + "\\";

                    if (!Directory.Exists(dir)) { Directory.CreateDirectory(dir); }

                    if (!Directory.Exists(dir)) { iostatus.AddNote("File directory do not exist and failed to create", WSConstants.ACCESS_LEVEL.READ); }
                    else
                    {
                        bool saved = false;

                        HttpPostedFile file = meta.Request.Files[0];
                        string filename = file.FileName;

                        if (!string.IsNullOrEmpty(filename))
                        {
                            if (File.Exists(dir + filename) && !meta.Request.OVERRIDE)
                            {
                                filename = new WSHelperDefault().getNewFileName(dir, filename);
                            }
                        }
                        FileInfo fInfo = new FileInfo(dir + filename);
                        if (string.IsNullOrEmpty(fInfo.Name)) { iostatus.AddNote("File name missing"); }
                        else
                        {
                            file.SaveAs(fInfo.FullName);
                            saved = File.Exists(fInfo.FullName);
                        }

                        if (!saved) { iostatus.AddNote("File not saved", WSConstants.ACCESS_LEVEL.READ); }
                        else
                        {
                            if (meta.Request.IsLocal)
                            {
                                string host = meta.Request.Url.GetLeftPart(UriPartial.Authority);
                                iostatus.AddNote("{\"LocalURL\":\"" + host + "/" + subDir.Replace("\\", "/") + "/" + filename + "\"}");
                            }
                            else
                            {
                                return initInsertRecord();
                            }
                        }
                    }
                }
            }
            catch (Exception e) {
                meta.RegError(GetType(), e, ref iostatus);
                iostatus.AddNote("File fail to upload.", WSConstants.ACCESS_LEVEL.READ, WSStatus.ERROR.CODE);
                iostatus.AddNote("Error(" + e.Message + ")");
            }
            return null;
        }

        private A initInsertRecord()
        {
            A TRec = (A)Activator.CreateInstance(typeof(A));
            try
            {
                bool saveAllowed = true;
                bool overrideIfExists = meta.Request.OVERRIDE;

                if (!verifyAccess(WSConstants.ACCESS_LEVEL.INSERT)) { iostatus.AddNote("Insert access denied.", WSConstants.ACCESS_LEVEL.READ, WSStatus.ERROR.CODE); }
                else if (meta.Request.SOURCE == null) { iostatus.AddNote("Attantion! Record type undefined.", WSConstants.ACCESS_LEVEL.READ, WSStatus.ERROR.CODE); }
                else if (!(meta.Request.SOURCE is WSTableSource)) { iostatus.AddNote("Attantion! Record type is not valid.", WSConstants.ACCESS_LEVEL.READ, WSStatus.ERROR.CODE); }
                else
                {
                    WSTableSource table = (WSTableSource)meta.Request.SOURCE;
                    if (!table.IsCreatable) { iostatus.AddNote("Attantion! Sorry, but you may not add new records. :(", WSConstants.ACCESS_LEVEL.READ, WSStatus.ERROR.CODE); }
                    else
                    {
                        IEnumerable<WSTableParam> AllTableParams = table.DBParams;

                        #region VERIFY DEFAULT PARAMS
                        IEnumerable<WSTableParam> missingParamsWithInitValue = AllTableParams.Where(p => !string.IsNullOrEmpty(p.DEFAULT_VALUE) && !meta.Request.INPUT.Any(i => p.Match(i.Key)));
                        if (missingParamsWithInitValue != null && missingParamsWithInitValue.Any())
                        {
                            foreach (WSTableParam dp in missingParamsWithInitValue)
                            {
                                WSJValue defaultValue = new WSJValue(dp.DEFAULT_VALUE);
                                defaultValue.apply(meta.Request, meta.ClientFunctions);
                                if (defaultValue != null && defaultValue.IsValid)
                                {
                                    meta.Request.INPUT.Save(dp.NAME, defaultValue.Value);
                                }
                            }
                        }
                        #endregion

                        #region SET RECORD PRIMITIVE FIELDS
                        WSTableParam[] InputTableParams = AllTableParams.Where(p => meta.Request.INPUT.Any(x => p.Match(x.Key))).ToArray();

                        IEnumerable<WSTableParam> SimpleTableParams = InputTableParams.Where(p => p.DataType.IsSimple());
                        foreach (WSTableParam p in SimpleTableParams)
                        {
                            string key = p.NAME;
                            string newValue = meta.Request.INPUT.FirstOrDefault(x => p.Match(x.Key)).Value;

                            WSJValue newVal = new WSJValue(newValue);

                            if (newVal.apply(meta.Request, meta.ClientFunctions) && !TRec.TrySetRecordValue(p.WSColumnRef.NAME, newVal.Value, meta.Request.DBContext, meta.ClientFunctions))
                            {
                                saveAllowed = false;
                                iostatus.AddNote("Failed to change " + p.DISPLAY_NAME + " parameter", WSConstants.ACCESS_LEVEL.READ);
                                break;
                            }
                        }
                        #endregion

                        #region SET MISSING (NOT NULLABLE AND NOT PRIMARY KEY!) RECORD PRIMITIVE FIELDS WITH IT's DEFAULT VALUES (IF EXISTS)
                        List<System.Data.Linq.Mapping.MetaDataMember> PrimitiveProps = meta.Request.DBContext.PrimitiveProperties(table.ReturnType, ref iostatus);
                        if (PrimitiveProps != null)
                        {
                            IEnumerable<System.Data.Linq.Mapping.MetaDataMember> RequiredProps = PrimitiveProps.Where(x => !x.Type.IsNullable() && !x.IsDbGenerated);

                            if (RequiredProps != null)
                            {
                                IEnumerable<WSTableParam> MissingWSParams = AllTableParams.Where(p => !InputTableParams.Contains(p));
                                if (MissingWSParams != null)
                                {
                                    IEnumerable<WSTableParam> MissingRequiredWSParams = MissingWSParams.Where(p => RequiredProps.Any(r => p.Match(r.Name)));

                                    if (MissingRequiredWSParams != null && MissingRequiredWSParams.Any())
                                    {
                                        saveAllowed = false;
                                        iostatus.AddNote("Failed to create new record. Required parameters missing: [" + MissingRequiredWSParams.Select(x => x.NAME).Aggregate((a, b) => a + "," + b) + "]", WSConstants.ACCESS_LEVEL.READ);
                                    }
                                }
                            }
                        }
                        #endregion

                        #region SAVE CHANGES
                        if (!saveAllowed) { iostatus.AddNote("Failed to save record. Verify all fields and try again.", WSConstants.ACCESS_LEVEL.READ); }
                        else
                        {
                            A similar = GetSimilar(TRec);
                            if (similar != null)
                            {
                                #region HANDLE EXISTING RECORD
                                if (!overrideIfExists)
                                {
                                    iostatus.AddNote("Similar record(s) found.", WSConstants.ACCESS_LEVEL.READ);
                                    iostatus.AddNote("Verify record uniqueness and try again. (alternative set 'override' parameter to 'true'. Example: 'http://[your base url](?[query string])&override=1')", WSConstants.ACCESS_LEVEL.READ);
                                }
                                else if (table.PrimParams == null || table.PrimParams.Any(x => !x.isValid)) { iostatus.AddNote("Primary parameters(s) missing", WSConstants.ACCESS_LEVEL.READ); }
                                else
                                {
                                    foreach (WSTableParam PrimParam in table.PrimParams)
                                    {
                                        meta.Request.INPUT.Save(PrimParam.WSColumnRef.NAME, "" + similar.readPropertyValue(PrimParam.WSColumnRef.NAME));
                                    }
                                    TRec = initUpdateRecord();
                                }
                                #endregion
                            }
                            else
                            {
                                #region SAVE RECORD & LOG
                                meta.Request.DBContext.GetTable<A>().InsertOnSubmit(TRec);
                                meta.Request.DBContext.SubmitChanges();

                                iostatus.AddNote("Record saved", WSConstants.ACCESS_LEVEL.READ, WSStatus.SUCCESS.CODE);

                                Dictionary<string, string> history_record = null;
                                if (TRec.TryReadRecordToDictionary(meta.Request.DBContext, out history_record, ref iostatus))
                                {
                                    if (SaveHistory(typeof(A), history_record, WSConstants.EVENT_TYPE_INSERT)) { iostatus.AddNote("Record log created", WSConstants.ACCESS_LEVEL.READ); }
                                    else { iostatus.AddNote("Record log failed to create", WSConstants.ACCESS_LEVEL.READ, WSStatus.ATTANTION.CODE); }
                                }

                                #endregion
                            }
                        }
                        #endregion SAVE CHANGES
                    }
                }
            }
            catch (Exception e) {
                iostatus.AddNote("Record creation failed:[" + e.Message + "]", WSConstants.ACCESS_LEVEL.INSERT, WSStatus.ERROR.CODE);
                meta.RegError(GetType(), e, ref iostatus);
            }            
            return TRec;
        }

        private A initUpdateRecord()
        {
            A TRec = null;
            A tempRec = null;
             WSAccessMode RequiredWSAccessMode = WSAccessMode.UPDATE;
            try
            {
                if (verifyPrimaryKey(true))
                {
                    WSTableSource table = (WSTableSource)meta.Request.SOURCE;
                    if (table == null) { iostatus.AddNote("Requested source not found.", WSConstants.ACCESS_LEVEL.READ, WSStatus.ERROR.CODE); }
                    else
                    {
                        tempRec = getItemByPrimaryKeys(true);
                        if (tempRec == null) { iostatus.AddNote("No records awailable to edit.", WSConstants.ACCESS_LEVEL.READ, WSStatus.ERROR.CODE); }
                        else
                        {
                            bool OwnerAccessAllowed = AllowOwnerAccess(tempRec);
                            if (OwnerAccessAllowed || verifyAccess(RequiredWSAccessMode.ACCESS_LEVEL))
                            {
                                TRec = tempRec;
                            }
                            else if (table.EditableFilter != null)
                            {
                                #region Validate 'Editable' filter
                                List<A> list = new List<A>() { tempRec };
                                Func<A, bool> func = x => x != null;
                                if (!table.EditableFilter.IsValid) { iostatus.AddNote("Attantion! Not valid 'EditableFilter' found in schema. :(", WSConstants.ACCESS_LEVEL.READ, WSStatus.ATTANTION.CODE); throw new SyntaxErrorException(table.EditableFilter.JString); }
                                else
                                {
                                    try
                                    {
                                        int level = 0;
                                        string objID = level.ToHex();
                                        WSJson EditJFilter = table.EditableFilter;
                                        EditJFilter.apply(meta.Request, meta.ClientFunctions);
                                        ParameterExpression paramExp = Expression.Parameter(table.ReturnType, objID);
                                        WSFilter EditFilter = table.DynamicSchema.GetBaseFilter(meta.Request, paramExp, level, EditJFilter);
                                        Expression<Func<A, bool>> expr = EditFilter.ToLambda<A>(paramExp);
                                        if (expr != null)
                                        {
                                            iostatus.AddNote($"EditFilter [{expr.ToString()}] applied");
                                            func = expr.Compile();
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        meta.RegError(GetType(), e, ref iostatus);
                                        iostatus.AddNote("Attantion! Failed to read 'EditFilter' from schema. :(", WSConstants.ACCESS_LEVEL.READ, WSStatus.ATTANTION.CODE);
                                    }
                                }
                                if (list.Any(func)) { TRec = list.FirstOrDefault(); }
                                #endregion
                            }

                            if (TRec == null) { iostatus.AddNote("Record not found", WSConstants.ACCESS_LEVEL.READ, WSStatus.ATTANTION.CODE); }
                            else
                            {
                                #region proceed Update
                                Dictionary<string, string> original_values = new Dictionary<string, string>();
                                foreach (WSTableParam primaryParam in table.PrimParams)
                                {
                                    object RecIDValue = null;
                                    if (TRec.TryReadPropertyValue(primaryParam.WSColumnRef.NAME, out RecIDValue) && RecIDValue != null) { original_values.Save(primaryParam.WSColumnRef.NAME, RecIDValue.ToString()); }
                                }

                                IEnumerable<WSTableParam> updatableParams = table.DBPrimitiveParams.Where(p => p.IsEditable(OwnerAccessAllowed, meta.Request.Security.AuthToken.User.role));
                                IEnumerable<WSTableParam> updateParams = updatableParams.Where(p => meta.Request.INPUT.Any(x => p.Match(x.Key)));
                                foreach (WSTableParam p in updateParams)
                                {
                                    object oldValue = TRec.readPropertyValue(p.WSColumnRef.NAME, null);

                                    KeyValuePair<string, string> pair = meta.Request.INPUT.FirstOrDefault(x => p.Match(x.Key));
                                    string key = pair.Key;

                                    WSJValue newVal = new WSJValue(pair.Value);

                                    if (newVal.apply(meta.Request, meta.ClientFunctions) && !TRec.TrySetRecordValue(p.WSColumnRef.NAME, newVal.Value, meta.Request.DBContext, meta.ClientFunctions, RegException))
                                    {
                                        iostatus.AddNote(string.Format("Failed to change '{0}' parameter from[{1}] to[{2}]", p.DISPLAY_NAME, oldValue, newVal.Value), WSConstants.ACCESS_LEVEL.READ, WSStatus.ATTANTION.CODE);
                                    }
                                    else { original_values.Save(p.WSColumnRef.NAME, "" + oldValue); }
                                }

                                if (!iostatus.IsPositive)
                                {
                                    iostatus.AddNote("Record failed to save", WSConstants.ACCESS_LEVEL.READ);
                                    meta.Request.DBContext.GetChangeSet().Updates.Clear();
                                }
                                else
                                {
                                    if (!SaveHistory(typeof(A), original_values, WSConstants.EVENT_TYPE_UPDATE)) { iostatus.AddNote("Failed to save Log record. Target record has not being changed.", WSConstants.ACCESS_LEVEL.READ, WSStatus.ERROR.CODE); }
                                    else
                                    {
                                        meta.Request.DBContext.SubmitChanges();
                                        iostatus.AddNote("Record saved", WSConstants.ACCESS_LEVEL.READ, WSStatus.SUCCESS.CODE);
                                    }
                                }
                                #endregion
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                iostatus.AddNote("Record update failed", WSConstants.ACCESS_LEVEL.UPDATE, WSStatus.ERROR.CODE);
                meta.RegError(GetType(), e, ref iostatus);
            }
            return TRec;
        }
        public bool RegException(Exception e)
        {
            return iostatus.AddNote($"Exception[{e.Message}:{e.StackTrace}]", WSConstants.ACCESS_LEVEL.ADMIN, WSStatus.ERROR.CODE).CODE == WSStatus.ERROR.CODE;
        }

        private void ApplySoftDelete<T>(WSTableSource table, T entity, ref WSStatus status, bool _IsOwner, bool IsCascade = false) where T : WSDynamicEntity
        {
            ApplySoftDeletes(table, new List<T>() { entity }, ref status, _IsOwner, IsCascade);
        }
        private void ApplySoftDeletes<T>(WSTableSource table, IEnumerable<T> entityEnum, ref WSStatus status, bool _IsOwner, bool IsCascade = false) where T : WSDynamicEntity
        {
            try
            {
                if (table == null) { status.AddNote($"Schema for '{table.NAME}' not found", WSConstants.ACCESS_LEVEL.READ, WSStatus.ERROR.CODE); }
                else
                {
                    WSTableParam DeleteFlag = table.DeleteFlag;
                    if (DeleteFlag == null)
                    {
                        status.AddNote($"No 'DeleteFlag' found for [{table.NAME}] schema, - soft delete can't be implemented.", WSConstants.ACCESS_LEVEL.READ, WSStatus.ACCESS_DENIED.CODE);
                        if (!IsCascade)
                        {
                            status.AddNote($"Please confirm permanent delete by sending parameter [{WSConstants.PARAMS.DELETE_MODE.NAME}={WSConstants.PARAMS.DELETE_MODE_PARAM.PERMANENT.NAME}].", WSConstants.ACCESS_LEVEL.READ);
                            status.AddNote($"! IMPORTANT: By setting 'deletemode' to '{WSConstants.PARAMS.DELETE_MODE_PARAM.PERMANENT.NAME}' the record will be permanently removed from the database!!!", WSConstants.ACCESS_LEVEL.READ);
                        }
                    }
                    else if (!DeleteFlag.IsEditable(_IsOwner, meta.Request.Security.AuthToken.User.role))
                    {
                        status.AddNote("User not permitted to change '" + DeleteFlag.NAME + "' parameter", WSConstants.ACCESS_LEVEL.READ, WSStatus.ERROR.CODE);
                    }
                    else
                    {
                        List<T> entityList = entityEnum.ToList();
                        for (int i = 0; i < entityList.Count(); i++)
                        {
                            WSDynamicEntity entity = entityList[i];
                            Dictionary<string,object> ids = entity.getIdentities(meta.ClientFunctions);
                            string keys = $"{{{(ids.Any() ? ids.Select(x => x.Key + ":" + x.Value).Aggregate((a, b) => a + "," + b) : "")}}}";
                            status.AddNote($"Initiate{(IsCascade ? " cascade" : "")} delete on [{table.NAME}:{keys}]", WSConstants.ACCESS_LEVEL.READ);

                            if (!entity.TrySetRecordValue(DeleteFlag.WSColumnRef.NAME, true, meta.Request.DBContext, null))
                            {
                                status.AddNote($"Failed to change '{DeleteFlag.NAME}' field of [{table.NAME}:{keys}]", WSConstants.ACCESS_LEVEL.READ, WSStatus.ERROR.CODE);
                            }
                            else
                            {
                                Dictionary<string, string> e_values = null;
                                if (entity.TryReadRecordToDictionary(meta.Request.DBContext, out e_values, ref status))
                                {
                                    if (!SaveHistory(typeof(A), e_values, WSConstants.EVENT_TYPE_DELETE)) { status.AddNote($"Failed to save Log record. Record [{table.NAME}:{keys}] has not being changed.", WSConstants.ACCESS_LEVEL.READ, WSStatus.ERROR.CODE); }
                                    else
                                    {

                                        ApplySoftDeleteOnChilds(table, entity, ref status, _IsOwner);

                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                meta.RegError(GetType(), e, ref iostatus);
                status.AddNote("UserRole:"+meta.Request.Security.AuthToken.User.role, WSConstants.ACCESS_LEVEL.READ, WSStatus.ERROR.CODE);
                status.AddNote("Record update failed", WSConstants.ACCESS_LEVEL.READ, WSStatus.ERROR.CODE);
                status.AddNote($"Error:{{Message:[{e.Message}],StackTrace:[{e.StackTrace}]}}", WSConstants.ACCESS_LEVEL.ADMIN, WSStatus.ERROR.CODE);
            }
        }
        private void ApplySoftDeleteOnChilds(WSTableSource table, WSDynamicEntity entity, ref WSStatus parentStatus, bool _IsOwner)
        {
            try
            {
                foreach (WSTableParam param in table.DBParams.Where(p => p.DataType.GetEntityType() != null))
                {
                    if (param.DataType.IsCollectionOf<WSDynamicEntity>())
                    {
                        PropertyInfo property = entity.GetType().GetProperty(param.WSColumnRef.NAME);
                        if (property == null) { }
                        else
                        {
                            IEnumerable<WSDynamicEntity> val = (IEnumerable<WSDynamicEntity>)property.GetValue(entity, null);
                            if (val == null) { parentStatus.AddNote($"Value of '{param.DISPLAY_NAME}' is NULL"); }
                            else {
                                Type eType = val.GetType().GetEntityType();
                                WSTableSource src = (WSTableSource)meta.ClientFunctions.GetSourceByType(eType, null);

                                if (src != null)
                                {
                                    if (!val.Any()) { parentStatus.AddNote($"Collection of '{param.DISPLAY_NAME}' is empty"); }
                                    else {
                                        ApplySoftDeletes(src, val, ref parentStatus, _IsOwner, true);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e) {
                meta.RegError(GetType(), e, ref iostatus);
                parentStatus.AddNote("Record failed delete child(s)", WSConstants.ACCESS_LEVEL.READ, WSStatus.ERROR.CODE);
            }
        }
        private A initDeleteRecord()
        {
            A TRec = null;
            A tempRec = null;
            WSAccessMode RequiredWSAccessMode = WSAccessMode.DELETE;
            try
            {
                if (verifyPrimaryKey(true))
                {
                    WSTableSource table = (WSTableSource)meta.Request.SOURCE;
                    if (table == null) { iostatus.AddNote("Requested source not found.", WSConstants.ACCESS_LEVEL.READ, WSStatus.ERROR.CODE); }
                    else
                    {
                        tempRec = getItemByPrimaryKeys(true);
                        if (tempRec == null) { iostatus.AddNote("No records awailable to delete.", WSConstants.ACCESS_LEVEL.READ, WSStatus.ERROR.CODE); }
                        else
                        {
                            iostatus.AddNote("Cascading delete initiated.", WSConstants.ACCESS_LEVEL.READ);

                            bool OwnerAccessAllowed = AllowOwnerAccess(tempRec);
                            if (OwnerAccessAllowed || verifyAccess(RequiredWSAccessMode.ACCESS_LEVEL))
                            {
                                TRec = tempRec;
                            }
                            else if (table.DeletableFilter != null)
                            {
                                #region Validate 'deletable' filter
                                List<A> list = new List<A>() { tempRec };
                                Func<A, bool> func = x => x != null;

                                if (!table.DeletableFilter.IsValid) { iostatus.AddNote("Attantion! Not valid 'DeleteFilter' found in schema. :(", WSConstants.ACCESS_LEVEL.READ, WSStatus.ATTANTION.CODE); }
                                else
                                {
                                    try
                                    {
                                        int level = 0;
                                        string objID = level.ToHex();
                                        WSJson DeleteJFilter = table.DeletableFilter;
                                        DeleteJFilter.apply(meta.Request, meta.ClientFunctions);
                                        ParameterExpression paramExp = Expression.Parameter(table.ReturnType, objID);
                                        WSFilter DeleteFilter = table.DynamicSchema.GetBaseFilter(meta.Request, paramExp, level, DeleteJFilter);
                                        Expression<Func<A, bool>> expr = DeleteFilter.ToLambda<A>(paramExp);
                                        if (expr != null)
                                        {
                                            iostatus.AddNote($"DeleteFilter [{expr.ToString()}] applied", WSConstants.ACCESS_LEVEL.ADMIN);
                                            func = expr.Compile();
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        meta.RegError(GetType(), e, ref iostatus);
                                        iostatus.AddNote("Attantion! Failed to read 'DeleteFilter' from schema. :(", WSConstants.ACCESS_LEVEL.READ, WSStatus.ATTANTION.CODE);
                                    }
                                }
                                if (list.Any(func))
                                {
                                    TRec = list.FirstOrDefault();
                                }
                                #endregion
                            }
                            else { iostatus.AddNote($"Access denied! Sorry, but you may not delete this '{table.NAME}' record. :(", WSConstants.ACCESS_LEVEL.READ, WSStatus.ERROR.CODE); }

                            if (TRec == null) { iostatus.AddNote("Record not found", WSConstants.ACCESS_LEVEL.READ, WSStatus.ATTANTION.CODE); }
                            else
                            {
                                #region Proceed Delete
                                Dictionary<string, string> original_values = null;

                                if (TRec.TryReadRecordToDictionary(meta.Request.DBContext, out original_values, ref iostatus))
                                {
                                    string forceDeletedParam = null;
                                    bool PremanentDelete = meta.Request.INPUT.ReadValue(WSConstants.PARAMS.DELETE_MODE, out forceDeletedParam) ? WSConstants.PARAMS.DELETE_MODE_PARAM.PERMANENT.Match(forceDeletedParam) : false;

                                    if (PremanentDelete)
                                    {
                                        iostatus.AddNote($"Record [{table.PrimParams.Select(x => x.ReadValue(meta.Request.INPUT)).Aggregate((a, b) => a + ":" + b)}] deleted", WSConstants.ACCESS_LEVEL.READ, WSStatus.SUCCESS.CODE);
                                        meta.Request.DBContext.GetTable<A>().DeleteOnSubmit(TRec);
                                    }
                                    else { ApplySoftDelete(table, TRec, ref iostatus, OwnerAccessAllowed); }

                                    #region Apply Delete
                                    if (!iostatus.IsPositive) { if (PremanentDelete) { meta.Request.DBContext.GetChangeSet().Deletes.Clear(); } else { meta.Request.DBContext.GetChangeSet().Updates.Clear(); } }
                                    else
                                    {
                                        if (!SaveHistory(typeof(A), original_values, WSConstants.EVENT_TYPE_DELETE)) { iostatus.AddNote("Failed to save Log record. Target record has not being changed.", WSConstants.ACCESS_LEVEL.READ, WSStatus.ERROR.CODE); }
                                        else
                                        {
                                            meta.Request.DBContext.SubmitChanges(ConflictMode.FailOnFirstConflict);
                                            iostatus.AddNote("Record deleted", WSConstants.ACCESS_LEVEL.READ, WSStatus.SUCCESS.CODE);
                                        }
                                    }
                                    #endregion
                                }
                                #endregion
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                meta.RegError(GetType(), e, ref iostatus);
                iostatus.AddNote("Failed to delete record.", WSConstants.ACCESS_LEVEL.READ, WSStatus.ERROR.CODE);
            }

            if(!iostatus.IsPositive){
                TRec = tempRec;
                iostatus.AddNote("Record failed to delete. No changes applied to the data.", WSConstants.ACCESS_LEVEL.READ, WSStatus.SUCCESS.CODE);
            }
            return TRec;
        }
        #endregion

        #region GET FUNCTIONS
        private List<A> getItems(bool IgnoreSchema = false)
        {
            List<A> items = new List<A>();
            try
            {
                /***********************************************************************************
                 *              READ INITIAL TABLE
                 */
                iostatus.AddNote($"DR.GI : DBContext:[{(meta.Request.DBContext == null ? "NONE" : meta.Request.DBContext.ToString())}]", WSAccessMode.ADMIN.ACCESS_LEVEL);

                if (meta.Request.DBContext == null)
                {
                    iostatus.AddNote($"No connection established with[{meta.Request.DB}]", WSAccessMode.READ.ACCESS_LEVEL);
                    iostatus.AddNotes(meta.Request.status.NOTES);
                }
                else
                {
                    ITable source = meta.Request.DBContext.GetTable(typeof(A));

                    iostatus.AddNote($"DR.GI : source:[{(source == null ? "NONE" : source.ToString())}]", WSAccessMode.ADMIN.ACCESS_LEVEL);

                    /***********************************************************************************
                     *              SORT TABLE's RECORDS
                     */
                    IQueryable<A> sorted = (IQueryable<A>)sortOutput();

                    /***********************************************************************************
                     *              FILTER TABLE's RECORDS
                     */
                    if (sorted == null) { iostatus.AddNote("No items found", WSConstants.ACCESS_LEVEL.READ); }
                    else
                    {
                        Expression<Func<A, bool>> expression = GetSelectExpression(IgnoreSchema);

                        #region IMPLEMENT SELECT-EXPRESSION
                        if (expression == null) { iostatus.AddNote("No sql expression generated", WSAccessMode.READ.ACCESS_LEVEL); }
                        else if (sorted == null || sorted.FirstOrDefault() == null) { iostatus.AddNote("No records found", WSAccessMode.READ.ACCESS_LEVEL); }
                        else
                        {
                            iostatus.AddNote($"Expression:[{expression.ToString()}]", WSAccessMode.ADMIN.ACCESS_LEVEL);//WSConstants.ACCESS_LEVEL.READ); //

                            int sCount = sorted.Count();
                            IQueryable<A> XList = sorted.Where(expression);

                            if (XList != null)
                            {
                                #region IMPLEMENT DISTINCT
                                if (WSParamList.IsValid(meta.Request.DISTINCT_FIELDS))
                                {
                                    IEnumerable<string> PList = meta.Request.DISTINCT_FIELDS.Select(x => ((WSTableParam)x).WSColumnRef.NAME);
                                    IEnumerable<A> EList = XList.SelectDistinct(PList);

                                    //XList = EList.AsQueryable(); //!important : TODO@ANDVO : check it and implement if works

                                    List<A> LList = EList.ToList();
                                    XList = LList.AsQueryable();
                                }
                                #endregion

                                #region IMPLEMENT PAGINATION
                                XList = XList == null ? null : XList.SkipAndTake(meta.Request.OFFSET, meta.Request.COUNT);
                                #endregion

                                items = XList == null ? new List<A>() : XList.ToList();
                            }
                            if (!items.Any()) { iostatus.AddNote("No records received", WSConstants.ACCESS_LEVEL.READ); }
                            else { iostatus.CODE = WSStatus.SUCCESS.CODE; }
                        }
                        #endregion
                    }
                }
            }
            catch (Exception e)
            {
                items = items == null ? new List<A>() : items;
                meta.RegError(GetType(), e, ref iostatus);
                iostatus.AddNote($"DR.GI : Failed read data: [{e.Message}:{e.StackTrace}]", WSAccessMode.ADMIN.ACCESS_LEVEL, WSStatus.ERROR.CODE);
            }
            return items;
        }
        private A getItemByPrimaryKeys(bool IgnoreSchema)
        {
            A result = null;
            try
            {
                WSTableSource table = (WSTableSource)meta.Request.SOURCE;
                if (table.PrimParams.Any())
                {
                    Expression exp = GetSelectExpression(IgnoreSchema, table.PrimParams);

                    iostatus.AddNote($"Delete expression applied: [{exp.ToString()}]");
                    
                    result = meta.Request.DBContext.GetTable<A>().FirstOrDefault((Expression<Func<A, bool>>)exp);
                }
            }
            catch (Exception e) { meta.RegError(GetType(), e, ref iostatus); }
            return result;
        }

        #region FILTER RECORDS
        private Expression<Func<A, bool>> GetSelectExpression(bool IgnoreSchema, IEnumerable<WSTableParam> iofields = null)
        {
            Expression<Func<A, bool>> expression = null;
            try
            {
                WSCombineFilter filter = new WSCombineFilter(WSCombineFilter.SQLMode.AndAlso);

                int level = 0;
                string objID = level.ToHex();
                ParameterExpression paramExp = Expression.Parameter(meta.Request.SOURCE.ReturnType, objID);

                WSEntitySchema dSchema = ((WSTableSource)meta.Request.SOURCE).DynamicSchema;

                if (!dSchema.IsValidSchema && !IgnoreSchema) {
                    iostatus.AddNote($"DynamicSchema not valid. Using default schema [print all primitive fields] instead.", WSAccessMode.READ.ACCESS_LEVEL, WSStatus.ATTANTION.CODE);
                }

                WSFilter CustomFilter_ = dSchema.GetMainFilter(meta.Request, paramExp, level);
                WSCombineFilter DynamicFilter = new WSCombineFilter();
                if (CustomFilter_ is WSCombineFilter) { DynamicFilter = (WSCombineFilter)CustomFilter_; }
                else { DynamicFilter.Add(CustomFilter_); }

                iostatus.AddNote($"DR.GSE() : {{DynamicFilter:{(DynamicFilter==null?"NONE": DynamicFilter.ToString())}}}");

                /********************************************************
                    WARNING : USING DEPRECATED METHOD!!!
                    TODO@ANDVO-2016-08-30: => Awoid using this method ['VerifyRequiredFilters()'] !!! Use instead 'baseFilter' from '.config' file*/

                if (WSConstants.ALIACES.ACTION_READ.Match(meta.Request.ACTION)) { VerifyRequiredFilters(iofields, DynamicFilter); }

                /*
                 * *******************************************************/
                 
                IEnumerable<WSTableParam> all_fields = iofields == null ? meta.Request.SOURCE.Params.OfType<WSTableParam>() == null ? new List<WSTableParam>() : meta.Request.SOURCE.Params.OfType<WSTableParam>() : iofields;
                if (all_fields != null && all_fields.Any())
                {
                    all_fields = all_fields.Where(p => p is WSTableParam);
                    if (all_fields != null && all_fields.Any())
                    {
                        IEnumerable<WSTableParam> table_fields = all_fields.Where(f => meta.Request.INPUT.Any(d => f.Match(d.Key)));

                        if (table_fields != null && table_fields.Any())
                        {
                            foreach (WSTableParam field in table_fields)
                            {
                                Expression member = Expression.Property(paramExp, field.WSColumnRef.NAME);
                                Func<KeyValuePair<string, string>, bool> func = d => field.Match(d.Key);
                                if (meta.Request.INPUT.Any(func) && field != null && field.isValid)
                                {
                                    string JFilterValue = meta.Request.INPUT.FirstOrDefault(func).Value;

                                    if (field.DataType.IsNullable() || !string.IsNullOrEmpty(JFilterValue))
                                    {
                                        WSJson JFilter = null;
                                        if (field.ToWSJson(JFilterValue, out JFilter))
                                        {
                                            iostatus.AddNote($"DR.GSE() [{field.NAME}].apply()");

                                            JFilter.apply(meta.Request, meta.ClientFunctions);

                                            iostatus.AddNote($"DR.GSE() : {{JFilter:[{(JFilter == null ? "NONE" : JFilter.ToString())})]}}");

                                            filter.Save(JFilter.GetFieldFilter(meta.ClientFunctions, field, paramExp, level));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                if (DynamicFilter != null && DynamicFilter.IsValid)
                {
                    if (filter.Mode.Equals(DynamicFilter.Mode)) { filter.AddRange(DynamicFilter.Where(x => x != null && x.IsValid)); }
                    else { filter.Add(DynamicFilter); }
                }
                expression = filter.ToLambda<A>(paramExp);//.Compile();

                expression = expression == null ? x => x != null : expression;
            }
            catch (Exception e) {
                meta.RegError(GetType(), e, ref iostatus);
                iostatus.AddNote($"DR.GSE : Failed read data: [{e.Message}:{e.StackTrace}]", WSAccessMode.ADMIN.ACCESS_LEVEL, WSStatus.ERROR.CODE);
                expression = null;
            }
            /***************************************************************************************************************************************
             * test links : 
             * http://localhost:57350/hadb5.Tag.json?count=10&schema={tag:[id,plurname,{parents:[empty,exists,{parent:[tagid]}]}]}
             * http://localhost:57350/hadb5.Tag.json?count=10&schema={tag:[id,plurname,{parents:[empty,{parent:[{tagid:{equals:89}}]}]}]}
             * http://localhost:57350/hadb5.Tag.json?count=10&schema={tag:[id,plurname,{parents:[{parent:[{tagid:[80,90]}]}]}]}
             * 
             *      deep filter example
             * http://localhost:57350/tag.json?category=9&count=20&schema={tag:[tagid,{parents:[empty,{parent:[tagid,category,created,[{tagid:{more:532},created:{more:2015-06-17}},{category:{equal:9},yearstart:empty,tagid:{equal:532}}]]}]}]}
             *
             *      deep collapse example
             * http://localhost:57350/hadb5.Tag.json?schema={tag:[id,plurname,{children:[{collapse:{child:tagid}},{child:[{tagid:{less:500}}]}]},{parents:[exists,{parent:[tagid]}]}]}
             *             *
             * http://localhost:55470/event.json?status=2&dates={eventid:{max:10070,min:200}}&organization={name:%27M%C3%B8nterg%C3%A5rden%27}
             * http://localhost:55470/event.json?status=2&dates={eventid:{max:10070,min:200}}&organization={name:[{start:%27fyr%27},{end:%27et%27}]}
             * //throw error on dates: 
             * http://localhost:55470/event.json?status=2&dates={startdate:{min:%272005-05-22%27},eventid:{max:10070,min:200}}&organization={name:{like:%27andersen%27}}&notes={start:%27pipstorn%27}
             * //Negation
             * http://localhost:57350/hadb5dev.Geo.json?count=1&v=1&schema={geo:[{filters:[{user:{id:{not:{any:[5,632,635,636,638,658,659,680,683]}}}}]},{fields:[geoid,{user:[id,{user_institutions:[{organization:[id,url]}]}]}]}]}
             * http://localhost:57350/hadb5dev.Geo.json?count=10&v=1&schema={geo:[{filters:{deleted:[0,1],user:{id:{is:$userid,not:{any:[5]}}},user_institutions:[empty,exists]}},{fields:[geoid,deleted,title,{tag_geos:[{tag:[id,plurname,{organization:[id,url]}]}]}]}]}
             * *************************************************************************************************************************************/
                return expression;
        }

        /*TODO:ANDVO-2016-08-30: => DEPRECATE THIS METHOD!!! Use instead 'baseFilter' from appropriate '.config' file ('~/config/[database-name].config')*/
        private void VerifyRequiredFilters(IEnumerable<WSParam> iofields, WSCombineFilter SRCFilter)
        {
            #region FILTER DELETED
            try
            {
                WSTableSource table = (WSTableSource)meta.Request.SOURCE;

                WSTableParam deleted_param = table.DBParams.FirstOrDefault(p=>p.Match(WSConstants.PARAMS.KEYS.DELETED));
                
                if (deleted_param!=null && (SRCFilter==null || !SRCFilter.Contains(deleted_param)))
                {
                    int next_code = (iofields != null && iofields.Any()) ? iofields.Max(f => f.CODE) + 1 : 0;
                    PropertyInfo pInfo = typeof(A).GetProperties().FirstOrDefault(p => p.Name.ToLower().Equals(WSConstants.PARAMS.KEYS.DELETED.ToLower()));
                    WSTableParam param = new WSTableParam(typeof(A), next_code, pInfo.Name, new WSColumnRef(pInfo.Name), pInfo.PropertyType, meta.ClientFunctions);
                    
                    if (param != null)
                    {
                        string val = meta.Request.INPUT.Any(x => param.Match(x.Key)) ? meta.Request.INPUT.FirstOrDefault(x => param.Match(x.Key)).Value : null;

                        if (string.IsNullOrEmpty(val))
                        {
                            if (param.DataType.IsAssignableFrom(typeof(bool)))
                            {
                                val = param.DataType.IsNullable() ? "empty" : "false";
                            }
                            else if (param.DataType.IsAssignableFrom(typeof(DateTime)))
                            {
                                val = param.DataType.IsNullable() ? "empty" : "{min:'" + (DateTime.Now.ToString(WSConstants.DATE_FORMAT)) + "'}";
                            }
                        }
                        if (!string.IsNullOrEmpty(val)) { meta.Request.INPUT.Save(param.WSColumnRef.NAME, val); }
                    }
                }
            }
            catch (Exception e) { meta.RegError(GetType(), e, ref iostatus); }
            #endregion
        }
        #endregion

        #region SORT FUNCTIONS
        private IQueryable sortOutput()
        {
            IQueryable sorted = null;
            try
            {
                if (meta.Request.DBContext == null) { }
                else
                {
                    sorted = meta.Request.DBContext.GetTable(typeof(A));
                    string sort = string.Empty;
                    if (sorted == null)
                    {
                        iostatus.AddNote("No source table created for[" + typeof(A).Name + "]", WSConstants.ACCESS_LEVEL.READ, WSStatus.ERROR.CODE);
                    }
                    else if (!meta.Request.INPUT.ReadValue(WSConstants.PARAMS.SORT, out sort))
                    {
                        //iostatus.AddNote("No valid sort parameters defined", WSConstants.ACCESS_LEVEL.READ, WSStatus.ATTANTION.CODE);
                    }
                    else
                    {
                        Expression sortExpr = sort.Trim().ToJson().SortTable<A>(meta.ClientFunctions, meta.Request.DBContext, null, null, ref iostatus);
                        if (sortExpr != null)
                        {
                            sorted = sorted.Provider.CreateQuery(sortExpr);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                meta.RegError(GetType(), e, ref iostatus);
                iostatus.AddNote("Failed to sort records: [" + e.Message + ":" + e.StackTrace + "]", WSConstants.ACCESS_LEVEL.ADMIN, WSStatus.ERROR.CODE);
            }

            return sorted;
        }
        #endregion

        #endregion

        #region MATCH FUNCTIONS
        private A GetSimilar(A TRec)
        {
            A result = null;
            try
            {
                Expression<Func<A, bool>> expression = GetMatchExpression(TRec);
                result = meta.Request.DBContext.GetTable<A>().FirstOrDefault(expression);
            }
            catch (Exception e) { meta.RegError(GetType(), e, ref iostatus); }
            return result;
        }
        private Expression<Func<A, bool>> GetMatchExpression(A TRec)
        {
            Expression<Func<A, bool>> expression = null;
            try
            {
                IEnumerable<WSParam> iofields = meta.Request.SOURCE.Params.Where(p => p is WSTableParam);
                IEnumerable<WSTableParam> fields = iofields.Any() ? iofields.Select(x => (WSTableParam)x).Where(x => x.IsComparable) : null;

                expression = GetSelectExpression(false, fields);
            }
            catch (Exception e) { meta.RegError(GetType(), e, ref iostatus); }
            return expression;
        }
        #endregion

        private bool verifyPrimaryKey(bool identityRequired = false)
        {
            bool ok = false;
            if (!identityRequired) { ok = true; }
            else
            {
                if (meta.Request.SOURCE == null) { iostatus.AddNote("No source defined", WSConstants.ACCESS_LEVEL.READ); }
                else
                {
                    WSTableSource table = (WSTableSource)meta.Request.SOURCE;

                    bool hasPrimary = table.PrimParams.Any();
                    if (!hasPrimary) { iostatus.AddNote("NO primary parameter(s) found! It is not possible to update with no primary key(s) specified!", WSConstants.ACCESS_LEVEL.READ, WSStatus.ERROR.CODE); }
                    else
                    {
                        if (table.PrimParams.Any(p => !meta.Request.INPUT.Any(v => p.Match(v.Key)))) { iostatus.AddNote("Primary parameter(s) missing :["+(table.PrimParams.Where(p => !meta.Request.INPUT.Any(v => p.Match(v.Key))).Select(x=>x.NAME).Aggregate((a,b)=>a+","+b)) +"]", WSConstants.ACCESS_LEVEL.READ, WSStatus.ERROR.CODE); }
                        else
                        {
                            bool pOK = true;
                            foreach (WSTableParam primaryParam in table.PrimParams)
                            {
                                if (pOK)
                                {
                                    string _identity = meta.Request.INPUT.FirstOrDefault(x => primaryParam.Match(x.Key)).Value;
                                    Type[] intTypes = new Type[] { typeof(System.Int16), typeof(System.Int32), typeof(int) };
                                    if (intTypes.Contains(primaryParam.DataType))
                                    {
                                        int id = 0;
                                        if (!int.TryParse(_identity, out id) || id < 0) { pOK = false; iostatus.AddNote("Not valid identity key '" + primaryParam.NAME + "'", WSConstants.ACCESS_LEVEL.READ); }
                                    }
                                    else if (primaryParam.DataType == typeof(Int64))
                                    {
                                        long temp = 0;
                                        if (!long.TryParse(_identity, out temp) || temp < 0) { pOK = false; iostatus.AddNote("Not valid identity key '" + primaryParam.NAME + "'", WSConstants.ACCESS_LEVEL.READ); }
                                    }
                                    else if (primaryParam.DataType == typeof(Guid))
                                    {
                                        Guid guid = Guid.Empty;
                                        if (!Guid.TryParse(_identity, out guid) || guid.ToString().Equals(Guid.Empty.ToString())) { pOK = false; iostatus.AddNote("Not valid identity key '" + primaryParam.NAME + "'", WSConstants.ACCESS_LEVEL.READ); }
                                    }
                                    else
                                    {
                                        pOK = !string.IsNullOrEmpty(_identity);
                                    }
                                }
                            }
                            ok = pOK;
                        }
                    }
                }
            }
            return ok;
        }
        
    }
}