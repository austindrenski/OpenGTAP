using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using HeaderArrayConverter.Collections;
using HeaderArrayConverter.IO;
using HeaderArrayConverter.Types;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace HeaderArrayConverter
{
    /// <summary>
    /// Represents a single entry from a Header Array (HAR) file.
    /// </summary>
    [PublicAPI]
    public abstract class HeaderArray : IHeaderArray
    {
        /// <summary>
        /// Gets the <see cref="IHeaderArray.JsonSchema"/> for this object.
        /// </summary>
        JSchema IHeaderArray.JsonSchema => throw new NotSupportedException();

        /// <summary>
        /// Provides a converter for the <see cref="Deserialize(string)"/> method.
        /// </summary>
        [NotNull]
        private static HeaderArrayJsonConverter Converter { get; } = new HeaderArrayJsonConverter();

        /// <summary>
        /// The header of the array.
        /// </summary>
        public abstract string Header { get; }

        /// <summary>
        /// The coeffecient related to this <see cref="HeaderArray"/>
        /// </summary>
        public abstract string Coefficient { get; }

        /// <summary>
        /// An optional description of the array.
        /// </summary>
        public abstract string Description { get; }

        /// <summary>
        /// The type of the array.
        /// </summary>
        public abstract HeaderArrayType Type { get; }

        /// <summary>
        /// The dimensions of the array.
        /// </summary>
        public abstract IImmutableList<int> Dimensions { get; }

        /// <summary>
        /// The sets of the array.
        /// </summary>
        public abstract IImmutableList<KeyValuePair<string, IImmutableList<string>>> Sets { get; }
        
        /// <summary>
        /// Gets the total number of entries in the array.
        /// </summary>
        public abstract int Total { get; }

        /// <summary>
        /// Returns the value with the key defined by the key components or throws an exception if the key is not found.
        /// </summary>
        /// <param name="keys">
        /// The components that define the key whose value is returned.
        /// </param>
        /// <returns>
        /// The value stored by the given key.
        /// </returns>
        IImmutableSequenceDictionary<string> IHeaderArray.this[params string[] keys] => throw new NotSupportedException();

        /// <summary>
        /// Returns the value with the key defined by the key components or throws an exception if the key is not found.
        /// </summary>
        /// <param name="key">
        /// The components that define the key whose value is returned.
        /// </param>
        /// <returns>
        /// The value stored by the given key.
        /// </returns>
        object IHeaderArray.this[int key] => throw new NotSupportedException();
        
        /// <summary>
        /// Gets an <see cref="IEnumerable"/> for the given keys.
        /// </summary>
        /// <param name="keys">
        /// The collection of keys.
        /// </param>
        /// <returns>
        /// An <see cref="IEnumerable"/> for the given keys.
        /// </returns>
        IEnumerable ISequenceIndexer<string>.this[params string[] keys] => throw new NotSupportedException();

        /// <summary>
        /// Returns an <see cref="IHeaderArray"/> from the JSON string.
        /// </summary>
        [Pure]
        [NotNull]
        public static IHeaderArray Deserialize([NotNull] string json)
        {
            if (json is null)
            {
                throw new ArgumentNullException(nameof(json));
            }

            return JsonConvert.DeserializeObject<IHeaderArray>(json, Converter);
        }

        /// <summary>
        /// Casts the <see cref="IHeaderArray"/> as an <see cref="IHeaderArray{TResult}"/>.
        /// </summary>
        /// <typeparam name="TResult">
        /// The type of the array.
        /// </typeparam>
        /// <returns>
        /// An <see cref="IHeaderArray{TResult}"/>.
        /// </returns>
        public virtual IHeaderArray<TResult> As<TResult>()
        {
            return (IHeaderArray<TResult>)this;
        }

        /// <summary>
        /// Returns a copy of this <see cref="IHeaderArray"/> with the header modified.
        /// </summary>
        /// <param name="header">
        /// The new header.
        /// </param>
        /// <returns>
        /// A copy of this <see cref="IHeaderArray"/> with a new name.
        /// </returns>
        IHeaderArray IHeaderArray.With(string header)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Validates that the sets defined throughout the <see cref="HeaderArrayFile"/>. Validation information is logged to <paramref name="output"/>.
        /// </summary>
        /// <returns>
        /// True if there are no conflicts; otherwise false.
        /// </returns>
        public static bool ValidateSets([NotNull] IEnumerable<IHeaderArray> arrays, [CanBeNull] TextWriter output = null)
        {
            if (arrays is null)
            {
                throw new ArgumentNullException(nameof(arrays));
            }

            bool valid = true;

            IDictionary<string, IImmutableList<string>> verifiedSets =
                new Dictionary<string, IImmutableList<string>>();

            foreach (IHeaderArray array in arrays)
            {
                foreach (KeyValuePair<string, IImmutableList<string>> set in array.Sets)
                {
                    if (!verifiedSets.TryGetValue(set.Key, out IImmutableList<string> existingSet))
                    {
                        verifiedSets.Add(set);
                        continue;
                    }

                    if (set.Value.SequenceEqual(existingSet))
                    {
                        continue;
                    }

                    valid = false;

                    output?.WriteLineAsync($"Set '{set.Key}' in '{array.Header}' does not match the existing definition of '{set.Key}'");
                    output?.WriteLineAsync($"Existing definition: {string.Join(", ", existingSet)}.");
                    output?.WriteLineAsync($"'{array.Header}' definition: {string.Join(", ", set.Value)}.");
                }
            }

            output?.WriteLineAsync($"Sets validated: {valid}.");

            return valid;
        }

        /// <summary>
        /// Returns a JSON representation of the contents of this <see cref="HeaderArray{TValue}"/>.
        /// </summary>
        public abstract string Serialize(bool indent);

        /// <summary>
        /// Returns an enumerable that iterates through the logical collection as defined by the <see cref="IHeaderArray.Sets"/>.
        /// </summary>
        /// <returns>
        /// An enumerable that can be used to iterate through the logical collection as defined by the <see cref="IHeaderArray.Sets"/>.
        /// </returns>
        IEnumerable<KeyValuePair<KeySequence<string>, object>> IHeaderArray.GetLogicalEnumerable()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Returns an enumerable that iterates through the logical values collection as defined by the <see cref="IHeaderArray.Sets"/>.
        /// </summary>
        /// <returns>
        /// An enumerable that can be used to iterate through the logical values collection as defined by the <see cref="IHeaderArray.Sets"/>.
        /// </returns>
        IEnumerable IHeaderArray.GetLogicalValuesEnumerable()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Returns an enumerable that iterates through the logical values collection as defined by the <see cref="IHeaderArray.Sets"/>.
        /// </summary>
        /// <returns>
        /// An enumerable that can be used to iterate through the logical values collection as defined by the <see cref="IHeaderArray.Sets"/>.
        /// </returns>
        IEnumerable IHeaderArray.GetLogicalValuesEnumerable(IComparer<KeySequence<string>> keyComparer)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        [Pure]
        [NotNull]
        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Custom converter from JSON <see cref="IHeaderArray{TValue}"/>.
        /// </summary>
        private sealed class HeaderArrayJsonConverter : JsonConverter
        {
            /// <summary>
            /// Maps between valid the type values and valid enum names.
            /// </summary>
            private static IDictionary<string, HeaderArrayType> Map { get; }

            /// <summary>
            /// Static constructor to initialize resources for conversion.
            /// </summary>
            static HeaderArrayJsonConverter()
            {
                Map =
                    Enum.GetValues(typeof(HeaderArrayType))
                        .Cast<HeaderArrayType>()
                        .Select(x => (key: GetEnumMemberAttributeValue(x), value: x))
                        .ToImmutableDictionary(
                            x => x.key,
                            x => x.value);

                string GetEnumMemberAttributeValue(HeaderArrayType value)
                {
                    return
                        value.GetType()
                             .GetTypeInfo()
                             .GetMember(value.ToString())
                             .FirstOrDefault()
                             .GetCustomAttribute<EnumMemberAttribute>()
                             .Value;
                }
            }

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

                HeaderArrayType type = Map[jObject["Type"].Value<string>()];

                switch (type)
                {
                    case HeaderArrayType.RE:
                    case HeaderArrayType.R2:
                    {
                        return Create<float>(jObject, type);
                    }
                    case HeaderArrayType.I2:
                    {
                        return Create<int>(jObject, type);
                    }
                    case HeaderArrayType.C1:
                    {
                        return Create<string>(jObject, type);
                    }
                    default:
                    {
                        throw new DataValidationException(nameof(HeaderArrayType), Map, type);
                    }
                }
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

            /// <summary>
            /// Creates an <see cref="IHeaderArray{TValue}"/> and returns it as an <see cref="IHeaderArray"/>.
            /// </summary>
            /// <typeparam name="TValue">
            /// The value of the items in the array.
            /// </typeparam>
            /// <param name="jObject">
            /// The JSON object representing the array.
            /// </param>
            /// <param name="type">
            /// The <see cref="HeaderArrayType"/> of this array.
            /// </param>
            [Pure]
            [NotNull]
            private static IHeaderArray Create<TValue>(JObject jObject, HeaderArrayType type)
            {
                return
                    new HeaderArray<TValue>(
                        jObject["Header"].Value<string>(),
                        jObject["Coefficient"].Value<string>(),
                        jObject["Description"].Value<string>(),
                        type,
                        ParseEntries(jObject["Entries"]),
                        jObject["Dimensions"].Values<int>().ToImmutableArray(),
                        ParseSets(jObject["Sets"]));

                IEnumerable<KeyValuePair<KeySequence<string>, TValue>> ParseEntries(JToken entries)
                {
                    return
                        JsonConvert.DeserializeObject<IDictionary<string, TValue>>(entries.ToString())
                                   .Select(
                                       x =>
                                           new KeyValuePair<KeySequence<string>, TValue>(
                                               KeySequence<string>.Parse(x.Key),
                                               x.Value));
                }

                IImmutableList<KeyValuePair<string, IImmutableList<string>>> ParseSets(JToken sets)
                {
                    return
                        sets.Values<JToken>()
                            .Select(
                                x =>
                                    new KeyValuePair<string, IImmutableList<string>>(
                                        x.Value<string>("Key"),
                                        x.Value<JArray>("Value")
                                         .Values<string>()
                                         .ToImmutableArray()))
                            .ToImmutableArray();
                }
            }
        }
    }
}