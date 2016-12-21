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

using System;
using System.Linq;

namespace OBMWS
{
    public abstract class WSStaticResponse<A> : WSResponse<A> where A : WSStaticEntity
    {
        public WSStaticResponse(WSClientMeta _meta) : base(_meta) { }

        public override void callAction()
        {
            iostatus.AddNote($"Init:[{meta.Request.Url.PathAndQuery}]");
            if (meta.Request.SOURCE.AccessLevel > meta.Request.Security.AuthToken.User.role) { 
                iostatus.AddNote("Access denied. User not permitted to access the source[" + meta.Request.SOURCE.NAME + "].", WSConstants.ACCESS_LEVEL.READ, WSStatus.ERROR.CODE);

                WSSystemRecord rec = new WSSystemRecord(meta.ClientFunctions, new WSSystemEntity() { status = iostatus }, meta.Request.Security.AuthToken.User.role);

                #region VERSION 0
                if (WSConstants.PARAMS.IOVERSION.VERSION0.Match(meta.Request.VERSION)) { response = rec; }
                #endregion

                #region VERSION 1
                else if (WSConstants.PARAMS.IOVERSION.VERSION1.Match(meta.Request.VERSION))
                {
                    response = new V1SystemResponseRecord(meta.ClientFunctions, new V1SystemResponseEntity()
                    {
                        status = (rec == null || rec.entity == null) ? WSStatus.ERROR.NAME : iostatus.NAME,
                        data = rec,
                        message = iostatus.DeepNotes
                    }
                    ,meta.Request.Security.AuthToken.User.role);
                    if (response == null) { response = new V1SystemResponseRecord(meta.ClientFunctions, new V1SystemResponseEntity() { status = WSStatus.ERROR.NAME, message = iostatus.DeepNotes }, meta.Request.Security.AuthToken.User.role); }
                }
                #endregion
            }
            else { proceedCall(); }

            if (response is V1SystemResponseRecord) { ((V1SystemResponseEntity)((V1SystemResponseRecord)response).entity).message = iostatus.DeepNotes; }
        }
        
        protected bool reloadCoreSources()
        {
            return WSServerMeta.reloadCoreSources();
        }
        public abstract void proceedCall();
    }
}