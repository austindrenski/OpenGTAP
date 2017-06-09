using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HeaderArrayConverter.Converters
{
    /// <summary>
    /// Custom converter from JSON <see cref="IHeaderArray{TValue}"/>.
    /// </summary>
    [PublicAPI]
    public sealed class HeaderArrayJsonConverter : JsonConverter
    {
        /// <summary>
        /// True if the type implements <see cref="IHeaderArray"/>; otherwise false.
        /// </summary>
        /// <param name="objectType">
        /// The type to compare.
        /// </param>
        public override bool CanConvert(Type objectType)
        {
            return typeof(IHeaderArray).GetTypeInfo().IsAssignableFrom(objectType);
        }

        /// <summary>
        /// Reads the JSON representation of the object.
        /// </summary>
        /// <param name="reader">
        /// The <see cref="JsonReader"/> to read from.
        /// </param>
        /// <param name="objectType">
        /// Type of the object.
        /// </param>
        /// <param name="existingValue">
        /// The existing value of object being read.
        /// </param>
        /// <param name="serializer">
        /// The calling serializer.
        /// </param>
        /// <returns>
        /// The object value.
        /// </returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jObject = JObject.Load(reader);

            return
                jObject["Type"].Value<string>() == "1C"
                    ? Create<string>(jObject)
                    : Create<float>(jObject);
        }

        private static IHeaderArray Create<T>(JObject jObject)
        {
            return
                new HeaderArray<T>(
                    jObject["Header"].Value<string>(),
                    jObject["Description"].Value<string>(),
                    jObject["Type"].Value<string>(),
                    jObject["Dimensions"].Values<int>(),
                    ParseEntries<T>(jObject["Entries"]),
                    ParseSets(jObject["Sets"]));
        }

        private static IEnumerable<KeyValuePair<KeySequence<string>, T>> ParseEntries<T>(JToken entries)
        {
            return
                entries.Values<JToken>()
                       .Select(
                           x => new
                           {
                               Key = KeySequence<string>.Parse(((JProperty) x).Name),
                               Value = x.Single().Value<T>()
                           })
                       .Select(
                           x =>
                               new KeyValuePair<KeySequence<string>, T>(x.Key, x.Value));
        }


        private static IEnumerable<KeyValuePair<string, IImmutableList<string>>> ParseSets(JToken sets)
        {
            return
                sets.Values<JToken>()
                    .SelectMany(x => x.Value<JToken>())
                    .Select(
                        x => new
                        {
                            Key = ((JProperty) x).Name,
                            Value = x.Single().Values<string>().ToImmutableArray()
                        })
                    .Select(
                        x =>
                            new KeyValuePair<string, IImmutableList<string>>(x.Key, x.Value))
                    .ToArray();
        }

        /// <summary>
        /// Writes the JSON representation of the object.
        /// </summary>
        /// <param name="writer">
        /// The <see cref="JsonWriter"/> to write to.
        /// </param>
        /// <param name="value">
        /// The value.
        /// </param>
        /// <param name="serializer">
        /// The calling serializer.
        /// </param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotSupportedException();
        }
    }
}
