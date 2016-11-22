using System.Collections.Generic;

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
    public abstract class WSOperation : WSValue
    {

        public string OperatorChar { get; private set; }
        public WSOperation(string _name, OperatorChars _operatorChar, List<string> _aliaces = null) : base(_name, _aliaces)
        {
            OperatorChar = Operators[_operatorChar];
        }
        public Dictionary<OperatorChars, string> Operators = new Dictionary<OperatorChars, string>
        {
            {OperatorChars.Equal, "="},
            {OperatorChars.NotEqual, "!="},
            {OperatorChars.GreaterThan,">" },
            {OperatorChars.GreaterThanOrEqual,">=" },
            {OperatorChars.LessThan,"<" },
            {OperatorChars.LessOrEqual,"<=" },
            {OperatorChars.Like,"≈" },
            {OperatorChars.NotLike,"!≈" },
            {OperatorChars.StartsWith,".StartsWith:" },
            {OperatorChars.NotStartsWith,".!StartsWith:" },
            {OperatorChars.EndsWith,".EndsWith:" },
            {OperatorChars.NotEndsWith ,"!EndsWith:" },
            {OperatorChars.Empty ,"empty" },
            {OperatorChars.Exist,"exists" },
            {OperatorChars.Any,".Any:" },
            {OperatorChars.Filter,".Filter:" },
            {OperatorChars.IsOwn,".IsOwn:" },
            {OperatorChars.WeekDayEqual,".weekDay:" },
            {OperatorChars.None,".None:" }
        };
        public enum OperatorChars : sbyte
        {
            Equal = 1,
            NotEqual = 2,
            GreaterThan = 3,
            GreaterThanOrEqual = 4,
            LessThan = 5,
            LessOrEqual = 6,
            Like = 7,
            NotLike = 8,
            StartsWith = 9,
            NotStartsWith = 10,
            EndsWith = 11,
            NotEndsWith = 12,
            Empty = 13,
            Exist = 14,
            Any = 15,
            Filter = 16,
            IsOwn = 17,
            WeekDayEqual = 18,
            None = 19
        }
    }
}