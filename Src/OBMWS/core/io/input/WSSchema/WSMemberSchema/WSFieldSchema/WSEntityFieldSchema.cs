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
    class WSEntityFieldSchema : WSFieldSchema
    {
        public WSEntityFieldSchema(MetaFunctions _Func, WSTableParam _Param, WSJProperty _Json, WSEntitySchema _Parent) : base(_Func, _Param, _Json, _Parent) { }

        public override WSFilter GetCustomFilter(Expression parent, int level)
        {
            WSFilter filter = !IsFiltrable ? null : IOBaseOptions.GetFieldFilter(Func, param, parent, level);
            return filter != null && filter.IsValid ? filter : null;
        }
        public WSTableSource SOURCE { get; internal set; }
        internal override WSMemberSchema Clone(WSTableSource _parent)
        {
            return new WSEntityFieldSchema(Func, param.Clone(), new WSJProperty(Name, IOBaseOptions.Clone()), _parent.BaseSchema) { SOURCE = SOURCE };
        }
        public override string ToString() { return $"{GetType().Name}[{param.NAME}:{(SOURCE == null ? "NONE" : SOURCE.NAME)}]"; }
    }
}