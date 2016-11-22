using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

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
    public class WSRecordJsonConverter : JsonConverter
    {
        private Type[] types;
        private WSParamList XParams;
        private WSRequest Request;
        private ClientFunctions CFunc;
        private WSDataContext DBContext;

        public WSRecordJsonConverter(WSParamList _XParams, WSRequest _Request, ClientFunctions _CFunc, WSDataContext _DBContext, params Type[] _types) {
            XParams = _XParams;
            Request = _Request;
            CFunc = _CFunc;
            DBContext = _DBContext;
            types = _types;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            WSStatus status = WSStatus.NONE_Copy();
            try
            {
                if (value != null)
                {
                    if (value is IList)
                    {
                        serializeWSRecords(writer, serializer, (IList)value, XParams);
                    }
                    else if (value is WSRecord)
                    {
                        ((WSRecord)value).WriteJson(writer, serializer, new List<Type>(), Request, CFunc, DBContext);
                    }
                }
            }
            catch (Exception e) { CFunc.RegError(GetType(), e, ref status); }
        }
        private void serializeWSRecords(JsonWriter writer, JsonSerializer serializer, IList values, WSParamList XParams)
        {
            writer.WriteStartArray();
            if (values.OfType<WSRecord>().Any()) { foreach (WSRecord rec in values) { rec.WriteJson(writer, serializer, new List<Type>(), Request, CFunc, DBContext); } }
            writer.WriteEndArray();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) { throw new NotImplementedException("Attantion! Convert to JSON has been disabled."); }
        public override bool CanRead { get { return false; } }
        public override bool CanConvert(Type objectType) { return types.Any(t => t == objectType); }
    }
}
