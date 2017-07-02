using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
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

            using (BinaryWriter writer = new BinaryWriter(File.Open(file, FileMode.Create, FileAccess.Write, FileShare.Read)))
            {
                foreach (IHeaderArray array in ValidateHeaders(source))
                {
                    foreach (byte[] bytes in ComposeArray(array))
                    {
                        writer.Write(bytes.Length);
                        writer.Write(bytes);
                        writer.Write(bytes.Length);
                    }
                }
            }
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

            await Task.Run(() => Write(file, source));
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
        /// Returns an enumerable of <see cref="byte"/> arrays representing the serialized <see cref="IHeaderArray"/>.
        /// </summary>
        /// <param name="array">
        /// The array to write.
        /// </param>
        [Pure]
        [NotNull]
        [ItemNotNull]
        private static IEnumerable<byte[]> ComposeArray([NotNull] IHeaderArray array)
        {
            yield return Encoding.ASCII.GetBytes(array.Header.PadRight(4).Substring(0, 4));
            yield return WriteMetadata(array);

            switch (array.Type)
            {
                case HeaderArrayType.C1:
                {
                    yield return WriteCharacterArray(array.Count, array.Dimensions.Last(), array.As<string>().Values);
                    yield break;
                }
                case HeaderArrayType.RE:
                {
                    foreach (byte[] values in WriteRealArrayWithSetLabels(array.As<float>()))
                    {
                        yield return values;
                    }

                    yield break;
                }
                case HeaderArrayType.I2:
                {
                    foreach (byte[] values in WriteTwoDimensionalNumericArray(array.As<int>(), (writer, value) => writer.Write(value)))
                    {
                        yield return values;
                    }                    
                    
                    yield break;
                }
                case HeaderArrayType.R2:
                {
                    foreach (byte[] values in WriteTwoDimensionalNumericArray(array.As<float>(), (writer, value) => writer.Write(value)))
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
        /// Writes the next array component.
        /// </summary>
        /// <param name="write">
        /// A delegate that writes to a <see cref="BinaryWriter"/>.
        /// </param>
        /// <returns>
        /// A <see cref="byte"/> array containing the serialized data written by <paramref name="write"/>.,
        /// </returns>
        [Pure]
        [NotNull]
        private static byte[] WriteComponent(Action<BinaryWriter> write)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(Padding);

                    write(writer);

                    return stream.ToArray();
                }
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
            return
                WriteComponent(
                    writer =>
                    {
                        writer.Write((short)array.Type);
                        writer.Write("FULL".ToCharArray());
                        writer.Write(array.Description.PadRight(70).Substring(0, 70).ToCharArray());
                        writer.Write(array.Dimensions.Count);

                        foreach (int dimension in array.Dimensions)
                        {
                            writer.Write(dimension);
                        }
                    });
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
        private static IEnumerable<byte[]> WriteSets(IHeaderArray array)
        {
            HashSet<string> setsUsed = new HashSet<string>();

            yield return 
                WriteComponent(
                    writer =>
                    {
                        writer.Write(array.Sets.Select(x => x.Key).Distinct().Count());
                        writer.Write(Spacer);
                        writer.Write(array.Sets.Count);
                        writer.Write(array.Coefficient.PadRight(12).Substring(0, 12).ToCharArray());
                        writer.Write(Spacer);

                        for (int i = 0; i < array.Sets.Count; i++)
                        {
                            writer.Write(array.Sets[i].Key.PadRight(12).Substring(0, 12).ToCharArray());
                        }

                        for (int i = 0; i < array.Sets.Count; i++)
                        {
                            writer.Write('k');
                        }

                        writer.Write(0);

                        for (int i = 0; i < array.Sets.Count; i++)
                        {
                            writer.Write(0);
                        }
                    });

            foreach (KeyValuePair<string, IImmutableList<string>> set in array.Sets)
            {
                if (!setsUsed.Add(set.Key))
                {
                    continue;
                }

                yield return WriteCharacterArray(set.Value.Count, 12, set.Value);
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
            return
                WriteComponent(
                    writer =>
                    {
                        writer.Write(vectorIndex);
                        writer.Write(array.Dimensions.Count);

                        foreach (int dimension in array.Dimensions)
                        {
                            writer.Write(dimension);
                        }
                    });
        }

        /// <summary>
        /// Writes the extents for an upcoming component.
        /// </summary>
        /// <param name="vectorIndex">
        /// The index of the extent array itself.
        /// </param>
        /// <param name="ranges">
        /// Information describing which set dimensions are in the following array.
        /// </param>
        /// <returns>
        /// A <see cref="byte"/> array of the serialized daya.
        /// </returns>
        private static byte[] WriteExtents(int vectorIndex, IReadOnlyList<(int Lower, int Upper)> ranges)
        {
            return
                WriteComponent(
                    writer =>
                    {
                        writer.Write(vectorIndex);

                        for (int i = 0; i < ranges.Count; i++)
                        {
                            writer.Write(ranges[i].Lower > 0 ? ranges[i].Lower : 1);
                            writer.Write(ranges[i].Upper);
                        }
                    });
        }

        /// <summary>
        /// Writes the contents of an <see cref="IHeaderArray{String}"/> with type '1C'.
        /// </summary>
        /// <param name="count">
        /// The number of items to write.
        /// </param>
        /// <param name="itemLength">
        /// The length of each item.
        /// </param>
        /// <param name="items">
        /// The items to write.
        /// </param>
        /// <returns>
        /// A byte array containing the serialized data.
        /// </returns>
        [Pure]
        [NotNull]
        private static byte[] WriteCharacterArray(int count, int itemLength, IEnumerable<string> items)
        {
            return
                WriteComponent(
                    writer =>
                    {
                        writer.Write(1);
                        writer.Write(count);
                        writer.Write(count);

                        foreach (string item in items)
                        {
                            writer.Write((item ?? string.Empty).PadRight(itemLength).Substring(0, itemLength).ToCharArray());
                        }
                    });
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
        private static IEnumerable<byte[]> WriteRealArrayWithSetLabels([NotNull] IHeaderArray<float> array)
        {
            foreach (byte[] component in WriteSets(array))
            {
                yield return component;
            }

            Partition<float> partitions = new Partition<float>(array);

            yield return WriteDimensions(array, 2 * partitions.Partitions + 1);
            
            foreach ((int vectorIndex, IReadOnlyList<(int Lower, int Upper)> ranges, IReadOnlyList<float> values) in partitions)
            {
                yield return WriteExtents(2 * vectorIndex, ranges);

                yield return 
                    WriteComponent(
                        writer =>
                        {
                            writer.Write(2 * vectorIndex - 1);

                            for (int i = 0; i < values.Count; i++)
                            {
                                writer.Write(values[i]);
                            }
                        });
            }
        }



        /// <summary>
        /// Writes the contents of a two dimensional numeric <see cref="IHeaderArray{T}"/> with type '2R' or '2I'.
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
        private static IEnumerable<byte[]> WriteTwoDimensionalNumericArray<T>([NotNull] IHeaderArray<T> array, Action<BinaryWriter, T> write) where T : IEquatable<T>
        {
            foreach ((int vectorIndex, IReadOnlyList<(int Lower, int Upper)> ranges, IReadOnlyCollection<T> values) in new Partition<T>(array))
            {
                yield return
                    WriteComponent(
                        writer =>
                        {
                            writer.Write(vectorIndex);
                            foreach (int item in array.Dimensions)
                            {
                                writer.Write(item);
                            }
                           
                            for (int i = 0; i < ranges.Count; i++)
                            {
                                writer.Write(ranges[i].Lower > 0 ? ranges[i].Lower : 1);
                                writer.Write(ranges[i].Upper);
                            }

                            foreach (T item in values)
                            {
                                write(writer, item);
                            }
                        });
            }
        }
    }
}