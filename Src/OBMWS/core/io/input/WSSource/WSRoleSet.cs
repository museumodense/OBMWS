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
    public class WSRoleSet: Dictionary<byte, WSDBSet>
    {
        private ServerFunctions Func = null;
        internal WSRoleSet(WSDBSet core_sources, ServerFunctions _Func)
        {
            Func = _Func;
            foreach (byte role in WSConstants.USER_ROLE.ROLES)
            {
                if (!Keys.Any(x => x == role))
                {
                    WSDBSet sources = new WSDBSet(role);
                    foreach (string db in core_sources.Keys)
                    {
                        IEnumerable<WSTableSource> role_sources = core_sources[db].Where(x => x.AccessLevel <= role);
                        if (role_sources.Any()) {
                            sources.Add(db, new WSSources<WSTableSource>(role_sources.Select(src => src.Clone(_Func, role))));
                        }
                    }
                    Add(role, sources);
                }
            }
        }
        public bool IsValid { get { if (_IsValid == null) { _IsValid = !this.Any(x => x.Value == null || !x.Value.IsValid); } return _IsValid != null && (bool)_IsValid; } }
        private bool? _IsValid = null;
        internal bool Load()
        {
            foreach (WSDBSet DBSet in Values)
            {
                if (!DBSet.Load(Func)) return false;
            }
            return true;
        }

        public override string ToString() {
            string text = base.ToString();
            try { text = string.Format("[Count:{0}]", Count); }
            catch (Exception e) {
                text = e.Message;
                WSStatus status = WSStatus.NONE.clone();
                Func.RegError(GetType(), e, ref status); }
            return text;
        }
    }
}