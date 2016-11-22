using System;
using System.Collections.Generic;
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
    public class WSJArray : WSJVal<List<WSJson>>
    {
        public WSJArray() { Value = new List<WSJson> { }; }
        public WSJArray(List<WSJson> _value) { Value = _value; }
        
        public override bool IsValid { get { return Value != null && !Value.Any(x => !x.IsValid); } }
        public override bool IsEmpty { get { return Value == null || !Value.Any(x => !x.IsEmpty); } }
        public override string ToString() { return JString; }
        public override bool Equals(object obj) { if (obj == null || obj.GetType() != typeof(WSJArray) || ((WSJArray)obj).GetHashCode() != GetHashCode()) return false; return true; }
        public override int GetHashCode() { return JString.GetHashCode(); }

        public override string JString {
            get {
                IEnumerable<string> pLines = IsValid ? Value.Where(x => x.IsValid).Select(x => x.JString) : new List<string>();
                return "[" + (pLines.Any() ? pLines.Aggregate((a, b) => a + "," + b) : string.Empty) + "]";
            }
        }

        public override string NiceUrlString {
            get {
                IEnumerable<string> pLines = IsValid ? Value.Where(x => x.IsValid).Select(x => x.NiceUrlString) : new List<string>();
                return "[" + (pLines.Any() ? pLines.Aggregate((a, b) => a + "," + b) : string.Empty) + "]";
            }
        }
        
        public override WSFilter GetFieldFilter(MetaFunctions CFunc, WSTableParam param, Expression parent, int level, string state = null, bool? negate = null)
        {
            try
            {
                ///**************************************************
                //* if(filter array contains any conditional 'IOJFilter' 
                //* like : "[{option_name1:value1},{option_name2:value2}]" (example:"[{max:123},{min:123}]") , - then use 'CombineMode.AndAlso' 
                //* else 
                //* (if all values in the filter array are simple type values)
                //* like : "{value1,value2,value3,...}" ,- use 'CombineMode.Or'
                //* ***********************************************/
                WSCombineFilter filter = new WSCombineFilter(WSCombineFilter.SQLMode.OrElse, negate);
                
                foreach (WSJson j in Value)
                {
                    WSFilter jFilter = j.GetFieldFilter(CFunc, param, parent, level, state, negate);
                    filter.Save(jFilter);
                }

                return filter.Any() ? (filter.Count == 1 && !filter.Negate) ? filter.FirstOrDefault() : filter : null;
            }
            catch (Exception e) { WSStatus status = WSStatus.NONE.clone(); CFunc.RegError(GetType(), e, ref status); }
            return null;
        }
        public override WSFilter GetOptionFilter(MetaFunctions CFunc, Expression parent, int level, string state = null, bool? negate = null)
        {
            try
            {
                WSCombineFilter filter = new WSCombineFilter(WSCombineFilter.SQLMode.OrElse, negate);
                filter.SaveRange(Value.Select(x => x.GetOptionFilter(CFunc, parent, level, state, null)).ToList());
                return filter.Any() ? (filter.Count == 1 && !filter.Negate) ? filter.FirstOrDefault() : filter : null;
            }
            catch (Exception e) { WSStatus status = WSStatus.NONE.clone(); CFunc.RegError(GetType(), e, ref status); }
            return null;
        }

        public override Expression SortTable<T>(MetaFunctions CFunc, WSDataContext dc, List<PropertyInfo> parents, Expression expression, ref WSStatus iostatus)
        {
            try
            {
                foreach (WSJson json in Value)
                {
                    List<PropertyInfo> subParents = new List<PropertyInfo>();
                    if (parents != null && parents.Any()) { subParents.AddRange(parents); }
                    expression = json.SortTable<T>(CFunc, dc, subParents, expression, ref iostatus);
                }
            }
            catch (Exception e) { CFunc.RegError(GetType(), e, ref iostatus); }
            return expression;
        }
        
        public override WSJson Clone() { return new WSJArray(Value.Any() ? Value.Select(x => x.Clone()).ToList() : new List<WSJson>()); }

        internal void Save(WSJson json)
        {
            try
            {
                Func<WSJson, bool> func = x => x.Equals(json);
                if (!Value.Any(func)) { Value.Add(json); }
                else { Value[Value.IndexOf(Value.FirstOrDefault(func))] = json; }
            }
            catch (Exception) { }
        }
        internal override bool applyInternal(WSRequest Request, MetaFunctions CFunc)
        {
            if (Request != null)
            {
                try
                {
                    if (Value != null && Value.Any())
                    {
                        for (int i = 0; i < Value.Count; i++)
                        {
                            if (Value[i] is WSJObject)
                            {
                                WSJProperty prop = ((WSJObject)Value[i]).Value[0];
                                if (prop.Key.StartsWith("$"))
                                {
                                    string commandKey = prop.Key.TrimStart(new char[] { '$' });

                                    if (prop.Value is WSJObject && prop.Value.IsValid)
                                    {
                                        #region apply $match command
                                        if (WSConstants.COMMAND_KEYS.MATCH.Match(commandKey))
                                        {
                                            WSJProperty jMatch = ((WSJObject)prop.Value).Value[0];

                                            #region SET $currentuser validation Filter
                                            if (WSConstants.COMMAND_KEYS.CURRENT_USER.Match(jMatch.Key))
                                            {
                                                WSJson jUser = jMatch.Value.Clone();
                                                if (jUser != null && jUser.IsValid)
                                                {
                                                    Value[i] = new WSJValue(Request.Security.IsValidUser(jUser) ? "1" : "0");
                                                }
                                            }
                                            #endregion
                                        }
                                        #endregion

                                        #region apply $read command
                                        else if (WSConstants.COMMAND_KEYS.READ.Match(commandKey))
                                        {
                                            WSJProperty jTarget = ((WSJObject)prop.Value).Value[0];
                                            string targetKey = jTarget.Key.TrimStart(new char[] { '$' });

                                            #region SET $currentuser validation Filter
                                            if (WSConstants.COMMAND_KEYS.CURRENT_USER.Match(targetKey))
                                            {
                                                List<WSJson> items = new List<WSJson> { };
                                                try
                                                {
                                                    items.AddRange(
                                                        (Request.Security.WSCurrentUser != null && Request.Security.WSCurrentUser.entity != null ? Request.Security.WSCurrentUser.entity.read(CFunc, jTarget.Value) : new List<dynamic> { })
                                                        .Select(x =>
                                                            new WSJValue((x as object).ToString())
                                                        )
                                                    );
                                                }
                                                catch (Exception e) { CFunc.RegError(GetType(), e, ref Request.status); }
                                                Value[i] = new WSJArray(items);
                                            }
                                            #endregion
                                        }
                                        #endregion

                                        continue;
                                    }
                                }
                            }
                            Value[i].apply(Request, CFunc);
                        }
                    }
                    return true;
                }
                catch (Exception e) { CFunc.RegError(GetType(), e, ref Request.status); }
            }
            return false;
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
                    WSJArray jArr1 = (WSJArray)json;
                    
                    IEnumerable<WSJValue> jValues = Value.OfType<WSJValue>();
                    jValues = jValues == null ? new List<WSJValue>() : jValues;
                    IEnumerable<WSJValue> jValues1 = jArr1.Value.OfType<WSJValue>();
                    jValues1 = jValues1 == null ? new List<WSJValue>() : jValues1;
                    
                    if (!jValues.Any() && !jValues1.Any()) { }
                    else {
                        if (jValues.Count() > jValues1.Count()) {
                            status = WSStatus.ERROR_Copy();
                            status.AddNote($"Extra properties generated by the current service:[{jValues.Where(p1 => !jValues1.Any(p => p1.Value.Equals(p.Value))).Select(x => x.Value).Aggregate((a, b) => a + "," + b)}]");
                            return false;
                        } else {
                            WSStatus subStatus = WSStatus.NONE_Copy();
                            foreach (WSJValue jVal1 in jValues1) {
                                status = jValues.Any(jVal => jVal1.Match(jVal, out subStatus)) ? WSStatus.SUCCESS_Copy() : subStatus;
                                if (status.CODE != WSStatus.SUCCESS.CODE) {
                                    return false;
                                }
                            }
                        }
                    }

                    IEnumerable<WSJObject> jObjects = Value.OfType<WSJObject>();
                    jObjects = jObjects == null ? new List<WSJObject>() : jObjects;
                    IEnumerable<WSJObject> jObjects1 = jArr1.Value.OfType<WSJObject>();
                    jObjects1 = jObjects1 == null ? new List<WSJObject>() : jObjects1;

                    if (!jObjects.Any() && !jObjects1.Any()) { }
                    else {
                        if (jObjects.Count() > jObjects1.Count()) {
                            status = WSStatus.ERROR_Copy();
                            status.AddNote($"Extra objects generated by the current service");
                            return false;
                        } else {
                            WSStatus subStatus = WSStatus.NONE_Copy();
                            foreach (WSJObject jObj1 in jObjects1){
                                status = jObjects.Any(jObj => jObj1.Match(jObj, out subStatus)) ? WSStatus.SUCCESS_Copy() : subStatus;
                                if (status.CODE != WSStatus.SUCCESS.CODE){
                                    return false;
                                }
                            }
                        }
                    }

                    IEnumerable<WSJArray> jArrays = Value.OfType<WSJArray>();
                    jArrays = jArrays == null ? new List<WSJArray>() : jArrays;
                    IEnumerable<WSJArray> jArrays1 = jArr1.Value.OfType<WSJArray>();
                    jArrays1 = jArrays1 == null ? new List<WSJArray>() : jArrays1;

                    if (!jArrays.Any() && !jArrays1.Any()) { }
                    else {
                        if (jArrays.Count() > jArrays1.Count()){
                            status = WSStatus.ERROR_Copy();
                            status.AddNote($"Extra arrays generated by the current service");
                            return false;
                        } else {
                            WSStatus subStatus = WSStatus.NONE_Copy();
                            foreach (WSJArray jObj1 in jArrays1) {
                                status = jArrays.Any(jObj => jObj1.Match(jObj, out subStatus)) ? WSStatus.SUCCESS_Copy() : subStatus;
                                if (status.CODE != WSStatus.SUCCESS.CODE) {
                                    return false;
                                }
                            }
                        }
                    }

                    status = WSStatus.SUCCESS_Copy();
                }
            }
            catch (Exception) { }
            return status.CODE == WSStatus.SUCCESS.CODE;
        }
        public override bool MatchEntity(MetaFunctions CFunc, WSDynamicEntity entity, WSTableSource src, string key = null, string matchOperation = null)
        {
            bool _match = true;//insure that the empty array gives true
            try
            {
                foreach (WSJson jItem in Value)
                {
                    _match = false;//insure that the NOT empty array gives false on failure
                    if (jItem is WSJValue && !string.IsNullOrEmpty(key)) { _match = ((WSJValue)jItem).MatchEntity(CFunc, entity, src, key, WSFieldFilter.GLOBAL_OPERATIONS.Equal.NAME); }
                    else if (jItem is WSJObject) { _match = ((WSJObject)jItem).MatchEntity(CFunc, entity, src, key); }
                    else if (jItem is WSJArray) { _match = ((WSJArray)jItem).MatchEntity(CFunc, entity, src, key); }
                    if (_match) break;
                }
            }
            catch (Exception e) { WSStatus status = WSStatus.NONE.clone(); CFunc.RegError(GetType(), e, ref status); }
            return _match;
        }
    }
}