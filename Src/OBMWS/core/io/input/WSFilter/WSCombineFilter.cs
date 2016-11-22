using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

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
    public class WSCombineFilter : List<WSFilter>, WSFilter
    {
        public WSCombineFilter() { }
        public WSCombineFilter(SQLMode _Mode, bool? negate = null) { Mode = _Mode; Negate = negate != null && (bool)negate; }

        public bool Negate { get; private set; }

        public SQLMode Mode = SQLMode.AndAlso;
        public bool IsValid { get { return !this.Any(x => x == null || !x.IsValid); } }
        public bool IsEmpty { get { return Count == 0 || !IsValid; } }

        public bool Save(WSFilter filter)
        {
            try
            {
                if (filter != null && filter.IsValid)
                {
                    if (this.Any(f => f.Equals(filter)))
                    {
                        for (int i = 0; i < Count; i++)
                        {
                            if (this[i].Equals(filter))
                            {
                                this[i] = filter;
                                break;
                            }
                        }
                    }
                    else { Add(filter); }
                    return true;
                }
            }
            catch (Exception) { }
            return false;
        }
        internal void SaveRange(List<WSFilter> list) { foreach (WSFilter f in list) { Save(f); } }

        internal WSFilter Reduce() 
        {
            WSCombineFilter cFilter = new WSCombineFilter(Mode, Negate);
            if (!IsEmpty)
            {
                foreach (WSFilter child in this)
                {
                    WSFilter reduced = (child is WSCombineFilter) ? ((WSCombineFilter)child).Reduce() : child;

                    if (reduced != null)
                    {
                        if (reduced is WSCombineFilter && ((WSCombineFilter)reduced).Any() && ((WSCombineFilter)reduced).Mode == Mode && !((WSCombineFilter)reduced).Negate)
                        {
                            cFilter.SaveRange(((WSCombineFilter)reduced));
                        }
                        else { cFilter.Save(reduced); }
                    }
                }
            }
            return !cFilter.Any() ? null : (cFilter.Count == 1 && !cFilter.Negate) ? cFilter[0] : cFilter;
        }

        public Expression ToExpression()
        {
            Expression exp = null;
            foreach (WSFilter f in this)
            {
                if (f != null && f.IsValid)
                {
                    Expression right = f.ToExpression();
                    if (right != null)
                    {
                        if (exp == null) { exp = right; }
                        else if (Mode == SQLMode.And)           { exp = Expression.And          (exp, right); }
                        else if (Mode == SQLMode.AndAlso)       { exp = Expression.AndAlso      (exp, right); }
                        else if (Mode == SQLMode.ExclusiveOr)   { exp = Expression.ExclusiveOr  (exp, right); }
                        else if (Mode == SQLMode.Or)            { exp = Expression.Or           (exp, right); }
                        else if (Mode == SQLMode.OrElse)        { exp = Expression.OrElse       (exp, right); }
                    }
                }
            }
            if (exp == null) return null;
            else {
                exp = exp.CanReduce ? exp.Reduce() : exp;
                return Negate ? Expression.Not(exp) : exp;
            }
        }
        public Expression<Func<A, bool>> ToLambda<A>(ParameterExpression id) { try { Expression expr = ToExpression(); return expr == null ? null : Expression.Lambda<Func<A, bool>>(expr, id); } catch (Exception) { } return null; }
        public enum SQLMode
        {
            And,
            AndAlso,
            ExclusiveOr,
            Or,
            OrElse
        }

        public override string ToString() { return !IsValid ? "" : ("[" + (this.Any() ? this.Select(x => x.ToString()).Aggregate((a, b) => a + " " + Mode + " " + b) : "") + "]"); }
        public override bool Equals(object obj) { return obj != null && GetType() == obj.GetType() && ToString().Equals(obj.ToString()); }
        public override int GetHashCode() { return ToString().GetHashCode(); }

        public bool Contains(WSTableParam param)
        {
            try
            {
                return this.Any(x => x.Contains(param));
            }
            catch (Exception) { }
            return false;
        }
    }
}