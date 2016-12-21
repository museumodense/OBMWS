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
    public class WSStatusNote
    {
        public string note { get; private set; }
        public byte role { get; private set; }

        private WSStatusNote() { }
        public WSStatusNote(string _note, byte _role = WSConstants.ACCESS_LEVEL.ADMIN)
        {
            note = _note;
            role = _role;
        }

        public override string ToString()
        {
            return $"{(role >= WSConstants.ACCESS_LEVEL.ADMIN ? role + ":" : "")}{note}";
        }
    }
}
