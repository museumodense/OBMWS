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
    public class WSJValue : WSJVal<string>
    {
        public WSJValue(string _value) { Value = _value; }

        public override bool IsValid { get { return true; } }
        public override bool IsEmpty { get { return string.IsNullOrEmpty(Value); } }

        public override string ToString() { return JString; }
        public override bool Equals(object obj) { if (obj == null || obj.GetType() != typeof(WSJValue) || ((WSJValue)obj).GetHashCode() != GetHashCode()) return false; return true; }
        public override int GetHashCode() { return JString.GetHashCode(); }

        public override string JString { get { return Value == null ? "NULL" : "\"" + Value + "\""; } }
        
        public override string NiceUrlString { get { return Value == null ? "NULL" : Value; } }

        public override WSFilter GetOptionFilter(MetaFunctions CFunc, Expression parent, int level, string state = null, bool? negate = null)
        {
            try
            {
                if (Value.IsTrue() || Value.IsFalse()) { return new WSBoolOFilter(this); }
                else if (WSConstants.SPECIAL_CASES.Any(x => x.Match(Value)))
                {
                    state = Value;
                    if (parent.Type.IsSameOrSubclassOf(typeof(WSEntity)) || parent.Type.IsCollectionOf<WSEntity>())
                    {
                        WSTableSource Source = (WSTableSource)CFunc.GetSourceByType/*<WSTableSource>*/(parent.Type);
                        if (parent.Type.IsSameOrSubclassOf(typeof(WSEntity)) && WSEntityFFilter.OPERATIONS.STATE_OPERATIONS.Any(x => x.Match(state)))
                        {
                            return new WSEntityFilter(Source, parent, WSEntityFFilter.OPERATIONS.STATE_OPERATIONS.FirstOrDefault(x => x.Match(state)));
                        }
                    }
                }
            }
            catch (Exception e) { WSStatus status = WSStatus.NONE.clone(); CFunc.RegError(GetType(), e, ref status); }
            return null;
        }
        
        public override WSFilter GetFieldFilter(MetaFunctions CFunc, WSTableParam param, Expression parent, int level, string state = null, bool? negate = null)
        {
            Expression member = Expression.Property(parent, param.WSColumnRef.NAME);
            WSCombineFilter filter = new WSCombineFilter();
            if (param != null && param.isValid)
            {
                try
                {
                    if (WSConstants.ALIACES.EXIST.Match(Value) || WSConstants.ALIACES.EMPTY.Match(Value) || WSConstants.ALIACES.IS_OWN.Match(Value))
                    {
                        state = Value;
                    }

                    #region Read WSEntity (Ready)
                    if (!string.IsNullOrEmpty(state) && (param.DataType.IsSameOrSubclassOf(typeof(WSEntity)) || param.DataType.IsCollectionOf<WSEntity>()))
                    {
                        if (param.DataType.IsSameOrSubclassOf(typeof(WSEntity)) && WSEntityFFilter.OPERATIONS.STATE_OPERATIONS.Any(x => x.Match(state)))
                        {
                            filter.Save(new WSEntityFFilter(param, member, WSEntityFFilter.OPERATIONS.STATE_OPERATIONS.FirstOrDefault(x => x.Match(state))) { Value = null });
                        }
                        else if (param.DataType.IsCollectionOf<WSEntity>() && WSEntityListFFilter.OPERATIONS.STATE_OPERATIONS.Any(x => x.Match(state)))
                        {
                            filter.Save(new WSEntityListFFilter(param, member, WSEntityListFFilter.OPERATIONS.STATE_OPERATIONS.FirstOrDefault(x => x.Match(state))) { Value = null });
                        }
                    }
                    #endregion

                    #region Read primitive 
                    else
                    {
                        dynamic dVal = null;

                        #region Numeric (Ready)
                        if (param.DataType.IsNumeric())
                        {
                            #region SPECIAL CASES
                            #region If Exists (Ready)
                            if (WSConstants.ALIACES.EXIST.Match(Value) && param.DataType.IsNullable())
                            {
                                filter.Save(new WSNumericFFilter(param, member, WSNumericFFilter.OPERATIONS.NotEqual) { Value = null });
                            }
                            #endregion
                            #region If Empty (Ready)
                            else if (WSConstants.ALIACES.EMPTY.Match(Value) && param.DataType.IsNullable())
                            {
                                filter.Save(new WSNumericFFilter(param, member, null) { Value = null });
                            }
                            #endregion
                            #endregion

                            #region If Value (Ready)
                            else
                            {
                                if (param.DataType.Read(Value, out dVal))
                                {
                                    Type dType = dVal == null ? param.DataType : dVal.GetType();
                                    WSOperation operation = WSNumericFFilter.OPERATIONS.GetOperation(state);

                                    if (operation.Match("max") && dType.IsCollection()) { dVal = ((List<dynamic>)dVal).Max(x => x); }
                                    else if (operation.Match("min") && dType.IsCollection()) { dVal = ((List<dynamic>)dVal).Min(x => x); }

                                    filter.Save(new WSNumericFFilter(param, member, operation) { Value = dVal });
                                }
                            }
                            #endregion
                        }
                        #endregion
                        #region bool (Ready)
                        else if (typeof(bool?).IsAssignableFrom(param.DataType))
                        {
                            #region SPECIAL CASES
                            #region If Exists (Ready)
                            if (WSConstants.ALIACES.EXIST.Match(Value) && param.DataType.IsNullable())
                            {
                                filter.Save(new WSBoolFFilter(param, member, WSBoolFFilter.OPERATIONS.NotEqual) { Value = null });
                            }
                            #endregion
                            #region If Empty (Ready)
                            else if (WSConstants.ALIACES.EMPTY.Match(Value) && param.DataType.IsNullable())
                            {
                                filter.Save(new WSBoolFFilter(param, member, null) { Value = null });
                            }
                            #endregion
                            #endregion

                            #region If Value (Ready)
                            else
                            {
                                if (param.DataType.Read(Value, out dVal))
                                {
                                    filter.Save(new WSBoolFFilter(param, member, WSBoolFFilter.OPERATIONS.GetOperation(state)) { Value = dVal });
                                }
                            }
                            #endregion
                        }
                        #endregion
                        #region string (Ready)
                        else if (typeof(string).IsAssignableFrom(param.DataType))
                        {
                            #region SPECIAL CASES
                            #region If Exists (Ready)
                            if (WSConstants.ALIACES.EXIST.Match(Value))
                            {
                                if (param.DataType.IsNullable()) filter.Save(new WSStringFFilter(param, member, WSStringFFilter.OPERATIONS.NotEqual) { Value = null });
                                filter.Save(new WSStringFFilter(param, member, WSStringFFilter.OPERATIONS.NotEqual) { Value = string.Empty });
                            }
                            #endregion
                            #region If Empty (Ready)
                            else if (WSConstants.ALIACES.EMPTY.Match(Value))
                            {
                                WSCombineFilter cFilter = new WSCombineFilter(WSCombineFilter.SQLMode.Or); ;
                                if (param.DataType.IsNullable()) cFilter.Save(new WSStringFFilter(param, member, null) { Value = null });
                                cFilter.Save(new WSStringFFilter(param, member, null) { Value = string.Empty });
                                filter.Save(cFilter);
                            }
                            #endregion
                            #endregion

                            #region If Value (Ready)
                            else if (param.DataType.IsNullable() || !string.IsNullOrEmpty(Value))
                            {
                                if (param.DataType.Read(Value, out dVal))
                                {
                                    filter.Save(new WSStringFFilter(param, member, WSStringFFilter.OPERATIONS.GetOperation(state)) { Value = dVal });
                                }
                            }
                            #endregion
                        }
                        #endregion
                        #region Guid (Ready)
                        else if (typeof(Guid?).IsAssignableFrom(param.DataType))
                        {
                            #region SPECIAL CASES
                            #region If Exists (Ready)
                            if (WSConstants.ALIACES.EXIST.Match(Value) && param.DataType.IsNullable())
                            {
                                filter.Save(new WSGuidFFilter(param, member, WSGuidFFilter.OPERATIONS.NotEqual) { Value = null });
                            }
                            #endregion
                            #region If Empty (Ready)
                            else if (WSConstants.ALIACES.EMPTY.Match(Value) && param.DataType.IsNullable())
                            {
                                filter.Save(new WSGuidFFilter(param, member, null) { Value = null });
                            }
                            #endregion
                            #endregion

                            #region If Value (Ready)
                            else
                            {
                                if (param.DataType.Read(Value, out dVal, null, WSConstants.GUID_LIST_SEPARATORS))
                                {
                                    filter.Save(new WSGuidFFilter(param, member, WSGuidFFilter.OPERATIONS.GetOperation(state)) { Value = dVal });
                                }
                            }
                            #endregion
                        }
                        #endregion
                        #region DateTime (Ready)
                        else if (typeof(DateTime?).IsAssignableFrom(param.DataType))
                        {
                            #region SPECIAL CASES
                            #region If Exists (Ready)
                            if (WSConstants.ALIACES.EXIST.Match(Value) && param.DataType.IsNullable())
                            {
                                filter.Save(new WSDateFFilter(param, member, WSDateFFilter.OPERATIONS.NotEqual) { Value = null });
                            }
                            #endregion
                            #region If Empty (Ready)
                            else if (WSConstants.ALIACES.EMPTY.Match(Value) && param.DataType.IsNullable())
                            {
                                filter.Save(new WSDateFFilter(param, member, null) { Value = null });
                            }
                            #endregion
                            #endregion

                            #region If Value (Ready)
                            else
                            {
                                WSOperation operation =
                                    Value.IsTrue() ?
                                    WSDateFFilter.OPERATIONS.LessOrEqual :
                                    (Value.IsFalse() && !(param.DataType.IsNullable())) ?
                                    WSDateFFilter.OPERATIONS.GreaterThanOrEqual :
                                    WSDateFFilter.OPERATIONS.GetOperation(state);
                                WSDateFFilter mainFilter = null;
                                if (operation != null && operation.Match(WSDateFFilter.OPERATIONS.WeekDayEqual))
                                {
                                    mainFilter = new WSDateFFilter(param, member, operation) { Value = Value };
                                }
                                else if (param.DataType.Read(Value, out dVal, new char[] { }, WSConstants.DATE_LIST_SEPARATORS, param.WSColumnRef.NAME))
                                {
                                    Type dType = dVal == null ? param.DataType : dVal.GetType();
                                    if (dType.IsCollection() && operation != null)
                                    {
                                        dVal = operation.Match(WSDateFFilter.OPERATIONS.LessOrEqual) ? ((List<dynamic>)dVal).Max(x => x) : operation.Match(WSDateFFilter.OPERATIONS.GreaterThanOrEqual) ? ((List<dynamic>)dVal).Min(x => x) : dVal;
                                    }
                                    mainFilter = new WSDateFFilter(param, member, operation) { Value = dVal };
                                }

                                if (param.DataType.IsNullable())
                                {
                                    WSCombineFilter cf = new WSCombineFilter(WSCombineFilter.SQLMode.AndAlso);

                                    if (mainFilter.Value != null) { cf.Save(new WSDateFFilter(param, member, WSDateFFilter.OPERATIONS.NotEqual) { Value = null }); }

                                    cf.Save(mainFilter);

                                    filter.Save(cf);
                                }
                                else
                                {
                                    filter.Save(mainFilter);
                                }
                            }
                            #endregion
                        }
                        #endregion
                        #region TimeSpan (Ready)
                        else if (typeof(TimeSpan?).IsAssignableFrom(param.DataType))
                        {
                            #region SPECIAL CASES
                            #region If Exists (Ready)
                            if (WSConstants.ALIACES.EXIST.Match(Value) && param.DataType.IsNullable())
                            {
                                filter.Save(new WSTimeFFilter(param, member, WSTimeFFilter.OPERATIONS.NotEqual) { Value = null });
                            }
                            #endregion
                            #region If Empty (Ready)
                            else if (WSConstants.ALIACES.EMPTY.Match(Value) && param.DataType.IsNullable())
                            {
                                filter.Save(new WSTimeFFilter(param, member, null) { Value = null });
                            }
                            #endregion
                            #endregion

                            #region If Value (Ready)
                            else
                            {
                                if (param.DataType.Read(Value, out dVal, new char[] { }, WSConstants.DATE_LIST_SEPARATORS))
                                {
                                    WSOperation operation = WSTimeFFilter.OPERATIONS.GetOperation(state);

                                    Type dType = dVal == null ? param.DataType : dVal.GetType();
                                    if (dType.IsCollection() && operation != null)
                                    {
                                        dVal = operation.Match("max") ? ((List<dynamic>)dVal).Max(x => x) : operation.Match("min") ? ((List<dynamic>)dVal).Min(x => x) : dVal;
                                    }

                                    if (param.DataType.IsNullable())
                                    {
                                        WSCombineFilter cf = new WSCombineFilter(WSCombineFilter.SQLMode.AndAlso);

                                        if (Value != null) { cf.Save(new WSTimeFFilter(param, member, WSTimeFFilter.OPERATIONS.NotEqual) { Value = null }); }

                                        cf.Save(new WSTimeFFilter(param, member, operation) { Value = dVal });

                                        filter.Save(cf);
                                    }
                                    else { filter.Save(new WSTimeFFilter(param, member, operation) { Value = dVal }); }
                                }
                            }
                            #endregion
                        }
                        #endregion
                    }
                    #endregion
                }
                catch (Exception e) { WSStatus status = WSStatus.NONE.clone(); if (CFunc != null) { CFunc.RegError(GetType(), e, ref status); } }
            }
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
                        WSParam param = schema.GetXParam(Value);

                        if (param != null && param is WSTableParam)
                        {
                            WSTableParam tParam = (WSTableParam)param;
                            PropertyInfo property = srcType.GetProperties().FirstOrDefault(p => tParam.WSColumnRef.NAME.Equals(p.Name));
                            if (property == null) { iostatus.AddNote(string.Format("No PropertyInfo found for : [{0}]", tParam.DISPLAY_NAME)); }
                            else
                            {
                                parents.Add(property);

                                if (tParam.DataType.IsSimple() && tParam.IsSortable)
                                {
                                    expression = SortPrimitiveType<TEntity>(initSource, source, param, false, parents, expression, ref iostatus);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e) { WSStatus status = WSStatus.NONE.clone(); CFunc.RegError(GetType(), e, ref status); }
            return expression;
        }

        public override WSJson Clone() { return new WSJValue(Value); }

        public bool Match(object _matchValue, string _matchOperation, Type vType = null) {
            try
            {
                vType = vType != null ? vType : _matchValue == null ? null : _matchValue.GetType();
                if (vType != null)
                {
                    dynamic matchVal = _matchValue != null && vType.Read(_matchValue, out matchVal) ? matchVal : null;
                    dynamic tempVal = Value != null && vType.Read(Value, out tempVal) ? tempVal : null;

                    #region Numeric (Ready)
                    if (vType.IsNumeric())
                    {
                        WSOperation operation = WSNumericFFilter.OPERATIONS.GetOperation(_matchOperation);

                        if (operation.Match(WSNumericFFilter.OPERATIONS.Equal)) { return (matchVal == null && tempVal == null) || matchVal == tempVal; }
                        else if (operation.Match(WSNumericFFilter.OPERATIONS.NotEqual)) { return !((matchVal == null && tempVal == null) || matchVal == tempVal); }
                        else if (operation.Match(WSNumericFFilter.OPERATIONS.GreaterThan))
                        {
                            return matchVal != null && tempVal != null && matchVal > tempVal;
                        }
                        else if (operation.Match(WSNumericFFilter.OPERATIONS.GreaterThanOrEqual)) { return matchVal != null && tempVal != null && matchVal >= tempVal; }
                        else if (operation.Match(WSNumericFFilter.OPERATIONS.LessThan)) { return matchVal != null && tempVal != null && matchVal < tempVal; }
                        else if (operation.Match(WSNumericFFilter.OPERATIONS.LessThanOrEqual)) { return matchVal != null && tempVal != null && matchVal <= tempVal; }
                    }
                    #endregion
                    #region bool (Ready)
                    else if (typeof(bool?).IsAssignableFrom(vType))
                    {
                        WSOperation operation = WSBoolFFilter.OPERATIONS.GetOperation(_matchOperation);

                        if (operation.Match(WSBoolFFilter.OPERATIONS.Equal)) { return (matchVal == null && tempVal == null) || (matchVal != null && tempVal != null && matchVal == tempVal); }
                        else if (operation.Match(WSBoolFFilter.OPERATIONS.NotEqual)) { return !((matchVal == null && tempVal == null) || (matchVal != null && tempVal != null && matchVal == tempVal)); }
                    }
                    #endregion
                    #region string (Ready)
                    else if (typeof(string).IsAssignableFrom(vType))
                    {
                        WSOperation operation = WSStringFFilter.OPERATIONS.GetOperation(_matchOperation);

                        if (operation.Match(WSStringFFilter.OPERATIONS.Equal)) { return (matchVal == null && tempVal == null) || (matchVal).Equals(tempVal); }
                        else if (operation.Match(WSStringFFilter.OPERATIONS.EndsWith)) { return matchVal.EndsWith(tempVal); }
                        else if (operation.Match(WSStringFFilter.OPERATIONS.StartsWith)) { return matchVal.StartsWith(tempVal); }
                        else if (operation.Match(WSStringFFilter.OPERATIONS.Like)) { return matchVal.Contains(tempVal); }

                        else if (operation.Match(WSStringFFilter.OPERATIONS.NotEqual)) { return !((matchVal == null && tempVal == null) || (matchVal).Equals(tempVal)); }
                        else if (operation.Match(WSStringFFilter.OPERATIONS.NotEndsWith)) { return matchVal != null && !matchVal.EndsWith(tempVal); }
                        else if (operation.Match(WSStringFFilter.OPERATIONS.NotStartsWith)) { return matchVal != null && !matchVal.StartsWith(tempVal); }
                        else if (operation.Match(WSStringFFilter.OPERATIONS.NotLike)) { return matchVal != null && !matchVal.Contains(tempVal); }
                    }
                    #endregion
                    #region Guid (Ready)
                    else if (typeof(Guid?).IsAssignableFrom(vType))
                    {
                        WSOperation operation = WSGuidFFilter.OPERATIONS.GetOperation(_matchOperation);

                        if (operation.Match(WSGuidFFilter.OPERATIONS.Equal)) { return (matchVal == null && tempVal == null) || matchVal.Equals(tempVal); }
                        else if (operation.Match(WSGuidFFilter.OPERATIONS.NotEqual)) { return !(matchVal == null && tempVal == null) || matchVal.Equals(tempVal); }
                    }
                    #endregion
                    #region DateTime (Ready)
                    else if (typeof(DateTime?).IsAssignableFrom(vType))
                    {
                        WSOperation operation = WSDateFFilter.OPERATIONS.GetOperation(_matchOperation);

                        if (operation.Match(WSDateFFilter.OPERATIONS.Equal)) { return (matchVal == null && tempVal == null) || matchVal.Equals(tempVal); }
                        else if (operation.Match(WSDateFFilter.OPERATIONS.NotEqual)) { return !((matchVal == null && tempVal == null) || matchVal.Equals(tempVal)); }
                        else if (operation.Match(WSDateFFilter.OPERATIONS.GreaterThanOrEqual)) { return matchVal >= tempVal; }
                        else if (operation.Match(WSDateFFilter.OPERATIONS.LessOrEqual)) { return matchVal <= tempVal; }
                    }
                    #endregion
                    #region TimeSpan (Ready)
                    else if (typeof(TimeSpan?).IsAssignableFrom(vType))
                    {
                        WSOperation operation = WSTimeFFilter.OPERATIONS.GetOperation(_matchOperation);

                        if (operation.Match(WSTimeFFilter.OPERATIONS.Equal)) { return (matchVal == null && tempVal == null) || matchVal.Equals(tempVal); }
                        else if (operation.Match(WSTimeFFilter.OPERATIONS.NotEqual)) { return !((matchVal == null && tempVal == null) || matchVal.Equals(tempVal)); }
                    }
                    #endregion
                }
            }
            catch (Exception) { }
            return false;
        }

        internal override bool applyInternal(WSRequest Request, MetaFunctions CFunc)
        {
            try {
                if (Value.StartsWith("$") || (Value.StartsWith("[") && Value.EndsWith("]")))
                {
                    string temp = Value.Trim(new char[] { '[', ']' }).TrimStart(new char[] { '$' });

                    if (!string.IsNullOrEmpty(temp))
                    {
                        if (WSConstants.COMMAND_VALUES.Items.Any(v => v.Match(temp)))
                        {
                            if (WSConstants.COMMAND_VALUES.USER_ID.Match(temp))
                            {
                                Value = Request.Security.AuthToken.User.id.ToString();
                            }
                            else
                            {
                                if (WSConstants.COMMAND_VALUES.SQL_COMMAND_VALUE_GETDATE.Match(temp))
                                {
                                    DateTime d = Request.DBContext.GetSystemDate();
                                    Value = d.ToString(WSConstants.DATE_FORMAT);
                                }
                                else if (WSConstants.COMMAND_VALUES.SQL_COMMAND_VALUE_GETTIME.Match(temp))
                                {
                                    DateTime d = Request.DBContext.GetSystemDate();
                                    Value = new TimeSpan(0, d.Hour, d.Minute, d.Second, d.Millisecond).ToString(WSConstants.DATE_FORMAT);
                                }
                            }
                        }
                        else if (WSConstants.COMMAND_KEYS.Items.Any(v => v.Match(temp)))
                        {
                            if (WSConstants.COMMAND_KEYS.SHARED_KEY.Match(temp))
                            {
                                Value = Request.SOURCE.GetXParam(temp).ReadValue(Request.INPUT).ToString();
                            }
                        }
                    }
                }
                return true;
            } catch (Exception e) { CFunc.RegError(GetType(), e, ref Request.status); }
            return false;
        }

        public override bool Match(WSJson json, out WSStatus status)
        {
            status = json != null && json is WSJValue && Value.Equals(((WSJValue)json).Value) ? WSStatus.SUCCESS_Copy() : WSStatus.ERROR_Copy();
            if (status.CODE != WSStatus.SUCCESS.CODE)
            {
                status.AddNote($"Values not match:[{{current:{Value}}}<->{{original:{((WSJValue)json).Value}}}]");
                return false;
            }
            return true;
        }

        public override bool MatchEntity(MetaFunctions CFunc, WSDynamicEntity entity, WSTableSource src, string key = null, string matchOperation = null)
        {
            bool isMatch = true;
            try
            {
                if (!string.IsNullOrEmpty(key) && IsValid)
                {
                    matchOperation = matchOperation == null ? WSFieldFilter.GLOBAL_OPERATIONS.Equal.NAME : matchOperation;

                    WSTableParam param = src.DBPrimitiveParams.FirstOrDefault(p=>p.Match(key));

                    if (param != null) { isMatch = Match(entity.GetType().GetProperty(param.WSColumnRef.NAME).GetValue(entity, null), matchOperation, param.DataType); }
                }
            }
            catch (Exception e) { isMatch = false;  WSStatus status = WSStatus.NONE.clone(); CFunc.RegError(GetType(), e, ref status);}
            return isMatch;
        }
    }
}