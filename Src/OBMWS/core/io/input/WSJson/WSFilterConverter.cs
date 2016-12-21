using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

namespace OBMWS
{
    public class WSFilterConverter : WSFilterCreationConverter<WSJson>
    {
        protected override WSJson ToJson(JToken token) 
        {
            WSJson json = null;
            if (token != null)
            {
                try
                {
                    if (token is JValue)
                    {
                        JValue inner = (JValue)token;
                        json = new WSJValue(inner.Value == null ? null : inner.Value.ToString());
                    }
                    else if (token is JProperty)
                    {
                        JProperty inner = (JProperty)token;
                        json = new WSJProperty(inner.Name, ToJson(inner.Value));
                    }
                    else if (token is JArray)
                    {
                        JArray inner = (JArray)token;
                        WSJArray jArray = new WSJArray();
                        jArray.Value.AddRange(inner.Select(x => ToJson(x)).Where(x => x.IsValid));
                        json = jArray;
                    }
                    else if (token is JObject)
                    {
                        JObject inner = (JObject)token;
                        IEnumerable<WSJProperty> props = inner.Children<JProperty>().Select(x => new WSJProperty(x.Name, ToJson(x.Value)));
                        WSJObject jObject = new WSJObject(props.ToList());
                        json = jObject;
                    }
                }
                catch (Exception) { }
            }
            return json;
        }
    }
    public abstract class WSFilterCreationConverter<J> : JsonConverter
    {
        protected abstract J ToJson(JToken jObject);

        public override bool CanConvert(Type objectType) { return typeof(J).IsAssignableFrom(objectType); }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return ToJson(JObject.Load(reader));
        }
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            writer.WriteRawValue(value is WSJson ? ((WSJson)value).JString : value.ToString());
        }
    }
}