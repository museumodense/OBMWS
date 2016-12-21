using System;
using System.Collections.Generic;

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
    public class WSAccessKey : WSStaticEntity
    {
        private WSSecurity Security;
        internal WSAccessKey(WSSecurity _Security, string _hostKey, string _MD5Key, DateTime? _startDate = null, DateTime? _endDate = null)
        {
            Security = _Security;
            hostKey = _hostKey;
            MD5Key = _MD5Key;
            startDate = _startDate;
            endDate = _endDate;
        }
        private string hostKey { get; set; }
        internal string MD5Key { get; private set; }
        internal DateTime? startDate { get; private set; }
        internal DateTime? endDate { get; private set; }

        internal bool Match(WSAccessKey key)
        {
            try
            {
                return 
                    key!= null
                    && !string.IsNullOrEmpty(key.hostKey)
                    && key.hostKey.Equals(hostKey)
                    && !string.IsNullOrEmpty(key.MD5Key)
                    && key.MD5Key.Equals(MD5Key)
                    && ((startDate == null && key.startDate == null) || (key.startDate == startDate)) 
                    && ((endDate == null && key.endDate == null) || (key.endDate == endDate));
            }
            catch (Exception) { }
            return false;
        }
        internal bool IsValid
        {
            get
            {
                try {
                    WSAccessKey tempKey = null;
                    return !string.IsNullOrEmpty(MD5Key) && (startDate == null || startDate < DateTime.Now) && (endDate == null || endDate > DateTime.Now) && /*IE.Meta.Request.*/Security.validateWSAccessKey(hostKey, Json, out tempKey);
                } catch (Exception) { }
                return false;
            }
        }
        private WSJson _Json = null;

        private WSJson Json
        {
            get
            {
                if (_Json == null)
                {
                    _Json = new WSJValue(MD5Key);
                    if (startDate != null || endDate != null)
                    {
                        List<WSJProperty> props = new List<WSJProperty>() { new WSJProperty("MD5Key", new WSJValue(MD5Key)) };
                        _Json = new WSJObject(props);
                        if (startDate != null) { props.Add(new WSJProperty("startDate", new WSJValue(((DateTime)startDate).ToString(WSConstants.DATE_FORMAT_MIN)))); }
                        if (endDate != null) { props.Add(new WSJProperty("endDate", new WSJValue(((DateTime)endDate).ToString(WSConstants.DATE_FORMAT_MIN)))); }
                    }
                }
                return _Json;
            }
        }
        public override bool Equals(object obj) { if (obj == null || obj.GetType() != typeof(WSAccessKey) || obj.GetHashCode() != GetHashCode()) { return false; } return true; }
        public override string ToString() { return MD5Key; }
        public override int GetHashCode() { return ToString().GetHashCode(); }
    }
}
