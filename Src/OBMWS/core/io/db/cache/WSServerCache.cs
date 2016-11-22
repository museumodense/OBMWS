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
    internal class WSServerCache
    {
        internal List<WSSessionCache> SessionsCache { get; private set; } = new List<WSSessionCache>();
        internal bool GetContext(Type _ContextType, WSRequestID _RequestID, out WSDataContext _context)
        {
            _context = null;
            try
            {
                WSSessionCache SessionCache = SessionsCache.FirstOrDefault(x => x.SessionID.Equals(_RequestID.SessionID));
                if (SessionCache == null)
                {
                    SessionCache = new WSSessionCache(_RequestID.SessionID);
                    SessionsCache.Add(SessionCache);
                }

                return SessionCache.GetContext(_ContextType, _RequestID, out _context);
            }
            catch (Exception) { return false; }
        }
        public override string ToString() { return $"[{(SessionsCache.Any() ? SessionsCache.Select(x => x.Items.ToString()).Aggregate((a, b) => a + "," + b) : "")}]"; }

        internal static bool IsValid(WSServerCache dBCache) { return dBCache != null || dBCache.SessionsCache != null; }

        internal void Flush() { foreach (WSSessionCache RCache in SessionsCache) { if (WSSessionCache.IsValid(RCache)) RCache.Flush(); } }

        internal void CleanUp()
        {
            try
            {
                foreach (WSSessionCache RCache in SessionsCache) { RCache.CleanUp(); }

                SessionsCache = SessionsCache == null ? new List<WSSessionCache>() : SessionsCache.Where(x => x.Items.Any()).ToList();
            }
            catch (Exception e) { }
        }
    }
}
