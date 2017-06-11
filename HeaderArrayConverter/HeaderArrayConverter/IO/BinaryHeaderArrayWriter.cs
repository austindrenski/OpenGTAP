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
                foreach (IHeaderArray array in source.Where(x => x.Type != "RL"))
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
            switch (array.Type)
            {
                case "1C":
                {
                    Write1CArray(array.Dimensions.Last());
                    break;
                }
                case "RE":
                {
                    WriteSets();
                    WriteDimensions();
                    WriteExtents();
                    WriteReArrayValues();
                    break;
                }
                default:
                {
                    throw new NotSupportedException();
                }
            }
            await Task.CompletedTask;

            void Write1CArray(int recordLength)
            {
                IHeaderArray<string> typedArray = array.As<string>();
                int size = 4 * 4 + recordLength * typedArray.Total;
                writer.Write(size);
                writer.Write(padding);
                writer.Write(1);
                writer.Write(typedArray.Total);
                writer.Write(typedArray.Total);
                foreach (string item in typedArray.Select(x => x.Value))
                {
                    writer.Write(item.PadRight(recordLength).ToCharArray());
                }
                writer.Write(size);
            }

            void WriteReArrayValues()
            {
                IHeaderArray<float> typedArray = array.As<float>();
                int size = 4 * (2 + typedArray.Total);
                writer.Write(size);
                writer.Write(padding);
                writer.Write(1);
                foreach (KeySequence<string> item in typedArray.Sets.AsExpandedSet())
                {
                    writer.Write(typedArray[item].SingleOrDefault().Value);
                }
                writer.Write(size);
            }

            // Writes the array header.
            void WriteHeader()
            {
                writer.Write(array.Header.Length);
                writer.Write(array.Header.ToCharArray());
                writer.Write(array.Header.Length);
            }

            // Writes the array metadata, including type, full/sparse, description (70-byte padded), and the length-prefixed dimmensions.
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
            
            // Writes the set dimensions, the set names, and then the set entries.
            void WriteSets()
            {
                int setLabelSize = 4 * (1 + 1 + 1 + 1 + 1) + 12 + 12 * array.Sets.Select(x => x.Key).Count() + array.Sets.Count + 4 * (array.Sets.Count + 1);
                writer.Write(setLabelSize);
                writer.Write(padding);
                writer.Write(array.Sets.Select(x => x.Key).Distinct().Count());
                writer.Write(0xFF_FF_FF_FF);
                writer.Write(array.Sets.Select(x => x.Key).Count());
                writer.Write(array.Header.PadRight(12).ToCharArray());
                writer.Write(0xFF_FF_FF_FF);
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
                writer.Write(setLabelSize);

                HashSet<string> setsUsed = new HashSet<string>();
                foreach (KeyValuePair<string, IImmutableList<string>> set in array.Sets)
                {
                    if (!setsUsed.Add(set.Key))
                    {
                        continue;
                    }
                    int setSize = 4 * 4 + 12 * set.Value.Count;

                    writer.Write(setSize);
                    writer.Write(padding);
                    writer.Write(1);
                    writer.Write(set.Value.Count);
                    writer.Write(set.Value.Count);
                    foreach (string value in set.Value)
                    {
                        writer.Write(value.PadRight(12).ToCharArray());
                    }
                    writer.Write(setSize);
                }
            }

            // Writes the dimensions array.
            void WriteDimensions()
            {
                int dimensionSize = 4 * (3 + array.Dimensions.Count);
                writer.Write(dimensionSize);
                writer.Write(padding);
                writer.Write(3);
                writer.Write(array.Dimensions.Count);
                foreach (int dimension in array.Dimensions)
                {
                    writer.Write(dimension);
                }
                writer.Write(dimensionSize);
            }

            // Writes the extent array that describes the positions in the logical array that the next array represents.
            void WriteExtents()
            {
                int extentSize = 4 * (2 + 2 * array.Dimensions.Count);
                writer.Write(extentSize);
                writer.Write(padding);
                writer.Write(2);
                foreach (int dimension in array.Dimensions)
                {
                    writer.Write(1);
                    writer.Write(dimension);
                }
                writer.Write(extentSize);
            }
        }
    }
}