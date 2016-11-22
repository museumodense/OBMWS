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
    public class WSEntityListSchema : WSEntityBaseSchema
    {
        public WSEntityListSchema(WSTableParam _Param, WSJProperty _Json, MetaFunctions _Func, WSEntitySchema _Parent) : base(_Func, _Parent)
        {
            Param = _Param;
            if (_Json != null && !_Json.IsEmpty)
            {
                if (WSEntityFilter.OPERATIONS.STATE_OPERATIONS.Any(x => x.Match(_Json.Key)))
                {
                    IOBaseOptions.Save(new WSJObject(new List<WSJProperty>() { _Json }));
                }
                else if (_Json.Value != null)
                {
                    if (_Json.Value is WSJArray)
                    {
                        WSJArray arr = (WSJArray)_Json.Value;
                        WSJArray temp = new WSJArray();
                        foreach (WSJson item in arr.Value)
                        {
                            if (item is WSJValue && WSEntityFilter.OPERATIONS.STATE_OPERATIONS.Any(x => x.Match(((WSJValue)item).Value)))
                            {
                                IOBaseOptions.Save(item);
                            }
                            else
                            {
                                temp.Value.Add(item);
                            }
                        }
                        _Json.Value = temp;
                    }
                    else if (_Json.Value is WSJObject)
                    {
                        WSJObject obj = (WSJObject)_Json.Value;
                        List<WSJProperty> tempItems = new List<WSJProperty>();
                        WSJObject temp = new WSJObject(tempItems);
                        WSJObject baseLocal = new WSJObject(tempItems);
                        foreach (WSJProperty item in obj.Value)
                        {
                            if (WSEntityFilter.OPERATIONS.STATE_OPERATIONS.Any(x => x.Match(item.Key)))
                            {
                                baseLocal.Value.Add(item);
                            }
                            else if (item.Value is WSJValue && WSEntityFilter.OPERATIONS.STATE_OPERATIONS.Any(x => x.Match(((WSJValue)item.Value).Value)))
                            {
                                baseLocal.Value.Add(item);
                            }
                            else if (item.Value is WSJArray && !item.Value.IsEmpty && !((WSJArray)item.Value).Value.Any(v => !(v is WSJValue) || !WSEntityFilter.OPERATIONS.STATE_OPERATIONS.Any(x => x.Match(((WSJValue)v).Value))))
                            {
                                baseLocal.Value.Add(item);
                            }
                            else if (item.Value is WSJObject && !item.Value.IsEmpty && !((WSJObject)item.Value).Value.Any(p => !(p.Value is WSJValue) || !WSEntityFilter.OPERATIONS.STATE_OPERATIONS.Any(x =>x.Match(((WSJValue)p.Value).Value))))
                            {
                                baseLocal.Value.Add(item);
                            }
                            else
                            {
                                temp.Value.Add(item);
                            }
                        }
                         _Json.Value = temp;
                        if (!baseLocal.IsEmpty) { IOBaseOptions.Save(baseLocal); }
                    }
                }
            }
            if (Param != null)
            {
                EntitySchema = new WSEntitySchema((WSTableSource)Func.GetSourceByType(Param.DataType.GetEntityType()), _Json, Func, Parent);
                if (EntitySchema != null) { Name = EntitySchema.Name; }
            }
        }
        public WSTableParam Param { get; private set; }
        public WSEntitySchema EntitySchema { get; private set; }

        public override WSFilter GetCustomFilter( Expression parent, int level)
        {
            try
            {
                if (parent != null && IsFiltrable && EntitySchema != null)
                {
                    level++;
                    Expression member = Expression.Property(parent, Param.WSColumnRef.NAME);
                    if (member != null)
                    {
                        ParameterExpression id = Expression.Parameter(EntitySchema.Source.ReturnType, level.ToHex());

                        WSFilter subFilter = EntitySchema.GetCustomFilter(id, level);

                        if (subFilter != null && subFilter.IsValid)
                        {
                            dynamic subExpr = subFilter.GetType().GetMethod("ToLambda").MakeGenericMethod(new Type[] { EntitySchema.Source.ReturnType }).Invoke(subFilter, new object[] { id });

                            return new WSEntityListFFilter(Param, member, WSEntityListFFilter.OPERATIONS.Any) { Value = (subExpr == null) ? true : subExpr };
                        }
                    }
                }
            }
            catch (Exception e) { WSStatus status = WSStatus.NONE.clone(); Func.RegError(GetType(), e, ref status); }
            return null;
        }
        public override bool IsFiltrable
        {
            get
            {
                if (_IsFiltrable == null)
                {
                    _IsFiltrable = base.IsFiltrable || EntitySchema.IsFiltrable;
                }
                return (bool)_IsFiltrable;
            }
        }
        private bool? _IsFiltrable = null;
        public override WSFilter GetBaseFilter(WSRequest Request, Expression _member, int _level, WSJson BaseFilter = null) {
            return null;
        }
        internal override void applyMembers<A>(WSDynamicResponse<A> response)
        {
            try
            {
                if (EntitySchema != null) { EntitySchema.apply(response); }
            }
            catch (Exception e) { Func.RegError(GetType(), e, ref response.iostatus); }
        }
        
        public override bool IsValid { get { return Param != null && Param.isValid; } }
        public override string ToString() { return string.Format("WSEntityListSchema[{0}]", Param.ToString()); }
        internal void Merge(WSFieldSchema field) { try { throw new NotImplementedException("Under construction"); } catch (Exception e) { WSStatus status = WSStatus.NONE.clone(); Func.RegError(GetType(), e, ref status); } }

        internal override WSMemberSchema Clone(WSTableSource src)
        {
            return new WSEntityListSchema(Param.Clone(), null, Func, src.BaseSchema)
            {
                EntitySchema = (WSEntitySchema)EntitySchema.Clone(src),
                Name = Name,
                Param = Param.Clone()
            };
        }
    }
}