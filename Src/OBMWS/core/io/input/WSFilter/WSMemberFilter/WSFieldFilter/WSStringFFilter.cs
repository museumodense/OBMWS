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
    public class WSStringFFilter : WSFieldFilter
    {
        public WSStringFFilter(WSTableParam _Field, Expression _member, WSOperation _Operation): base(_Field, _member)
        {
            operation = _Operation != null ? _Operation : OPERATIONS.GetOperation(null);
        }
        public override Expression ToExpression()
        {
            if (Value == null)
            {
                if (operation.Match(OPERATIONS.Equal)) { Expression expr = null; if (CallToString()) { expr = Expression.Call(member, WSConstants.stringEqualsMethod, Expression.Constant(string.Empty, Field.DataType)); } return expr; }
                else if (operation.Match(OPERATIONS.NotEqual)) { Expression expr = null; if (CallToString()) { expr = Expression.Not(Expression.Call(member, WSConstants.stringEqualsMethod, Expression.Constant(string.Empty, Field.DataType))); } return expr; }
            }
            else
            {
                if (Value is string)
                {
                    #region POSITIVE
                    if (operation.Match(OPERATIONS.StartsWith)) { Expression expr = null; if (CallToString()) { expr = Expression.Call(member, WSConstants.stringStartsWithMethod, Expression.Constant(((string)Value).ToLower(), typeof(string))); } return expr; }
                    else if (operation.Match(OPERATIONS.EndsWith)) { Expression expr = null; if (CallToString()) { expr = Expression.Call(member, WSConstants.stringEndsWithMethod, Expression.Constant(((string)Value).ToLower(), typeof(string))); } return expr; }
                    else if (operation.Match(OPERATIONS.Like)) { Expression expr = null; if (CallToString()) { expr = Expression.Call(member, WSConstants.stringContainsMethod, Expression.Constant(((string)Value).ToLower(), typeof(string))); } return expr; }
                    else if (operation.Match(OPERATIONS.Equal)) { Expression expr = null; if (CallToString()) { expr = Expression.Call(member, WSConstants.stringEqualsMethod, Expression.Constant(((string)Value).ToLower(), Field.DataType)); } return expr; }
                    #endregion

                    #region NEGATIVE
                    else if (operation.Match(OPERATIONS.NotStartsWith)) { Expression expr = null; if (CallToString()) { expr = Expression.Not(Expression.Call(member, WSConstants.stringStartsWithMethod, Expression.Constant(((string)Value).ToLower(), Field.DataType))); } return expr; }
                    else if (operation.Match(OPERATIONS.NotEndsWith)) { Expression expr = null; if (CallToString()) { expr = Expression.Not(Expression.Call(member, WSConstants.stringEndsWithMethod, Expression.Constant(((string)Value).ToLower(), Field.DataType))); } return expr; }
                    else if (operation.Match(OPERATIONS.NotLike)) { Expression expr = null; if (CallToString()) { expr = Expression.Not(Expression.Call(member, WSConstants.stringContainsMethod, Expression.Constant(((string)Value).ToLower(), Field.DataType))); } return expr; }
                    else if (operation.Match(OPERATIONS.NotEqual)) { Expression expr = null; if (CallToString()) { expr = Expression.Not(Expression.Call(member, WSConstants.stringEqualsMethod, Expression.Constant(((string)Value).ToLower(), Field.DataType))); } return expr; }
                    #endregion
                }
                else
                {
                    Type type = Value.GetType();
                    if (type.IsCollection()) { /*TODO@ANDVO:2015-10-19 : throw error "Collection<string> must convert to WSStringFFilter (in WSJValue.GetFilter()).*/ }
                }
            }
            return null;
        }

        public class OPERATIONS
        {
            public static WSOperation GetOperation(string key) { return (string.IsNullOrEmpty(key) || ALL == null || !ALL.Any(o => o.Match(key))) ? Equal : ALL.FirstOrDefault(o => o.Match(key)); }
            
            private static IEnumerable<WSValueOperation> _VALUE_OPERATIONS = null;
            public static IEnumerable<WSValueOperation> VALUE_OPERATIONS
            {
                get
                {
                    if (_VALUE_OPERATIONS == null)
                    {
                        _VALUE_OPERATIONS = typeof(OPERATIONS).GetFields().Where(f => f.FieldType.IsAssignableFrom(typeof(WSValueOperation))).Select(f => (WSValueOperation)f.GetValue(null));
                    } return _VALUE_OPERATIONS == null ? new List<WSValueOperation>() : _VALUE_OPERATIONS;
                }
            }

            private static IEnumerable<WSStateOperation> _STATE_OPERATIONS = null;
            public static IEnumerable<WSStateOperation> STATE_OPERATIONS
            {
                get
                {
                    if (_STATE_OPERATIONS == null)
                    {
                        _STATE_OPERATIONS = typeof(OPERATIONS).GetFields().Where(f => f.FieldType.IsAssignableFrom(typeof(WSStateOperation))).Select(f => (WSStateOperation)f.GetValue(null));
                    } return _STATE_OPERATIONS == null ? new List<WSStateOperation>() : _STATE_OPERATIONS;
                }
            }

            private static IEnumerable<WSOperation> _ALL = null;
            public static IEnumerable<WSOperation> ALL { get { if (_ALL == null) { _ALL = new List<WSOperation>().Union(STATE_OPERATIONS).Union(VALUE_OPERATIONS).ToList(); } return _ALL; } }

            public static readonly WSValueOperation Like =          new WSValueOperation("Like",        WSOperation.OperatorChars.Like,         new List<string> { "contain", "contains" });
            public static readonly WSValueOperation NotLike =       new WSValueOperation("NotLike",     WSOperation.OperatorChars.NotLike,      new List<string> { "notcontain", "notcontains" });

            public static readonly WSValueOperation Equal =         new WSValueOperation("Equal",       WSOperation.OperatorChars.Equal,        new List<string> { "equals", "is", "er" });
            public static readonly WSValueOperation NotEqual =      new WSValueOperation("NotEqual",    WSOperation.OperatorChars.NotEqual,     new List<string> { "isnot", "not", "ikke", "except", "notequal", "notequals" });

            public static readonly WSValueOperation StartsWith =    new WSValueOperation("StartsWith",  WSOperation.OperatorChars.StartsWith,   new List<string> { "start", "startwith" });
            public static readonly WSValueOperation NotStartsWith = new WSValueOperation("NotStartsWith",WSOperation.OperatorChars.NotStartsWith,new List<string> { "notstart", "notstarts" });

            public static readonly WSValueOperation EndsWith =      new WSValueOperation("EndsWith",    WSOperation.OperatorChars.EndsWith,     new List<string> { "end", "ends", "endwith" });
            public static readonly WSValueOperation NotEndsWith =   new WSValueOperation("NotEndsWith", WSOperation.OperatorChars.NotEndsWith,  new List<string> { "notend", "notends" });
        }

        public override string ToString() { string text = base.ToString(); return text; }
        public override bool Equals(object obj) { return obj != null && this.GetType() == obj.GetType() && this.ToString().Equals(((WSStringFFilter)obj).ToString()); }
        public override int GetHashCode() { return ToString().GetHashCode(); }
    }   
}