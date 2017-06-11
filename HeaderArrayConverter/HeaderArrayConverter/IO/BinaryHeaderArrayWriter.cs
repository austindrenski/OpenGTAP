using System;
using System.Collections.Generic;
using System.IO;
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
        /// Synchronously writes the <see cref="IHeaderArray"/> collection to a zipped archive of JSON files.
        /// </summary>
        /// <param name="file">
        /// The output file.
        /// </param>
        /// <param name="source">
        /// The array collection to write.
        /// </param>
        public override void Write(string file, IEnumerable<IHeaderArray> source)
        {
            if (file is null)
            {
                throw new ArgumentNullException(nameof(file));
            }
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            WriteAsync(file, source).Wait();
        }

        /// <summary>
        /// Asynchronously writes the <see cref="IHeaderArray"/> collection to a zipped archive of JSON files.
        /// </summary>
        /// <param name="file">
        /// The output file.
        /// </param>
        /// <param name="source">
        /// The array collection to write.
        /// </param>
        public override async Task WriteAsync(string file, IEnumerable<IHeaderArray> source)
        {
            if (file is null)
            {
                throw new ArgumentNullException(nameof(file));
            }
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            using (BinaryWriter writer = new BinaryWriter(new FileStream(file, FileMode.Create)))
            {
                foreach (IHeaderArray array in source)
                {
                    await WriteArrayAsync(writer, array);
                }
            }
        }

        private static async Task WriteArrayAsync([NotNull] BinaryWriter writer, [NotNull] IHeaderArray array)
        {
            if (writer is null)
            {
                throw new ArgumentNullException(nameof(writer));
            }
            if (array is null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            const int padding = 0x20_20_20_20;

            WriteHeader();
            WriteMetadata();

            await Task.CompletedTask;

            void WriteHeader()
            {
                writer.Write(array.Header.Length);
                writer.Write(array.Header.ToCharArray());
                writer.Write(array.Header.Length);
            }

            void WriteMetadata()
            {
                int size = 4 + array.Type.Length + "FULL".Length + 70 + 4 * (1 + array.Dimensions.Count);

                writer.Write(size);
                writer.Write(padding);
                writer.Write(array.Type.ToCharArray());
                writer.Write("FULL".ToCharArray());
                writer.Write(array.Description.PadRight(70).ToCharArray());
                writer.Write(array.Dimensions.Count);
                foreach (int dim in array.Dimensions)
                {
                    writer.Write(dim);
                }
                writer.Write(size);
            }
        }
    }
}