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

namespace OBMWS.security
{
    public class WSUserToken : WSStaticEntity
    {
        public int id = -1;
        public string login;
        public string email;
        public string firstname;
        public string lastname;
        public byte role = WSConstants.DEFAULT_USER_ROLE;
        public string roleName;
        public bool isactive = false;

        public bool IsValid { get { return id > 0; } }

        internal bool Match(WSJson jUserData, string _key = null)
        {
            try
            {
                if (jUserData != null && jUserData.IsValid)
                {
                    if (jUserData is WSJObject) {
                        return MatchObject((WSJObject)jUserData, _key);
                    } else if (jUserData is WSJArray) {
                        return MatchArray((WSJArray)jUserData, _key);
                    }
                }
            }
            catch (Exception) { /*CFunc.RegError(GetType(), e);*/ return false; }
            return true;
        }

        internal WSUserToken Clone
        {
            get
            {
                return new WSUserToken()
                {
                    id = id,
                    login = login,
                    email = email,
                    firstname = firstname,
                    lastname = lastname,
                    role = role,
                    roleName = roleName,
                    isactive = isactive
                };
            }
        }

        internal string Json { get {
            return $"\"id\":\"{id}\",\"login\":\"{login}\",\"email\":\"{email}\",\"firstname\":\"{firstname}\",\"lastname\":\"{lastname}\",\"role\":\"{role}\",\"rolename\":\"{roleName}\",\"isactive\":\"{isactive}\"";
        } }

        private bool MatchObject(WSJObject jUserData, string _key = null)
        {
            try
            {
                foreach (WSJProperty jProp in (jUserData).Value)
                {
                    if (!MatchProperty(jProp, _key)) { return false; }
                }
            }
            catch (Exception) { return false; }
            return true;
        }
        private bool MatchArray(WSJArray jUserData, string _key = null)
        {
            bool _match = true;//insure that the empty array gives true
            try
            {
                foreach (WSJson jItem in (jUserData).Value)
                {
                    _match = false;//insure that the NOT empty array gives false on failure
                    if (jItem is WSJValue && !string.IsNullOrEmpty(_key)) { _match = MatchValue(_key, (WSJValue)jItem, WSFieldFilter.GLOBAL_OPERATIONS.Equal.NAME); }
                    else if (jItem is WSJObject) { _match = MatchObject((WSJObject)jItem, _key); }
                    else if (jItem is WSJArray) { _match = MatchArray((WSJArray)jItem, _key); }
                    if (_match) break;
                }
            }
            catch (Exception) { }
            return _match;
        }
        private bool MatchProperty(WSJProperty jProp, string _key = null)
        {
            try
            {
                if (jProp==null || !jProp.IsValid) { return false; }
                else
                {
                    string matchOperation = WSFieldFilter.GLOBAL_OPERATIONS.Equal.NAME;
                    if (string.IsNullOrEmpty(_key)) { _key = jProp.Key; }
                    else { matchOperation = jProp.Key; }

                    if (jProp.Value is WSJValue) {          return MatchValue(_key, (WSJValue)jProp.Value, matchOperation); }
                    else if (jProp.Value is WSJObject) { return MatchObject((WSJObject)jProp.Value, _key); }
                    else if (jProp.Value is WSJArray) { return MatchArray((WSJArray)jProp.Value, _key); }
                }
            }
            catch (Exception) { return false; }
            return true;
        }
        private bool MatchValue(string matchKey, WSJValue matchValue, string matchOperation)
        {
            try
            {
                if (!string.IsNullOrEmpty(matchKey) && matchValue != null && matchValue.IsValid)
                {
                    matchOperation = matchOperation == null ? WSFieldFilter.GLOBAL_OPERATIONS.Equal.NAME : matchOperation;
                    
                    switch (matchKey)
                    {
                        case "id":
                            return matchValue.Match(id, matchOperation);
                        case "login":
                            return  matchValue.Match(login, matchOperation);
                        case "email":
                            return matchValue.Match(email, matchOperation);
                        case "firstname":
                            return matchValue.Match(firstname, matchOperation);
                        case "lastname":
                            return matchValue.Match(lastname, matchOperation);
                        case "role":
                            return matchValue.Match(role, matchOperation);
                        case "isactive":
                            return matchValue.Match(isactive, matchOperation);
                        default: break;
                    }
                }
            }
            catch (Exception) { return false; }
            return false;
        }
    }

}
