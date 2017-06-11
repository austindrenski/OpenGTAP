using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HeaderArrayConverter.Collections;
using HeaderArrayConverter.Extensions;
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
        /// The padding sequence used in binary HAR files.
        /// </summary>
        private const int Padding = 0x20_20_20_20;

        /// <summary>
        /// The spacer sequence used in binary HAR files.
        /// </summary>
        private const uint Spacer = 0xFF_FF_FF_FF;

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

        /// <summary>
        /// Asynchronously writes the next array from the <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="writer">
        /// The <see cref="BinaryWriter"/> to which the array is written.
        /// </param>
        /// <param name="array">
        /// The array to write.
        /// </param>
        [NotNull]
        private static async Task WriteArrayAsync([NotNull] BinaryWriter writer, [NotNull] IHeaderArray array)
        {

            foreach (byte[] bytes in WriteArray(array))
            {
                writer.Write(bytes.Length);
                writer.Write(bytes);
                writer.Write(bytes.Length);
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Returns an enumerable of <see cref="byte"/> arrays representing the serialized <see cref="IHeaderArray"/>.
        /// </summary>
        /// <param name="array">
        /// The array to write.
        /// </param>
        [Pure]
        [NotNull]
        [ItemNotNull]
        private static IEnumerable<byte[]> WriteArray([NotNull] IHeaderArray array)
        {
            yield return WriteHeader(array);
            yield return WriteMetadata(array);

            switch (array.Type)
            {
                case "1C":
                {
                    yield return Write1CArrayValues(array.As<string>());
                    break;
                }
                case "RE":
                {
                    yield return WriteSetNames(array);
                    foreach (byte[] setEntries in WriteSetEntries(array))
                    {
                        yield return setEntries;
                    }
                    yield return WriteDimensions(array);
                    yield return WriteExtents(array);
                    yield return WriteReArrayValues(array.As<float>());
                    break;
                }
                default:
                {
                    throw new NotSupportedException($"Type: {array.Type}");
                }
            }
        }

        /// <summary>
        /// Writes the <see cref="IHeaderArray.Header"/>.
        /// </summary>
        /// <param name="array">
        /// The <see cref="IHeaderArray"/> to write.
        /// </param>
        /// <returns>
        /// A byte array containing the serialized data.
        /// </returns>
        [Pure]
        [NotNull]
        private static byte[] WriteHeader([NotNull] IHeaderArray array)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(array.Header.ToCharArray());
                }
                return stream.ToArray();
            }
        }

        /// <summary>
        /// Writes the <see cref="IHeaderArray.Type"/>, sparseness, <see cref="IHeaderArray.Description"/> (70-byte padded), and the length-prefixed <see cref="IHeaderArray.Dimensions"/>.
        /// </summary>
        /// <param name="array">
        /// The <see cref="IHeaderArray"/> to write.
        /// </param>
        /// <returns>
        /// A byte array containing the serialized data.
        /// </returns>
        [Pure]
        [NotNull]
        private static byte[] WriteMetadata([NotNull] IHeaderArray array)
        {
           using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(Padding);
                    writer.Write(array.Type.ToCharArray());
                    writer.Write("FULL".ToCharArray());
                    writer.Write(array.Description.PadRight(70).ToCharArray());
                    writer.Write(array.Dimensions.Count);
                    foreach (int dim in array.Dimensions)
                    {
                        writer.Write(dim);
                    }
                }
                return stream.ToArray();
            }
        }

        /// <summary>
        /// Writes the entries of <see cref="IHeaderArray.Sets"/>.
        /// </summary>
        /// <param name="array">
        /// The <see cref="IHeaderArray"/> to write.
        /// </param>
        /// <returns>
        /// A byte array containing the serialized data.
        /// </returns>
        [Pure]
        [NotNull]
        private static IEnumerable<byte[]> WriteSetEntries([NotNull] IHeaderArray array)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    HashSet<string> setsUsed = new HashSet<string>();
                    foreach (KeyValuePair<string, IImmutableList<string>> set in array.Sets)
                    {
                        if (!setsUsed.Add(set.Key))
                        {
                            continue;
                        }

                        yield return WriteSetsLocal(set);
                    }
                }
            }

            byte[] WriteSetsLocal(KeyValuePair<string, IImmutableList<string>> set)
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    using (BinaryWriter writer = new BinaryWriter(stream))
                    {
                        writer.Write(Padding);
                        writer.Write(1);
                        writer.Write(set.Value.Count);
                        writer.Write(set.Value.Count);
                        foreach (string value in set.Value)
                        {
                            writer.Write(value.PadRight(12).ToCharArray());
                        }
                    }
                    return stream.ToArray();
                }
            }
        }

        /// <summary>
        /// Writes the names and dimensions of <see cref="IHeaderArray.Sets"/>.
        /// </summary>
        /// <param name="array">
        /// The <see cref="IHeaderArray"/> to write.
        /// </param>
        /// <returns>
        /// A byte array containing the serialized data.
        /// </returns>
        [Pure]
        [NotNull]
        private static byte[] WriteSetNames(IHeaderArray array)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(Padding);
                    writer.Write(array.Sets.Select(x => x.Key).Distinct().Count());
                    writer.Write(Spacer);
                    writer.Write(array.Sets.Select(x => x.Key).Count());
                    writer.Write(array.Header.PadRight(12).ToCharArray());
                    writer.Write(Spacer);
                    foreach (string name in array.Sets.Select(x => x.Key))
                    {
                        writer.Write(name.PadRight(12).ToCharArray());
                    }
                    for (int i = 0; i < array.Sets.Count; i++)
                    {
                        writer.Write((byte)0x6B);
                    }
                    for (int i = 0; i < array.Sets.Count + 1; i++)
                    {
                        writer.Write(0x00_00_00_00);
                    }
                }
                return stream.ToArray();
            }
        }

        /// <summary>
        /// Writes the <see cref="IHeaderArray.Dimensions"/>.
        /// </summary>
        /// <param name="array">
        /// The <see cref="IHeaderArray"/> to write.
        /// </param>
        /// <returns>
        /// A byte array containing the serialized data.
        /// </returns>
        [Pure]
        [NotNull]
        private static byte[] WriteDimensions([NotNull] IHeaderArray array)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(Padding);
                    writer.Write(3);
                    writer.Write(array.Dimensions.Count);
                    foreach (int dimension in array.Dimensions)
                    {
                        writer.Write(dimension);
                    }
                }
                return stream.ToArray();
            }
        }

        /// <summary>
        /// Writes the extent array that describes the positions in the logical array that the next array represents.
        /// </summary>
        /// <param name="array">
        /// The <see cref="IHeaderArray"/> to write.
        /// </param>
        /// <returns>
        /// A byte array containing the serialized data.
        /// </returns>
        [Pure]
        [NotNull]
        private static byte[] WriteExtents([NotNull] IHeaderArray array)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(Padding);
                    writer.Write(2);
                    foreach (int dimension in array.Dimensions)
                    {
                        writer.Write(1);
                        writer.Write(dimension);
                    }
                }
                return stream.ToArray();
            }
        }

        /// <summary>
        /// Writes the contents of an <see cref="IHeaderArray{Single}"/> with type 'RE'.
        /// </summary>
        /// <param name="array">
        /// The <see cref="IHeaderArray"/> to write.
        /// </param>
        /// <returns>
        /// A byte array containing the serialized data.
        /// </returns>
        [Pure]
        [NotNull]
        private static byte[] WriteReArrayValues([NotNull] IHeaderArray<float> array)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(Padding);
                    writer.Write(1);
                    foreach (KeySequence<string> item in array.Sets.AsExpandedSet())
                    {
                        writer.Write(array.TryGetValue(item));
                    }
                }
                return stream.ToArray();
            }
        }

        /// <summary>
        /// Writes the contents of an <see cref="IHeaderArray{String}"/> with type '1C'.
        /// </summary>
        /// <param name="array">
        /// The <see cref="IHeaderArray"/> to write.
        /// </param>
        /// <returns>
        /// A byte array containing the serialized data.
        /// </returns>
        [Pure]
        [NotNull]
        private static byte[] Write1CArrayValues([NotNull] IHeaderArray<string> array)
        {
            int recordLength = array.Dimensions.Last();
            int lengthOfRecords = recordLength * array.Total;

            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(Padding);
                    writer.Write(1);
                    writer.Write(array.Total);
                    writer.Write(array.Total);
                    foreach (KeyValuePair<KeySequence<string>, string> item in array)
                    {
                        writer.Write(item.Value.PadRight(recordLength).ToCharArray());
                    }
                }
                return stream.ToArray();
            }
        }
    }
}