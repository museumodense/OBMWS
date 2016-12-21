using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

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
    public class WSDynamicEntity : WSEntity
    {
        private string _dbTableName = null;
        public string getDBTableName()
        {
            if (_dbTableName == null)
            {
                _dbTableName = "";
                _dbTableName = GetType().CustomAttribute<System.Data.Linq.Mapping.TableAttribute>(true).Name;
            }
            return _dbTableName;
        }
        public bool TrySetRecordValue(string colName, object newValue, WSDataContext DBContext, MetaFunctions CFunc, Func<Exception, bool> AddError = null)
        {
            bool done = false;
            if (!string.IsNullOrEmpty(colName))
            {
                PropertyInfo pInfo = GetType().GetProperty(colName);
                if (pInfo != null)
                {
                    object orgValue = pInfo.GetValue(this, null);
                    newValue = fixSpecialCaseValue(colName, newValue, pInfo.PropertyType);
                    try
                    {

                        if (
                            orgValue == newValue
                            || (orgValue == null && newValue == null)
                            || (orgValue != null && newValue != null && orgValue.ToString().Equals(newValue.ToString()))
                        ) { return true; }
                        else
                        {
                            if (newValue == null)
                            {
                                if (pInfo.PropertyType.IsNullable()) { pInfo.SetPropertyValue(this, null); done = true; }
                            }
                            else
                            {
                                object newValueConverted = pInfo.PropertyType.IsAssignableFrom(newValue.GetType()) ? newValue : null;
                                if (newValueConverted != null || pInfo.PropertyType.Read(newValue, out newValueConverted, null, null, pInfo.Name))
                                {
                                    try
                                    {
                                        bool IsAssiciation = false;
                                        PropertyInfo association = null;
                                        WSTableSource associationSrc = null;
                                        WSTableParam associationKey = null;
                                        PropertyInfo[] props = GetType().GetProperties();
                                        foreach(PropertyInfo prop in props)
                                        {
                                            IEnumerable<CustomAttributeData> cAttrs = prop.CustomAttributesData();
                                            foreach(CustomAttributeData cad in cAttrs)
                                            {
                                                CustomAttributeNamedArgument IsForeignKey = cad.NamedArguments.FirstOrDefault(x => x.MemberInfo.Name.Equals("IsForeignKey"));
                                                if (IsForeignKey != null && IsForeignKey.TypedValue.Value!=null && (true.ToString()).Equals(IsForeignKey.TypedValue.Value.ToString()))
                                                {
                                                    CustomAttributeNamedArgument cana = cad.NamedArguments.FirstOrDefault(x => x.MemberInfo.Name.Equals("ThisKey"));
                                                    if (cana != null && pInfo.Name.Equals(cana.TypedValue.Value == null ? null : cana.TypedValue.Value.ToString()))
                                                    {
                                                        CustomAttributeNamedArgument canaKey = cad.NamedArguments.FirstOrDefault(x => x.MemberInfo.Name.Equals("OtherKey"));
                                                        if (canaKey != null && canaKey.TypedValue.Value != null)
                                                        {
                                                            IsAssiciation = true;
                                                            association = prop;
                                                            associationSrc = (WSTableSource)CFunc.GetSourceByType(association.PropertyType);
                                                            associationKey = (WSTableParam)associationSrc.GetXParam(canaKey.TypedValue.Value.ToString());
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        if (IsAssiciation)
                                        {
                                            string pName = pInfo.Name;

                                            ParameterExpression paramExp = Expression.Parameter(association.PropertyType, "x");
                                            WSCombineFilter filter = new WSCombineFilter(WSCombineFilter.SQLMode.AndAlso);

                                            filter.Save(new WSJValue(newValueConverted.ToString()).GetFieldFilter(CFunc, associationKey, paramExp, 0));

                                            object subExpr = (Expression)filter.GetType().GetMethod("ToLambda").MakeGenericMethod(new Type[] { association.PropertyType }).Invoke(filter, new object[] { paramExp });

                                            MethodInfo mInfo = DBContext.GetType().GetMethod("GetTable", new Type[] { });

                                            var tObj = mInfo.MakeGenericMethod(new Type[] { association.PropertyType }).Invoke(DBContext, new object[] { });

                                            Func<WSDynamicEntity, bool> func = s => s.readPropertyValue(associationKey.WSColumnRef.NAME).ToString().Equals(newValueConverted.ToString());

                                            var method = typeof(Enumerable).GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
                                                .FirstOrDefault(m => m.Name == "FirstOrDefault" && m.GetParameters().Count() == 2).MakeGenericMethod(typeof(WSDynamicEntity));

                                            WSDynamicEntity newAssociation = (WSDynamicEntity)method.Invoke(null, new object[] { tObj, func });

                                            if (newAssociation != null) { association.SetPropertyValue(this, newAssociation); done = true; }
                                        }
                                        else { pInfo.SetPropertyValue(this, newValueConverted); done = true; }
                                    }
                                    catch (Exception e1) { AddError(e1); }
                                }
                            }
                        }
                    }
                    catch (Exception e) { AddError(e); }

                    if (!done) { pInfo.SetPropertyValue(this, orgValue); }
                }
            }
            return done;
        }
        private IQueryable<A> castTo<A>(ITable list) where A : WSDynamicEntity
        {
            return (IQueryable<A>)list;
        }
        private object fixSpecialCaseValue(string colName, object newValue, Type type)
        {
            switch (colName.ToLower())
            {
                case "deleted":
                    {
                        if (typeof(DateTime?).IsAssignableFrom(type))
                        {
                            if (WSConstants.ALIACES.TRUE.Match(newValue.ToString()))
                            {
                                newValue = DateTime.Now;
                            }
                            if (WSConstants.ALIACES.FALSE.Match(newValue.ToString()))
                            {
                                if (type.IsNullable()) { newValue = null; }
                                else { newValue = DateTime.MaxValue; }
                            }
                        }
                        break;
                    }
                default: break;
            }
            return newValue;
        }
        public bool TryReadRecordToDictionary(WSDataContext dc, out Dictionary<string, string> value, ref WSStatus statusLines)
        {
            value = new Dictionary<string, string>();
            bool ok = true;
            try
            {
                if (dc != null)
                {
                    var qFields = dc.PrimitiveProperties(GetType(), ref statusLines);

                    foreach (var field in qFields)
                    {
                        object pVal = null;
                        if (!TryReadPropertyValue(field.Name, out pVal)) { value = null; break; }
                        else
                        {
                            value.Save(
                                field.Name,
                                pVal == null ? "NULL" :
                                typeof(DateTime?).IsAssignableFrom(field.Type) ? ((DateTime)pVal).ToString(WSConstants.DATE_FORMAT) :
                                typeof(TimeSpan?).IsAssignableFrom(field.Type) ? ((TimeSpan)pVal).ToString(WSConstants.TIMESPAN_FORMAT_SIMPLE) :
                                pVal.ToString()
                            );
                        }
                    }
                }
            }
            catch (Exception) { ok = false; }
            return ok ? value != null && value.Any() : false;
        }

        public Dictionary<string,object> getIdentities(ClientFunctions CFunc)
        {
            if (_Identities == null)
            {
                _Identities = new Dictionary<string, object>();
                WSTableSource src = (WSTableSource)getSource(CFunc);
                IEnumerable <string> ids = src.PrimParams.Select(x=>x.WSColumnRef.NAME);
                if (ids == null || ids.FirstOrDefault() == null) {
                    _Identities = GetType().GetProperties().Where(p => ids.Contains(p.Name)).ToDictionary(x => x.Name, x => x.GetValue(this, null));
                }
            }
            return _Identities;
        }
        private Dictionary<string, object> _Identities = null;

        public WSDynamicEntity getRelatedParent<A>(ClientFunctions CFunc, ref WSStatus _statusLines, IEnumerable<Type> _refTypes = null)
        {
            WSDynamicEntity relEntity = null;
            try
            {
                WSTableSource orgSrc = (WSTableSource)getSource(CFunc);
                WSTableSource relSrc = (WSTableSource)CFunc.GetSourceByType(typeof(A));
                if (relSrc != null)
                {
                    WSTableParam refParam = orgSrc.DBParams.FirstOrDefault(x => x.DataType == relSrc.ReturnType);
                    if (refParam == null)
                    {
                        IEnumerable<Type> refTypes = _refTypes == null ? new List<Type>() : _refTypes.Select(x => x);
                        IEnumerable<WSTableParam> eParams = orgSrc.DBParams.Any() ? orgSrc.DBParams.Where(x => x.DataType.IsValidDynamicEntity() && !x.DataType.IsCollection() && !refTypes.Any(t => t == x.DataType)) : null;
                        if (eParams != null && eParams.Any())
                        {
                            foreach (WSTableParam eParam in eParams)
                            {
                                object pValue = null;
                                WSDynamicEntity pEntity = TryReadPropertyValue(eParam.WSColumnRef.NAME, out pValue, null) ? (WSDynamicEntity)pValue : null;

                                relEntity = pEntity.getRelatedParent<A>(CFunc, ref _statusLines, refTypes);

                                if (relEntity != null)
                                {
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        object PValue = null;
                        relEntity = TryReadPropertyValue(refParam.WSColumnRef.NAME, out PValue, null) ? (WSDynamicEntity)PValue : null;
                    }
                }
            }
            catch (Exception e) { CFunc.RegError(GetType(), e, ref _statusLines, $"getRelatedParent():211"); }
            return relEntity;
        }
        public bool IsRelationTo(ClientFunctions CFunc, WSDynamicEntity _RelationTEntity, ref WSStatus _statusLines, IEnumerable<Type> _refTypes = null)
        {
            bool _IsRelationTo = false;
            try
            {
                if (_RelationTEntity != null)
                {
                    WSTableSource orgSrc = (WSTableSource)getSource(CFunc);
                    WSTableSource relSrc = (WSTableSource)_RelationTEntity.getSource(CFunc);
                    if (relSrc != null)
                    {
                        WSTableParam refParam = orgSrc.DBParams.FirstOrDefault(x => x.DataType == relSrc.ReturnType);
                        if (refParam == null)
                        {
                            IEnumerable<Type> refTypes = _refTypes == null ? new List<Type>() : _refTypes.Select(x => x);
                            IEnumerable<WSTableParam> eParams = orgSrc.DBParams.Any() ? orgSrc.DBParams.Where(x => x.DataType.IsValidDynamicEntity() && !x.DataType.IsCollection() && !refTypes.Any(t => t == x.DataType)) : null;
                            if (eParams != null && eParams.Any())
                            {
                                foreach (WSTableParam eParam in eParams)
                                {
                                    object pValue = null;
                                    WSDynamicEntity pEntity = TryReadPropertyValue(eParam.WSColumnRef.NAME, out pValue, null) ? (WSDynamicEntity)pValue : null;

                                    _IsRelationTo = pEntity.IsRelationTo(CFunc, _RelationTEntity, ref _statusLines, refTypes);

                                    if (_IsRelationTo)
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            object PValue = null;
                            WSDynamicEntity RefEntity = TryReadPropertyValue(refParam.WSColumnRef.NAME, out PValue, null) ? (WSDynamicEntity)PValue : null;

                            _IsRelationTo = RefEntity != null && _RelationTEntity.Match(CFunc, RefEntity);
                        }
                    }
                }
            }
            catch (Exception e) { CFunc.RegError(GetType(), e, ref _statusLines, $"IsRelationTo():256"); }
            return _IsRelationTo;
        }
        private List<dynamic> readEntity(MetaFunctions CFunc, WSJProperty jProp, WSDynamicEntity _entity, bool multydimentional = false)
        {
            try
            {
                if (_entity != null)
                {
                    WSTableSource src = (WSTableSource)_entity.getSource(CFunc);
                    WSTableParam param = src.DBParams.FirstOrDefault(p => p.Match(jProp.Key));
                    PropertyInfo pInfo = src.ReturnType.GetProperties().FirstOrDefault(p => p.Name.Equals(param.WSColumnRef.NAME));
                    dynamic val = pInfo.GetValue(_entity, null);
                    Type pType = pInfo.PropertyType.GetEntityType();
                    if (pInfo.PropertyType.IsCollectionOf<WSDynamicEntity>())
                    {
                        IEnumerable<WSDynamicEntity> entities = (IEnumerable<WSDynamicEntity>)val;
                        List<dynamic> subItems = new List<dynamic>();
                        foreach (WSDynamicEntity iEntity in entities)
                        {
                            subItems.AddRange(read(CFunc, jProp.Value, iEntity, multydimentional));
                        }
                        return subItems;
                    }
                    else { return read(CFunc, jProp.Value, val, multydimentional); }
                }
            }
            catch (Exception e) { WSStatus status = WSStatus.NONE.clone(); CFunc.RegError(GetType(), e, ref status, $"readEntity():284"); }
            return null;
        }

        private bool Match(ClientFunctions CFunc, WSDynamicEntity refEntity)
        {
            try
            {
                if (refEntity == null) { return false; }
                else if (refEntity.GetType() != GetType()) { return false; }
                else
                {
                    Type orgType = GetType();
                    WSTableSource orgSrc = (WSTableSource)getSource(CFunc);
                    IEnumerable<WSTableParam> orgParams = orgSrc.DBParams.Where(p => p.DataType.IsSimple());

                    Type refType = refEntity.GetType();
                    WSTableSource refSrc = ((WSTableSource)CFunc.GetSourceByType(refType));
                    IEnumerable<WSTableParam> refParams = refSrc.DBParams.Where(p => p.DataType.IsSimple());

                    IEnumerable<WSTableParam> orgExceptParams = orgParams.Where(p1 => !refParams.Any(p2 => p2.Match(p1)));
                    IEnumerable<WSTableParam> refExceptParams = refParams.Where(p1 => !orgParams.Any(p2 => p2.Match(p1)));

                    if (orgExceptParams.Any() || refExceptParams.Any()) { return false; }
                    else
                    {
                        foreach (WSTableParam param in orgParams)
                        {
                            object orgInfo = orgType.GetProperties().FirstOrDefault(p => p.Name.Equals(param.WSColumnRef.NAME)).GetValue(this, null);
                            object refInfo = refType.GetProperties().FirstOrDefault(p => p.Name.Equals(param.WSColumnRef.NAME)).GetValue(refEntity, null);
                            if (!(orgInfo == null && refInfo == null) && !orgInfo.ToString().Equals(refInfo.ToString())) return false;
                        }
                        return true;
                    }
                }
            }
            catch (Exception e) { WSStatus status = WSStatus.NONE.clone(); CFunc.RegError(GetType(), e, ref status, $"Match():321"); }
            return false;
        }
        
        public List<dynamic> read(MetaFunctions CFunc, WSJson json, WSDynamicEntity _entity = null, bool multydimentional = false)
        {
            List<dynamic> result = new List<dynamic>();
            try
            {
                _entity = _entity == null ? this : _entity;
                if (json is WSJValue) { dynamic val = readPrimitive(CFunc, (WSJValue)json, _entity); if (val != null) { result.Add(val); } }
                else if (json is WSJArray)
                {
                    foreach (WSJson item in ((WSJArray)json).Value)
                    {
                        List<dynamic> val = read(CFunc, item, _entity, multydimentional);
                        if (multydimentional) { result.Add(val); }
                        else { result.AddRange(val); }
                    }
                }
                else if (json is WSJObject)
                {
                    List<dynamic> val = readEntity(CFunc, ((WSJObject)json).Value[0], _entity, multydimentional);
                    if (val != null && val.Any())
                    {
                        if (multydimentional)
                        {
                            result.Add(val);
                        }
                        else
                        {
                            result.AddRange(val);
                        }
                    }
                }
            }
            catch (Exception e) { WSStatus status = WSStatus.NONE.clone(); CFunc.RegError(GetType(), e, ref status, $"read():357"); }
            return result;
        }
        private dynamic readPrimitive(MetaFunctions CFunc, WSJValue jVal, WSDynamicEntity _entity)
        {
            try
            {
                if (_entity != null)
                {
                    Type eType = _entity.GetType();
                    WSTableSource src = ((WSTableSource)CFunc.GetSourceByType(eType));
                    WSTableParam param = src.DBParams.FirstOrDefault(p => p.Match(jVal.Value));
                    PropertyInfo pInfo = eType.GetProperties().FirstOrDefault(p => p.Name.Equals(param.WSColumnRef.NAME));
                    return pInfo.GetValue(_entity, null);
                }
            }
            catch (Exception e) { WSStatus status = WSStatus.NONE.clone(); CFunc.RegError(GetType(), e, ref status, $"readPrimitive():373"); }
            return null;
        }
        
        public override bool Equals(object obj)
        {
            try
            {
                if (obj == null) return false;
                if (obj.GetType() != GetType()) return false;
                WSTableSource eSchema = WSServerMeta.SYSTEM_SOURCES.GetSourceByType(GetType());
                if (eSchema == null) return base.Equals(obj);
                else
                {
                    WSDynamicEntity eObj = (WSDynamicEntity)obj;
                    foreach (WSTableParam param in eSchema.DBPrimitiveParams)
                    {
                        object p1 = readPropertyValue(param.WSColumnRef.NAME);
                        object p2 = eObj.readPropertyValue(param.WSColumnRef.NAME);
                        if (p1 != null)
                        {
                            if (!p1.Equals(p2)) return false;
                        }
                        else if (p2 != null) { return false; }
                    }
                }
            }
            catch (Exception) { return false; }
            return true;
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Match(WSRequest Request, WSDataContext DBContext, MetaFunctions CFunc, WSSchema schema)
        {
            bool ok = false;
            try
            {
                WSEntitySchema eSchema = null;
                if (schema is WSEntitySchema){eSchema = (WSEntitySchema)schema;}
                else if (schema is WSEntityListSchema){eSchema = ((WSEntityListSchema)schema).EntitySchema;}

                if (eSchema != null)
                {
                    bool baseFilterMatch = true;
                    if (eSchema.Source.BaseFilter != null && eSchema.Source.BaseFilter.IsValid && eSchema.Source.BaseFilter.apply(Request, CFunc)) { baseFilterMatch = eSchema.Source.BaseFilter.MatchEntity(CFunc, this, ((WSTableSource)CFunc.GetSourceByType(GetType()))); }

                    bool dynamicFilterMatch = true;
                    if (eSchema.Fields != null || eSchema.Fields != null) {
                        dynamicFilterMatch = MatchFields(Request, CFunc, eSchema) && MatchFilters(Request, CFunc, eSchema);
                    }

                    ok = baseFilterMatch && dynamicFilterMatch;
                }
            }
            catch (Exception e) {
                if (Request != null) { CFunc.RegError(GetType(), e, ref Request.status, $"Match():434"); }
                else { WSStatus status = WSStatus.NONE.clone(); CFunc.RegError(GetType(), e, ref status, $"Match():435"); }
            }
            return ok;
        }

        private bool MatchFilters(WSRequest request, MetaFunctions cFunc, WSEntitySchema eSchema)
        {
            bool match = true;
            try
            {
                //eSchema.Filters.Match(this);
            }
            catch (Exception) { }
            return match;
        }

        private bool MatchFields(WSRequest request, MetaFunctions cFunc, WSEntitySchema eSchema)
        {
            bool match = true;
            try
            {

            }
            catch (Exception) { }
            return match;
        }
    }
}