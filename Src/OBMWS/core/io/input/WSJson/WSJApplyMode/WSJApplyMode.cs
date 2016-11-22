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
    public class WSJApplyMode
    {
        internal WSJApplyMode(int code, string name) { CODE = code; NAME = name; }
        public int CODE { get; } = 0;
        public string NAME { get; } = string.Empty;
        public override string ToString() { return "{" + CODE + ":" + NAME + "}"; }
        public override int GetHashCode() { return ToString().GetHashCode(); }
        public override bool Equals(object obj) { if (obj == null || obj.GetType() != typeof(WSJApplyMode) || !obj.Equals(this)) return false; return true; }

        public static class MODE
        {
            public static readonly WSJApplyMode KEY = new WSJApplyMode(0, "Key");
            public static readonly WSJApplyMode VALUE  = new WSJApplyMode(1, "Value");
            public static readonly WSJApplyMode BYTE = new WSJApplyMode(2, "Byte");
        }
    }
}