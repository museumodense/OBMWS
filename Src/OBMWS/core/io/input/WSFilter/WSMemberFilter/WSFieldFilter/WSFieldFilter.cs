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
    public abstract class WSFieldFilter : WSMemberFilter
    {
        public WSFieldFilter(WSTableParam _Field, Expression _member) : base(_Field?.DataType, _member) { Field = _Field; }

        public WSTableParam Field { get; private set; }

        private dynamic _Value = null;
        public dynamic Value { get { return _Value; } set { _Value = value; IsValidRead = false; } }
        
        public bool _IsValid = false;
        public bool IsValidRead = false;
        public override bool IsValid
        {
            get
            {
                if (!IsValidRead)
                {
                    _IsValid = Field != null && Field.isValid && operation != null && operation.isValid && (Field.DataType.IsNullable() || (Value != null && !string.IsNullOrEmpty(((object)Value).ToString())));
                    IsValidRead = true;
                }
                return _IsValid;
            }
        }

        internal Expression GetExpressionContains<X>(Expression innerExpr, List<dynamic> dynamicList, bool Negate = false)
        {
            Expression callExpression = null;
            if (innerExpr != null && dynamicList != null && dynamicList.Any())
            {
                try
                {
                    List<X> list = dynamicList.Cast<X>().ToList();

                    MethodInfo method_contains = list.GetType().GetMethod("Contains", new[] { typeof(X) });

                    callExpression = Expression.Call(
                        Expression.Constant(list, list.GetType()),
                        method_contains,
                        innerExpr
                    );
                    callExpression = Negate ? Expression.Not(callExpression) : callExpression;
                }
                catch (Exception) { }
            }
            return callExpression;
        }
        internal Expression IsAnyExpression(Expression collection, Expression predicate = null)
        {
            Expression AnyExpr = null;
            try
            {
                if (collection.Type.IsCollection())
                {
                    Type elemType = collection.Type.GetEntityType();

                    if (elemType != null)
                    {
                        Type predType = typeof(Func<,>).MakeGenericType(elemType, typeof(bool));
                        Type[] GenericArguments = new[] { elemType };
                        Type[] InputParamTypes = predicate == null ? new[] { collection.Type } : new[] { collection.Type, predType };

                        /*******************************************************************************************************************************
                         * Enumerable.Any<T>(this IEnumerable<T> source)    ||  Enumerable.Any<T>(this IEnumerable<T> source, Func<T, bool> predicate);
                         *               ---      --------------                                      ---------------        -------------
                         *                |             |                                                    |                      |
                         *        ________|_______      |____________________________________________________|______________________|
                         *        GenericArguments                                  InputParamTypes
                         *                           
                         * where :  
                         *          T - is of [elemType]
                         *          source - is of [collection.Type]
                         *          predicate - is of [predType]
                         ******************************************************************************************************************************/

                        MethodInfo anyMethod = (MethodInfo)GetGenericMethod(typeof(Enumerable), "Any", GenericArguments, InputParamTypes, BindingFlags.Static);

                        if (predicate == null)
                        {
                            AnyExpr = Expression.Call(anyMethod, collection);
                        }
                        else
                        {
                            AnyExpr = Expression.Call(anyMethod, collection, predicate);
                        }
                    }
                }
            }
            catch (Exception) { }
            return AnyExpr;
        }
        private MethodBase GetGenericMethod(Type type, string name, Type[] GenericArguments, Type[] InputParamTypes, BindingFlags flags)
        {
            var methods = type.GetMethods()
                .Where(m => m.Name == name && m.GetGenericArguments().Length == GenericArguments.Length)
                .Select(m => m.MakeGenericMethod(GenericArguments));
            return Type.DefaultBinder.SelectMethod(flags, methods.ToArray(), InputParamTypes, null);
        }
        private bool IsIEnumerable(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>);
        }
        private Type GetIEnumerableImpl(Type type)
        {
            return (IsIEnumerable(type))?type:type.FindInterfaces((m, o) => IsIEnumerable(m), null)[0];
        }
        internal bool CallToString()
        {
            if (member != null)
            {
                IEnumerable<CustomAttributeData> cadList = ((MemberExpression)member).Member.CustomAttributesData().Where(ca => ca.NamedArguments.FirstOrDefault(na => na.MemberInfo.Name.Equals("DbType"))!=null);
                CustomAttributeData cad = cadList != null ? cadList.FirstOrDefault() : null;

                string DbType = cad == null ? null : cad.NamedArguments.FirstOrDefault(x => x.MemberInfo.Name.Equals("DbType")).TypedValue.Value.ToString();

                if (!string.IsNullOrEmpty(DbType) && WSConstants.LONG_TEXT_DBTYPES.Contains(DbType)) {
                    try {
                        member = Expression.Call(member, WSConstants.toStringMethod);
                    } catch (Exception) { member = null; }
                }
            }
            return member != null;
        }

        public override string ToString() { string text = "" + (Field != null ? Field.NAME : "none") + operation.OperatorChar + "(" + (Value != null ? Value.ToString() : "none") + ")"; return text; }
        public override bool Equals(object obj) { return obj != null && this.GetType() == obj.GetType() && this.ToString().Equals(((WSFieldFilter)obj).ToString()); }
        public override int GetHashCode() { return ToString().GetHashCode(); }

        public override bool Contains(WSTableParam param)
        {
            try
            {
                return IsValid && Field.Match(param);
            }
            catch (Exception) { }
            return false;
        }
        public static class GLOBAL_OPERATIONS
        {
            public static readonly WSValueOperation Equal = new WSValueOperation("Equal",       WSOperation.OperatorChars.Equal,    new List<string> { "equals", "is", "er" });
            public static readonly WSValueOperation NotEqual = new WSValueOperation("NotEqual", WSOperation.OperatorChars.NotEqual, new List<string> { "isnot", "not", "ikke", "except", "notequal", "notequals" });
        }
    }
}