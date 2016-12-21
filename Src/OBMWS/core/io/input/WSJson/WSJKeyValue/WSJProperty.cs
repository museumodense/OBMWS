using OBMWS.security;
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
    public class WSJProperty : WSJKeyValue<WSJson>
    {
        public WSJProperty(string _key, WSJson _value) { Key = _key; Value = _value; }

        public override bool IsValid { get { return !string.IsNullOrEmpty(Key); } }
        public override bool IsEmpty { get { return !IsValid || Value == null || Value.IsEmpty; } }
        public override string ToString() { return JString; }
        public override bool Equals(object obj) { if (obj == null || obj.GetType() != typeof(WSJProperty) || ((WSJProperty)obj).GetHashCode() != GetHashCode()) return false; return true; }
        public override int GetHashCode() { return JString.GetHashCode(); }

        public override string JString { get { return "\"" + Key + "\":" + (Value == null ? "NULL" : Value.JString); } }

        public override string NiceUrlString { get { return Key + ":" + (Value == null ? "NULL" : Value.NiceUrlString); } }

        public override WSFilter GetFieldFilter(MetaFunctions CFunc, WSTableParam param, Expression parent, int level, string state = null, bool? negate = null)
        {
            WSCombineFilter filter = new WSCombineFilter(WSCombineFilter.SQLMode.AndAlso);
            try
            {
                if (param != null && param.isValid && (param.DataType.IsNullable() || Value != null))
                {

                    if (param.DataType.IsSimple())
                    {
                        bool localNegate = WSConstants.ALIACES.NOT.Match(state);
                        negate = negate == null ? localNegate : localNegate != negate;
                        WSFilter pFilter = Value.GetFieldFilter(CFunc, param, parent, level, Key, negate);
                        filter.Save(pFilter);
                    }
                    else if (param.DataType.IsSameOrSubclassOf(typeof(WSEntity)) || param.DataType.IsCollectionOf<WSEntity>())
                    {
                        if (WSConstants.ALIACES.NOT.Match(Key))
                        {
                            return Value.GetFieldFilter(CFunc, param, parent, level, state, true);
                        }
                        else if (WSConstants.ALIACES.ANY.Match(Key))
                        {
                            return Value.GetFieldFilter(CFunc, param, parent, level, Key, true);
                        }
                        else
                        {
                            WSTableSource PSource = (WSTableSource)CFunc.GetSourceByType/*<WSTableSource>*/(param.DataType.GetEntityType());

                            WSTableParam subParam = PSource == null ? null : (WSTableParam)PSource.GetXParam(Key);
                            Expression member = Expression.Property(parent, param.WSColumnRef.NAME);
                            if (subParam == null && WSConstants.SPECIAL_CASES.Any(c => c.Match(Key)))
                            {
                                if (param.DataType.IsSameOrSubclassOf(typeof(WSEntity))) { return new WSEntityFFilter(param, member, WSEntityFFilter.OPERATIONS.STATE_OPERATIONS.FirstOrDefault(x => x.Match(Key))) { Value = null }; }
                                else if (param.DataType.IsCollectionOf<WSEntity>()) { return new WSEntityListFFilter(param, member, WSEntityListFFilter.OPERATIONS.STATE_OPERATIONS.FirstOrDefault(x => x.Match(Key))) { Value = null }; }
                            }
                            else
                            {
                                WSFilter subFilter = null;
                                if (param.DataType.IsSameOrSubclassOf(typeof(WSEntity)))
                                {
                                    filter.Save(new WSEntityFFilter(param, member, WSEntityFFilter.OPERATIONS.NotEqual) { Value = null });

                                    subFilter = Value.GetFieldFilter(CFunc, subParam, member, level, Key);
                                    if (subFilter != null && subFilter.IsValid)
                                    {
                                        filter.Save(new WSEntityFFilter(param, member, WSEntityFFilter.OPERATIONS.Filter) { Value = subFilter });
                                    }
                                }
                                else if (param.DataType.IsCollectionOf<WSEntity>())
                                {
                                    level++;

                                    Type elemType = param.DataType.GetEntityType();
                                    ParameterExpression id = Expression.Parameter(elemType, level.ToHex());
                                    subFilter = Value.GetFieldFilter(CFunc, subParam, id, level, Key, negate);
                                    if (subFilter != null && subFilter.IsValid)
                                    {
                                        dynamic subExpr = subFilter.GetType().GetMethod("ToLambda").MakeGenericMethod(new Type[] { elemType }).Invoke(subFilter, new object[] { id });

                                        filter.Save(new WSEntityListFFilter(subParam, member, WSEntityListFFilter.OPERATIONS.Any) { Value = (subExpr == null) ? true : subExpr });
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e) { WSStatus status = WSStatus.NONE.clone(); CFunc.RegError(GetType(), e, ref status); }
            return filter.Any() ? (filter.Count == 1 && !filter.Negate) ? filter.FirstOrDefault() : filter : null;
        }

        public override Expression SortTable<TEntity>(MetaFunctions CFunc, WSDataContext dc, List<PropertyInfo> parents, Expression expression, ref WSStatus iostatus)
        {
            try
            {
                if (dc != null)
                {
                    parents = parents != null ? parents : new List<PropertyInfo>();
                    Type srcType = parents.Any() ? parents.LastOrDefault().PropertyType.IsCollection() ? parents.LastOrDefault().PropertyType.GetEntityType() : parents.LastOrDefault().PropertyType : typeof(TEntity);
                    ITable initSource = srcType == null ? null : dc.GetTable(typeof(TEntity));
                    ITable source = srcType == null ? null : dc.GetTable(srcType);
                    WSTableSource schema = srcType == null ? null : (WSTableSource)CFunc.GetSourceByType(srcType);
                    if (schema != null)
                    {
                        WSParam param = schema.GetXParam(Key);

                        if (param != null && param is WSTableParam)
                        {
                            WSTableParam tParam = (WSTableParam)param;
                            PropertyInfo property = srcType.GetProperties().FirstOrDefault(p => tParam.WSColumnRef.NAME.Equals(p.Name));
                            if (property == null) { iostatus.AddNote(string.Format("No PropertyInfo found for : [{0}]",tParam.WSColumnRef.NAME)); }
                            else
                            {
                                parents.Add(property);

                                if (tParam.DataType.IsSimple() && tParam.IsSortable)
                                {
                                    bool IsDesc = false;
                                    if (Value is WSJValue) { IsDesc = ((WSJValue)Value).Value.ToLower().Equals("desc"); }
                                    expression = SortPrimitiveType<TEntity>(initSource, source, param, IsDesc, parents, expression, ref iostatus);
                                }
                                else if (tParam.DataType.IsSameOrSubclassOf(typeof(WSEntity)) || tParam.DataType.IsCollectionOf<WSEntity>())
                                {
                                    expression = Value.SortTable<TEntity>(CFunc, dc, parents, expression, ref iostatus);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e) { CFunc.RegError(GetType(), e, ref iostatus); }
            return expression;
        }
        public override WSJson Clone() { return new WSJProperty(Key, Value); }
        internal override bool applyInternal(WSRequest Request, MetaFunctions CFunc)
        {
            try
            {
                if (Value!=null && Value.IsValid)
                {
                    if (Value is WSJObject)
                    {
                        Func<WSJProperty, bool> READFunc = v => v.Key.StartsWith("$") && WSConstants.COMMAND_KEYS.READ.Match(v.Key.TrimStart(new char[] { '$' }));
                        
                        WSJProperty READProperty = ((WSJObject)Value).Value.FirstOrDefault(READFunc);

                        if (READProperty != null)
                        {
                            string commandKey = READProperty.Key.TrimStart(new char[] { '$' });
                            WSJson temp = new WSJArray();


                            #region MATCH / READ properties
                            //if (prop.Value is WSJObject && prop.Value.IsValid)
                            //{
                            //    #region apply $match command
                            //    if (WSConstants.COMMAND_KEYS.MATCH.Match(commandKey))
                            //    {
                            //        WSJProperty jMatch = ((WSJObject)prop.Value).Value[0];

                            //        #region SET $currentuser validation Filter
                            //        if (WSConstants.COMMAND_KEYS.CURRENT_USER.Match(jMatch.Key))
                            //        {
                            //            WSJson jUser = jMatch.Value.Clone();
                            //            if (jUser != null && jUser.IsValid)
                            //            {
                            //                Value[i] = new WSJValue(Request.Security.IsValidUser(jUser) ? "1" : "0");
                            //            }
                            //        }
                            //        #endregion
                            //    }
                            //    #endregion

                            //    #region apply $read command
                            //    else if (WSConstants.COMMAND_KEYS.READ.Match(commandKey))
                            //    {
                            //        WSJProperty jTarget = ((WSJObject)prop.Value).Value[0];
                            //        string targetKey = jTarget.Key.TrimStart(new char[] { '$' });

                            //        #region SET $currentuser validation Filter
                            //        if (WSConstants.COMMAND_KEYS.CURRENT_USER.Match(targetKey))
                            //        {
                            //            List<WSJson> items = new List<WSJson> { };
                            //            try
                            //            {
                            //                items.AddRange(
                            //                    (Request.Security.WSCurrentUser != null && Request.Security.WSCurrentUser.entity != null ? Request.Security.WSCurrentUser.entity.read(CFunc, jTarget.Value) : new List<dynamic> { })
                            //                    .Select(x =>
                            //                        new WSJValue((x as object).ToString())
                            //                    )
                            //                );
                            //            }
                            //            catch (Exception e) { CFunc.RegError(GetType(), e, ref Request.status); }
                            //            Value[i] = new WSJArray(items);
                            //        }
                            //        #endregion
                            //    }
                            //    #endregion

                            //    continue;
                            //}
                            #endregion


                            #region apply 'READ' property
                            if (WSConstants.COMMAND_KEYS.READ.Match(commandKey) && READProperty.Value.IsValid)
                            {
                                if (READProperty.Value is WSJObject)
                                {
                                    List<WSJProperty> props = ((WSJObject)READProperty.Value).Value;
                                    if (props != null && props.Any())
                                    {
                                        Func<WSJProperty, bool> CurUserFunc = v => WSConstants.COMMAND_KEYS.CURRENT_USER.Match(v.Key);
                                        Func<WSJProperty, bool> ExplicitFunc = v => WSConstants.COMMAND_KEYS.EXPLICIT.Match(v.Key);

                                        if (props.Any(CurUserFunc))
                                        {
                                            #region SET $currentuser validation Filter
                                            WSJProperty CurrentUser = props.FirstOrDefault(CurUserFunc);
                                            if (CurrentUser != null/* && WSConstants.COMMAND_KEYS.CURRENT_USER.Match(CurrentUser.Key)*/)
                                            {
                                                List<WSJson> items = new List<WSJson> { };
                                                try
                                                {
                                                    items.AddRange(
                                                        (Request.Security.WSCurrentUser != null && Request.Security.WSCurrentUser.entity != null ? Request.Security.WSCurrentUser.entity.read(CFunc, CurrentUser.Value) : new List<dynamic> { })
                                                        .Select(x =>
                                                            new WSJValue((x as object).ToString())
                                                        )
                                                    );
                                                }
                                                catch (Exception e) { CFunc.RegError(GetType(), e, ref Request.status); }
                                                temp = new WSJArray(items);

                                            }
                                            #endregion
                                        }
                                    }
                                }
                            }
                            #endregion


                            Value = temp;
                            return true;
                        }
                    }
                }
                Value.apply(Request, CFunc);
            }
            catch (Exception e) { CFunc.RegError(GetType(), e, ref Request.status); }
            return false;
        }
        
        public override WSFilter GetOptionFilter(MetaFunctions CFunc, Expression parent, int level, string state = null, bool? negate = null)
        {
            WSCombineFilter filter = new WSCombineFilter(WSCombineFilter.SQLMode.AndAlso);
            try
            {
                if (Value != null)
                {
                    if (WSConstants.ALIACES.NOT.Match(Key))
                    {
                        return Value.GetOptionFilter(CFunc, parent, level, state, true);
                    }
                    else if (WSConstants.ALIACES.ANY.Match(Key))
                    {
                        return Value.GetOptionFilter(CFunc, parent, level, Key, true);
                    }
                    else
                    {
                        if (Value is WSJValue)
                        {
                            filter.Save(((WSJValue)Value).GetOptionFilter(CFunc, parent, level, state, negate));
                        }
                        else
                        {
                            WSTableSource PSource = (WSTableSource)CFunc.GetSourceByType(parent.Type.GetEntityType());
                            WSTableParam subParam = PSource == null ? null : (WSTableParam)PSource.GetXParam(Key);
                            
                            //TODO@ANDVO:2016-11-15: implement deep filtering
                        }
                        return filter.Any() ? (filter.Count == 1 && !filter.Negate) ? filter.FirstOrDefault() : filter : null;
                    }
                }
            }
            catch (Exception e) { WSStatus status = WSStatus.NONE.clone(); CFunc.RegError(GetType(), e, ref status); }
            return null;
        }

        public override bool Match(WSJson json, out WSStatus status)
        {
            status = WSStatus.NONE_Copy();
            try
            {
                if (json == null) { status = WSStatus.ERROR_Copy(); return false; }
                else if (!(json is WSJArray)) { status = WSStatus.ERROR_Copy(); return false; }
                else
                {
                    WSJProperty jProp1 = (WSJProperty)json;
                    if (!Key.Equals(jProp1.Key)){
                        status = WSStatus.ERROR_Copy();
                        status.AddNote($"Keys not match:[{Key}<->{jProp1.Key}]");
                    }
                    else
                    {
                        status = Value.Match(jProp1.Value, out status) ? WSStatus.SUCCESS_Copy() : status;
                    }
                }
            }
            catch (Exception) { }
            return status.CODE == WSStatus.SUCCESS.CODE;
        }

        public override bool MatchEntity(MetaFunctions CFunc, WSDynamicEntity entity, WSTableSource src, string key = null, string matchOperation = null)
        {
            bool isMatch = true;
            try
            {
                if (!IsValid) { isMatch = false; }
                else
                {
                    if (string.IsNullOrEmpty(key)) { key = Key; }
                    else { matchOperation = Key; }

                    if (Value is WSJValue) { isMatch = ((WSJValue)Value).MatchEntity(CFunc, entity, src, key, matchOperation); }
                    else
                    {
                        if (src.DBAssociationParams.Any(p => p.Match(Key)))
                        {
                            WSTableParam param = src.DBAssociationParams.FirstOrDefault(p => p.Match(Key));
                            src = (WSTableSource)CFunc.GetSourceByType(param.DataType.GetEntityType());
                            object oEntity = entity.GetType().GetProperty(param.WSColumnRef.NAME).GetValue(entity, null);
                            if (oEntity.GetType().IsCollectionOf<WSDynamicEntity>())
                            {
                                foreach (WSDynamicEntity e in (IEnumerable<WSDynamicEntity>)oEntity)
                                {
                                    if (Value is WSJObject) { if (((WSJObject)Value).MatchEntity(CFunc, e, src)) isMatch = true; }
                                    else if (Value is WSJArray) { if (((WSJArray)Value).MatchEntity(CFunc, e, src)) isMatch = true; }
                                }
                            }
                            else
                            {
                                if (Value is WSJObject) { isMatch = ((WSJObject)Value).MatchEntity(CFunc, (WSDynamicEntity)oEntity, src); }
                                else if (Value is WSJArray) { isMatch = ((WSJArray)Value).MatchEntity(CFunc, (WSDynamicEntity)oEntity, src); }
                            }
                        }
                        else
                        {
                            if (Value is WSJObject) { isMatch = ((WSJObject)Value).MatchEntity(CFunc, entity, src, key); }
                            else if (Value is WSJArray) { isMatch = ((WSJArray)Value).MatchEntity(CFunc, entity, src, key); }
                        }
                    }
                }
            }
            catch (Exception e) { isMatch = false; WSStatus status = WSStatus.NONE.clone(); CFunc.RegError(GetType(), e, ref status); }
            return isMatch;
        }
    }
}