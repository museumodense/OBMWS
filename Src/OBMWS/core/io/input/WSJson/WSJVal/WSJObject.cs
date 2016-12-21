using System;
using System.Collections.Generic;
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
    public class WSJObject : WSJVal<List<WSJProperty>>
    {
        public WSJObject(List<WSJProperty> _value) { Value = _value; }

        public override bool IsValid { get { return Value != null && !Value.Any(x => !x.IsValid); } }

        public override bool IsEmpty { get { return Value == null || !Value.Any(x => !x.IsEmpty); } }

        public override string ToString() { return JString; }
        public override bool Equals(object obj) { if (obj == null || obj.GetType() != typeof(WSJObject) || ((WSJObject)obj).GetHashCode() != GetHashCode()) return false; return true; }
        public override int GetHashCode() { return JString.GetHashCode(); }
        
        public override string JString {
            get {
                IEnumerable<string> pLines = IsValid ? Value.Where(x => x.IsValid).Select(x => x.JString) : new List<string>();
                return "{" + (pLines.Any() ? pLines.Aggregate((a, b) => a + "," + b) : string.Empty) + "}";
            }
        }

        public override string NiceUrlString {
            get {
                IEnumerable<string> pLines = IsValid ? Value.Where(x => x.IsValid).Select(x => x.NiceUrlString) : new List<string>();
                return "{" + (pLines.Any() ? pLines.Aggregate((a, b) => a + "," + b) : string.Empty) + "}";
            }
        }

        public override WSFilter GetFieldFilter(MetaFunctions CFunc, WSTableParam param, Expression parent, int level, string state = null, bool? negate = null)
        {
            try
            {
                WSCombineFilter filter = new WSCombineFilter(WSCombineFilter.SQLMode.AndAlso);
                filter.SaveRange(Value.Select(x => x.GetFieldFilter( CFunc, param, parent, level, state, negate)).ToList());/*DONT FORVARD 'negate' parameter to avoid complicatency*/
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
            catch (Exception e) { WSStatus status = WSStatus.NONE.clone(); CFunc.RegError(GetType(), e, ref status); }
            return expression;
        }

        public override WSJson Clone() { return new WSJObject(Value.Any()?Value.Select(x=>(WSJProperty)x.Clone()).ToList():new List<WSJProperty>()); }

        internal override bool applyInternal(WSRequest Request, MetaFunctions CFunc)
        {
            try
            {
                foreach (WSJProperty prop in Value){prop.apply(Request, CFunc);}
                return true;
            } catch (Exception e) { WSStatus status = WSStatus.NONE.clone(); CFunc.RegError(GetType(), e, ref status); }
            return false;
        }

        public override WSFilter GetOptionFilter(MetaFunctions CFunc, Expression parent, int level, string state = null, bool? negate = null)
        {
            try
            {
                WSCombineFilter filter = new WSCombineFilter(WSCombineFilter.SQLMode.AndAlso);
                filter.SaveRange(Value.Select(x => x.GetOptionFilter(CFunc, parent, level, state, negate)).ToList());
                return filter.Any() ? (filter.Count == 1 && !filter.Negate) ? filter.FirstOrDefault() : filter : null;
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
                else if (!(json is WSJObject)) { status = WSStatus.ERROR_Copy(); return false; }
                else
                {
                    WSJObject jObj = (WSJObject)json;
                    IEnumerable<string> keys = Value.Select(v1 => v1.Key);
                    IEnumerable<string> keys1 = jObj.Value.Select(v1 => v1.Key);
                    if (keys1.Any(p1 => !keys.Any(p => p1.Equals(p))))
                    {
                        status = WSStatus.ERROR_Copy();
                        status.AddNote($"An original service generates response with additional properties:[{keys1.Where(p1 => !keys.Any(p => p1.Equals(p))).Aggregate((a,b)=>a+","+b)}] which is not exists in current object.");
                    }
                    else if (keys.Count() != keys1.Count())
                    {
                        status = WSStatus.ERROR_Copy();
                        status.AddNote($"The current service generates response with additional properties:[{keys.Where(p1 => !keys1.Any(p => p1.Equals(p))).Aggregate((a, b) => a + "," + b)}] which is not exists in original object.");
                    }
                    else
                    {
                        foreach (WSJProperty jProp1 in jObj.Value)
                        {
                            WSJProperty jProp = Value.FirstOrDefault(x => x.Key.Equals(jProp1.Key));

                            WSStatus pStatus = jProp.Match(jProp1, out pStatus) ? WSStatus.SUCCESS_Copy() : pStatus;

                            status = pStatus;

                            if (pStatus.CODE != WSStatus.SUCCESS.CODE) { return false; }
                        }
                        return !status.childs.Any(x => x.CODE != WSStatus.SUCCESS.CODE);
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
                foreach (WSJProperty jProp in Value)
                {
                    if (!jProp.MatchEntity(CFunc, entity, src, key)) { isMatch = false; }
                }
            }
            catch (Exception e) { isMatch = false; WSStatus status = WSStatus.NONE.clone(); CFunc.RegError(GetType(), e, ref status); }
            return isMatch;
        }
    }
}