using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using HeaderArrayConverter.IO;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace HeaderArrayConverter
{
    /// <summary>
    /// Represents the contents of a HAR file.
    /// </summary>
    [PublicAPI]
    [JsonObject(MemberSerialization.OptIn)]
    public class HeaderArrayFile : IEnumerable<IHeaderArray>
    {
        /// <summary>
        /// Provides methods to read <see cref="IHeaderArray"/> collections from Header Array files in binary format (HAR).
        /// </summary>
        [NotNull]
        public static HeaderArrayReader BinaryReader { get; } = new BinaryHeaderArrayReader();

        /// <summary>
        /// Provides methods to read <see cref="IHeaderArray"/> collections to Header Array files in binary format (HAR).
        /// </summary>
        [NotNull]
        public static HeaderArrayWriter BinaryWriter { get; } = new BinaryHeaderArrayWriter();

        /// <summary>
        /// Provides methods to read <see cref="IHeaderArray"/> collections from Header Array files in zipped JSON format (HARX).
        /// </summary>
        [NotNull]
        public static HeaderArrayReader JsonReader { get; } = new JsonHeaderArrayReader();

        /// <summary>
        /// Provides methods to write <see cref="IHeaderArray"/> collections to Header Array files in zipped JSON format (HARX).
        /// </summary>
        [NotNull]
        public static HeaderArrayWriter JsonWriter { get; } = new JsonHeaderArrayWriter();

        /// <summary>
        /// The contents of the HAR file.
        /// </summary>
        [NotNull]
        [JsonProperty("Arrays", Order = int.MaxValue)]
        private readonly IImmutableDictionary<string, IHeaderArray> _arrays;

        /// <summary>
        /// Gets the count of arrays in the file, including metadata arrays.
        /// </summary>
        public int Count => _arrays.Count;

        /// <summary>
        /// Gets the <see cref="IHeaderArray"/> with the given header.
        /// </summary>
        /// <param name="header">
        /// The header of the array.
        /// </param>
        /// <returns>
        /// The <see cref="IHeaderArray"/> with the given header.
        /// </returns>
        [NotNull]
        public IHeaderArray this[string header] => _arrays[header];

        /// <summary>
        /// Constructs a <see cref="HeaderArrayFile"/> from an <see cref="IHeaderArray"/> collection.
        /// </summary>
        /// <param name="arrays">
        /// The collection of arrays from which to construct the <see cref="HeaderArrayFile"/>.
        /// </param>
        public HeaderArrayFile([NotNull] IEnumerable<IHeaderArray> arrays)
        {
            if (arrays is null)
            {
                throw new ArgumentNullException(nameof(arrays));
            }

            _arrays = arrays.ToImmutableSortedDictionary(x => x.Header, x => x);
        }


        /// <summary>
        /// Validates that the sets defined throughout the <see cref="HeaderArrayFile"/>. Validation information is logged to <paramref name="output"/>.
        /// </summary>
        /// <returns>
        /// True if there are no conflicts; otherwise false.
        /// </returns>
        public bool ValidateSets([CanBeNull] TextWriter output = null)
        {
            return ValidateSets(this, output);
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
        /// Returns a string representation of the contents of the <see cref="HeaderArrayFile"/>.
        /// </summary>
        [Pure]
        [NotNull]
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// An enumerator that can be used to iterate through the collection.
        /// </returns>
        [Pure]
        [NotNull]
        public IEnumerator<IHeaderArray> GetEnumerator()
        {
            return _arrays.Select(x => x.Value).GetEnumerator();
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
    }
}