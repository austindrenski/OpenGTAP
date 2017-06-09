using System;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace HeaderArrayConverter.IO
{
    /// <summary>
    /// Writes Header Array (HAR) files in binary format.
    /// </summary>
    [PublicAPI]
    public class BinaryHeaderArrayWriter : HeaderArrayWriter
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
        public override Task WriteAsync(string file, params IHeaderArray[] source)
        {
            if (file is null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            throw new NotImplementedException();
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
        public override void Write(string file, params IHeaderArray[] source)
        {
            if (file is null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            throw new NotImplementedException();
        }
    }
}