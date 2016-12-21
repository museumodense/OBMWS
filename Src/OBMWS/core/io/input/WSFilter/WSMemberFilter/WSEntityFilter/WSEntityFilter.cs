using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

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
    public class WSEntityFilter : WSMemberFilter
    {
        public WSEntityFilter(WSTableSource _Source, Expression _member, WSOperation _Operation) : base(_Source?.ReturnType, _member) {
            Source = _Source;
            operation = _Operation != null ? _Operation : OPERATIONS.GetOperation(null);
        }
        public WSTableSource Source { get; private set; }

        public bool _IsValid = false;
        public bool IsValidRead = false;
        public override bool IsValid { get { if (!IsValidRead) { _IsValid = Source != null && Source.isValid && operation != null && operation.isValid; IsValidRead = true; } return _IsValid; } }
        
        public override Expression ToExpression()
        {
            if (operation.Match(OPERATIONS.Exist))
            {
                return Expression.NotEqual(member, Expression.Constant(null, Source.ReturnType));
            }
            else if (operation.Match(OPERATIONS.Empty))
            {
                return Expression.Equal(member, Expression.Constant(null, Source.ReturnType));
            }
            return null;
        }

        public class OPERATIONS
        {
            public static WSOperation GetOperation(string key) { return (ALL != null && ALL.Any(o => o.Match(key))) ? ALL.FirstOrDefault(o => o.Match(key)) : NONE; }

            private static IEnumerable<WSValueOperation> _VALUE_OPERATIONS = null;
            public static IEnumerable<WSValueOperation> VALUE_OPERATIONS
            {
                get
                {
                    if (_VALUE_OPERATIONS == null)
                    {
                        _VALUE_OPERATIONS = typeof(OPERATIONS).GetFields().Where(f => f.FieldType.IsAssignableFrom(typeof(WSValueOperation))).Select(f => (WSValueOperation)f.GetValue(null));
                    }
                    return _VALUE_OPERATIONS == null ? new List<WSValueOperation>() : _VALUE_OPERATIONS;
                }
            }

            private static IEnumerable<WSStateOperation> _STATE_OPERATIONS = null;
            public static IEnumerable<WSStateOperation> STATE_OPERATIONS
            {
                get
                {
                    if (_STATE_OPERATIONS == null)
                    {
                        _STATE_OPERATIONS = typeof(OPERATIONS).GetFields().Where(f => f.FieldType.IsAssignableFrom(typeof(WSStateOperation)))?.Select(f => (WSStateOperation)f.GetValue(null));
                    }
                    return _STATE_OPERATIONS == null ? new List<WSStateOperation>() : _STATE_OPERATIONS;
                }
            }

            private static IEnumerable<WSOperation> _ALL = null;
            public static IEnumerable<WSOperation> ALL { get { if (_ALL == null) { _ALL = new List<WSOperation>().Union(STATE_OPERATIONS).Union(VALUE_OPERATIONS)/*.ToList()*/; } return _ALL; } }

            public static readonly WSStateOperation Empty = new WSStateOperation(WSConstants.ALIACES.EMPTY.NAME, WSOperation.OperatorChars.Empty, WSConstants.ALIACES.EMPTY.ALIACES);
            public static readonly WSStateOperation Exist = new WSStateOperation(WSConstants.ALIACES.EXIST.NAME, WSOperation.OperatorChars.Exist, WSConstants.ALIACES.EXIST.ALIACES);

            public static readonly WSValueOperation NONE = new WSValueOperation(WSConstants.ALIACES.NONE.NAME, WSOperation.OperatorChars.None, WSConstants.ALIACES.NONE.ALIACES);
        }

        public override string ToString() { string text = "{" + (member != null ? member.ToString() : "none") + ":" + (Source != null ? Source.NAME : "none") + ":[mode]" + "}"; return text.Replace("[mode]", operation.ToString()); }
        public override bool Equals(object obj) { return obj != null && GetType() == obj.GetType() && ToString().Equals(((WSEntityFilter)obj).ToString()); }
        public override int GetHashCode() { return ToString().GetHashCode(); }
        public override bool Contains(WSTableParam param)
        {
            //TODO@2016-03-04 : find out if WSTableParam 'param' is releted to this filter
            //try
            //{
            //    return IsValid && Source.ReturnType==param.WSEntityType;
            //}
            //catch (Exception) { }
            return false;
        }
    }
}