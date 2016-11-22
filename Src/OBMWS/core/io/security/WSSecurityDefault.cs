using System;

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

namespace OBMWS.security
{
    public class WSSecurityDefault : WSSecurity
    {
        public WSSecurityDefault(WSDynamicEntity _Session, string _SessionID, MetaFunctions _CFunc) : base(_Session,  _SessionID, _CFunc) { }

        protected override void setAuthToken<S>(S _session, ref WSAuthEntity _AuthToken)
        {
            long sessionAgeSeconds = 0;

            AuthToken.issued = DateTime.Now;
            AuthToken.expires = DateTime.Now.AddSeconds(WSConstants.SessionExpiresInSeconds);
            AuthToken.expires_in = (WSConstants.SessionExpiresInSeconds > sessionAgeSeconds ? (WSConstants.SessionExpiresInSeconds - sessionAgeSeconds) : 0);
            AuthToken.status = WSConstants.AUTH_STATES.AUTHORIZED;
        }

        protected override void setSession<S>(DateTime _Date, string _SessionID, int _UserID, ref S _session) { }
        internal string OneMinTicketInternal(DateTime? _initTime = default(DateTime?))
        {
            return OneMinTicket(_initTime);
        }
        protected override string OneMinTicket(DateTime? _initTime = default(DateTime?))
        {
            DateTime initTime = _initTime == null ? DateTime.Now : (DateTime)_initTime;
            string key1 = string.Empty;
            try { new OBMWS.WSConverter().ToMd5Hash(initTime.ToString("yyyyMMddhhmm"), out key1, true); } catch (Exception e) { WSStatus status = WSStatus.NONE.clone(); WSServerMeta.LogError(GetType(), e, ref status); }
            return key1;
        }
    }
}
