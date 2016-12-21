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
    public abstract class WSSchema : WSBaseSchema
    {
        internal MetaFunctions Func = null;
        public WSEntitySchema Parent = null;
        public string Name = string.Empty;
        public WSSchema(MetaFunctions _Func, WSEntitySchema _Parent) { Func = _Func; Parent = _Parent; }
        
        public abstract bool IsValid { get; }
        public WSFilter GetMainFilter(WSRequest Request, Expression _EntityExpression, int _level)
        {
            WSCombineFilter mainFilter = new WSCombineFilter(WSCombineFilter.SQLMode.AndAlso);

            WSFilter BaseFilter = GetBaseFilter(Request, _EntityExpression, _level);
            if (BaseFilter != null && BaseFilter.IsValid) { mainFilter.Save(BaseFilter); }

            WSFilter CustomFilter = GetCustomFilter(_EntityExpression, _level);
            if (CustomFilter != null && CustomFilter.IsValid) { mainFilter.Save(CustomFilter); }

            return mainFilter != null && mainFilter.IsValid ? mainFilter.Reduce() : null;
        }
        public abstract WSFilter GetBaseFilter(WSRequest Request, Expression _member, int _level, WSJson BaseFilter = null);
        public abstract WSFilter GetCustomFilter(Expression _member, int _level);
    }
}