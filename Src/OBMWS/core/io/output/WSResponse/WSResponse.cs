using System;
using System.Linq;
using System.Collections.Generic;

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
    public abstract class WSResponse<A> where A : WSEntity
    {
        #region PROPERTIES
        public WSStatus iostatus = WSStatus.NONE.clone(WSConstants.ACCESS_LEVEL.READ);
        public WSClientMeta meta { get; private set; }
        public object response { get; protected set; }
        #endregion

        #region INITIALIZATION
        public WSResponse(WSClientMeta _meta) {         
            meta = _meta;
            if (meta.Request != null && meta.Request.Security != null)
            {
                if (meta.Request.Security.IsLogged && meta.Request.Security.AuthToken.User.role != iostatus.CODE) { iostatus = WSStatus.NONE.clone(meta.Request.Security.AuthToken.User.role); }
                init();
            }
            else { iostatus = WSStatus.NONE.clone(WSConstants.ACCESS_LEVEL.READ); }
        }
        private void init()
        {
            try
            {
                if (meta.Request.SOURCE == null) { iostatus.AddNote($"No data source found for [{meta.Request.SrcName}]. Make sure you trying to access a valid source with a valid permission.", WSConstants.ACCESS_LEVEL.READ); }
                else if (meta.Request.Security == null) { iostatus.AddNote("Access denied. Security initialization failed.", WSConstants.ACCESS_LEVEL.READ); }
                else if (meta.Request.Security.AuthToken == null || meta.Request.Security.AuthToken.User == null) { iostatus.AddNote("Access denied. User initialization failed.", WSConstants.ACCESS_LEVEL.READ); }
                else
                {
                    if ((meta.Request.ACTION == null || WSConstants.ALIACES.ACTION_NONE.Match(meta.Request.ACTION)))
                    {
                        meta.Request.INPUT.Save(WSConstants.PARAMS.KEYS.ACTION, WSConstants.ALIACES.ACTION_READ.ALIACES.FirstOrDefault());
                    }

                    if (meta.Request.ACTION == null) { }
                    else
                    {
                        callAction();
                    }
                }

                response = response == null
                    ? new V1SystemResponseRecord(meta.ClientFunctions, new V1SystemResponseEntity()
                    {
                        status = WSStatusBase.ERROR.NAME,
                        message = iostatus.DeepNotes
                    }, meta.Request.Security.AuthToken.User.role)
                    : response;
            }
            catch (Exception e) { meta.RegError(GetType(), e, ref iostatus); }
        }
        public abstract void callAction();
        #endregion

        #region VERIFICATION
        public bool verifyNotEmpty(List<A> items = null, bool itemsRequired = true)
        {
            if (itemsRequired && (items == null || !items.Any())) { iostatus.AddNote("No records found", WSConstants.ACCESS_LEVEL.READ); return false; }
            else return true;
        }
        public bool verifyAccess(int minimumURole = WSConstants.ACCESS_LEVEL.READ)
        {

            if (!meta.Request.Security.IsLogged){
                iostatus.AddNotes(new List<string>{
                    "Authentication failed",
                    "AuthRole:"+(meta.Request.Security.AuthToken==null?"notoken":meta.Request.Security.AuthToken.User==null?"nouser":""+meta.Request.Security.AuthToken.User.role)
                });
            }
            else if (!meta.Request.Security.AuthToken.User.isactive) {
                iostatus.AddNote("User not permitted to access data, - user account is not activated. Please activate your acount and try again.", WSConstants.ACCESS_LEVEL.READ, WSStatus.ERROR.CODE);
            }
            else if ((meta.Request.Security.AuthToken.User.role < minimumURole)) {
                iostatus.AddNote("User not permitted to access data. Minimum required access level for your request is: ["+ minimumURole + "]", WSConstants.ACCESS_LEVEL.READ, WSStatus.ERROR.CODE);
            }
            else return true;
            return false;
        }
        #endregion
    }
}