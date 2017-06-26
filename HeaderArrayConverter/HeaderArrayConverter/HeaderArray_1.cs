using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HeaderArrayConverter.Collections;
using HeaderArrayConverter.Types;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Schema.Generation;

namespace HeaderArrayConverter
{
    /// <summary>
    /// Represents a single entry from a Header Array (HAR) file.
    /// </summary>
    /// <typeparam name="TValue">
    /// The type of data in the array.
    /// </typeparam>
    [PublicAPI]
    [JsonObject(nameof(HeaderArray<TValue>), MemberSerialization = MemberSerialization.OptIn)]
    public class HeaderArray<TValue> : HeaderArray, IHeaderArray<TValue> where TValue : IEquatable<TValue>
    {
        /// <summary>
        /// Gets the <see cref="IHeaderArray.JsonSchema"/> for this object.
        /// </summary>
        public static JSchema JsonSchema { get; } = GetJsonSchema();

        /// <summary>
        /// Gets the <see cref="IHeaderArray.JsonSchema"/> for this object.
        /// </summary>
        JSchema IHeaderArray.JsonSchema => JsonSchema;

        /// <summary>
        /// An immutable dictionary whose entries are stored by a sequence of the defining sets.
        /// </summary>
        [NotNull]
        [JsonProperty("Entries", Order = int.MaxValue)]
        private readonly IImmutableSequenceDictionary<string, TValue> _entries;

        /// <summary>
        /// The four character identifier for this <see cref="HeaderArray{T}"/>.
        /// </summary>
        [JsonProperty]
        public override string Header { get; }

        /// <summary>
        /// The coeffecient related to this <see cref="HeaderArray{TValue}"/>
        /// </summary>
        [JsonProperty]
        public override string Coefficient { get; }

        /// <summary>
        /// The long name description of the <see cref="HeaderArray{T}"/>.
        /// </summary>
        [JsonProperty]
        public override string Description { get; }

        /// <summary>
        /// The type of element stored in the array.
        /// </summary>
        [JsonProperty]
        public override HeaderArrayType Type { get; }

        /// <summary>
        /// The dimensions of the array.
        /// </summary>
        [JsonProperty]
        public override IImmutableList<int> Dimensions { get; }

        /// <summary>
        /// The sets defined on the array.
        /// </summary>
        [JsonProperty]
        public override IImmutableList<KeyValuePair<string, IImmutableList<string>>> Sets { get; }

        /// <summary>
        /// Gets the total number of entries in the array.
        /// </summary>
        public override int Count => _entries.Total;

        /// <summary>
        /// Returns the value with the key defined by the key components or throws an exception if the key is not found.
        /// </summary>
        /// <param name="keys">
        /// The components that define the key whose value is returned.
        /// </param>
        /// <returns>
        /// The value stored by the given key.
        /// </returns>
        public IImmutableSequenceDictionary<string, TValue> this[KeySequence<string> keys] => _entries[keys.ToArray()];

        /// <summary>
        /// Returns the value with the key defined by the key components or throws an exception if the key is not found.
        /// </summary>
        /// <param name="keys">
        /// The components that define the key whose value is returned.
        /// </param>
        /// <returns>
        /// The value stored by the given key.
        /// </returns>
        public IImmutableSequenceDictionary<string, TValue> this[params string[] keys] => this[(KeySequence<string>) keys];

        /// <summary>
        /// Returns the value with the key defined by the key components or throws an exception if the key is not found.
        /// </summary>
        /// <param name="keys">
        /// The components that define the key whose value is returned.
        /// </param>
        /// <returns>
        /// The value stored by the given key.
        /// </returns>
        IImmutableSequenceDictionary<string> IHeaderArray.this[params string[] keys] => this[(KeySequence<string>)keys];

        /// <summary>
        /// Gets an <see cref="IEnumerable{T}"/> for the given keys.
        /// </summary>
        /// <param name="keys">
        /// The collection of keys.
        /// </param>
        /// <returns>
        /// An <see cref="IEnumerable{T}"/> for the given keys.
        /// </returns>
        IEnumerable<KeyValuePair<KeySequence<string>, TValue>> ISequenceIndexer<string, TValue>.this[params string[] keys] => _entries[keys];

        /// <summary>
        /// Gets an <see cref="IEnumerable"/> for the given keys.
        /// </summary>
        /// <param name="keys">
        /// The collection of keys.
        /// </param>
        /// <returns>
        /// An <see cref="IEnumerable"/> for the given keys.
        /// </returns>
        IEnumerable ISequenceIndexer<string>.this[params string[] keys] => this[keys];

