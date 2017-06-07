using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

            _arrays = arrays.ToImmutableSequenceDictionary(x => (KeySequence<string>)x.Header, x => x);
        }

        /// <summary>
        /// Returns a string representation of the contents of the <see cref="HeaderArrayFile"/>.
        /// </summary>
        [Pure]
        [NotNull]
        public override string ToString()
        {
            return
                _arrays.OrderBy(x => x.Key.ToString())
                       .Aggregate(
                           new StringBuilder(),
                           (current, next) =>
                               current.AppendLine(next.Value.ToString()),
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

        /// <summary>
        /// Reads the contents of a HAR file.
        /// </summary>
        /// <param name="file">
        /// The file to read.
        /// </param>
        /// <returns>
        /// The contents of the HAR file.
        /// </returns>
        [NotNull]
        public static HeaderArrayFile ReadHarFile([NotNull] FilePath file)
        {
            if (file is null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            return new HeaderArrayFile(HeaderArray.ReadHarArrays(file));
        }

        /// <summary>
        /// Reads the contents of a HARX file.
        /// </summary>
        /// <param name="file">
        /// The file to read.
        /// </param>
        /// <returns>
        /// The contents of the HARX file.
        /// </returns>
        [NotNull]
        public static HeaderArrayFile ReadHarxFile([NotNull] FilePath file)
        {
            if (file is null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            return new HeaderArrayFile(HeaderArray.ReadHarxArrays(file));
        }

        /// <summary>
        /// Asynchronously writes the header arrays to a zipped archive of JSON files.
        /// </summary>
        /// <param name="file">
        /// The output file.
        /// </param>
        /// <param name="arrays">
        /// The arrays to write.
        /// </param>
        public static async Task WriteHarxAsync([NotNull] string file, IEnumerable<IHeaderArray> arrays)
        {
            if (file is null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            using (ZipArchive archive = new ZipArchive(new FileStream(file, FileMode.Create), ZipArchiveMode.Create))
            {
                foreach (IHeaderArray item in arrays)
                {
                    ZipArchiveEntry entry = archive.CreateEntry($"{item.Header}.json", CompressionLevel.Optimal);
                    using (StreamWriter writer = new StreamWriter(entry.Open()))
                    {
                        await writer.WriteAsync(item.ToJson());
                    }
                }
            }
        }

        /// <summary>
        /// Asynchronously writes the header arrays to a zipped archive of JSON files.
        /// </summary>
        /// <param name="file">
        /// The output file.
        /// </param>
        /// <param name="arrays">
        /// The arrays to write.
        /// </param>
        public static async Task WriteHarxAsync([NotNull] string file, params IHeaderArray[] arrays)
        {
            if (file is null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            await WriteHarxAsync(file, arrays as IEnumerable<IHeaderArray>);
        }

        /// <summary>
        /// Writes the header arrays to a zipped archive of JSON files.
        /// </summary>
        /// <param name="file">
        /// The output file.
        /// </param>
        /// <param name="arrays">
        /// The arrays to write.
        /// </param>
        public static void WriteHarx([NotNull] string file, IEnumerable<IHeaderArray> arrays)
        {
            if (file is null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            WriteHarxAsync(file, arrays).Wait();
        }

        /// <summary>
        /// Writes the header arrays to a zipped archive of JSON files.
        /// </summary>
        /// <param name="file">
        /// The output file.
        /// </param>
        /// <param name="arrays">
        /// The arrays to write.
        /// </param>
        public static void WriteHarx([NotNull] string file, params IHeaderArray[] arrays)
        {
            if (file is null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            WriteHarxAsync(file, arrays as IEnumerable<IHeaderArray>).Wait();
        }
    }
}