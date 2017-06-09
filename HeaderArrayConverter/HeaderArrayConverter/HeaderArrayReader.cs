using System.Collections.Generic;
using AD.IO;
using JetBrains.Annotations;

namespace HeaderArrayConverter
{
    /// <summary>
    /// Reads <see cref="IHeaderArray"/> collections from file.
    /// </summary>
    [PublicAPI]
    public abstract class HeaderArrayReader
    {
        /// <summary>
        /// Reads <see cref="IHeaderArray"/> collections from file..
        /// </summary>
        /// <param name="file">
        /// The file to read.
        /// </param>
        /// <return>
        /// A <see cref="HeaderArrayFile"/> representing the contents of the file.
        /// </return>
        [NotNull]
        public abstract HeaderArrayFile Read([NotNull] FilePath file);

        /// <summary>
        /// Enumerates the arrays from the HARX file.
        /// </summary>
        /// <param name="file">
        /// The HARX file from which to read arrays.
        /// </param>
        /// <returns>
        /// An enumerable collection of the arrays in the file.
        /// </returns>
        public abstract IEnumerable<IHeaderArray> ReadArrays([NotNull] FilePath file);
    }
}