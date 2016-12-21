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
    public abstract class WSFieldSchema : WSMemberSchema
    {
        public WSTableParam param { get; private set; }
        public WSFieldSchema(MetaFunctions _Func, WSTableParam _param, WSJProperty _Json, WSEntitySchema _Parent) : base(_Func, _Parent)
        {
            param = _param;
            if (_Json != null)
            {
                Name = _Json.Key;
                IOBaseOptions.Save(_Json.Value);
            }
        }
        public override bool IsFiltrable
        {
            get
            {
                if (_IsFiltrable == null)
                {
                    _IsFiltrable = base.IsFiltrable && (
                        IOBaseOptions != null && !IOBaseOptions.IsEmpty
                    );
                }
                return (bool)_IsFiltrable;
            }
        }
        private bool? _IsFiltrable = null;
        public override WSFilter GetBaseFilter(WSRequest Request, Expression _member, int _level, WSJson BaseFilter = null) { return null; }
        internal override void applyMembers<A>(WSDynamicResponse<A> response) { }
        public override bool IsValid { get { return param != null && param.isValid; } }
        public override string ToString() { return string.Format("WSFieldSchema[{0}]", param.ToString()); }
    }
}