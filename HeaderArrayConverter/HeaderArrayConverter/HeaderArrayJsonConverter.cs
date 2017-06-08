using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HeaderArrayConverter
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TValue">
    /// 
    /// </typeparam>
    public class HeaderArrayJsonConverter<TValue> : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(IHeaderArray).GetTypeInfo().IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jObject = JObject.Load(reader);

            IEnumerable<KeyValuePair<KeySequence<string>, TValue>> items =
                jObject["_entries"].Values<JToken>()
                                   .Select(
                                       x =>
                                           new KeyValuePair<KeySequence<string>, TValue>(
                                               KeySequence<string>.Parse(x),
                                               x.Single().Value<TValue>()));

            IEnumerable<IEnumerable<string>> sets = 
                jObject["Sets"].Values<JToken>().Select(x => x.Values<string>());

            IHeaderArray<TValue> array =
                new HeaderArray<TValue>(
                    jObject["Header"].Value<string>(),
                    jObject["Description"].Value<string>(),
                    jObject["Type"].Value<string>(),
                    jObject["Dimensions"].Values<int>().ToArray(),
                    items,
                    sets);

            return array;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}