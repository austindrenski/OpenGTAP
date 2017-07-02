using System;
using System.Collections;
using System.Collections.Generic;
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
    public class HeaderArrayFile : IEnumerable<IHeaderArray>
    {
        /// <summary>
        /// Provides methods to read <see cref="IHeaderArray"/> collections from Header Array files in binary format (HAR).
        /// </summary>
        [NotNull]
        public static HeaderArrayReader BinaryReader { get; } = new BinaryHeaderArrayReader();

        /// <summary>
        /// Provides methods to read <see cref="IHeaderArray"/> collections from Solution files in binary format (SL4).
        /// </summary>
        [NotNull]
        public static HeaderArrayReader BinarySolutionReader { get; } = new BinarySolutionReader();

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
        private readonly IReadOnlyDictionary<string, IHeaderArray> _arrays;

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

            _arrays = arrays.ToDictionary(x => x.Header, x => x);
        }

        /// <summary>
        /// Validates that the sets defined throughout the <see cref="HeaderArrayFile"/>. Validation information is logged to <paramref name="output"/>.
        /// </summary>
        /// <returns>
        /// True if there are no conflicts; otherwise false.
        /// </returns>
        public bool ValidateSets([CanBeNull] TextWriter output = null)
        {
            return HeaderArray.ValidateSets(this, output);
        }
       
        /// <summary>
        /// Returns a string representation of the contents of the <see cref="HeaderArrayFile"/>.
        /// </summary>
        [Pure]
        [NotNull]
        public override string ToString()
        {
            return JsonConvert.SerializeObject(_arrays, Formatting.Indented);
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
            return _arrays.OrderBy(x => x.Key).Select(x => x.Value).GetEnumerator();
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