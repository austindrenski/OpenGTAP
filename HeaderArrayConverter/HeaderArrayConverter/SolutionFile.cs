using System;
using System.Collections.Generic;
using System.Linq;
using HeaderArrayConverter.IO;
using JetBrains.Annotations;

namespace HeaderArrayConverter
{
    /// <summary>
    /// Represents a Gempack Solution file (SL4).
    /// </summary>
    [PublicAPI]
    public static class SolutionFile
    {
        /// <summary>
        /// Provides methods to read <see cref="IHeaderArray"/> collections from Solution files in binary format (SL4).
        /// </summary>
        [NotNull]
        public static HeaderArrayReader BinaryReader { get; } = new BinarySolutionReader();

        /// <summary>
        /// Returns the <see cref="IHeaderArray"/> identified by the header from each <see cref="IHeaderArray"/> collection.
        /// </summary>
        /// <param name="header">
        /// The header to return from each collection.
        /// </param>
        /// <param name="source">
        /// The collection of collections from which the results are returned.
        /// </param>
        /// <returns>
        /// An <see cref="IHeaderArray"/> collection where each source collection provides a single entry.
        /// </returns>
        [Pure]
        [NotNull]
        public static IEnumerable<IHeaderArray> Resolve([NotNull] string header, params IEnumerable<IHeaderArray>[] source)
        {
            if (header is null)
            {
                throw new ArgumentNullException(nameof(header));
            }

            return source.Select(x => x.SingleOrDefault(y => y.Header == header));
        }
    }
}