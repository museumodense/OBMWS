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

namespace OBMWS
{
    public class WSHelperDefault : WSHelper
    {
        public override string registerError(Guid key, string ip, string source, string title, string exception)
        {
            throw new NotImplementedException();
        }

        public override string registerHttpActivity(string url, string uip, string http_request, string httpSession, string urlQuery, string postParams, string referrer, string _Notes, bool save = false)
        {
            throw new NotImplementedException();
        }
    }
}
