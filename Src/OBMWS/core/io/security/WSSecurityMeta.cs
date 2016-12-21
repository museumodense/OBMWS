using System;

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
    public class WSSecurityMeta
    {
        public WSSecurityMeta(string _Zone, string _DB, Type _SecurityType, Type _SessionType, Type _UserType, Type _RoleType, Type _ErrorType)
        {
            Zone = _Zone;
            DB = _DB;
            SecurityType = _SecurityType;
            SessionType = _SessionType;
            UserType = _UserType;
            RoleType = _RoleType;
            ErrorType = _ErrorType;
        }
        public string Zone { get; private set; }
        public string DB { get; private set; }
        public Type SecurityType { get; private set; }
        public Type SessionType { get; private set; }
        public Type UserType { get; private set; }
        public Type RoleType { get; private set; }
        public Type ErrorType { get; private set; }

        public override string ToString()
        {
            return $"{{Meta:{{Zone:{Zone},DB:{DB}}}}}";
        }
    }
}