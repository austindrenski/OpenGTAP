using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using Newtonsoft.Json;

namespace HeaderArrayConverter.Converters
{
    public sealed class KeyValuePairJsonConverter : JsonConverter
    {
        /// <summary>
        /// True if the type implements a key-value list; otherwise false.
        /// </summary>
        /// <param name="objectType">
        /// The type to compare.
        /// </param>
        public override bool CanConvert(Type objectType)
        {
            return typeof(IEnumerable<KeyValuePair<string, IImmutableList<string>>>).GetTypeInfo().IsAssignableFrom(objectType);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            KeyValuePair<string, IImmutableList<string>> item =
                (KeyValuePair<string, IImmutableList<string>>)value;

            writer.WriteStartObject();
            writer.WritePropertyName(item.Key);
            writer.WriteStartArray();
            foreach (string entry in item.Value)
            {
                writer.WriteValue(entry);
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotSupportedException();
        }
    }
}