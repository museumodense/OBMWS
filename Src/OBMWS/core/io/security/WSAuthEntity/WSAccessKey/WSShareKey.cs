using System;
using System.Reflection;

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
    public class WSShareKey : WSStaticEntity
    {
        public WSShareKey(Assembly assembly, string server_key, string owner_uid, string receiver_uid=null)
        {
            dbkey = new WSConverter().ToMd5Hash(assembly.FullName + server_key + owner_uid + receiver_uid);
        }
        public WSShareKey(Type eType, string server_key, string owner_uid, string receiver_uid=null)
        {
            srckey = new WSConverter().ToMd5Hash(eType + server_key + owner_uid + receiver_uid);
        }
        public WSShareKey(Type eType, string server_key, string owner_uid, string recId, string receiver_uid = null)
        {
            reckey = new WSConverter().ToMd5Hash(eType + server_key + owner_uid + recId + receiver_uid);
        }
        public string srckey { get; private set; }
        public string dbkey { get; private set; }
        public string reckey { get; private set; }

        internal bool Match(WSJson jData, string _key = null)
        {
            try
            {
                if (jData != null && jData.IsValid)
                {
                    if (jData is WSJObject) {
                        return MatchObject((WSJObject)jData, _key);
                    } else if (jData is WSJArray) {
                        return MatchArray((WSJArray)jData, _key);
                    }
                }
            }
            catch (Exception) { return false; }
            return true;
        }
        private bool MatchObject(WSJObject jData, string _key = null)
        {
            try
            {
                foreach (WSJProperty jProp in jData.Value)
                {
                    if (!MatchProperty(jProp, _key)) { return false; }
                }
            }
            catch (Exception) { return false; }
            return true;
        }
        private bool MatchArray(WSJArray jData, string _key = null)
        {
            bool _match = true;//insure that the empty array gives true
            try
            {
                foreach (WSJson jItem in jData.Value)
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
                        case "srckey":
                            return matchValue.Match(srckey, matchOperation);
                        case "dbkey":
                            return matchValue.Match(dbkey, matchOperation);
                        case "reckey":
                            return matchValue.Match(reckey, matchOperation);
                        default: break;
                    }
                }
            }
            catch (Exception) { return false; }
            return false;
        }
    }

}
