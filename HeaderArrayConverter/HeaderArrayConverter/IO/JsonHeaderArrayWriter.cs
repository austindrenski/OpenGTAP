using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace HeaderArrayConverter.IO
{
    /// <summary>
    /// Writes <see cref="IHeaderArray"/> collections to a zipped archive of JSON files.
    /// </summary>
    [PublicAPI]
    public class JsonHeaderArrayWriter : HeaderArrayWriter
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
        public override async Task WriteAsync(string file, params IHeaderArray[] source)
        {
            if (file is null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            using (ZipArchive archive = new ZipArchive(new FileStream(file, FileMode.Create), ZipArchiveMode.Create))
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

            WriteAsync(file, source).Wait();
        }
    }
}