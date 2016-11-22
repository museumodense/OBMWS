using System;
using OBMWS.security;
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
    public class WSSystemResponse<A> : WSStaticResponse<A> where A : WSStaticEntity
    {
        public WSSystemResponse(WSClientMeta _meta) : base(_meta) { }

        public override void proceedCall()
        {
            try
            {
                WSSystemEntity entity = new WSSystemEntity() { };
                WSSystemRecord rec = new WSSystemRecord(meta.ClientFunctions, entity, meta.Request.Security.AuthToken.User.role);

                if (WSConstants.ALIACES.ACTION_READ.Match(meta.Request.ACTION))
                {
                    entity.status = WSServerMeta.LoadStatusStatic;
                }
                else if (WSConstants.ALIACES.ACTION_WRITE.Match(meta.Request.ACTION))
                {
                    WSEntitySchema USchema = meta.Request.SOURCE!=null && meta.Request.SOURCE is WSTableSource? ((WSTableSource)meta.Request.SOURCE).DynamicSchema : null;

                    if (USchema != null && USchema is WSEntitySchema)
                    {
                        WSEntitySchema schema = USchema;
                    }
                }
                else if (WSConstants.ALIACES.ACTION_FLUSH.Match(meta.Request.ACTION))
                {
                    try
                    {
                        if (reloadCoreSources()) { entity.status.AddNote($"Reset succeeded.", WSConstants.ACCESS_LEVEL.READ, WSStatus.SUCCESS.CODE); }
                        else { entity.status.AddNote("Reset failed.", WSConstants.ACCESS_LEVEL.READ, WSStatus.ERROR.CODE); }
                    }
                    catch (Exception e) { meta.RegError(GetType(), e, ref iostatus); }
                }
                else if (WSConstants.ALIACES.ACTION_CREATE.Match(meta.Request.ACTION))
                {
                    string ENTITY = meta.Request.INPUT.ReadValue(WSConstants.ALIACES.ENTITY, out ENTITY) ? ENTITY : null;

                    if (!string.IsNullOrEmpty(ENTITY))
                    {
                        if (WSConstants.ALIACES.ACCESS_KEY.Match(ENTITY))
                        {
                            entity.data = meta.GenerateWSAccessKey();
                        }
                    }
                }

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
                        message = iostatus.NOTES
                    }, meta.Request.Security.AuthToken.User.role);
                    if (response == null) { response = new V1SystemResponseRecord(meta.ClientFunctions, new V1SystemResponseEntity() { status = WSStatus.ERROR.NAME, message = iostatus.NOTES }, meta.Request.Security.AuthToken.User.role); }
                }
                #endregion
            }
            catch (Exception e) { meta.RegError(GetType(), e, ref iostatus); }
        }
    }
}

