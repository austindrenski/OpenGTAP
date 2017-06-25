using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace HeaderArrayConverter.IO
{
    /// <summary>
    /// Implements a <see cref="HeaderArrayWriter"/> for writing Header Array (HARX) files in zipped JSON format.
    /// </summary>
    [PublicAPI]
    public class JsonHeaderArrayWriter : HeaderArrayWriter
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

            using (FileStream stream = new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.Read, BufferSize, true))
            {
                using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create))
                {
                    foreach (IHeaderArray item in source)
                    {
                        ZipArchiveEntry entry = archive.CreateEntry($"{item.Header}.json", CompressionLevel.Optimal);

                        using (StreamWriter writer = new StreamWriter(entry.Open()))
                        {
                            await writer.WriteAsync(item.Serialize(false));
                        }
                    }
                }
            }
        }
    }
}