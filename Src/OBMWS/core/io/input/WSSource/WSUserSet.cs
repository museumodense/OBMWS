using OBMWS.security;
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
    public class WSUserSet : Dictionary<string, WSUserDBSet>
    {
        private MetaFunctions CFunc = null;
        public WSUserSet(WSRoleSet RoleSet, Func<string, WSSession> ReadWSSession, MetaFunctions _CFunc)
        {
            CFunc = _CFunc;
            if (RoleSet != null)
            {
                foreach (string DBName in RoleSet.Last().Value.Keys)
                {
                    WSSession session = ReadWSSession(DBName);
                    if (session != null && RoleSet.ContainsKey(session.user.role) && RoleSet[session.user.role].ContainsKey(DBName))
                    {
                        WSUserDBSet DBSet = new WSUserDBSet(session);
                        DBSet.AddRange(RoleSet[session.user.role][DBName].Clone(ref DBSet, CFunc));
                        Add(DBName, DBSet);
                    }
                }
            }
        }
        internal WSUserSet() { }

        public WSTableSource GetSourceByType(Type type)
        {
            WSTableSource src = null;
            if (Values != null) foreach (WSUserDBSet srcs in Values)
            {
                src = srcs.FirstOrDefault(x => x.ReturnType == type);
                if (src != null) break;
            }
            return src;
        }

        private bool? _isValid = null;
        public bool isValid
        {
            get
            {
                if (_isValid==null)
                {
                    _isValid = !this.Any(x => x.Value == null || !x.Value.Any(s=>!s.isValid));
                }
                return _isValid!=null && (bool)_isValid;
            }
        }
        public override string ToString()
        {
            string text = base.ToString();
            try { text = string.Format("[Count:{0}]", Count); }
            catch (Exception e)
            {
                text = e.Message;
                WSStatus status = WSStatus.NONE.clone();
                CFunc.RegError(GetType(), e, ref status);
            }
            return text;
        }
    }
}