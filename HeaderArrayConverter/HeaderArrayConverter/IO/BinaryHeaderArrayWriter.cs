using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HeaderArrayConverter.Types;
using JetBrains.Annotations;

namespace HeaderArrayConverter.IO
{
    /// <summary>
    /// Implements a <see cref="HeaderArrayWriter"/> for writing Header Array (HAR) files in binary format.
    /// </summary>
    [PublicAPI]
    public class BinaryHeaderArrayWriter : HeaderArrayWriter
    {
        /// <summary>
        /// The padding sequence used in binary HAR files.
        /// </summary>
        private static readonly int Padding = 0x20_20_20_20;

        /// <summary>
        /// The spacer sequence used in binary HAR files.
        /// </summary>
        private static readonly uint Spacer = 0xFF_FF_FF_FF;

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
                foreach (IHeaderArray array in ValidateHeaders(source))
                {
                    await WriteArrayAsync(writer, array);
                }
            }
        }

        /// <summary>
        /// Validates the headers for uniqueness and length. 
        /// If not unique or too long, an attempt to truncate is made. 
        /// If that fails, then a numeric header is generated.
        /// </summary>
        /// <param name="arrays">
        /// The list of arrays to validate.
        /// </param>
        /// <returns>
        /// A sequnece of arrays with valid headers.
        /// </returns>
        private static IEnumerable<IHeaderArray> ValidateHeaders([NotNull] IEnumerable<IHeaderArray> arrays)
        {
            HashSet<string> headers = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);

            foreach (IHeaderArray array in arrays)
            {
                if (array.Header.Length <= 4 && headers.Add(array.Header))
                {
                    yield return array;
                    continue;
                }
                
                string truncated = array.Header.Substring(0, 4);

                if (headers.Add(truncated))
                {
                    Console.Out.WriteLineAsync($"Renaming header '{array.Header}' to {truncated}.");

                    yield return array.With(truncated);
                    continue;
                }

                for (int i = 0; i < 10000; i++)
                {
                    truncated = $"{array.Header.Substring(0, 4 - i.ToString().Length)}{i}";

                    if (!headers.Add(truncated))
                    {
                        continue;
                    }

                    Console.Out.WriteLineAsync($"Renaming header '{array.Header}' to {truncated}.");

                    yield return array.With(truncated);
                    break;
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
                case HeaderArrayType.C1:
                {
                    yield return Write1CArrayValues(array.As<string>());
                    yield break;
                }
                case HeaderArrayType.RE:
                {
                    foreach (byte[] values in WriteReArrayValues(array.As<float>()))
                    {
                        yield return values;
                    }

                    yield break;
                }
                case HeaderArrayType.I2:
                {
                    foreach (byte[] values in Write2_ArrayValues(array.As<int>(), (writer, value) => writer.Write(value)))
                    {
                        yield return values;
                    }                    
                    
                    yield break;
                }
                case HeaderArrayType.R2:
                {
                    foreach (byte[] values in Write2_ArrayValues(array.As<float>(), (writer, value) => writer.Write(value)))
                    {
                        yield return values;
                    }

                    yield break;
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
                    writer.Write(array.Header.PadRight(4).Take(4).ToArray());
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
                    writer.Write((short)array.Type);
                    writer.Write("FULL".ToCharArray());
                    writer.Write(array.Description.PadRight(70).Take(70).ToArray());
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
            HashSet<string> setsUsed = new HashSet<string>();

            foreach (KeyValuePair<string, IImmutableList<string>> set in array.Sets)
            {
                if (!setsUsed.Add(set.Key))
                {
                    continue;
                }

                yield return WriteSetsLocal(set);
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
                    writer.Write(array.Coefficient.PadRight(12).Take(12).ToArray());
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
        /// <param name="vectorIndex">
        /// Index from the end of the record.
        /// </param>
        /// <returns>
        /// A byte array containing the serialized data.
        /// </returns>
        [Pure]
        [NotNull]
        private static byte[] WriteDimensions([NotNull] IHeaderArray array, int vectorIndex)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(Padding);
                    writer.Write(vectorIndex);
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
        private static IEnumerable<byte[]> WriteReArrayValues([NotNull] IHeaderArray<float> array)
        {
            yield return WriteSetNames(array);

            foreach (byte[] setEntries in WriteSetEntries(array))
            {
                yield return setEntries;
            }
            
           Partition<float> partitions = new Partition<float>(array);

            yield return WriteDimensions(array, 2 * partitions.Partitions + 1);

            foreach ((int vectorIndex, IReadOnlyList<int> min, IReadOnlyList<int> max, IReadOnlyCollection<float> values) in partitions)
            {
                yield return WriteNextExtents(2 * vectorIndex, min, max);

                yield return WriteSegment(2 * vectorIndex - 1, values);
            }

            // Writes the extent array that describes the positions in the logical array that the next array represents.
            byte[] WriteNextExtents(int vectorIndex, IReadOnlyList<int> min, IReadOnlyList<int> max)
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    using (BinaryWriter writer = new BinaryWriter(stream))
                    {
                        writer.Write(Padding);
                        writer.Write(vectorIndex);

                        for (int i = 0; i < min.Count; i++)
                        {
                            writer.Write(min[i] > 0 ? min[i] : 1);
                            writer.Write(max[i] > 0 ? max[i] : 1);
                        }

                        return stream.ToArray();
                    }
                }
            }

            // Writes the next array segment.
            byte[] WriteSegment(int vectorIndex, IEnumerable<float> values)
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    using (BinaryWriter writer = new BinaryWriter(stream))
                    {
                        writer.Write(Padding);
                        writer.Write(vectorIndex);

                        foreach (float item in values)
                        {
                            writer.Write(item);
                        }

                        return stream.ToArray();
                    }
                }
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

            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(Padding);
                    writer.Write(1);
                    writer.Write(array.Total);
                    writer.Write(array.Total);
                    foreach (string item in array.GetLogicalValuesEnumerable())
                    {
                        writer.Write((item ?? string.Empty).PadRight(recordLength).ToCharArray());
                    }
                }
                return stream.ToArray();
            }
        }

        /// <summary>
        /// Writes the contents of an <see cref="IHeaderArray{Single}"/> with type '2R' or '2I'.
        /// </summary>
        /// <param name="array">
        /// The <see cref="IHeaderArray"/> to write.
        /// </param>
        /// <param name="write">
        /// Writes the next value to the <see cref="BinaryWriter"/>.
        /// </param>
        /// <returns>
        /// A byte array containing the serialized data.
        /// </returns>
        [Pure]
        [NotNull]
        private static IEnumerable<byte[]> Write2_ArrayValues<T>([NotNull] IHeaderArray<T> array, Action<BinaryWriter, T> write)
        {
            int counter = 0;

            foreach ((int vectorIndex, _, _, IReadOnlyCollection<T> values) in new Partition<T>(array))
            {
                yield return ProcessNext(values, vectorIndex);
                counter += values.Count;
            }

            // <summary>
            // Returns a byte array representing the source collection.
            // </summary>
            byte[] ProcessNext(IReadOnlyCollection<T> source, int vectorIndex)
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    using (BinaryWriter writer = new BinaryWriter(stream))
                    {
                        writer.Write(Padding);

                        writer.Write(vectorIndex);
                        foreach (int item in array.Dimensions)
                        {
                            writer.Write(item);
                        }

                        // counter is tracking total elements written.
                        // so 1 + counter is the next element in a single dimensional context.
                        // but this may have n-dimensions.

                        writer.Write(1 + counter);
                        writer.Write(counter + source.Count);

                        writer.Write(1);
                        writer.Write(1);

                        foreach (T item in source)
                        {
                            write(writer, item);
                        }

                        return stream.ToArray();
                    }
                }
            }
        }
    }
}