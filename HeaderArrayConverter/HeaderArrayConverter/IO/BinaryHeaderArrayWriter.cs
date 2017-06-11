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
        private static async Task WriteArrayAsync([NotNull] BinaryWriter writer, [NotNull] IHeaderArray array)
        {
            WriteHeader(writer, array);
            WriteMetadata(writer, array);
            switch (array.Type)
            {
                case "1C":
                {
                    byte[] bytes = Write1CArrayValues(array.As<string>());
                    writer.Write(bytes.Length);
                    writer.Write(bytes);
                    writer.Write(bytes.Length);

                    break;
                }
                case "RE":
                {
                    byte[] setNames = WriteSetNames(array);
                    writer.Write(setNames.Length);
                    writer.Write(setNames);
                    writer.Write(setNames.Length);

                    foreach (byte[] setEntries in WriteSetEntries(array))
                    {
                        writer.Write(setEntries.Length);
                        writer.Write(setEntries);
                        writer.Write(setEntries.Length);
                    }

                    byte[] dimensionBytes = WriteDimensions(array);
                    writer.Write(dimensionBytes.Length);
                    writer.Write(dimensionBytes);
                    writer.Write(dimensionBytes.Length);

                    byte[] extentBytes = WriteExtents(array);
                    writer.Write(extentBytes.Length);
                    writer.Write(extentBytes);
                    writer.Write(extentBytes.Length);

                    byte[] bytes = WriteReArrayValues(array.As<float>());
                    writer.Write(bytes.Length);
                    writer.Write(bytes);
                    writer.Write(bytes.Length);

                    break;
                }
                default:
                {
                    throw new NotSupportedException($"Type: {array.Type}");
                }
            }
            await Task.CompletedTask;
        }

        /// <summary>
        /// Writes the <see cref="IHeaderArray.Header"/>.
        /// </summary>
        /// <param name="writer">
        /// The <see cref="BinaryWriter"/> positioned at the start of the file, or immediately following the previous header array.
        /// </param>
        /// <param name="array">
        /// The <see cref="IHeaderArray"/> to write.
        /// </param>
        private static void WriteHeader([NotNull] BinaryWriter writer, [NotNull] IHeaderArray array)
        {
            int headerLength = array.Header.Length;

            writer.Write(headerLength);
            writer.Write(array.Header.ToCharArray());
            writer.Write(headerLength);
        }

        /// <summary>
        /// Writes the <see cref="IHeaderArray.Type"/>, sparseness, <see cref="IHeaderArray.Description"/> (70-byte padded), and the length-prefixed <see cref="IHeaderArray.Dimensions"/>.
        /// </summary>
        /// <param name="writer">
        /// The <see cref="BinaryWriter"/> positioned after the call to <see cref="WriteHeader(BinaryWriter, IHeaderArray)"/>.
        /// </param>
        /// <param name="array">
        /// The <see cref="IHeaderArray"/> to write.
        /// </param>
        private static void WriteMetadata([NotNull] BinaryWriter writer, [NotNull] IHeaderArray array)
        {
            const int paddingLength = 4;
            int typeLength = array.Type.Length;
            const int fullLength = 4;
            const int descriptionLength = 70;
            const int dimensionCount = 4;
            int dimensionsLength = 4 * array.Dimensions.Count;

            int lengthInBytes =
                paddingLength + 
                typeLength + 
                fullLength + 
                descriptionLength + 
                dimensionCount + 
                dimensionsLength;
            
            writer.Write(lengthInBytes);
            writer.Write(Padding);
            writer.Write(array.Type.ToCharArray());
            writer.Write("FULL".ToCharArray());
            writer.Write(array.Description.PadRight(70).ToCharArray());
            writer.Write(array.Dimensions.Count);
            foreach (int dim in array.Dimensions)
            {
                writer.Write(dim);
            }
            writer.Write(lengthInBytes);
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
                        writer.Write((byte)0x00);
                        writer.Write((byte)0x00);
                        writer.Write((byte)0x00);
                        writer.Write((byte)0x00);
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
                        writer.Write(array[item].SingleOrDefault().Value);
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