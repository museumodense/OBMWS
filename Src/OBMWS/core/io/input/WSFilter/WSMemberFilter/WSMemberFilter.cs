using System;
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
    public abstract class WSMemberFilter : WSFilter
    {
        public WSMemberFilter(Type _Type, Expression _Member)
        {
            type = _Type;
            member = _Member;
        }
        internal Type type { get; private set; }
        internal Expression member { get; set; }
        internal WSOperation operation { get; set; }

        public abstract bool IsValid { get; }
        public abstract Expression ToExpression();
        public Expression<Func<A, bool>> ToLambda<A>(ParameterExpression id) { try { Expression expr = ToExpression(); return expr == null ? null : Expression.Lambda<Func<A, bool>>(expr, id); } catch (Exception) { } return null; }

        public abstract bool Contains(WSTableParam param);
    }
}