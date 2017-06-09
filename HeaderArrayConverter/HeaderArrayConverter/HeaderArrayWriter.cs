using System.Threading.Tasks;
using JetBrains.Annotations;

namespace HeaderArrayConverter
{
    /// <summary>
    /// Writes <see cref="IHeaderArray"/> collections to file.
    /// </summary>
    [PublicAPI]
    public abstract class HeaderArrayWriter
    {
        /// <summary>
        /// Asynchronously writes the <see cref="IHeaderArray"/> collection to a zipped archive of JSON files.
        /// </summary>
        /// <param name="file">
        /// The output file.
        /// </param>
        /// <param name="source">
        /// The array collection to write.
        /// </param>
        public abstract Task WriteAsync([NotNull] string file, params IHeaderArray[] source);

        /// <summary>
        /// Synchronously writes the <see cref="IHeaderArray"/> collection to a zipped archive of JSON files.
        /// </summary>
        /// <param name="file">
        /// The output file.
        /// </param>
        /// <param name="source">
        /// The array collection to write.
        /// </param>
        public abstract void Write([NotNull] string file, params IHeaderArray[] source);
    }
}