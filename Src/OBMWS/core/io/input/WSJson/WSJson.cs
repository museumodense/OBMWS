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
    public abstract class WSJson
    {
        public abstract bool IsValid { get; }
        public abstract bool IsEmpty { get; }
        public abstract WSFilter GetFieldFilter(MetaFunctions CFunc, WSTableParam param, Expression parent, int level, string state = null, bool? negate = null);
        public abstract WSFilter GetOptionFilter(MetaFunctions CFunc, Expression parent, int level, string state = null, bool? negate = null);
        public abstract Expression SortTable<T>(MetaFunctions CFunc, WSDataContext dc, List<PropertyInfo> parents, Expression expression, ref WSStatus iostatus);
        public abstract string JString { get; }
        public abstract string NiceUrlString { get; }

        internal Expression SortPrimitiveType<TEntity>(ITable initSource, ITable source, WSParam param, bool IsDesc, List<PropertyInfo> parents, Expression expression, ref WSStatus iostatus)
        {
            //EXAMPLE: event.json?sort={EventID:asc} :=> Events = db.Events.OrderBy(p => p.EventID);
            try
            {
                if (source != null && param != null && parents != null)
                {
                    Expression sourceExpr = source.Expression;
                    string command = expression == null ? "OrderBy" : "ThenBy";
                    expression = expression == null ? initSource.Expression : expression;

                    command = IsDesc ? (command + "Descending") : command;                    //{OrderBy} / {OrderByDescending}
                    ParameterExpression parameter = Expression.Parameter(typeof(TEntity), "p");                     //{p}
                    List<Type> pTypes = new List<Type> { typeof(TEntity), parents.LastOrDefault().PropertyType };

                    Expression innerExpr = CreateSortExpression(parameter, IsDesc, parents, 0);

                    LambdaExpression lExpr = Expression.Lambda(innerExpr, parameter);               //{p=>p.EventID} / {x=>x.Organization.ID}
                    UnaryExpression uExpr = Expression.Quote(lExpr);                                //{p=>p.EventID} / {x=>x.Organization.ID}

                    //public static IOrderedQueryable<TSource> OrderBy<TSource, TKey>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector);
                    expression = Expression.Call(
                        typeof(Queryable),
                        command,                        //"OrderBy"                                 OrderBy
                        pTypes.ToArray(),               //{Event,Int32}                             <TSource, TKey>
                        expression,                     //{db.Events}                               this IQueryable<TSource> source
                        uExpr                           //{p=>p.EventID} / {x=>x.Organization.ID}   Expression<Func<TSource, TKey>> keySelector
                    );
                    //}
                }
            }
            catch (Exception) { }
            return expression;
        }
        
        internal Expression CreateSortExpression(Expression member, bool IsDesc, List<PropertyInfo> props, int offset)
        {
            if (props.Count > offset)
            {
                PropertyInfo pInfo = props[offset];
                offset++;
                if (pInfo.PropertyType.IsCollectionOf<WSEntity>())//    {p => p.EventCalendars.Max(c => c.StartDate)}
                {
                    Type cType = pInfo.PropertyType;
                    member = Expression.Property(member, pInfo);                                        //{p.EventCalendars}

                    Type innerType = pInfo.PropertyType.GetEntityType();                                //{EventCalendar}
                    ParameterExpression innerParameter = Expression.Parameter(innerType, "c");          //{c}
                    pInfo = props[offset];                                                              //{StartDate}
                    offset++;
                    Expression innerMember = Expression.Property(innerParameter, pInfo);                //{c.StartDate}

                    LambdaExpression innerExpr = Expression.Lambda(innerMember, innerParameter);                                //{c=>c.StartDate}                                      Func<TSource, TResult> selector
                    Type[] GenericArguments = new Type[] { innerType, pInfo.PropertyType };                                     //<EventCalendar, DateTime?>                            <TSource, TResult>
                    Type[] InputParamTypes = new[] { cType, typeof(Func<,>).MakeGenericType(innerType, pInfo.PropertyType) };   //{p.EventCalendars, Func<EventCalendar, DateTime?>}   (this IEnumerable<TSource> source, Func<TSource, TResult> selector)

                    //public static TResult Max<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector);
                    MethodInfo method = (MethodInfo)GetGenericMethod(typeof(Enumerable), IsDesc ? "Max" : "Min", GenericArguments, InputParamTypes, BindingFlags.Static);

                    Expression subMember = Expression.Call(
                        method,     //Max                           public static TResult Max<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector);
                        member,     //{p.EventCalendars}            this IEnumerable<TSource> source
                        innerExpr   //{c=>c.StartDate}              Func<TSource, TResult> selector
                    );
                    member = CreateSortExpression(subMember, IsDesc, props, offset);
                }
                else
                {
                    member = CreateSortExpression(Expression.Property(member, pInfo), IsDesc, props, offset);
                }
            }
            return member;
        }
        internal MethodBase GetGenericMethod(Type type, string name, Type[] GenericArguments, Type[] InputParamTypes, BindingFlags flags)
        {
            var methods = type.GetMethods()
                .Where(m => m.Name == name && m.GetGenericArguments().Length == GenericArguments.Length)
                .Select(m => m.MakeGenericMethod(GenericArguments));
            return Type.DefaultBinder.SelectMethod(flags, methods.ToArray(), InputParamTypes, null);
        }

        public abstract WSJson Clone();

        private bool? applied;
        public bool apply(WSRequest Request, MetaFunctions CFunc) { if (applied == null) { applied = false; applied = applyInternal(Request, CFunc); } return (bool)applied; }
        internal abstract bool applyInternal(WSRequest request, MetaFunctions CFunc);

        public abstract bool Match(WSJson json, out WSStatus status);
        public bool Match(WSJson json) { WSStatus status = WSStatus.NONE_Copy(); return Match(json, out status); }

        public abstract bool MatchEntity(MetaFunctions CFunc, WSDynamicEntity entity, WSTableSource src, string key = null, string matchOperation = null);
    }
}