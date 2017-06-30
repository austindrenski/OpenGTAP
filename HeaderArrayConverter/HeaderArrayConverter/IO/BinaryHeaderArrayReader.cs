using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AD.IO;
using HeaderArrayConverter.Collections;
using HeaderArrayConverter.Extensions;
using HeaderArrayConverter.Types;
using JetBrains.Annotations;

namespace HeaderArrayConverter.IO
{
    /// <summary>
    /// Implements a <see cref="HeaderArrayReader"/> for reading Header Array (HAR) files in binary format.
    /// </summary>
    [PublicAPI]
    public sealed class BinaryHeaderArrayReader : HeaderArrayReader
    {
        /// <summary>
        /// The padding sequence used in binary HAR files.
        /// </summary>
        private static readonly int Padding = 0x20_20_20_20;

        /// <summary>
        /// Reads <see cref="IHeaderArray"/> collections from file..
        /// </summary>
        /// <param name="file">
        /// The file to read.
        /// </param>
        /// <return>
        /// A <see cref="HeaderArrayFile"/> representing the contents of the file.
        /// </return>
        public override HeaderArrayFile Read(FilePath file)
        {
            if (file is null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            return new HeaderArrayFile(ReadArrays(file));
        }

        /// <summary>
        /// Asynchronously reads <see cref="IHeaderArray"/> collections from file..
        /// </summary>
        /// <param name="file">
        /// The file to read.
        /// </param>
        /// <return>
        /// A task that upon completion returns a <see cref="HeaderArrayFile"/> representing the contents of the file.
        /// </return>
        public override async Task<HeaderArrayFile> ReadAsync(FilePath file)
        {
            if (file is null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            return new HeaderArrayFile(await Task.Run(() => ReadArraysAsync(file).AsParallel().Select(x => x.Result)));
        }

        /// <summary>
        /// Enumerates the <see cref="IHeaderArray"/> collection from file.
        /// </summary>
        /// <param name="file">
        /// The file from which to read arrays.
        /// </param>
        /// <returns>
        /// A <see cref="IHeaderArray"/> collection from the file.
        /// </returns>
        public override IEnumerable<IHeaderArray> ReadArrays(FilePath file)
        {
            if (file is null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            using (BinaryReader reader = new BinaryReader(File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                long length = reader.BaseStream.Length;
                while (reader.BaseStream.Position < length)
                {
                    yield return ReadNext(reader);
                }
            }
        }

        /// <summary>
        /// Asynchronously enumerates the arrays from file.
        /// </summary>
        /// <param name="file">
        /// The file from which to read arrays.
        /// </param>
        /// <returns>
        /// An enumerable collection of tasks that when completed return an <see cref="IHeaderArray"/> from file.
        /// </returns>
        public override IEnumerable<Task<IHeaderArray>> ReadArraysAsync(FilePath file)
        {
            if (file is null)
            {
                throw new ArgumentNullException(nameof(file));
            }
            
            using (BinaryReader reader = new BinaryReader(File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                long length = reader.BaseStream.Length;
                while (reader.BaseStream.Position < length)
                {
                    yield return Task.FromResult(ReadNext(reader));
                }
            }
        }

        /// <summary>
        /// Reads one header array from the <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader">
        /// The reader from which the next header array is read.
        /// </param>
        /// <returns>
        /// An <see cref="IHeaderArray"/>.
        /// </returns>
        [NotNull]
        private static IHeaderArray ReadNext([NotNull] BinaryReader reader)
        {
            string header = GetHeader(reader);

            (string description, bool sparse, HeaderArrayType type, IReadOnlyList<int> dimensions, int count) = GetMetadata(reader);

            switch (type)
            {
                case HeaderArrayType.C1:
                {
                    return Get1C(reader, header, description, type, dimensions);
                }
                case HeaderArrayType.RE:
                {
                    return GetRE(reader, header, description, type, dimensions, count, sparse);
                }
                case HeaderArrayType.I2:
                {
                    return Get_2(reader, header, description, type, dimensions, count, BitConverter.ToInt32);
                }
                case HeaderArrayType.R2:
                {
                    return Get_2(reader, header, description, type, dimensions, count, BitConverter.ToSingle);
                }
                default:
                {
                    throw new DataValidationException("Type", "1C, RE, 2I, 2R", type);
                }
            }
        }

        /// <summary>
        /// Composes an <see cref="IHeaderArray"/> from a 1C binary entry.
        /// </summary>
        [NotNull]
        private static IHeaderArray Get1C([NotNull] BinaryReader reader, [NotNull] string header, string description, HeaderArrayType type, [NotNull] IReadOnlyList<int> dimensions)
        {
            string[] strings = GetStringArray(reader);

            ImmutableArray<KeyValuePair<string, IImmutableList<string>>> sets =
                new KeyValuePair<string, IImmutableList<string>>[]
                {
                    new KeyValuePair<string, IImmutableList<string>>(
                        "INDEX",
                        Enumerable.Range(0, strings.Length).Select(x => x.ToString()).ToImmutableArray())
                }.ToImmutableArray();

            KeyValuePair<KeySequence<string>, string>[] items = new KeyValuePair<KeySequence<string>, string>[strings.Length];

            for (int i = 0; i < items.Length; i++)
            {
                items[i] = new KeyValuePair<KeySequence<string>, string>(i.ToString(), strings[i]);
            }

            return new HeaderArray<string>(header, header, description, type, items, dimensions, sets);
        }

        /// <summary>
        /// Composes an <see cref="IHeaderArray"/> from an RE binary entry.
        /// </summary>
        [NotNull]
        private static IHeaderArray GetRE([NotNull] BinaryReader reader, [NotNull] string header, string description, HeaderArrayType type, [NotNull] IReadOnlyList<int> dimensions, int count, bool sparse)
        {
            (string coefficient, IReadOnlyList<KeyValuePair<string, IImmutableList<string>>> sets) = ReadSets(reader);

            float[] floats = sparse ? GetSparseRealArrayWithSets(reader, count) : GetFullRealArrayWithSets(reader, BitConverter.ToSingle);

            KeySequence<string>[] expandedSets = sets.AsExpandedSet().ToArray();

            KeyValuePair<KeySequence<string>, float>[] items = new KeyValuePair<KeySequence<string>, float>[floats.Length];

            for (int i = 0; i < items.Length; i++)
            {
                items[i] = new KeyValuePair<KeySequence<string>, float>(expandedSets[i], floats[i]);
            }

            return new HeaderArray<float>(header, coefficient, description, type, items, dimensions, sets.ToImmutableArray());
        }

        /// <summary>
        /// Composes an <see cref="IHeaderArray"/> from a 2I or 2R binary entry.
        /// </summary>
        [NotNull]
        private static IHeaderArray Get_2<TValue>([NotNull] BinaryReader reader, [NotNull] string header, string description, HeaderArrayType type, [NotNull] IReadOnlyList<int> dimensions, int count, [NotNull] Func<byte[], int, TValue> converter) where TValue : IEquatable<TValue>
        {
            TValue[] values = GetFullNumericArray(reader, converter, count);

            ImmutableArray<KeyValuePair<string, IImmutableList<string>>> sets =
                ImmutableArray.Create(new KeyValuePair<string, IImmutableList<string>>("INDEX", Enumerable.Range(0, values.Length).Select(x => x.ToString()).ToImmutableArray()));

            KeyValuePair<KeySequence<string>, TValue>[] items =
                new KeyValuePair<KeySequence<string>, TValue>[values.Length];

            for (int i = 0; i < items.Length; i++)
            {
                items[i] = new KeyValuePair<KeySequence<string>, TValue>(i.ToString(), values[i]);
            }

            return new HeaderArray<TValue>(header, header, description, type, items, dimensions, sets);
        }

        private static (string Coefficient, IReadOnlyList<KeyValuePair<string, IImmutableList<string>>> Sets) ReadSets([NotNull] BinaryReader reader)
        {
            byte[] setsArray = InitializeArray(reader);

            int numberOfDistinctSets = BitConverter.ToInt32(setsArray, 0 * sizeof(int));

            bool knownSets = BitConverter.ToBoolean(setsArray, 1 * sizeof(int));

            if (!knownSets && numberOfDistinctSets != 0)
            {
                throw new DataValidationException("Binary boolean check", true, false);
            }

            int numberOfSets = BitConverter.ToInt32(setsArray, 2 * sizeof(int));

            string coefficient = Encoding.ASCII.GetString(setsArray, 3 * sizeof(int), 12).Trim();

            bool check = BitConverter.ToBoolean(setsArray, 3 * sizeof(int) + 12);

            if (!check && numberOfDistinctSets != 0)
            {
                throw new DataValidationException("Binary boolean check", true, false);
            }

            // Read set names
            string[] setNames = new string[numberOfSets];
            for (int i = 0; i < setNames.Length; i++)
            {
                setNames[i] = Encoding.ASCII.GetString(setsArray, 4 * sizeof(int) + 12 + i * 12, 12).Trim();
            }

            string[][] labelStrings = new string[setNames.Length][];
            for (int h = 0; h < numberOfDistinctSets; h++)
            {
                byte[] labels = InitializeArray(reader);
                // get label dimensions
                int labelX0 = BitConverter.ToInt32(labels, 0 * sizeof(int));
                int labelX1 = BitConverter.ToInt32(labels, 1 * sizeof(int));
                int labelX2 = BitConverter.ToInt32(labels, 2 * sizeof(int));


                labelStrings[h] = new string[labelX1];
                for (int i = 0; i < labelX2; i++)
                {
                    labelStrings[h][i] = Encoding.ASCII.GetString(labels, 3 * sizeof(int) + i * 3 * sizeof(int), 3 * sizeof(int)).Trim();
                }
            }

            if (numberOfDistinctSets > 0 && numberOfSets > 0 && numberOfSets - numberOfDistinctSets == 1)
            {
                labelStrings[labelStrings.Length - 1] = labelStrings[labelStrings.Length - 2];
            }

            KeyValuePair<string, IImmutableList<string>>[] sets = new KeyValuePair<string, IImmutableList<string>>[setNames.Length];
            for (int i = 0; i < setNames.Length; i++)
            {
                sets[i] = new KeyValuePair<string, IImmutableList<string>>(setNames[i], labelStrings[i].ToImmutableArray());
            }
            if (setNames.Length == 0)
            {
                sets = new KeyValuePair<string, IImmutableList<string>>[]
                {
                    new KeyValuePair<string, IImmutableList<string>>(coefficient, ImmutableArray.Create(coefficient))
                };
            }

            return (coefficient, sets);
        }

        ///  <summary>
        ///  Calculates the next array of data for an array stored as non-sparse and with sets.
        ///  </summary>
        ///  <param name="reader">
        ///  The reader from which the data is read.
        ///  </param>
        ///  <param name="converter">
        ///  A delegate returning a value from an index inside a byte array.
        ///  </param>
        /// <returns>
        ///  An array of data.
        ///  </returns>
        ///  <remarks>
        ///  The array is composed of one or more:
        ///  
        ///      [extents] = <see cref="ReadExtents(BinaryReader)"/>.
        ///      [segment] = <see cref="GetNextRESegment{T}(BinaryReader, Func{byte[], int, T}, out T[])"/>.
        /// 
        ///  </remarks>
        [NotNull]
        private static TValue[] GetFullRealArrayWithSets<TValue>([NotNull] BinaryReader reader, [NotNull] Func<byte[], int, TValue> converter)
        {
            (int index, int count) = ReadDimensions(reader);

            TValue[] results = new TValue[count];

            while (index > 1)
            {
                (int _, IReadOnlyList<int> _, int offset) = ReadExtents(reader);

                index = GetNextRESegment(reader, converter, out TValue[] floats);

                Array.Copy(floats, 0, results, offset, floats.Length);
            }

            return results;
        }

        [NotNull]
        private static float[] GetSparseRealArrayWithSets(BinaryReader reader, int count)
        {
            byte[] meta = InitializeArray(reader);

            int valueCount = BitConverter.ToInt32(meta, 0 * sizeof(int));
            int idk0 = BitConverter.ToInt32(meta, 1 * sizeof(int));
            int idk1 = BitConverter.ToInt32(meta, 2 * sizeof(int));

            byte[] data = InitializeArray(reader);
            int numberOfVectors = BitConverter.ToInt32(data, 0 * sizeof(int));
            int totalCountOfEntries = BitConverter.ToInt32(data, 1 * sizeof(int));
            int maxEntriesPerVector = BitConverter.ToInt32(data, 2 * sizeof(int));

            int[] indices = new int[totalCountOfEntries];
            float[] floats = new float[count];

            const int offset = 12;

            for (int i = 0; i < numberOfVectors; i++)
            {
                if (i > 0)
                {
                    data = InitializeArray(reader);
                }
                int length = i + 1 == numberOfVectors ? totalCountOfEntries - i * maxEntriesPerVector : maxEntriesPerVector;
                for (int j = 0; j < length; j++)
                {
                    indices[i * maxEntriesPerVector + j] = BitConverter.ToInt32(data, offset + j * sizeof(int)) - 1;
                }
                for (int j = 0; j < length; j++)
                {
                    floats[indices[i * maxEntriesPerVector + j]] = BitConverter.ToSingle(data, offset + length * sizeof(int) + j * sizeof(int));
                }
            }

            return floats;
        }

        ///  <summary>
        ///  Calculates the next array of data for an array stored as non-sparse and with sets.
        ///  </summary>
        ///  <param name="reader">
        ///  The reader from which the data is read.
        ///  </param>
        ///  <param name="converter">
        ///  A delegate returning a value from an index inside a byte array.
        ///  </param>
        /// <param name="count">
        /// The number of items in the segment.
        /// </param>
        /// <returns>
        ///  An array of data.
        ///  </returns>
        ///  <remarks>
        ///  The array is composed of one or more:
        ///  
        ///      [segment] = <see cref="GetNextRESegment{T}(BinaryReader, Func{byte[], int, T}, out T[])"/>.
        /// 
        ///  </remarks>
        [NotNull]
        private static TValue[] GetFullNumericArray<TValue>([NotNull] BinaryReader reader, [NotNull] Func<byte[], int, TValue> converter, int count)
        {
            TValue[] results = new TValue[count];
            int index = int.MaxValue;
            while (index > 1)
            {
                int offset;

                (index, offset) = GetNext2_Segment(reader, converter, out TValue[] floats);

                Array.Copy(floats, 0, results, offset, floats.Length);
            }

            return results;
        }

        /// <summary>
        /// Calculates the next string array.
        /// </summary>
        /// <param name="reader">
        /// The reader from which the string array is read.
        /// </param>
        /// <returns>
        /// An array of string values.
        /// </returns>
        /// <remarks>
        /// 
        /// </remarks>
        [NotNull]
        private static string[] GetStringArray([NotNull] BinaryReader reader)
        {
            byte[] data = InitializeArray(reader);

            int vectorIndex = BitConverter.ToInt32(data, 0 * sizeof(int));
            int extent0 = BitConverter.ToInt32(data, 1 * sizeof(int));
            int extent1 = BitConverter.ToInt32(data, 2 * sizeof(int));

            int elementSize = (data.Length - 3 * sizeof(int)) / extent1;

            string[] strings = new string[extent0];

            for (int i = 0; i < vectorIndex; i++)
            {
                if (i > 0)
                {
                    data = InitializeArray(reader);
                }
                for (int j = 0; j < extent1; j++)
                {
                    int item = i * extent1 + j;
                    if (extent0 <= item)
                    {
                        break;
                    }
                    strings[item] = Encoding.ASCII.GetString(data, 3 * sizeof(int) + j * elementSize, elementSize).Trim();
                }
            }

            return strings;
        }

        /// <summary>
        /// Reads the next array from the <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader">
        /// The <see cref="BinaryReader"/> from which the array is read.
        /// </param>
        /// <returns>
        /// The next array from the <see cref="BinaryReader"/> positioned after the initial padding (e.g. <see cref="Padding"/>).
        /// </returns>
        [NotNull]
        private static byte[] InitializeArray(BinaryReader reader)
        {
            int length = reader.ReadInt32();
            int padding = reader.ReadInt32();

            byte[] data = reader.ReadBytes(length - sizeof(int));

            int closingLength = reader.ReadInt32();

            if (length != closingLength)
            {
                throw new DataValidationException("Binary length check", length, closingLength);
            }

            if (Padding != padding)
            {
                throw new DataValidationException("Binary padding check", Padding, padding);
            }

            return data;
        }

        /// <summary>
        /// Reads a character string that is pre- and post-fixed by 32-bit integers of its length.
        /// </summary>
        /// <param name="reader">
        /// The reader from which the header is returned.
        /// </param>
        /// <returns>
        /// A character string uniquely naming the <see cref="IHeaderArray"/>.
        /// </returns>
        [NotNull]
        private static string GetHeader([NotNull] BinaryReader reader)
        {
            int length = reader.ReadInt32();

            string header = Encoding.ASCII.GetString(reader.ReadBytes(length));

            int closingLength = reader.ReadInt32();

            if (length != closingLength)
            {
                throw new DataValidationException("Binary length check", length, closingLength);
            }

            return header;
        }

        /// <summary>
        /// Reads the metadata entry and returns a long-name description, whether or not the full matrix is stored, the data type, and the dimensions.
        /// </summary>
        /// <param name="reader">
        /// The reader from which the metadata is returned.
        /// </param>
        /// <returns>
        /// A long-name description, a sparse indicator, the data type, the dimensions, and the count of values.
        /// </returns>
        private static (string Description, bool Sparse, HeaderArrayType Type, IReadOnlyList<int> Dimensions, int Count) GetMetadata([NotNull] BinaryReader reader)
        {
            byte[] descriptionBuffer = InitializeArray(reader);

            HeaderArrayType type = (HeaderArrayType)BitConverter.ToInt16(descriptionBuffer, 0 * sizeof(short));

            bool sparse = Encoding.ASCII.GetString(descriptionBuffer, sizeof(short), 4) != "FULL";

            string description = Encoding.ASCII.GetString(descriptionBuffer, sizeof(short) + 4, 70).Trim('\u0000', '\u0002', '\u0020');

            int[] dimensions = new int[BitConverter.ToInt32(descriptionBuffer, sizeof(short) + 4 + 70)];

            int count = 1;
            for (int i = 0; i < dimensions.Length; i++)
            {
                dimensions[i] = BitConverter.ToInt32(descriptionBuffer, sizeof(short) + 4 + 70 + sizeof(int) + i * sizeof(int));
                count *= dimensions[i];
            }
            
            return (description, sparse, type, dimensions, count);
        }

        /// <summary>
        /// Calculates the count of elements in the record based on the dimensions.
        /// </summary>
        /// <param name="reader">
        /// The reader from which the dimension definitions are read.
        /// </param>
        /// <returns>
        /// An integer array indicating the count of elements in each dimension of the record.
        /// </returns>
        /// <remarks>
        /// The dimension array contains the following:
        /// 
        ///     [0 * sizeof(int)] = 1-based index of the current vector from the end of the record.
        ///     [1 * sizeof(int)] = the count of dimensions for the record.
        ///     [(2 + i) * sizeof(int)] = the count of elements in the 0-based i-th dimension of the record. 
        /// </remarks>
        private static (int Index, int Count) ReadDimensions([NotNull] BinaryReader reader)
        {
            byte[] dimensionArray = InitializeArray(reader);

            int index = BitConverter.ToInt32(dimensionArray, 0 * sizeof(int));

            int dimensions = BitConverter.ToInt32(dimensionArray, 1 * sizeof(int));

            int count = 1;
            for (int i = 0; i < dimensions; i++)
            {
                count *= BitConverter.ToInt32(dimensionArray, (2 + i) * sizeof(int));
            }

            return (index, count);
        }

        /// <summary>
        /// Calculates the extents of each dimension represented on the next vector.
        /// </summary>
        /// <param name="reader">
        /// The reader from which the extent definitions are read.
        /// </param>
        /// <returns>
        /// An integer array indicating the extents of each dimension represented on the next vector.
        /// </returns>
        /// <remarks>
        ///  The extents array contains the following:
        ///
        ///     [0 * sizeof(int)] = 1-based index of the current vector from the end of the record.
        ///     [(1 + i) * sizeof(int)] = the 0-based index indicating the first entry in the i-th set represented in this vector.
        ///     [(2 + i) * sizeof(int)] = the 0-based index indicating the last entry in the i-th set represented in this vector.
        /// 
        /// The count of dimensions can be determined in-line as the length of the extents array divided by the byte-size of
        /// an integer. The array length is actually the count of the dimensions + 1, but integer division rounds down.
        /// 
        /// Note that the extents must be deincremented by one to move from a 1-based index to a 0-based index. 
        /// </remarks>
        private static (int Index, IReadOnlyList<int> Extents, int Offset) ReadExtents([NotNull] BinaryReader reader)
        {
            byte[] extentsArray = InitializeArray(reader);

            int[] extents = new int[(extentsArray.Length - sizeof(int)) / 2 / sizeof(int)];

            int index = BitConverter.ToInt32(extentsArray, 0 * sizeof(int));

            int offset = 1;
            for (int i = 0; i < extents.Length; i++)
            {
                int start = BitConverter.ToInt32(extentsArray, (1 + i) * sizeof(int)) - 1;
                extents[i] = BitConverter.ToInt32(extentsArray, (2 + i) * sizeof(int)) - start;
                offset *= start;
            }

            return (index, extents, offset);
        }

        ///  <summary>
        ///  Calculates the next value segment that starts with a 32-bit integer representing the segment index and values beginning at the specified offset.
        ///  </summary>
        ///  <typeparam name="TValue">
        ///  The type of data in the array.
        ///  </typeparam>
        ///  <param name="reader">
        ///  The reader from which the segment is read.
        ///  </param>
        ///  <param name="converter">
        ///  A delegate returning a value from an index inside a byte array.
        ///  </param>
        /// <param name="results">
        ///  The values of the array segment.
        ///  </param>
        ///  <returns>
        ///  The index of this segment.
        ///  </returns>
        ///  <remarks>
        ///  For seven dimensional arrays, the next segment is composed of:
        /// 
        ///      [0 * sizeof(int)] = 1-based index of the current vector from the end of the record.
        ///      [(1 + i) * sizeof(int)] = the i-th value of the segment. 
        /// 
        ///  </remarks>
        private static int GetNextRESegment<TValue>([NotNull] BinaryReader reader, [NotNull] Func<byte[], int, TValue> converter, [NotNull] out TValue[] results)
        {
            byte[] data = InitializeArray(reader);

            int vectorIndex = BitConverter.ToInt32(data, 0 * sizeof(int));

            results = new TValue[(data.Length - 1 * sizeof(int)) / sizeof(int)];

            for (int i = 0; i < results.Length; i++)
            {
                results[i] = converter(data, (i + 1) * sizeof(int));
            }

            return vectorIndex;
        }

        ///  <summary>
        ///  Calculates the next value segment that starts with a 32-bit integer representing the segment index and values beginning at the specified offset.
        ///  </summary>
        ///  <typeparam name="TValue">
        ///  The type of data in the array.
        ///  </typeparam>
        ///  <param name="reader">
        ///  The reader from which the segment is read.
        ///  </param>
        ///  <param name="converter">
        ///  A delegate returning a value from an index inside a byte array.
        ///  </param>
        /// <param name="results">
        ///  The values of the array segment.
        ///  </param>
        ///  <returns>
        ///  The index of this segment.
        ///  </returns>
        ///  <remarks>
        ///  For two dimensional arrays, the next segment is composed of:
        /// 
        ///       [0 * sizeof(int)] = vector index.
        ///       [1 * sizeof(int)] = the number of elements in the first dimension.
        ///       [2 * sizeof(int)] = the number of elements in the second dimension.
        ///       [3 * sizeof(int)] = the 1-based starting index of the first dimension in the segment on the logical array.
        ///       [4 * sizeof(int)] = the 1-based ending index of the first dimension in the segment on the logical array.
        ///       [5 * sizeof(int)] = the 1-based starting index of the second dimension in the segment on the logical array.
        ///       [6 * sizeof(int)] = the 1-based ending index of the second dimension in the segment on the logical array.
        ///       [(7 + i) * sizeof(int)] = the i-th data value in the segment. 
        /// 
        ///  </remarks>
        private static (int Index, int Offset) GetNext2_Segment<TValue>([NotNull] BinaryReader reader, [NotNull] Func<byte[], int, TValue> converter, [NotNull] out TValue[] results)
        {
            byte[] data = InitializeArray(reader);

            int index = BitConverter.ToInt32(data, 0 * sizeof(int));

            int offset = BitConverter.ToInt32(data, 3 * sizeof(int)) * BitConverter.ToInt32(data, 5 * sizeof(int)) - 1;

            results = new TValue[(data.Length - 7 * sizeof(int)) / sizeof(int)];

            for (int i = 0; i < results.Length; i++)
            {
                results[i] = converter(data, (i + 7) * sizeof(int));
            }

            return (index, offset);
        }
    }
}