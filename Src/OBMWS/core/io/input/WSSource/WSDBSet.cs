using System;
using System.Collections.Generic;
using System.Linq;

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
    public class WSDBSet : Dictionary<string, WSSources<WSTableSource>>
    {
        public byte role { get; private set; } = WSConstants.ACCESS_LEVEL.ADMIN;
        public WSDBSet(byte _role) { role = _role; }
        public bool IsValid { get { if (_IsValid == null) { _IsValid = !this.Any(x => x.Value == null || x.Value.Any(v => !v.isValid)); } return _IsValid != null && (bool)_IsValid; } }
        
        private bool? _IsValid = null;
        
        internal bool Load(MetaFunctions CFunc)
        {
            foreach (string db in Keys)
            {
                if (!this[db].Load(CFunc)) return false;
            }
            return true;
        }
        public override string ToString() { try { return $"[{(this.Any() ? this.Select(x => "{" + x.Key + ":" + x.Value.Count() + "}").Aggregate((a, b) => a + "," + b) : "")}]"; } catch (Exception e) { return e.Message; } }
    }
}