        /// <summary>
        /// Represents one entry from a Header Array (HAR) file.
        /// </summary>
        /// <param name="header">
        /// The four character identifier for this <see cref="HeaderArray{TValue}"/>.
        /// </param>
        /// <param name="coefficient">
        /// The coefficient related to the <see cref="HeaderArray{TValue}"/>
        /// </param>
        /// <param name="description">
        /// The long name description of the <see cref="HeaderArray{TValue}"/>.
        /// </param>
        /// <param name="type">
        /// The type of element stored in the array.
        /// </param>
        /// <param name="entries">
        /// The data in the array.
        /// </param>
        /// <param name="dimensions">
        /// The dimensions of the array.
        /// </param>
        /// <param name="sets">
        /// The sets defined on the array.
        /// </param>
        public HeaderArray([NotNull] string header, [NotNull] string coefficient, [CanBeNull] string description, HeaderArrayType type, [NotNull] IEnumerable<KeyValuePair<KeySequence<string>, TValue>> entries, [NotNull] IEnumerable<int> dimensions, [NotNull] IImmutableList<KeyValuePair<string, IImmutableList<string>>> sets)
        {
            if (entries is null)
            {
                throw new ArgumentNullException(nameof(entries));
            }
            if (header is null)
            {
                throw new ArgumentNullException(nameof(header));
            }
            if (coefficient is null)
            {
                throw new ArgumentNullException(nameof(coefficient));
            }
            if (dimensions is null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            if (sets is null)
            {
                throw new ArgumentNullException(nameof(sets));
            }
            
            Header = header;
            Coefficient = coefficient;
            Description = description ?? string.Empty;
            Type = type;
            Dimensions = dimensions.ToImmutableArray();
            Sets = sets;
            _entries = entries.ToImmutableSequenceDictionary(sets);
        }

        /// <summary>
        /// Returns an indented JSON representation of the contents of this <see cref="HeaderArray{TValue}"/>.
        /// </summary>
        [Pure]
        public override string ToString()
        {
            return Serialize(true);
        }

        /// <summary>
        /// Returns a JSON representation of the contents of this <see cref="HeaderArray{TValue}"/>.
        /// </summary>
        [Pure]
        public override string Serialize(bool indent)
        {
            return JsonConvert.SerializeObject(this, indent ? Formatting.Indented : Formatting.None);
        }

        /// <summary>
        /// Returns a copy of this <see cref="IHeaderArray{TValue}"/> with the header modified.
        /// </summary>
        /// <param name="header">
        /// The new header.
        /// </param>
        /// <returns>
        /// A copy of this <see cref="IHeaderArray{TValue}"/> with a new name.
        /// </returns>
        [Pure]
        public IHeaderArray<TValue> With(string header)
        {
            return new HeaderArray<TValue>(header, Coefficient, Description, Type, _entries, Dimensions, Sets);
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
        [Pure]
        IHeaderArray IHeaderArray.With(string header)
        {
            return With(header);
        }

        /// <summary>
        /// Returns an enumerable that iterates through the logical collection as defined by the <see cref="IHeaderArray.Sets"/>.
        /// </summary>
        /// <returns>
        /// An enumerable that can be used to iterate through the logical collection as defined by the <see cref="IHeaderArray.Sets"/>.
        /// </returns>
        public IEnumerable<KeyValuePair<KeySequence<string>, TValue>> GetLogicalEnumerable()
        {
            return _entries.GetLogicalEnumerable();
        }

        /// <summary>
        /// Returns an enumerable that iterates through the logical collection as defined by the <see cref="IHeaderArray.Sets"/>.
        /// </summary>
        /// <returns>
        /// An enumerable that can be used to iterate through the logical collection as defined by the <see cref="IHeaderArray.Sets"/>.
        /// </returns>
        public IEnumerable<TValue> GetLogicalValuesEnumerable()
        {
            return _entries.GetLogicalValuesEnumerable();
        }

        /// <summary>
        /// Returns an enumerable that iterates through the logical collection as defined by the <see cref="IHeaderArray.Sets"/>.
        /// </summary>
        /// <returns>
        /// An enumerable that can be used to iterate through the logical collection as defined by the <see cref="IHeaderArray.Sets"/>.
        /// </returns>
        public IEnumerable<TValue> GetLogicalValuesEnumerable(IComparer<KeySequence<string>> keyComparer)
        {
            return _entries.GetLogicalValuesEnumerable(keyComparer);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// An enumerator that can be used to iterate through the collection.
        /// </returns>
        [Pure]
        [NotNull]
        public IEnumerator<KeyValuePair<KeySequence<string>, TValue>> GetEnumerator()
        {
            return _entries.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// An enumerator that can be used to iterate through the collection.
        /// </returns>
        [Pure]
        [NotNull]
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Produces a <see cref="JSchema"/> for the <see cref="IHeaderArray{TValue}"/>.
        /// </summary>
        [Pure]
        [NotNull]
        private static JSchema GetJsonSchema()
        {
            JSchemaGenerator generator = new JSchemaGenerator
            {
                DefaultRequired = Required.Always,
                SchemaIdGenerationHandling = SchemaIdGenerationHandling.TypeName,
                SchemaPropertyOrderHandling = SchemaPropertyOrderHandling.Default,
                SchemaLocationHandling = SchemaLocationHandling.Definitions,
                SchemaReferenceHandling = SchemaReferenceHandling.All
            };

            generator.GenerationProviders.Add(new StringEnumGenerationProvider());

            return generator.Generate(typeof(HeaderArray<TValue>));
        }
    }
}