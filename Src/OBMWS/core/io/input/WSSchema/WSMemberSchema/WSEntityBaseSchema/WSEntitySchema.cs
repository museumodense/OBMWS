using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

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
    public class WSEntitySchema : WSEntityBaseSchema
    {
        #region DECLARATION
        private bool Failed = false;
        public WSTableSource Source = null;
        public WSFieldFilters Fields = new WSFieldFilters();
        public WSFieldFilters Filters = new WSFieldFilters();
        public WSCombineFilter.SQLMode CombineMode = WSCombineFilter.SQLMode.AndAlso;

        public bool IsValidSchema { get; private set; } = true;
        public WSJObject OriginalJson { get; private set; }
        #endregion

        #region INIT by WSTableSource
        public WSEntitySchema(MetaFunctions _Func, WSTableSource _Source = null, IEnumerable<WSTableSource> sources = null) : base(_Func, null)
        {
            try {
                if(_Source != null)
                {
                    Source = _Source;
                    Name = _Source.NAME.ToLower();
                    if (_Source != null && _Source.UserRole >= _Source.AccessLevel)
                    {
                        foreach (WSTableParam param in _Source.Params) { parseBaseParam(param, sources); }
                    }
                }
            }
            catch (Exception e) { Failed = true; WSStatus status = WSStatus.NONE.clone(); Func.RegError(GetType(), e, ref status); }
        }
        private void parseBaseParam(WSTableParam param, IEnumerable<WSTableSource> sources = null)
        {
            try
            {
                if (param == null) { }
                else
                {
                    bool isValidFields = (Fields == null || !Fields.Any(x => x is WSFieldSchema && ((WSFieldSchema)x).param.Match(param.NAME,sources)));
                    if (!isValidFields) { }
                    else
                    {
                        if (param.DataType.IsSimple() || param.DataType.IsSimpleCollection())
                        {
                            Fields.Add(new WSPrimitiveFieldSchema(Func, param, new WSJProperty(param.DISPLAY_NAME.ToLower(), new WSJArray()), this));
                        } else {
                            Fields.Add(new WSEntityFieldSchema(Func, param, new WSJProperty(param.DISPLAY_NAME.ToLower(), new WSJArray()), this));
                        }
                    }
                }
            }
            catch (Exception e) { Failed = true; WSStatus status = WSStatus.NONE.clone(); Func.RegError(GetType(), e, ref status); }
        }
        #endregion

        #region INIT by Json
        public WSEntitySchema(WSTableSource _Source, WSJProperty _Json, MetaFunctions _Func, WSEntitySchema _Parent = null, bool _IsValidSchema = true) : base(_Func, _Parent)
        {
            try
            {
                IsValidSchema = _IsValidSchema;
                Source = _Source;
                setUp(_Json);
            }
            catch (Exception e) { Failed = true; WSStatus status = WSStatus.NONE.clone(); Func.RegError(GetType(), e, ref status); }
        }
        private void setUp(WSJProperty _Json)//Json = > {source:[id,name,...]}  or {source:{id:1,name:abc,...}}  or {source:{filters:[],fields:{id:1,name:abc,...}}}
        {
            if (Source != null && _Json.IsValid)
            {
                OriginalJson = new WSJObject(new List<WSJProperty> { _Json });
                Name = _Json.Key;

                List<WSJson> jFields = new List<WSJson>();

                CombineMode = _Json.Value is WSJObject ? WSCombineFilter.SQLMode.AndAlso : _Json.Value is WSJArray ? CombineMode = WSCombineFilter.SQLMode.OrElse : WSCombineFilter.SQLMode.AndAlso;
                Fields.CombineMode = CombineMode;

                if (_Json.Value is WSJObject)       //Json = > {source:/***{JFIELDS}***/}  or {source:/***{filters:JFIELDS,fields:JFIELDS}***/}
                {                                   //                     ---------                      --------------------------------
                    jFields.AddRange(((WSJObject)_Json.Value).Value);
                }
                else if (_Json.Value is WSJArray)   //Json = > {source:/***[JFIELDS]***/}  or {source:/***[{filters:JFIELDS},{fields:JFIELDS}]***/}
                {                                   //                     ---------                      ------------------------------------
                    jFields.AddRange(((WSJArray)_Json.Value).Value);
                }
                else if (_Json.Value is WSJValue)   //Json = > {source:/***[JFIELD]***/}  or {source:/***[{filters:JFIELD},{fields:JFIELD}]***/}
                {                                   //                     ---------                      ------------------------------------
                    jFields.Add(_Json.Value);
                }

                foreach (WSJson item in jFields){proceedRootParam(item);}
            }
        }
        
        #region proceed root parameters
        private void proceedRootParam(WSJson param)
        {
            if (param is WSJValue) { proceedRootParam((WSJValue)param); }
            else if (param is WSJProperty) { proceedRootParam((WSJProperty)param); }
            else if (param is WSJObject) { proceedRootParam((WSJObject)param); }
            else if (param is WSJArray) { proceedRootParam((WSJArray)param); }
        }
        private void proceedRootParam(WSJValue param)
        {
            if (!WSConstants.ALIACES.SCHEMA.Match(param.Value) && !WSConstants.ALIACES.OUTPUT.Match(param.Value))
            {
                proceedFieldFilter(param, ref Fields);
            }
        }
        private void proceedRootParam(WSJProperty jProp)
        {
            if (WSConstants.ALIACES.SCHEMA.Match(jProp.Key))
            {
                List<WSJson> items = jProp.Value is WSJArray ? ((WSJArray)jProp.Value).Value : jProp.Value is WSJObject ? ((WSJObject)jProp.Value).Value.ToList<WSJson>() : new List<WSJson> { jProp.Value };
                Filters.CombineMode = jProp.Value is WSJArray ? WSCombineFilter.SQLMode.OrElse : WSCombineFilter.SQLMode.AndAlso;
                foreach (WSJson item in items)
                {
                    proceedFieldFilter(item, ref Filters);
                }
            }
            else if (WSConstants.ALIACES.OUTPUT.Match(jProp.Key))
            {
                List<WSJson> items = jProp.Value is WSJArray ? ((WSJArray)jProp.Value).Value : jProp.Value is WSJObject ? ((WSJObject)jProp.Value).Value.ToList<WSJson>() : new List<WSJson> { jProp.Value };
                Fields.CombineMode = jProp.Value is WSJArray ? WSCombineFilter.SQLMode.OrElse : WSCombineFilter.SQLMode.AndAlso;
                foreach (WSJson item in items)
                {
                    proceedFieldFilter(item, ref Fields);
                }
            }
            else
            {
                proceedFieldFilter(jProp, ref Fields);
            }
        }
        private void proceedRootParam(WSJObject jObj)
        {
            if (jObj.Value.Any())
            {
                proceedRootParam(jObj.Value[0]);
            }
        }
        private void proceedRootParam(WSJArray jArr)
        {
            if (jArr.Value.Any())
            {
                proceedRootParam(jArr.Value[0]);
            }
        }
        #endregion

        #region proceed field filter
        private void proceedFieldFilter(WSJson json, ref WSFieldFilters filters)
        {
            if (json is WSJValue) { proceedFieldFilter((WSJValue)json, ref filters); }
            else if (json is WSJProperty) { proceedFieldFilter((WSJProperty)json, ref filters); }
            else if (json is WSJObject) { proceedFieldFilter((WSJObject)json, ref filters); }
            else if (json is WSJArray) { proceedFieldFilter((WSJArray)json, ref filters); }
        }
        private void proceedFieldFilter(WSJValue jField, ref WSFieldFilters filters)
        {
            bool replace = true;
            List<WSMemberSchema> schemas = readFieldSchema(jField, out replace);
            if (schemas!=null && schemas.Any())
            {
                foreach (WSMemberSchema schema in schemas)
                {
                    saveFieldSchema(schema, replace, ref filters);
                }
            }
            else
            {
                IOBaseOptions.Save(jField);
            }
        }
        private void proceedFieldFilter(WSJProperty jField, ref WSFieldFilters filters)
        {
            bool replace = true;
            List<WSMemberSchema> schemas = readFieldSchema(jField, out replace);
            if (schemas.Any())
            {
                foreach (WSMemberSchema schema in schemas)
                {
                    saveFieldSchema(schema, replace, ref filters);
                }
            }
            else
            {
                IOBaseOptions.Save(new WSJObject(new List<WSJProperty> { jField }));
            }
        }
        private void proceedFieldFilter(WSJObject jObj, ref WSFieldFilters filters)
        {
            //TODO@ANDVO : make it possible to add all comming properties!!!
            proceedFieldFilter(jObj.Value[0], ref filters);
        }
        private void proceedFieldFilter(WSJArray jArr, ref WSFieldFilters filters)
        {
            proceedFieldFilter(jArr.Value[0], ref filters);
        }
        private bool saveFieldSchema(WSMemberSchema field, bool replace, ref WSFieldFilters filters)
        {
            if (field != null)
            {
                Func<WSMemberSchema, bool> func = x => x is WSFieldSchema && ((WSFieldSchema)x).param.Match(field.Name);
                if (!filters.Any(func)) { filters.Add(field); }
                else {
                    if (replace)
                    {
                        /*  TODO@ANDVO : 2016-03-08 : combine redundant field's options somehow,... (when field used more than once in schema)
                        *   fx.: 'userid:{less:10}&schema={user:[*,{userid:{more:1}}]}'
                        *   note: ofcourse we can combine them like: 'schema={user:[*,{userid:{less:10,more:1}}]}',
                        *   but what if there will be need for option's separation as : AND <-> OR?  
                        */

                        filters[filters.IndexOf(filters.FirstOrDefault(func))] = field;
                    }
                }
                return true;
            }
            return false;
        }
        private List<WSMemberSchema> readFieldSchema(WSJson json, out bool replace)// json => WSJValue || WSJProperty || WSJObject
        {
            replace = true;

            if (json is WSJValue) { return readFieldSchema(new WSJProperty(((WSJValue)json).Value, new WSJArray()), out replace); }
            else if (json is WSJProperty) { return readFieldSchema((WSJProperty)json, out replace); }
            else if (json is WSJObject) { return readFieldSchema(((WSJObject)json).Value[0], out replace); }
            else if (json is WSJArray) { return readFieldSchema(((WSJArray)json).Value[0], out replace); }

            return null;
        }
        private List<WSMemberSchema> readFieldSchema(WSJProperty json, out bool replace)// json => WSJValue || WSJProperty || WSJObject
        {
            replace = true;
            List<WSMemberSchema> fSchema = new List<WSMemberSchema>();
            try
            {
                if (json != null && Source != null)
                {
                    if (!string.IsNullOrEmpty(json.Key))
                    {
                        WSTableParam param = (WSTableParam)Source.GetXParam(json.Key);
                        if (param != null && param.isValid)
                        {
                            replace = true;
                            if (param.DataType.IsSimple() || param.DataType.IsSimpleCollection())
                            {
                                fSchema.Add(new WSPrimitiveFieldSchema(Func, param, json, this));
                            }
                            else if(json.IsValid)
                            {
                                WSTableSource source = (WSTableSource)Func.GetSourceByType(param.DataType.GetEntityType());
                                if (source != null)
                                {
                                    if(param.Match(json.Key))
                                    {
                                        if (param.DataType.IsSameOrSubclassOf(typeof(WSEntity)))
                                        {
                                            fSchema.Add(new WSEntitySchema(source, json, Func, this));
                                        }
                                        else if (param.DataType.IsCollectionOf<WSEntity>())
                                        {
                                            fSchema.Add(new WSEntityListSchema(param, json, Func, this));
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            #region SET '*' [ALL PRIMITIVE FIELDS] Filter
                            if (WSConstants.ALIACES.ALL_PRIMITIVE_FIELDS.Match(json.Key))
                            {
                                replace = false;

                                IEnumerable<WSTableParam> DBPrimitiveParams = 
                                    Source.DBPrimitiveParams.Any() ? 
                                    Source.DBPrimitiveParams.Where(x => Func.IsAccessible(x.READ_ACCESS_MODE.ACCESS_LEVEL)) : 
                                    new List<WSTableParam>();
                                IEnumerable<WSJValue> all_primitive_params = 
                                    DBPrimitiveParams.Any() ? 
                                    DBPrimitiveParams.Select(x => new WSJValue(x.DISPLAY_NAME)) : 
                                    new List<WSJValue>();

                                foreach (WSJValue _item in all_primitive_params)
                                {
                                    WSMemberSchema schema = Source.BaseSchema.Fields.FirstOrDefault(x => x.Name.Equals(_item.Value));
                                    if (schema != null) { fSchema.Add(schema); }
                                }
                            }
                            #endregion
                        }
                    }
                }
            }
            catch (Exception e) { WSStatus status = WSStatus.NONE.clone(); Func.RegError(GetType(), e, ref status); }
            return fSchema;
        }

        private WSJProperty mergeFilters(WSJson baseFilter, WSJProperty dynamicFilter)
        {
            WSJProperty json = null;
            if (baseFilter == null || !baseFilter.IsValid) { json = dynamicFilter; }
            else
            {
                List<WSJProperty> fProps = new List<WSJProperty>();
                json = new WSJProperty(Source.NAME, new WSJObject(fProps));
                foreach (WSTableParam param in Source.DBParams)
                {
                    List<WSJProperty> mProps = new List<WSJProperty>();

                    WSJProperty bpjValue = getFieldJson(param, baseFilter);
                    if (bpjValue != null && bpjValue.IsValid)
                    {
                        mProps.Add(bpjValue);
                    }

                    if (dynamicFilter != null && dynamicFilter.IsValid)
                    {
                        WSJProperty dpjValue = getFieldJson(param, dynamicFilter);
                        if (dpjValue != null && dpjValue.IsValid)
                        {
                            mProps.Add(dpjValue);
                        }
                    }

                    fProps.AddRange(mProps);
                }
            }
            return json;
        }

        private WSJProperty getFieldJson(WSTableParam param, WSJson json)
        {
            WSJProperty fJson = null;
            if (json is WSJObject)
            {
                fJson = ((WSJObject)json).Value.FirstOrDefault(x => param.Match(x.Key));
            }
            else if(json is WSJArray)
            {
                fJson = ((WSJArray)json).Value.OfType<WSJObject>().FirstOrDefault(x => param.Match(x.Value[0].Key)).Value[0];
            }
            return fJson;
        }
        #endregion

        internal override void applyMembers<A>(WSDynamicResponse<A> response) { if(response!=null) try
        {
            if (Fields != null && Fields.Any())
            {
                foreach (WSMemberSchema field in Fields)
                {
                    field.apply(response);
                }
            }
            if (Filters != null && Filters.Any())
            {
                foreach (WSMemberSchema field in Filters)
                {
                    field.apply(response);
                }
            }
        } catch (Exception e) { Func.RegError(GetType(), e, ref response.iostatus); } }
        
        #endregion

        public override WSFilter GetBaseFilter(WSRequest Request, Expression _member, int _level, WSJson _BaseFilter = null)
        {
            WSJson BaseFilter = _BaseFilter== null ? Source.BaseFilter : _BaseFilter;
            WSCombineFilter filter = new WSCombineFilter(WSCombineFilter.SQLMode.AndAlso);
            if (filter != null && BaseFilter != null && BaseFilter.IsValid)
            {
                Expression member = null;
                filter = BaseFilter is WSJArray ? new WSCombineFilter(WSCombineFilter.SQLMode.OrElse) : filter;

                BaseFilter.apply(Request, Func);

                if (BaseFilter != null && BaseFilter.IsValid)
                {
                    if (_member.Type == Source.ReturnType) { member = _member; }
                    else if (Parent != null)
                    {
                        WSTableParam paramExt = (WSTableParam)Parent.Source.GetXParam(Name);
                        if (paramExt != null) { member = Expression.Property(_member, paramExt.WSColumnRef.NAME); }
                    }

                    if (member != null)
                    {
                        if (BaseFilter is WSJValue)
                        {
                            if (((WSJValue)BaseFilter).Value.IsTrue() || ((WSJValue)BaseFilter).Value.IsFalse())
                            {
                                WSFilter subFilter = ((WSJValue)BaseFilter).GetOptionFilter(Func, member, _level);
                                if (subFilter != null) { filter.Add(subFilter); }
                            }
                        }
                        else if (BaseFilter is WSJObject)
                        {
                            foreach (WSJProperty item in ((WSJObject)BaseFilter).Value)
                            {
                                bool replace = true;
                                List<WSMemberSchema> _schemas = readFieldSchema(item, out replace);
                                if (_schemas.Any())
                                {
                                    foreach (WSMemberSchema _schema in _schemas)
                                    {
                                        WSFilter subFilter = _schema.GetCustomFilter(member, _level);
                                        if (subFilter != null) { filter.Add(subFilter); }
                                    }
                                }
                            }
                        }
                        else if (BaseFilter is WSJArray)
                        {
                            foreach (WSJson item in ((WSJArray)BaseFilter).Value)
                            {
                                bool replace = true;
                                List<WSMemberSchema> _schemas = readFieldSchema(item, out replace);
                                if (_schemas.Any())
                                {
                                    foreach (WSMemberSchema _schema in _schemas)
                                    {
                                        WSFilter subFilter = _schema.GetCustomFilter(member, _level);
                                        if (subFilter != null) { filter.Add(subFilter); }
                                    }
                                }
                                else if (item is WSJValue && (((WSJValue)item).Value.IsTrue() || ((WSJValue)item).Value.IsFalse()))
                                {
                                    WSFilter subFilter = ((WSJValue)item).GetOptionFilter(Func, member, _level);
                                    if (subFilter != null) { filter.Add(subFilter); }
                                }
                            }
                        }
                    }
                }
            }
            WSFilter result = filter != null && filter.IsValid ? filter.Reduce() : null;
            return result;
        }
        public override WSFilter GetCustomFilter(Expression _EntityExpression, int _level)
        {
            WSCombineFilter mainFilter = !IsFiltrable ? null : new WSCombineFilter(CombineMode);
            if (mainFilter != null && _EntityExpression != null)
            {
                Expression member = null;
                if (_EntityExpression.Type == Source.ReturnType) { member = _EntityExpression; }
                else if (Parent != null)
                {
                    WSTableParam paramExt = (WSTableParam)Parent.Source.GetXParam(Name);
                    if (paramExt != null) { member = Expression.Property(_EntityExpression, paramExt.WSColumnRef.NAME); }
                }

                if (member != null && IsFiltrable)
                {
                    if (Fields != null && Fields.Any())
                    {
                        WSCombineFilter cFields = new WSCombineFilter(Fields.CombineMode);
                        mainFilter.Save(cFields);
                        foreach (WSSchema field in Fields)
                        {
                            cFields.Save(field.GetCustomFilter(member, _level));
                        }
                    }
                    if (Filters != null && Filters.Any())
                    {
                        WSCombineFilter cFilters = new WSCombineFilter(Filters.CombineMode);
                        mainFilter.Save(cFilters);
                        foreach (WSSchema field in Filters)
                        {
                            cFilters.Save(field.GetCustomFilter(member, _level));
                        }
                    }
                }

                if (IOBaseOptions != null && !IOBaseOptions.IsEmpty) { mainFilter.Save(IOBaseOptions.GetOptionFilter(Func, member, _level + 1)); }
            }
            return mainFilter != null && mainFilter.IsValid ? mainFilter.Reduce() : mainFilter;
        }
        public override bool IsFiltrable
        {
            get
            {
                if (_IsFiltrable == null)
                {
                    _IsFiltrable = base.IsFiltrable || HasFilters();
                }
                return (bool)_IsFiltrable;
            }
        }
        private bool? _IsFiltrable = null;
        internal bool HasFilters()
        {
            bool fieldsFiltrable = Fields.Any(f => f.IsFiltrable);
            bool filtersFiltrable = Filters.Any(f => f.IsFiltrable);
            bool baseFiltersFiltrable = Source.BaseFilter!=null && Source.BaseFilter.IsValid;
            return fieldsFiltrable || filtersFiltrable || baseFiltersFiltrable;
        }

        #region OVERRITEN METHODS
        public override bool IsValid
        {
            get {
                if (Failed){
                    return false;
                }else if (!Fields.Any()){
                    return false;
                }else if (Fields.Any(f => !f.IsValid)){
                    return false;
                }
                return true;
            }
        }
        public override string ToString()
        {
            return string.Format("WSEntitySchema[{0}:{1}]", Name, Source.ToString());
        }
        #endregion

        #region Html
        private string _Html = null;
        public string Html
        {
            get
            {
                if (Source != null && !Source.IsBase && string.IsNullOrEmpty(_Html))
                {
                    StringBuilder sb = new StringBuilder();
                    string isChecked = (Parent==null?" checked":" unchecked");
                    sb.Append("<div class=\"j-schema" + isChecked + "\" title=\"schema:" + Name.ToLower() + "\">");
                        sb.Append("<div class=\"j-shadow checkable\">");
                            sb.Append("<div class=\"j-key\">");
                                sb.Append("{<span>{" + Name.ToLower() + ":[...]}</span>");
                            sb.Append("</div>");
                            sb.Append("<div class=\"j-selector\">");
                                sb.Append("<i class=\"unselected fa fa-ban\"></i>");
                            sb.Append("</div>");
                        sb.Append("</div>");
                        sb.Append("<div class=\"j-header checkable\" title=\"" + Name.ToLower() + "\">");
                            
                            sb.Append("<div class=\"j-key\">");
                                sb.Append("{<span class=\"j-title\">" + Name.ToLower() + "</span>:[");
                            sb.Append("</div>");

                            sb.Append("<div class=\"j-selector\">");
                                sb.Append("<i class=\"selected fa fa-check\"></i>");
                                sb.Append("<i class=\"unselected fa fa-ban\"></i>");
                            sb.Append("</div>");
                    
                        sb.Append("</div>");
                        sb.Append("<div class=\"j-fields sortable\">");
                        List<string> fields = new List<string>();
                        if (Fields.Any())
                        {
                            for(int i=0; i< Fields.Count;i++)
                            {
                                bool isLast = i == Fields.Count - 1;
                                WSBaseSchema schema = Fields[i];
                                if (schema is WSFieldSchema)
                                {
                                    WSFieldSchema fSchema = (WSFieldSchema)schema;
                                    sb.Append("<div class=\"j-field checkable" + isChecked + "\" title=\"" + fSchema.Name.ToLower() + "\">");
                                        sb.Append("<div class=\"j-key\">");
                                            sb.Append("<span class=\"j-title\">" + fSchema.Name.ToLower() + "</span>");
                                            sb.Append((isLast ? "" : "<span class=\"j-comma\">,</span>"));
                                        sb.Append("</div>");
                                        sb.Append("<div class=\"j-selector\">");
                                            sb.Append("<i class=\"selected fa fa-check\"></i>");
                                            sb.Append("<i class=\"unselected fa fa-ban\"></i>");
                                        sb.Append("</div>");
                                    sb.Append("</div>");
                                }
                                else if (schema is WSEntitySchema)
                                {
                                    sb.Append(((WSEntitySchema)schema).Html);
                                }
                            }
                        }
                        sb.Append("</div>");
                        sb.Append("<div class=\"j-footer\">]},</div>");
                    sb.Append("</div>");


                    _Html = sb.ToString();
                }
                return _Html;
            }
        }
        #endregion
        #region FullJson
        internal WSJObject getFullJson(byte UserRole, List<Type> readChildes = null, string aliace = null)
        {
            if (UserRole <= WSConstants.ACCESS_LEVEL.READ) { return getBaseJson(UserRole); } //make sure hide multilevel for not registered users
            else if (Source != null && !Source.IsBase)
            {
                WSJArray jFields = new WSJArray();
                WSJProperty jProp = new WSJProperty(string.IsNullOrEmpty(aliace)?Name.ToLower():aliace, jFields);
                WSJObject json = new WSJObject(new List<WSJProperty> { jProp });
                if (Fields != null && Fields.Any())
                {
                    IEnumerable<WSBaseSchema> acessibleSchemaes = Fields.Where(x =>
                        (x is WSPrimitiveFieldSchema && UserRole >= ((WSPrimitiveFieldSchema)x).param.READ_ACCESS_MODE.ACCESS_LEVEL)
                        ||
                        (x is WSEntityFieldSchema && UserRole >= ((WSEntityFieldSchema)x).SOURCE.AccessLevel)
                    );
                    if (acessibleSchemaes != null && acessibleSchemaes.Any())
                    {
                        List<WSJson> _fields = new List<WSJson>();
                        foreach (WSBaseSchema f in acessibleSchemaes)
                        {
                            if (f is WSPrimitiveFieldSchema)
                            {
                                _fields.Add(new WSJValue(((WSPrimitiveFieldSchema)f).param.DISPLAY_NAME.ToLower()));
                            }
                            else if (f is WSEntityFieldSchema)
                            {
                                List<Type> _readChildes = new List<Type>() { };
                                if (readChildes != null) { _readChildes.AddRange(readChildes); }

                                if (!_readChildes.Any(p => p == Source.ReturnType))
                                {
                                    _readChildes.Add(Source.ReturnType);
                                    _fields.Add(((WSEntityFieldSchema)f).SOURCE.BaseSchema.getFullJson(UserRole, _readChildes, ((WSEntityFieldSchema)f).param.DISPLAY_NAME.ToLower()));
                                }
                            }
                        }
                        jFields.Value = _fields != null && _fields.Any(x => x != null) ? _fields.Where(x => x != null).ToList() : new List<WSJson>();
                    }                    
                }
                return json;
            }
            return null;
        }
        #endregion
        #region BaseJson
        internal WSJObject getBaseJson(byte UserRole)
        {
            if (Source != null && !Source.IsBase)
            {
                WSJArray jFields = new WSJArray();
                WSJProperty jProp = new WSJProperty(Name.ToLower(), jFields);
                WSJObject json = new WSJObject(new List<WSJProperty> { jProp });
                try
                {
                    if (Fields != null && Fields.Any())
                    {
                        IEnumerable<WSMemberSchema> acessibleSchemaes = Fields
                            .Where(x => x is WSPrimitiveFieldSchema && UserRole >= ((WSPrimitiveFieldSchema)x).param.READ_ACCESS_MODE.ACCESS_LEVEL);
                        if (acessibleSchemaes != null && acessibleSchemaes.Any())
                        {
                            IEnumerable<WSJson> _fields = acessibleSchemaes.Select(f => new WSJValue(((WSPrimitiveFieldSchema)f).param.DISPLAY_NAME.ToLower()));
                            jFields.Value = _fields != null && _fields.Any(x => x != null) ? _fields.Where(x => x != null).ToList() : new List<WSJson>();
                        }
                    }
                }
                catch (Exception e) { WSStatus status = WSStatus.NONE.clone(); Func.RegError(GetType(), e, ref status); }
                return json;
            }
            return null;
        }
        #endregion
        #region FieldsJson
        internal WSJObject getFieldsJson(byte UserRole, List<Type> readChildes = null, string aliace = null)
        {
            if (UserRole <= WSConstants.ACCESS_LEVEL.READ) { return getBaseJson(UserRole); } //make sure hide multilevel for not registered users
            else if (Source != null && !Source.IsBase)
            {
                WSJArray jFields = new WSJArray();
                WSJProperty jProp = new WSJProperty(string.IsNullOrEmpty(aliace) ? Name.ToLower() : aliace, jFields);
                WSJObject json = new WSJObject(new List<WSJProperty> { jProp });
                if (Fields != null && Fields.Any())
                {
                    IEnumerable<WSBaseSchema> acessibleSchemaes = Fields.Where(x =>
                        (x is WSPrimitiveFieldSchema && UserRole >= ((WSPrimitiveFieldSchema)x).param.READ_ACCESS_MODE.ACCESS_LEVEL)
                        ||
                        (x is WSEntityFieldSchema && UserRole >= ((WSEntityFieldSchema)x).SOURCE.AccessLevel)
                    );
                    if (acessibleSchemaes != null && acessibleSchemaes.Any())
                    {
                        List<WSJson> _fields = new List<WSJson>();
                        foreach (WSBaseSchema f in acessibleSchemaes)
                        {
                            if (f is WSPrimitiveFieldSchema)
                            {
                                _fields.Add(new WSJValue(((WSPrimitiveFieldSchema)f).param.DISPLAY_NAME.ToLower()));
                            }
                            else if (f is WSEntityFieldSchema)
                            {
                                List<Type> _readChildes = new List<Type>() { };
                                if (readChildes != null) { _readChildes.AddRange(readChildes); }

                                if (!_readChildes.Any(p => p == Source.ReturnType))
                                {
                                    _readChildes.Add(Source.ReturnType);
                                    _fields.Add(((WSEntityFieldSchema)f).SOURCE.BaseSchema.getFullJson(UserRole, _readChildes, ((WSEntityFieldSchema)f).param.DISPLAY_NAME.ToLower()));
                                }
                            }
                        }
                        jFields.Value = _fields != null && _fields.Any(x => x != null) ? _fields.Where(x => x != null).ToList() : new List<WSJson>();
                    }
                }
                return json;
            }
            return null;
        }
        #endregion
        
        internal bool Load(IEnumerable<WSTableSource> role_sources)
        {
            if (!Fields.Any()) { return false; }
            else
            {
                foreach (WSEntityFieldSchema schema in Fields.OfType<WSEntityFieldSchema>())
                {
                    Type eType = schema.param.DataType.GetEntityType();
                    schema.SOURCE = role_sources.FirstOrDefault(x => x.ReturnType == eType);
                }
                List<string> illegalFields = Fields.OfType<WSEntityFieldSchema>().Where(x => x.SOURCE == null).Select(x => x.param.WSColumnRef.NAME).ToList();
                if (illegalFields != null && illegalFields.Any())
                {
                    foreach (string field in illegalFields) {
                        WSMemberSchema schema = Fields.FirstOrDefault(f => ((WSFieldSchema)f).param.WSColumnRef.NAME.Equals(field));
                        if (schema != null) { Fields.Remove(schema); }
                    }
                } 
                return Fields.Any();
            }
        }
        
        internal override WSMemberSchema Clone(WSTableSource src)
        {
            WSEntitySchema _Schema = new WSEntitySchema(Func)
            {
                Source = src,
                Name = src.NAME.ToLower(),
                CombineMode = CombineMode,
                Filters = Filters,
                Fields = Fields.Clone(src),
                OriginalJson = OriginalJson,
                Failed = Failed,
                Func = Func
            };
            return _Schema;
        }
    }
    public class WSFieldFilters: List<WSMemberSchema>
    {
        public WSCombineFilter.SQLMode CombineMode { get { return _CombineMode; } internal set { _CombineMode = value; } }
        private WSCombineFilter.SQLMode _CombineMode = WSCombineFilter.SQLMode.AndAlso;
        
        public override string ToString() { return $"WSFieldFilters[{Count}:{(CombineMode == WSCombineFilter.SQLMode.AndAlso ? "AndAlso" : CombineMode == WSCombineFilter.SQLMode.OrElse ? "OrElse" : "" + CombineMode)}]"; }

        internal WSFieldFilters Clone(WSTableSource src)
        {
            WSFieldFilters filters = new WSFieldFilters()
            {
                CombineMode = CombineMode
            };
            foreach (WSMemberSchema i in this)
            {
                if (i is WSPrimitiveFieldSchema)
                {
                    filters.Add(((WSPrimitiveFieldSchema)i).Clone(src));
                }
                else if (i is WSEntityFieldSchema)
                {
                    filters.Add(((WSEntityFieldSchema)i).Clone(src));
                }
            }
            return filters;
        }
    }
}