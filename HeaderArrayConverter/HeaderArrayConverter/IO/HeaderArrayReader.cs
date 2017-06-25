using System.Collections.Generic;
using System.Threading.Tasks;
using AD.IO;
using JetBrains.Annotations;

namespace HeaderArrayConverter.IO
{
    /// <summary>
    /// Reads <see cref="IHeaderArray"/> collections from file.
    /// </summary>
    [PublicAPI]
    public abstract class HeaderArrayReader
    {
        /// <summary>
        /// An internal buffer size for asynchronous file streams.
        /// </summary>
        protected static readonly int BufferSize = 4096;

        /// <summary>
        /// Reads <see cref="IHeaderArray"/> collections from file.
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
        /// Asynchronously reads <see cref="IHeaderArray"/> collections from file.
        /// </summary>
        /// <param name="file">
        /// The file to read.
        /// </param>
        /// <return>
        /// A task that upon completion returns a <see cref="HeaderArrayFile"/> representing the contents of the file.
        /// </return>
        [NotNull]
        [ItemNotNull]
        public abstract Task<HeaderArrayFile> ReadAsync([NotNull] FilePath file);
        
        /// <summary>
        /// Enumerates the <see cref="IHeaderArray"/> collection from file.
        /// </summary>
        /// <param name="file">
        /// The file from which to read arrays.
        /// </param>
        /// <returns>
        /// A <see cref="IHeaderArray"/> collection from the file.
        /// </returns>
        [NotNull]
        [ItemNotNull]
        public abstract IEnumerable<IHeaderArray> ReadArrays([NotNull] FilePath file);

        /// <summary>
        /// Asynchronously enumerates the arrays from file.
        /// </summary>
        /// <param name="file">
        /// The file from which to read arrays.
        /// </param>
        /// <returns>
        /// An enumerable collection of tasks that when completed return an <see cref="IHeaderArray"/> from file.
        /// </returns>
        [NotNull]
        [ItemNotNull]
        public abstract IEnumerable<Task<IHeaderArray>> ReadArraysAsync([NotNull] FilePath file);
    }
}