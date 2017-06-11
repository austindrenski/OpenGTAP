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

        private static async Task WriteArrayAsync([NotNull] BinaryWriter writer, [NotNull] IHeaderArray array)
        {
            WriteHeader(writer, array);
            WriteMetadata(writer, array);
            switch (array.Type)
            {
                case "1C":
                {
                    Write1CArrayValues(writer, array.As<string>());
                    break;
                }
                case "RE":
                {
                    WriteSets(writer, array);
                    WriteDimensions(writer, array);
                    WriteExtents(writer, array);
                    WriteReArrayValues(writer, array.As<float>());
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
        /// Writes the dimensions, names, and entries of <see cref="IHeaderArray.Sets"/>.
        /// </summary>
        /// <param name="writer">
        /// The <see cref="BinaryWriter"/> positioned after the call to <see cref="WriteMetadata(BinaryWriter, IHeaderArray)"/>.
        /// </param>
        /// <param name="array">
        /// The <see cref="IHeaderArray"/> to write.
        /// </param>
        private static void WriteSets([NotNull] BinaryWriter writer, [NotNull] IHeaderArray array)
        {
            const int paddinglength = 4;
            const int distinctSetsLength = 4;
            const int spacerLength = 4;
            const int totalSetsLength = 4;
            const int coefficientLength = 12;
            int setNamesCount = 12 * array.Sets.Count;
            int x6BCount = array.Sets.Count;
            int x00Count = 4 * (array.Sets.Count + 1);

            int lengthInBytes = 
                paddinglength + 
                distinctSetsLength + 
                spacerLength + 
                totalSetsLength + 
                coefficientLength + 
                spacerLength + 
                setNamesCount + 
                x6BCount +
                x00Count;

            writer.Write(lengthInBytes);
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
            writer.Write(lengthInBytes);

            HashSet<string> setsUsed = new HashSet<string>();
            foreach (KeyValuePair<string, IImmutableList<string>> set in array.Sets)
            {
                if (!setsUsed.Add(set.Key))
                {
                    continue;
                }
                int setSize = 4 * 4 + 12 * set.Value.Count;

                writer.Write(setSize);
                writer.Write(Padding);
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

        /// <summary>
        /// Writes the <see cref="IHeaderArray.Dimensions"/>.
        /// </summary>
        /// <param name="writer">
        /// The <see cref="BinaryWriter"/> positioned after the call to <see cref="WriteSets(BinaryWriter, IHeaderArray)"/>.
        /// </param>
        /// <param name="array">
        /// The <see cref="IHeaderArray"/> to write.
        /// </param>
        private static void WriteDimensions([NotNull] BinaryWriter writer, [NotNull] IHeaderArray array)
        {
            int dimensionSize = 4 * (3 + array.Dimensions.Count);
            writer.Write(dimensionSize);
            writer.Write(Padding);
            writer.Write(3);
            writer.Write(array.Dimensions.Count);
            foreach (int dimension in array.Dimensions)
            {
                writer.Write(dimension);
            }
            writer.Write(dimensionSize);
        }

        /// <summary>
        /// Writes the extent array that describes the positions in the logical array that the next array represents.
        /// </summary>
        /// <param name="writer">
        /// The <see cref="BinaryWriter"/> positioned after the call to <see cref="WriteDimensions(BinaryWriter, IHeaderArray)"/>.
        /// </param>
        /// <param name="array">
        /// The <see cref="IHeaderArray"/> to write.
        /// </param>
        private static void WriteExtents([NotNull] BinaryWriter writer, [NotNull] IHeaderArray array)
        {
            int extentSize = 4 * (2 + 2 * array.Dimensions.Count);
            writer.Write(extentSize);
            writer.Write(Padding);
            writer.Write(2);
            foreach (int dimension in array.Dimensions)
            {
                writer.Write(1);
                writer.Write(dimension);
            }
            writer.Write(extentSize);
        }

        /// <summary>
        /// Writes the contents of an <see cref="IHeaderArray{Single}"/> with type 'RE'.
        /// </summary>
        /// <param name="writer">
        /// The <see cref="BinaryWriter"/> positioned after the call to <see cref="WriteExtents(BinaryWriter, IHeaderArray)"/>.
        /// </param>
        /// <param name="array">
        /// The <see cref="IHeaderArray"/> to write.
        /// </param>
        private static void WriteReArrayValues([NotNull] BinaryWriter writer, [NotNull] IHeaderArray<float> array)
        {
            int size = 4 * (2 + array.Total);
            writer.Write(size);
            writer.Write(Padding);
            writer.Write(1);
            foreach (KeySequence<string> item in array.Sets.AsExpandedSet())
            {
                writer.Write(array[item].SingleOrDefault().Value);
            }
            writer.Write(size);
        }

        /// <summary>
        /// Writes the contents of an <see cref="IHeaderArray{String}"/> with type '1C'.
        /// </summary>
        /// <param name="writer">
        /// The <see cref="BinaryWriter"/> positioned after the call to <see cref="WriteMetadata(BinaryWriter, IHeaderArray)"/>.
        /// </param>
        /// <param name="array">
        /// The <see cref="IHeaderArray"/> to write.
        /// </param>
        private static void Write1CArrayValues([NotNull] BinaryWriter writer, [NotNull] IHeaderArray<string> array)
        {
            int recordLength = array.Dimensions.Last();
            int size = 4 * 4 +  recordLength * array.Total;
            writer.Write(size);
            writer.Write(Padding);
            writer.Write(1);
            writer.Write(array.Total);
            writer.Write(array.Total);
            foreach (string item in array.Select(x => x.Value))
            {
                writer.Write(item.PadRight(recordLength).ToCharArray());
            }
            writer.Write(size);
        }
    }
}