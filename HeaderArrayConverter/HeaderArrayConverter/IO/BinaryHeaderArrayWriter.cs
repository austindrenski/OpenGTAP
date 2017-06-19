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
        /// The internal limitation on array length in the Fortran routines used by Gempack.
        /// </summary>
        private static readonly int GempackArrayLimit = 1_999_991;

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
                case HeaderArrayType.C1:
                {
                    yield return Write1CArrayValues(array.As<string>());
                    yield break;
                }
                case HeaderArrayType.RE:
                {
                    yield return WriteSetNames(array);

                    foreach (byte[] setEntries in WriteSetEntries(array))
                    {
                        yield return setEntries;
                    }

                    yield return WriteDimensions(array);
                    yield return WriteExtents(array);

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
                    // TODO: need this for files that go SL4 -> HARX -> HAR. Fix this later.
                    writer.Write(array.Header.PadRight(4).ToArray());
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

                    // TODO: need this for files that go SL4 -> HARX -> HAR. Fix this later.
                    writer.Write(array.Description.PadRight(70).ToArray());

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
        private static IEnumerable<byte[]> WriteReArrayValues([NotNull] IHeaderArray<float> array)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(Padding);
                    writer.Write(1);
                    foreach (float item in array.GetLogicalValuesEnumerable())
                    {
                        writer.Write(item);
                    }
                }
                yield return stream.ToArray();
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

            foreach ((int vectorIndex, T[] values) in Partition(array.GetLogicalValuesEnumerable(), array.SerializedVectors))
            {
                yield return ProcessNext(values, vectorIndex);
                counter += values.Length;
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
                    }
                    byte[] values = stream.ToArray();

                    return values;
                }
            }
        }

        /// <summary>
        /// Partitions a source collection into the specified number of sequential groups.
        /// </summary>
        /// <param name="source">
        /// The source collection.
        /// </param>
        /// <param name="partitions">
        /// The number of groups to create.
        /// </param>
        /// <returns>
        /// The source collection split into the specified number of groups.
        /// </returns>
        [Pure]
        [NotNull]
        private static IEnumerable<(int VectorIndex, T[] Values)> Partition<T>([NotNull] IEnumerable<T> source, int partitions)
        {
            source = source as T[] ?? source.ToArray();

            int vectors = partitions > 0 ? partitions : 1;

            int count = source.Count() / vectors + (vectors > 1 ? 2 : 0);

            while (count > GempackArrayLimit)
            {
                vectors++;
                count = source.Count() / vectors + (vectors > 1 ? 2 : 0);
            }

            for (int i = 0; i < vectors; i++)
            {
                T[] temp = source.Skip(i * count).Take(count).ToArray();

                if (temp.Any() || count == 0)
                {
                    yield return (vectors - i, temp);
                }
            }
        }
    }
}