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

namespace OBMWS.security
{
    public class WSSession
    {
        private WSUserToken UToken = new WSUserToken();
        
        public WSSession(string _SessionID, WSSecurityMeta _Meta)
        {
            SessionID = _SessionID.ToLower();
            Meta = _Meta;
        }
        
        public string SessionID { get; private set; }
        public WSSecurityMeta Meta { get; private set; }
        internal WSUserToken user { get { return UToken; } set { UToken = value; } }
        
        public string Json { get { return $"\"sessionid\":\"{SessionID}\",\"user\":{{{(user==null?null:user.Json)}}}"; } }
    }
}