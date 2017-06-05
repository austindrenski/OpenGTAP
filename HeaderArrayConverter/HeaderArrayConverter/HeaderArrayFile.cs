using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AD.IO;
using JetBrains.Annotations;

namespace HeaderArrayConverter
{
    /// <summary>
    /// Represents the contents of a HAR file.
    /// </summary>
    [PublicAPI]
    public class HeaderArrayFile : IEnumerable<IHeaderArray>
    {
        /// <summary>
        /// The contents of the HAR file.
        /// </summary>
        [NotNull]
        private readonly ImmutableSequenceDictionary<string, IHeaderArray> _arrays;

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
        public IHeaderArray this[string header] => _arrays[header].Single().Value;

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

            _arrays = arrays.ToImmutableOrderedDictionary(x => x.Header, x => x);
        }

        /// <summary>
        /// Reads the contents of the HAR file.
        /// </summary>
        /// <param name="file">
        /// The file to read.
        /// </param>
        /// <returns>
        /// The contents of the HAR file.
        /// </returns>
        [Pure]
        [NotNull]
        public static HeaderArrayFile Read([NotNull] FilePath file)
        {
            if (file is null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            return new HeaderArrayFile(HeaderArray.ReadArrays(file));
        }

        /// <summary>
        /// Returns a string representation of the contents of the <see cref="HeaderArrayFile"/>.
        /// </summary>
        [Pure]
        [NotNull]
        public override string ToString()
        {
            return
                _arrays.Aggregate(
                    new StringBuilder(),
                    (current, next) =>
                        current.AppendLine("-----------------------------------------------")
                               .AppendLine(next.Value.ToString()),
                    x =>
                        x.ToString());
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

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}