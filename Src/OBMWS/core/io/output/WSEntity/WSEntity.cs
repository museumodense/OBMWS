using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

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
    public class WSEntity
    {
        public bool TryReadPropertyValue(string propertyName, out object value, object DEFAULT = null)
        {
            object v = null;
            bool ok = false;
            if (!string.IsNullOrEmpty(propertyName))
            {
                try
                {
                    PropertyInfo prop = GetType().GetProperties().FirstOrDefault(p => p.Name.ToLower().Equals(propertyName.ToLower()));
                    if (prop != null)
                    {
                        v = prop.GetValue(this, null);
                        ok = true;
                    }
                }
                catch (Exception) { }
            }
            value = ok ? v == null ? DEFAULT : v : null;
            return ok;
        }

        public object readPropertyValue(string propertyName, object DEFAULT = null)
        {
            object v = null;
            return TryReadPropertyValue(propertyName, out v, DEFAULT) ? v : DEFAULT;
        }

        public virtual bool Match(WSRequest Request, WSDataContext DBContext, MetaFunctions CFunc, WSSchema schema)
        {
            return true;
        }

        public WSStatus WriteJson(JsonWriter writer, JsonSerializer serializer, WSSchema schema, WSParamList outFields, List<Type> printedTypes, WSRequest Request, MetaFunctions CFunc, WSDataContext DBContext)
        {
            WSStatus status = WSStatus.NONE_Copy();

            WSEntitySchema eSchema = null;
            WSSource xSource = CFunc.GetSourceByType(GetType());

            if (schema != null && schema is WSEntityBaseSchema)
            {
                if (schema is WSEntitySchema) eSchema = (WSEntitySchema)schema;
                else if (schema is WSEntityListSchema) eSchema = ((WSEntityListSchema)schema).EntitySchema;
            }

            if (eSchema == null && this is WSDynamicEntity)
            {
                if (xSource != null && xSource is WSTableSource && ((WSTableSource)xSource).BaseSchema != null)
                {
                    eSchema = ((WSTableSource)xSource).BaseSchema;
                }
            }

            #region Read if in 'COLLAPSED' mode
            bool collapsedMode = false;
            if (eSchema != null)
            {
                IEnumerable<WSJObject> _JOptions = eSchema.IOBaseOptions.Value.OfType<WSJObject>();
                Func<WSJProperty, bool> func = v => WSConstants.ALIACES.OPTION_COLLECT.Match(v.Key);
                WSJObject takeJOption = _JOptions.FirstOrDefault(x => x.Value.Any(func));
                if (takeJOption != null && takeJOption.IsValid)
                {
                    collapsedMode = true;
                    status.childs.Add(WriteCollapsedValues(writer, serializer, this, xSource, takeJOption.Value.FirstOrDefault(func).Value, Request, CFunc));
                }
            }
            #endregion

            if (!collapsedMode)
            {
                writer.WriteStartObject();
                if (this is WSStaticEntity || (eSchema != null && eSchema.Fields.Any()))
                {
                    List<Type> postPrintedTypes = printedTypes.Select(x => x).ToList();
                    if (!postPrintedTypes.Any(x => x == GetType())) { postPrintedTypes.Add(GetType()); }
                    status.childs.Add(
                        WriteJMembers(
                            GetType().GetMembers(BindingFlags.Public | BindingFlags.Instance).Where(m => m is PropertyInfo || m is FieldInfo),
                            writer,
                            serializer,
                            eSchema,
                            xSource,
                            outFields,
                            printedTypes,
                            Request,
                            CFunc, 
                            DBContext
                        )
                    );
                }
                writer.WriteEndObject();
            }
            return status;
        }

        private WSStatus WriteJMembers(IEnumerable<MemberInfo> members, JsonWriter writer, JsonSerializer serializer, WSEntitySchema eSchema, WSSource xSource, WSParamList outFields, List<Type> printedTypes, WSRequest Request, MetaFunctions CFunc, WSDataContext DBContext)
        {
            WSStatus status = WSStatus.NONE_Copy();
            try
            {
                Type eType = GetType();
                object obj = null;
                WSParam param = null;
                if (this is WSDynamicEntity)
                {
                    foreach (WSSchema fieldSchema in eSchema.Fields/*.Items*/)
                    {
                        if (fieldSchema is WSFieldSchema) { param = ((WSFieldSchema)fieldSchema).param; }
                        else if (fieldSchema is WSEntityBaseSchema) { param = GetParam(xSource, fieldSchema.Name); }
                        //else if (childSchema is WSEntitySchema) { param = GetParam(childSchema.Name); }
                        //else if (childSchema is WSEntityListSchema) { param = GetParam(((WSEntityListSchema)childSchema).EntitySchema.Name); }

                        MemberInfo member = param == null ? null : members.FirstOrDefault(p => param.Match(p.Name,null,null,false));
                        obj = member == null ? null : member is PropertyInfo ? ((PropertyInfo)member).GetValue(this, null) : member is FieldInfo ? ((FieldInfo)member).GetValue(this) : null;

                        if (param != null)
                        {
                            status.childs.Add(WriteJProperty(obj, param, writer, serializer, fieldSchema, xSource, outFields, printedTypes, Request, CFunc, DBContext));
                        }
                    }
                }
                else if (this is WSStaticEntity)
                {
                    foreach (MemberInfo member in members)
                    {
                        param = GetParam(xSource, member.Name, member.ReflectedType);
                        obj = member is PropertyInfo ? ((PropertyInfo)member).GetValue(this, null) : member is FieldInfo ? ((FieldInfo)member).GetValue(this) : null;

                        if (param != null)
                        {
                            status.childs.Add(WriteJProperty(obj, param, writer, serializer, null, xSource, outFields, printedTypes, Request, CFunc, DBContext));
                        }
                    }
                }

                status.AddNote("done", WSConstants.ACCESS_LEVEL.READ);
            }
            catch (Exception e)
            {
                status.CODE = WSStatus.ERROR.CODE;
                status.AddNote("Error(line" + e.LineNumber() + "- " + e.Message + ")");
                CFunc.RegError(GetType(), e, ref status);
            }
            return status;
        }
        private bool Validate(object obj, WSParam xParam, JsonWriter writer, JsonSerializer serializer, WSSchema schema, WSSource xSource, WSParamList outFields, ref WSStatus status, WSRequest Request, MetaFunctions CFunc)
        {
            try
            {
                if (this is WSDynamicEntity && schema == null) { status.CODE = WSStatus.ERROR.CODE; status.AddNote("No schema defined", WSConstants.ACCESS_LEVEL.READ); }
                else if (xParam == null) { status.CODE = WSStatus.ERROR.CODE; status.AddNote("Undefined parameters are not allowed", WSConstants.ACCESS_LEVEL.READ); }
                else
                {
                    bool IsOwner = false;//TODO@ANDVO:2015-09-11 : ADD IsOwner validation check
                    int paramAccessLevel = xParam.READ_ACCESS_MODE.ACCESS_LEVEL;
                    paramAccessLevel = (xSource != null && xSource.AccessLevel > paramAccessLevel) ? xSource.AccessLevel : paramAccessLevel;

                    if (Request.Security.AuthToken.User.role < paramAccessLevel && !IsOwner)
                    {
                        #region ACCESS DENIED HANDLING
                        if (xSource != null && xSource.ShowMessageInaccessible)
                        {
                            string accessNote = "Access denied";
                            status.CODE = WSStatus.ATTANTION.CODE;
                            status.AddNote(accessNote, WSConstants.ACCESS_LEVEL.READ);
                            WritePropName(writer, xParam.NAME);
                            serializer.Serialize(writer, accessNote);
                        }
                        #endregion
                    }
                    else
                    {
                        if (!WSParamList.IsEmpty(outFields) && !outFields.Any(a => a.Match(schema.Name))) { }
                        else
                        {
                            if (obj == null && !xParam.SkipEmpty)
                            {
                                obj = string.Empty;
                                status.CODE = WSStatus.ATTANTION.CODE;
                                status.AddNote("Can not write NULL to [" + xParam.DISPLAY_NAME + "]. Value set to empty string.", WSConstants.ACCESS_LEVEL.READ);
                            }

                            if (obj != null)
                            {
                                status.AddNote("done", WSConstants.ACCESS_LEVEL.READ);
                                return true;
                            }
                        }
                    }
                }
            }
            catch (Exception e) {
                status.CODE = WSStatus.ERROR.CODE; status.AddNote("Error(line" + e.LineNumber() + "- " + e.Message + ")");
                CFunc.RegError(GetType(), e, ref status);
            }
            return false;
        }

        private WSStatus WriteJProperty(object obj, WSParam xParam, JsonWriter writer, JsonSerializer serializer, WSSchema schema, WSSource xSource, WSParamList outFields, List<Type> printedTypes, WSRequest Request, MetaFunctions CFunc, WSDataContext DBContext)
        {
            WSStatus status = WSStatus.NONE_Copy();
            try
            {
                if (Validate(obj, xParam, writer, serializer, schema, xSource, outFields, ref status, Request, CFunc))
                {
                    if (obj == null)
                    {
                        WritePropName(writer, ((schema != null && !string.IsNullOrEmpty(schema.Name)) ? schema.Name : xParam.NAME), true, PrintMode.ValueCell);
                        serializer.Serialize(writer, obj);
                    }
                    else if (obj is WSStatus || obj is WSStatus_JSON)
                    {
                        #region PRINT WSStatus
                        WSStatus_JSON json = obj is WSStatus_JSON ? (WSStatus_JSON)obj : ((WSStatus)obj).GetJson();
                        if (json != null)
                        {
                            WritePropName(writer, ((schema != null && !string.IsNullOrEmpty(schema.Name)) ? schema.Name : xParam.NAME), true, PrintMode.TableHeader);
                            serializer.Serialize(writer, json);
                        }
                        #endregion
                    }
                    else if (obj.GetType().IsSimple())
                    {
                        #region PRINT PRIMITIVE FIELD

                        if (obj is DateTime) { obj = ((DateTime)obj).ToString(WSConstants.DATE_FORMAT); }
                        else if (obj is TimeSpan) { obj = ((TimeSpan)obj).ToString(WSConstants.TIMESPAN_FORMAT_SIMPLE); }
                        
                        WritePropName(writer, (schema != null && !string.IsNullOrEmpty(schema.Name)) ? schema.Name : xParam.NAME);
                        object _obj = null;
                        serializer.Serialize(writer, xParam.TryReadPrimitiveWithDefault(obj, string.Empty, out _obj) ? _obj : string.Empty);
                        
                        #endregion
                    }
                    else if (obj.GetType().IsSimpleCollection())
                    {
                        #region PRINT PRIMITIVE COLLECTION
                        string key = (schema != null && !string.IsNullOrEmpty(schema.Name)) ? schema.Name : xParam.NAME;
                        status.AddNote("ready to searialize primitive fields (" + key + ")");
                        
                        WritePropName(writer, key);
                        serializer.Serialize(writer, obj);
                        
                        #endregion
                    }
                    else if (obj is WSRecord)
                    {
                        #region PRINT WSRecord

                        string pKey = (schema != null && !string.IsNullOrEmpty(schema.Name)) ? schema.Name : xParam.NAME;
                        WritePropName(writer, pKey);
                        ((WSRecord)obj).WriteJson(writer, serializer, printedTypes, Request, CFunc, DBContext);
                        
                        #endregion
                    }
                    else if (obj.IsCollectionOf<WSRecord>())
                    {
                        #region PRINT WSRecord Collection

                        string pKey = (schema != null && !string.IsNullOrEmpty(schema.Name)) ? schema.Name : xParam.NAME;

                        WritePropName(writer, pKey);
                        writer.WriteStartArray();

                        IList list = obj as IList;
                        foreach (WSRecord record in list)
                        {
                            if (record != null)
                            {
                                record.WriteJson(writer, serializer, printedTypes, Request, CFunc, DBContext);
                            }
                        }

                        writer.WriteEndArray();
                        #endregion
                    }
                    else
                    {
                        #region PRINT ENTITY

                        bool printAllowed =
                            (this is WSStaticEntity)
                            ||
                            (
                                schema is WSEntityBaseSchema
                                &&
                                validateType(writer, xParam, obj, printedTypes, true, Request, CFunc)
                            );

                        if (printAllowed)
                        {
                            string pKey = (schema != null && !string.IsNullOrEmpty(schema.Name)) ? schema.Name : xParam.NAME;
                            WritePropName(writer, pKey, false);
                            
                            #region PRINT WSEntity
                            if (obj is WSEntity)
                            {
                                if (obj is WSDynamicEntity && !((WSDynamicEntity)obj).Match(Request, DBContext, CFunc, schema)) {
                                    serializer.Serialize(writer, "NULL");
                                }
                                else { ((WSEntity)obj).WriteJson(writer, serializer, schema, outFields, printedTypes, Request, CFunc, DBContext); }
                            }
                            #endregion

                            #region PRINT Collection
                            else if (obj.IsCollectionOf<WSEntity>())
                            {
                                IList list = obj as IList;
                                Type eType = list.GetEntityType();

                                writer.WriteStartArray();
                                foreach (WSEntity entity in list)
                                {
                                    if (entity != null)
                                    {
                                        if (entity is WSDynamicEntity)
                                        {
                                            WSDynamicEntity dEntity = (WSDynamicEntity)entity;
                                            if (dEntity.Match(Request, DBContext, CFunc, schema))
                                            {
                                                entity.WriteJson(writer, serializer, schema, outFields, printedTypes, Request, CFunc, DBContext);
                                            }
                                        }
                                        else { entity.WriteJson(writer, serializer, schema, outFields, printedTypes, Request, CFunc, DBContext); }
                                    }
                                }
                                writer.WriteEndArray();
                            }
                            #endregion
                        }

                        #endregion
                    }
                    status.AddNote("done", WSConstants.ACCESS_LEVEL.READ);
                }
            }
            catch (Exception e)
            {
                status.CODE = WSStatus.ERROR.CODE;
                status.AddNote("Error(line" + e.LineNumber() + "- " + e.Message + ")");
                CFunc.RegError(GetType(), e, ref status);
            }
            return status;
        }
        private void WritePropName(JsonWriter writer, string name, bool isSimpleValue = true, PrintMode mode = PrintMode.ValueCell)
        {
            writer.WritePropertyName(name);
        }
        private WSStatus WriteCollapsedValues(JsonWriter writer, JsonSerializer serializer, WSEntity entity, WSSource xSource, WSJson collapseOption, WSRequest Request, MetaFunctions CFunc)
        {
            WSStatus status = WSStatus.NONE_Copy();
            try
            {
                /*******************************************************************************
                
                //  TODO @ANDVO : 2016-02-15 : IMPORTANT!!! => Implement security check like this : 

                WSStatus status = Validate(obj, xParam, writer, serializer, security, schema);
                if (status.CODE == WSStatus.SUCCESS.CODE)
                {
                */

                if (entity != null && collapseOption != null && collapseOption.IsValid)
                {
                    WSTableSource childSource = (WSTableSource)CFunc.GetSourceByType(entity.GetType());

                    object fieldValue = null;
                    WSParam field = null;
                    if (collapseOption is WSJValue)
                    {
                        string fieldName = ((WSJValue)collapseOption).Value;
                        field = entity.GetParam(xSource, fieldName);
                        fieldValue = getMemberValue(entity, field, CFunc);

                        WSPrimitiveFieldSchema fieldSchema = new WSPrimitiveFieldSchema(CFunc, (WSTableParam)field, new WSJProperty(fieldName, new WSJArray()), /*((WSTableSource)entity.getSource())*/childSource.BaseSchema);
                        if (Validate(fieldValue, field, writer, serializer, childSource.BaseSchema, childSource, null, ref status, Request, CFunc))
                        {
                            object _obj = null;
                            serializer.Serialize(writer, field.TryReadPrimitiveWithDefault(fieldValue, string.Empty, out _obj) ? _obj : string.Empty);
                            writer.Flush();
                            status = WSStatus.SUCCESS_Copy();
                        }
                    }
                    else if (collapseOption is WSJObject)
                    {
                        WSJProperty collapseSrc = ((WSJObject)collapseOption).Value.FirstOrDefault();
                        field = entity.GetParam(childSource, collapseSrc.Key);
                        fieldValue = getMemberValue(entity, field, CFunc);
                        if (Validate(fieldValue, field, writer, serializer, childSource.BaseSchema, childSource, null, ref status, Request, CFunc))
                        {
                            if (fieldValue == null)
                            {
                                serializer.Serialize(writer, "NULL");
                                writer.Flush();
                            }
                            else if (fieldValue is WSEntity)
                            {
                                WSTableSource fieldSource = (WSTableSource)CFunc.GetSourceByType(fieldValue.GetType());
                                status = WriteCollapsedValues(writer, serializer,(WSEntity)fieldValue, fieldSource, collapseSrc.Value, Request, CFunc);
                            }
                            else if (fieldValue.IsCollectionOf<WSEntity>())
                            {
                                WSTableSource fieldSource = (WSTableSource)CFunc.GetSourceByType(fieldValue.GetType().GetEntityType());
                                if (!((IEnumerable<WSEntity>)fieldValue).Any())
                                {
                                    serializer.Serialize(writer, "NULL");
                                    writer.Flush();
                                }
                                else
                                {
                                    foreach (WSEntity eItem in (IEnumerable<WSEntity>)fieldValue)
                                    {
                                        status.childs.Add(WriteCollapsedValues(writer, serializer, eItem, fieldSource, collapseSrc.Value, Request, CFunc));
                                    }
                                    status = status.IsPositive ? WSStatus.SUCCESS_Copy() : WSStatus.ERROR_Copy();
                                }
                            }
                        }
                    }

                }


                /*}

                *******************************************************************************/

            }
            catch (Exception e) { CFunc.RegError(GetType(), e, ref status); }
            return status;
        }
        private object getMemberValue(WSEntity entity, WSParam eParam, MetaFunctions CFunc)
        {
            WSTableSource xSource = (WSTableSource)CFunc.GetSourceByType(entity.GetType());
            IEnumerable<MemberInfo> eMembers = xSource.ReturnType.GetMembers().Where(m => m is PropertyInfo || m is FieldInfo);
            MemberInfo eField = eMembers.FirstOrDefault(m => eParam.Match(m.Name));
            return eField is PropertyInfo ? ((PropertyInfo)eField).GetValue(entity, null) : eField is FieldInfo ? ((FieldInfo)eField).GetValue(entity) : null;
        }

        private bool validateType(JsonWriter writer, WSParam xParam, object obj, List<Type> printedTypes, bool ignoreCurcularity, WSRequest Request, MetaFunctions CFunc)
        {
            bool isValid = false;
            if (obj != null && Request.Security != null && Request.Security.IsValid && xParam != null)
            {
                byte uRole = Request.Security.IsLogged ? Request.Security.AuthToken.User.role : WSConstants.ACCESS_LEVEL.READ;
                int paramAccessLevel = xParam.READ_ACCESS_MODE.ACCESS_LEVEL;
                WSTableSource xSource = (WSTableSource)CFunc.GetSourceByType(obj.GetType());
                paramAccessLevel = (xSource != null && xSource.AccessLevel > paramAccessLevel) ? xSource.AccessLevel : paramAccessLevel;

                bool showCycledTypesMessage = uRole >= WSConstants.ACCESS_LEVEL.ADMIN && xSource != null && xSource.ShowMessageInaccessible;

                Type entityType = null;

                if (obj.IsCollection())
                {
                    if (obj.IsCollectionOf<WSEntity>()) { entityType = (obj as IList).GetEntityType(); }
                    else if (obj.IsCollectionOf<WSRecord>() && (obj as IList).Count > 0) { entityType = ((WSRecord)(obj as IList)[0]).entity.GetType(); }
                }
                else if (obj is WSRecord) { entityType = ((WSRecord)obj).entity.GetType(); }
                else if (obj is WSEntity) { entityType = obj.GetType(); }

                bool Ignore = ignoreCurcularity ? false : (printedTypes != null && printedTypes.Any(t => t == entityType));
                if (Ignore)
                {
                    if (showCycledTypesMessage)
                    {
                        WritePropName(writer, xParam.NAME);
                        writer.WriteValue("[unavailable : cycled references detected]");
                    }
                }
                else
                {
                    if (entityType.IsValidDynamicEntity())
                    {
                        WSSource src = CFunc.GetSourceByType(entityType);
                        isValid = src != null && uRole >= src.AccessLevel;
                    }
                    else { isValid = true; }
                }
            }
            return isValid;
        }

        private WSSource _xSource = null;
        public WSSource getSource(MetaFunctions CFunc)
        {
            if (_xSource == null)
            {
                _xSource = CFunc.GetSourceByType(GetType());
            }
            return _xSource;
        }

        private WSParam GetParam(WSSource xSource, string name, Type DefaultFieldType = null)
        {
            WSParam xParam = null;
            if (this is WSDynamicEntity && xSource != null)
            {
                xParam = xSource.GetXParam(name, DefaultFieldType);
            }
            else if (this is WSStaticEntity || this is WSV1ResponseEntity)
            {
                xParam = new WSParam(0, name, DefaultFieldType, null);
            }
            return xParam;
        }
    }
}