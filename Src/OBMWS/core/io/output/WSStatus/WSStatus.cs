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
    public class WSStatus : WSStatusBase
    {
        public WSStatus() { }

        public static WSStatus ACCESS_DENIED_Copy(byte _UserRole = WSConstants.ACCESS_LEVEL.READ) { return ACCESS_DENIED.clone(_UserRole); }
        public static WSStatus ERROR_Copy(byte _UserRole = WSConstants.ACCESS_LEVEL.READ) { return ERROR.clone(_UserRole); }
        public static WSStatus NONE_Copy(byte _UserRole = WSConstants.ACCESS_LEVEL.READ) { return NONE.clone(_UserRole); }
        public static WSStatus SUCCESS_Copy(byte _UserRole = WSConstants.ACCESS_LEVEL.READ) { return SUCCESS.clone(_UserRole); }
        public static WSStatus ATTANTION_Copy(byte _UserRole = WSConstants.ACCESS_LEVEL.READ) { return ATTANTION.clone(_UserRole); }


        internal WSStatus(short code, string name, byte _UserRole = WSConstants.ACCESS_LEVEL.READ) : base(code, name, _UserRole) { }

        internal WSStatus(short code, string name, byte _UserRole = WSConstants.ACCESS_LEVEL.READ, List<string> notes = null) : base(code, name, _UserRole)
        {
            if (notes != null) { AddNotes(notes.Select(x => new WSStatusNote(x, _UserRole)).ToList()); }
        }

        internal WSStatus(short code, string name, byte _UserRole = WSConstants.ACCESS_LEVEL.READ, List<WSStatusNote> notes = null) : base(code, name, _UserRole)
        {
            if (notes != null) { AddNotes(notes); }
        }
        public override short CODE
        {
            get { return _CODE; }
            internal set
            {
                if (value > 0 || Math.Abs(value) > Math.Abs(_CODE))
                {
                    _CODE = value;
                    WSStatusBase status = getByCode(_CODE);
                    NAME = status != null ? status.NAME : NAME;
                }
            }
        }

        public override List<string> NOTES
        {
            get { if (_NOTES == null) { _NOTES = new List<WSStatusNote>(); } return _NOTES.Where(x => x.role <= UserRole).Select(x=>x.note).ToList(); }
            set { AddNotes(value); }
        }
        public List<string> DeepNotes
        {
            get
            {
                List<string> DEEP_NOTES = NOTES;
                if (childs.Any()) { DEEP_NOTES.AddRange(childs.SelectMany(x => x.DeepNotes)); }
                return DEEP_NOTES;
            }
        }

        public List<WSStatus> childs { get { if (_childs == null) { _childs = new List<WSStatus>(); } return _childs; } set { _childs = value; } }
        private List<WSStatus> _childs = null;

        public WSStatus AddNote(WSStatusNote note, short? _code = null)
        {
            if (_code != null) { CODE = (short)_code; }
            string time_consumption = (UserRole >= WSConstants.USER_ROLE.ADMIN ? $"[{((DateTime.Now.Ticks - now.Ticks) / 10000)} millis]:" : "");
            if (note != null && !string.IsNullOrEmpty(note.note)) { _NOTES.Add(new WSStatusNote($"{(index++)}{(UserRole > WSAccessMode.READ.ACCESS_LEVEL ? ($"[{UserRole}]") : "")}. {time_consumption}{note}", note.role)); }
            now = DateTime.Now;
            return this;
        }
        public WSStatus AddNote(string note, byte AccessLevel = WSConstants.ACCESS_LEVEL.ADMIN, short? _code = null)
        {
            if (_code != null) { CODE = (short)_code; }
            string time_consumption = (UserRole >= WSConstants.USER_ROLE.ADMIN ? $"[{((DateTime.Now.Ticks - now.Ticks) / 10000)} millis]:" : "");
            if (note != null && !string.IsNullOrEmpty(note)) { _NOTES.Add(new WSStatusNote($"{(index++)}{(UserRole > WSAccessMode.READ.ACCESS_LEVEL ? ($"[{UserRole}]") : "")}. {time_consumption}{note}", AccessLevel)); }
            now = DateTime.Now;
            return this;
        }
        public WSStatus AddNotes(List<WSStatusNote> notes)
        {
            string time_consumption = (UserRole >= WSConstants.USER_ROLE.ADMIN ? $"[{((DateTime.Now.Ticks - now.Ticks) / 10000)} millis]:" : "");
            if (notes != null) _NOTES.AddRange(notes.Select(x => new WSStatusNote($"{(index++)}{(UserRole > WSAccessMode.READ.ACCESS_LEVEL ? ($"[{UserRole}]") : "")}. {time_consumption}{x.note}", x.role)));
            now = DateTime.Now;
            return this;
        }
        public WSStatus AddNotes(List<string> notes, byte AccessLevel = WSConstants.ACCESS_LEVEL.ADMIN)
        {
            string time_consumption = (UserRole >= WSConstants.USER_ROLE.ADMIN ? $"[{((DateTime.Now.Ticks - now.Ticks) / 10000)} millis]:" : "");
            if (notes != null) _NOTES.AddRange(notes.Select(x => new WSStatusNote($"{(index++)}{(UserRole > WSAccessMode.READ.ACCESS_LEVEL ? ($"[{UserRole}]") : "")}. {time_consumption}{x}", AccessLevel)));
            now = DateTime.Now;
            return this;
        }
        public WSStatus_JSON GetJson()
        {
            List<WSStatusNote> notes = _NOTES.Where(x => x.role >= UserRole).ToList();
            return new WSStatus_JSON()
            {
                name = NAME,
                notes = notes.Any()? notes.Select(x=>x.note).ToList() : new List<string>(),
                childs = childs.Select(x => x.GetJson())
            };
        }

        public override string ToString()
        {
            try
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.Append("{");
                sb.Append("\"name\":\"" + NAME + "\",");
                sb.Append("\"notes\":[");
                sb.Append("" + ((index + 1) + ". Total execution time: " + ((DateTime.Now.Ticks - init.Ticks) / 10000) + " millis") + "");
                if (_NOTES.Any(x => x.role >= UserRole)) { sb.Append(_NOTES.Where(x => x.role >= UserRole).Select(x => "\"" + x.note + "\"").Aggregate((a, b) => a + "," + b)); }
                sb.Append("],");
                sb.Append("\"childs\":[");
                sb.Append((childs.Any() ? childs.Select(x => x.ToString()).Aggregate((a, b) => a + "," + b) : ""));
                sb.Append(childs.Count);
                sb.Append("]");
                sb.Append("}");

                return sb.ToString();
            }
            catch (Exception) {}
            return NAME;
        }
        public override int GetHashCode() { return CODE; }
        public override bool Equals(object obj) { return obj != null && obj is WSStatus && this.GetHashCode() == ((WSStatus)obj).GetHashCode(); }
        public bool IsPositive { get { return CODE >= 0 && !childs.Any(c => !c.IsPositive); } }
    }
}
