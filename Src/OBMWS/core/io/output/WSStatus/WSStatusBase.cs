using System;
using System.Collections.Generic;
using System.Linq;

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
    public class WSStatusBase : WSStaticEntity
    {
        protected DateTime init = DateTime.Now;
        protected DateTime now = DateTime.Now;
        protected int index = 0;

        protected const short ACCESS_DENIED_CODE = -2;
        protected const short ERROR_CODE = -1;
        protected const short NONE_CODE = 0;
        protected const short SUCCESS_CODE = 1;
        protected const short ATTANTION_CODE = 2;

        public static readonly WSStatusBase ACCESS_DENIED = new WSStatusBase(ACCESS_DENIED_CODE, "Access denied", WSConstants.ACCESS_LEVEL.READ);
        public static readonly WSStatusBase ERROR = new WSStatusBase(ERROR_CODE, "Error", WSConstants.ACCESS_LEVEL.READ);
        public static readonly WSStatusBase NONE = new WSStatusBase(NONE_CODE, "None", WSConstants.ACCESS_LEVEL.READ);
        public static readonly WSStatusBase SUCCESS = new WSStatusBase(SUCCESS_CODE, "Success", WSConstants.ACCESS_LEVEL.READ);
        public static readonly WSStatusBase ATTANTION = new WSStatusBase(ATTANTION_CODE, "Attantion", WSConstants.ACCESS_LEVEL.READ) { _NOTES = new List<WSStatusNote>() { new WSStatusNote("Attantion! Action seems to be done, but require your attantion.", WSConstants.ACCESS_LEVEL.READ) } };

        public static WSStatusBase[] All = new WSStatusBase[] { ACCESS_DENIED, ERROR, NONE, SUCCESS, ATTANTION };

        public WSStatusBase() { }
        protected WSStatusBase(short code, string name, byte _UserRole)
        {
            _CODE = code;
            NAME = name;
            UserRole = _UserRole;
        }

        protected short _CODE = NONE_CODE;
        public virtual short CODE
        {
            get { return _CODE; }
            internal set { }
        }

        public string NAME { get; protected set; }

        public byte UserRole { get; protected set; } = WSConstants.ACCESS_LEVEL.READ;

        protected List<WSStatusNote> _NOTES = new List<WSStatusNote>();
        public virtual List<string> NOTES
        {
            get { if (_NOTES == null) { _NOTES = new List<WSStatusNote>(); } return _NOTES.Where(x => x.role <= UserRole).Select(x => x.note).ToList(); }
            set { }
        }
        public static WSStatusBase getByCode(int code) { return All.Any(x => x.CODE == code) ? All.FirstOrDefault(x => x.CODE == code) : null; }

        internal WSStatus clone(byte? _UserRole = null) { return new WSStatus(CODE, NAME, _UserRole == null ? UserRole : (byte)_UserRole); }
    }
}