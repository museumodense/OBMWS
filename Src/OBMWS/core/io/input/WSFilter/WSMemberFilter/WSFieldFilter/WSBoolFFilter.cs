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
    public class WSBoolFFilter : WSFieldFilter
    {
        public WSBoolFFilter(WSTableParam _Field, Expression _member, WSOperation _Operation)
            : base(_Field, _member)
        {
            operation = _Operation != null ? _Operation : OPERATIONS.GetOperation(null);
        }
        public override Expression ToExpression()
        {
            if (Field.DataType.IsNullable() || Value != null)
            {
                if (Value != null && Value is List<dynamic>)
                {
                    if (((List<dynamic>)Value).Any())
                    {
                        return GetExpressionContains<bool>(member, Value, operation == OPERATIONS.NotEqual);
                    }
                }
                else
                {
                    if (operation.Match(OPERATIONS.Equal)) return Expression.Equal(member, Expression.Constant(Value, Field.DataType));
                    else if (operation.Match(OPERATIONS.NotEqual)) return Expression.NotEqual(member, Expression.Constant(Value, Field.DataType));
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
            public static IEnumerable<WSOperation> ALL { get { if (_ALL == null) { _ALL = new List<WSOperation>().Union(STATE_OPERATIONS).Union(VALUE_OPERATIONS); } return _ALL; } }

            public static readonly WSValueOperation Equal =         new WSValueOperation("Equal", WSOperation.OperatorChars.Equal, new List<string> { "equals", "is", "er" });
            public static readonly WSValueOperation NotEqual =      new WSValueOperation("NotEqual", WSOperation.OperatorChars.NotEqual, new List<string> { "isnot", "not", "ikke", "except", "notequal", "notequals" });            
        }

        public override string ToString() { string text = base.ToString(); return text; }
        public override bool Equals(object obj) { return obj != null && this.GetType() == obj.GetType() && this.ToString().Equals(((WSBoolFFilter)obj).ToString()); }
        public override int GetHashCode() { return ToString().GetHashCode(); }
    }
}