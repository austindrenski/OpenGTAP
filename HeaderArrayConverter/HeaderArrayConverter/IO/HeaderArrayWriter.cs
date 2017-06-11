using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace HeaderArrayConverter.IO
{
    /// <summary>
    /// Writes <see cref="IHeaderArray"/> collections to file.
    /// </summary>
    [PublicAPI]
    public abstract class HeaderArrayWriter
    {
        /// <summary>
        /// Synchronously writes the <see cref="IHeaderArray"/> collection to a zipped archive of JSON files.
        /// </summary>
        /// <param name="file">
        /// The output file.
        /// </param>
        /// <param name="source">
        /// The array collection to write.
        /// </param>
        public abstract void Write([NotNull] string file, [NotNull] IEnumerable<IHeaderArray> source);

        /// <summary>
        /// Asynchronously writes the <see cref="IHeaderArray"/> collection to a zipped archive of JSON files.
        /// </summary>
        /// <param name="file">
        /// The output file.
        /// </param>
        /// <param name="source">
        /// The array collection to write.
        /// </param>
        public abstract Task WriteAsync([NotNull] string file, [NotNull] IEnumerable<IHeaderArray> source);

        /// <summary>
        /// Synchronously writes the <see cref="IHeaderArray"/> collection to a zipped archive of JSON files.
        /// </summary>
        /// <param name="file">
        /// The output file.
        /// </param>
        /// <param name="source">
        /// The array collection to write.
        /// </param>
        public void Write([NotNull] string file, params IHeaderArray[] source)
        {
            if (file is null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            Write(file, source as IEnumerable<IHeaderArray>);
        }

        /// <summary>
        /// Synchronously writes the <see cref="IHeaderArray"/> collection to a zipped archive of JSON files.
        /// </summary>
        /// <param name="file">
        /// The output file.
        /// </param>
        /// <param name="source">
        /// The array collection to write.
        /// </param>
        public Task WriteAsync([NotNull] string file, params IHeaderArray[] source)
        {
            if (file is null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            return WriteAsync(file, source as IEnumerable<IHeaderArray>);
        }
    }
}