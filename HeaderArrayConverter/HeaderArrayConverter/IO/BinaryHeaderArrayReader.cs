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
        /// The spacer sequence used in binary HAR files.
        /// </summary>
        private static readonly int Spacer = unchecked((int) 0xFF_FF_FF_FF);

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

            return ReadAsync(file).Result;
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

            return new HeaderArrayFile(await Task.WhenAll(ReadArraysAsync(file)));
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

            return Task.WhenAll(ReadArraysAsync(file)).Result;
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

            using (BinaryReader reader = new BinaryReader(File.Open(file, FileMode.Open)))
            {
                long length = reader.BaseStream.Length;
                while (reader.BaseStream.Position < length)
                {
                    yield return Task.FromResult(ReadNext(reader));
                }
            }
        }

        /// <summary>
        /// Reads one entry from a Header Array (HAR) file.
        /// </summary>
        [NotNull]
        private static IHeaderArray ReadNext(BinaryReader reader)
        {
            (string description, string header, bool sparse, HeaderArrayType type, IReadOnlyList<int> dimensions) = GetDescription(reader);

            int count = 1;
            for (int i = 0; i < dimensions.Count; i++)
            {
                count *= dimensions[i];
            }

            switch (type)
            {
                case HeaderArrayType.C1:
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
                case HeaderArrayType.RE:
                {
                    (string coefficient, float[] floats, IReadOnlyList<KeyValuePair<string, IImmutableList<string>>> sets) = GetRealArrayWithSetLabels(reader, sparse);

                    KeySequence<string>[] expandedSets = sets.AsExpandedSet().ToArray();
                    
                    KeyValuePair<KeySequence<string>, float>[] items =
                            new KeyValuePair<KeySequence<string>, float>[floats.Length];

                    for (int i = 0; i < items.Length; i++)
                    {
                        items[i] = new KeyValuePair<KeySequence<string>, float>(expandedSets[i], floats[i]);
                    }

                    return new HeaderArray<float>(header, coefficient, description, type, items, dimensions, sets.ToImmutableArray());
                }
                case HeaderArrayType.I2:
                {
                    int[] ints = GetTwoDimensionalNumericArray(reader, BitConverter.ToInt32, count);

                    ImmutableArray<KeyValuePair<string, IImmutableList<string>>> sets =
                        ImmutableArray.Create(
                            new KeyValuePair<string, IImmutableList<string>>(
                                "INDEX",
                                Enumerable.Range(0, ints.Length).Select(x => x.ToString()).ToImmutableArray()));

                    KeyValuePair<KeySequence<string>, int>[] items =
                            new KeyValuePair<KeySequence<string>, int>[ints.Length];

                    for (int i = 0; i < items.Length; i++)
                    {
                        items[i] = new KeyValuePair<KeySequence<string>, int>(i.ToString(), ints[i]);
                    }
                    
                    return new HeaderArray<int>(header, header, description, type, items, dimensions, sets);
                }
                case HeaderArrayType.R2:
                {
                    float[] floats = GetTwoDimensionalNumericArray(reader, BitConverter.ToSingle, count);

                    ImmutableArray<KeyValuePair<string, IImmutableList<string>>> sets =
                        ImmutableArray.Create(
                            new KeyValuePair<string, IImmutableList<string>>(
                                "INDEX",
                                Enumerable.Range(0, floats.Length).Select(x => x.ToString()).ToImmutableArray()));

                    KeyValuePair<KeySequence<string>, float>[] items = 
                            new KeyValuePair<KeySequence<string>, float>[floats.Length];

                    for (int i = 0; i < items.Length; i++)
                    {
                        items[i] = new KeyValuePair<KeySequence<string>, float>(i.ToString(), floats[i]);
                    }

                    return new HeaderArray<float>(header, header, description, type, items, dimensions, sets);
                }
                default:
                {
                    throw new DataValidationException("Type", "1C, RE, 2I, 2R", type);
                }
            }
        }

        /// <summary>
        /// Reads the next array from the <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader">
        /// The <see cref="BinaryReader"/> from which the array is read.
        /// </param>
        /// <returns>
        /// The next array from the <see cref="BinaryReader"/> starting after the initial padding (e.g. 0x20_20_20_20).
        /// </returns>
        [NotNull]
        private static byte[] InitializeArray(BinaryReader reader)
        {
            // Read array length
            int length = reader.ReadInt32();

            // Check padding
            int padding = reader.ReadInt32();

            // Read array
            byte[] data = reader.ReadBytes(length - sizeof(int));

            // Check closing
            int closingLength = reader.ReadInt32();

            // Verify array length
            if (length != closingLength)
            {
                throw new DataValidationException("Binary length check", length, closingLength);
            }

            // Verify the padding
            if (Padding != padding)
            {
                throw new DataValidationException("Binary padding check", Padding, padding);
            }

            // Skip padding and return
            return data;
        }

        private static (string Description, string Header, bool Sparse, HeaderArrayType Type, IReadOnlyList<int> Dimensions) GetDescription(BinaryReader reader)
        {
            // Read length of the header
            int length = reader.ReadInt32();
            
            // Read header
            string header = Encoding.ASCII.GetString(reader.ReadBytes(length));

            int closingLength = reader.ReadInt32();

            // Verify the length of the header
            if (length != closingLength)
            {
                throw new DataValidationException("Binary length check", length, closingLength);
            }

            byte[] descriptionBuffer = InitializeArray(reader);

            // Read type => '1C', 'RE', etc
            HeaderArrayType type = (HeaderArrayType) BitConverter.ToInt16(descriptionBuffer, 0 * sizeof(short));

            // Read length type => 'FULL'
            bool sparse = Encoding.ASCII.GetString(descriptionBuffer, 2, 4) != "FULL";

            // Read longer name description with limit of 70 characters
            string description = Encoding.ASCII.GetString(descriptionBuffer, 6, 70).Trim('\u0000', '\u0002', '\u0020');

            int[] dimensions = new int[BitConverter.ToInt32(descriptionBuffer, 76)];

            for (int i = 0; i < dimensions.Length; i++)
            {
                dimensions[i] = BitConverter.ToInt32(descriptionBuffer, 80 + i * sizeof(int));
            }

            return (description, header, sparse, type, dimensions);
        }

        private static (string Coefficient, float[] Data, IReadOnlyList<KeyValuePair<string, IImmutableList<string>>> Sets) GetRealArrayWithSetLabels(BinaryReader reader, bool sparse)
        {
            (string coefficient, string[] setNames, string[][] labelStrings) = ReadSets(reader);

            KeyValuePair<string, IImmutableList<string>>[] sets = new KeyValuePair<string, IImmutableList<string>>[setNames.Length];

            int dimensions = 1;
            for (int i = 0; i < setNames.Length; i++)
            {
                sets[i] = new KeyValuePair<string, IImmutableList<string>>(setNames[i], labelStrings[i].ToImmutableArray());
                dimensions *= labelStrings[i].Length;
            }

            if (setNames.Length == 0)
            {
                sets = new KeyValuePair<string, IImmutableList<string>>[]
                {
                    new KeyValuePair<string, IImmutableList<string>>(coefficient, ImmutableArray.Create(coefficient))
                };
            }

            float[] data = sparse ? GetReSparseArray(reader, dimensions) : GetRealArrayWithSetLabelsFull(reader);

            return (coefficient, data, sets);
        }

        private static (string Coefficient, string[] SetNames, string[][] LabelStrings) ReadSets([NotNull] BinaryReader reader)
        {
            // read dimension array
            byte[] setDimensions = InitializeArray(reader);

            // number of labels?
            int a = BitConverter.ToInt32(setDimensions, 0 * sizeof(int));

            int possibleSpacer0 = BitConverter.ToInt32(setDimensions, 1 * sizeof(int));

            if (Spacer != possibleSpacer0)
            {
                if (a != 0 && BitConverter.ToInt32(setDimensions, 1 * sizeof(int)) != 1)
                {
                    throw new DataValidationException("Binary spacer check", Spacer, possibleSpacer0);
                }
            }

            const int offset = 12;

            // number of labels...again?
            int c = BitConverter.ToInt32(setDimensions, 2 * sizeof(int));

            // Read coefficient
            string coefficient = Encoding.ASCII.GetString(setDimensions, offset, offset).Trim();

            int possibleSpacer1 = BitConverter.ToInt32(setDimensions, 6 * sizeof(int));

            if (Spacer != possibleSpacer1)
            {
                if (a != 0 && BitConverter.ToInt32(setDimensions, 1 * sizeof(int)) != 1)
                {
                    throw new DataValidationException("Binary spacer check", Spacer, possibleSpacer1);
                }
            }

            // Read set names
            string[] setNames = new string[a];
            for (int i = 0; i < a; i++)
            {
                setNames[i] = Encoding.ASCII.GetString(setDimensions, 4 * sizeof(int) + i * offset, offset).Trim();
            }

            string[][] labelStrings = new string[setNames.Length][];
            for (int h = 0; h < setNames.Length; h++)
            {
                byte[] labels = InitializeArray(reader);
                // get label dimensions
                int labelX0 = BitConverter.ToInt32(labels, 0 * sizeof(int));
                int labelX1 = BitConverter.ToInt32(labels, 1 * sizeof(int));
                int labelX2 = BitConverter.ToInt32(labels, 2 * sizeof(int));


                labelStrings[h] = new string[labelX1];
                for (int i = 0; i < labelX2; i++)
                {
                    labelStrings[h][i] = Encoding.ASCII.GetString(labels, offset + i * offset, offset).Trim();
                }
            }

            if (a > 0 && c > 0 && c - a == 1)
            {
                setNames = setNames.Append(setNames.LastOrDefault()).ToArray();
                labelStrings = labelStrings.Append(labelStrings.LastOrDefault()).ToArray();
            }

            return (coefficient, setNames, labelStrings);
        }

        /// <summary>
        /// Calculates the next array of data for a real array with set labels stored as non-sparse.
        /// </summary>
        /// <param name="reader">
        /// The reader from which the data is read.
        /// </param>
        /// <returns>
        /// An array of data.
        /// </returns>
        /// <remarks>
        /// The array is composed of:
        ///
        ///      [dimensions] = <see cref="ReadDimensions(BinaryReader)"/>.
        /// 
        /// Followed by one or more:
        /// 
        ///     [segment] = <see cref="GetNextSegment{T}(BinaryReader, Func{byte[], int, T}, out T[])"/>.
        /// </remarks>
        [NotNull]
        private static float[] GetRealArrayWithSetLabelsFull([NotNull] BinaryReader reader)
        {
            int count = ReadDimensions(reader);
           
            float[] results = new float[count];

            int counter = 0;
            while (GetNextSegment(reader, BitConverter.ToSingle, out float[] floats))
            {
                Array.Copy(floats, 0, results, counter, floats.Length);
                counter += floats.Length;
            }

            return results;
        }

        /// <summary>
        /// Calculates the next segment of the record.
        /// </summary>
        /// <typeparam name="T">
        /// The type of data in the segment.
        /// </typeparam>
        /// <param name="reader">
        /// The reader from which the segment is read.
        /// </param>
        /// <param name="convert">
        /// A delegate that converts byte data at an index into a value.
        /// </param>
        /// <param name="segment">
        /// The array into which the segment is read.
        /// </param>
        /// <returns>
        /// True if there is another segment; false otherwise. 
        /// </returns>
        /// <remarks>
        /// The next segment contains:
        ///
        ///     [extents] = <see cref="ReadExtents(BinaryReader)"/>.
        /// 
        /// Followed by:
        /// 
        ///     [0 * sizeof(int)] = 1-based index of the current vector from the end of the record.
        ///     [(1 + i) * sizeof(int)] = the i-th value of the segment. 
        /// </remarks>
        private static bool GetNextSegment<T>([NotNull] BinaryReader reader, [NotNull] Func<byte[], int, T> convert, [NotNull] out T[] segment)
        {
            (int count, _) = ReadExtents(reader);

            byte[] data = InitializeArray(reader);

            int vectorIndex = BitConverter.ToInt32(data, 0 * sizeof(int));

            segment = new T[count];
            for (int i = 0; i < segment.Length; i++)
            {
                segment[i] = convert(data, (1 + i) * sizeof(int));
            }

            return vectorIndex > 1;
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
        private static int ReadDimensions([NotNull] BinaryReader reader)
        {
            byte[] dimensionArray = InitializeArray(reader);

            int dimensions = BitConverter.ToInt32(dimensionArray, 0 * sizeof(int));

            int count = 1;
            for (int i = 0; i < dimensions; i++)
            {
                count *= BitConverter.ToInt32(dimensionArray, (2 + i) * sizeof(int));
            }

            return count;
        }

        /// <summary>
        /// Calculates the extents of each dimension represented on the next vector and the number of items therein.
        /// </summary>
        /// <param name="reader">
        /// The reader from which the extent definitions are read.
        /// </param>
        /// <returns>
        /// The count of items and an integer array indicating the extents of each dimension represented on the next vector.
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
        private static (int Count, IReadOnlyList<int> Extents) ReadExtents([NotNull] BinaryReader reader)
        {
            byte[] extentsArray = InitializeArray(reader);

            int[] extents = new int[extentsArray.Length / sizeof(int)];

            for (int i = 0; i < extents.Length; i++)
            {
                extents[i] = BitConverter.ToInt32(extentsArray, (2 + 2 + i) * sizeof(int)) - BitConverter.ToInt32(extentsArray, (2 + 1 + i) * sizeof(int)) - 1;
            }

            int count = 1;
            for (int i = 0; i < extents.Length; i++)
            {
                count *= extents[i];
            }

            return (count, extents);
        }

        [NotNull]
        private static float[] GetReSparseArray(BinaryReader reader, int count)
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

            const int offset = 3 * sizeof(int);

            int elementSize = (data.Length - offset) / extent1;

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
                    strings[item] = Encoding.ASCII.GetString(data, offset + j * elementSize, elementSize).Trim();
                }
            }

            return strings;
        }

        /// <summary>
        /// Returns a <typeparamref name="TValue"/> array and the number of binary vectors from which the array was read.
        /// </summary>
        /// <typeparam name="TValue">
        /// The type of data in the array.
        /// </typeparam>
        /// <param name="reader">
        /// A binary reader positioned immediately before the start of the data array.
        /// </param>
        /// <param name="converter">
        /// A function delegate that returns a <typeparamref name="TValue"/> from a <see cref="Byte"/> array based on an index.
        /// </param>
        /// <param name="count">
        /// The total count of entries in the result array.
        /// </param>
        /// <returns>
        /// A tuple with a <typeparamref name="TValue"/> array and the number of binary vectors from which the array was read.
        /// </returns>
        /// <remarks>
        /// The array is composed of one or more:
        ///
        ///     [segment] = <see cref="GetNextTwoDimensionalSegment{T}(BinaryReader, Func{byte[], int, T}, out T[])"/>.   
        /// </remarks>
        [NotNull]
        private static TValue[] GetTwoDimensionalNumericArray<TValue>([NotNull] BinaryReader reader, [NotNull] Func<byte[], int, TValue> converter, int count)
        {
            TValue[] results = new TValue[count];

            int counter = 0;

            bool test = true;

            while (test)
            {
                test = GetNextTwoDimensionalSegment(reader, converter, out TValue[] floats);

                Array.Copy(floats, 0, results, counter, floats.Length);

                counter += floats.Length;
            }

            return results;
        }

        /// <summary>
        /// Calculates the next array segment for a two dimensional numeric record (e.g. 2I, 2R).
        /// </summary>
        /// <typeparam name="T">
        /// The type of data in the array.
        /// </typeparam>
        /// <param name="reader">
        /// The reader from which the segment is read.
        /// </param>
        /// <param name="converter">
        /// A delegate returning a value from an index inside a byte array.
        /// </param>
        /// <param name="results">
        /// The values of the array segment.
        /// </param>
        /// <returns>
        /// True if there are additional segments; otherwise false.
        /// </returns>
        /// <remarks>
        /// The next array segment is composed of:
        ///
        ///      [0 * sizeof(int)] = vector index.
        ///      [1 * sizeof(int)] = the number of elements in the first dimension.
        ///      [2 * sizeof(int)] = the number of elements in the second dimension.
        ///      [3 * sizeof(int)] = the starting index of the first dimension in the segment on the logical array.
        ///      [4 * sizeof(int)] = the ending index of the first dimension in the segment on the logical array.
        ///      [5 * sizeof(int)] = the starting index of the second dimension in the segment on the logical array.
        ///      [6 * sizeof(int)] = the ending index of the second dimension in the segment on the logical array.
        ///      [(7 + i) * sizeof(int)] = the i-th data value in the segment. 
        /// 
        /// The count of dimensions can be determined in-line as the length of the array after the first seven entries divided by the byte-size of an integer. 
        /// 
        /// Note that the extents must be deincremented by one to move from a 1-based index to a 0-based index. 
        /// </remarks>
        private static bool GetNextTwoDimensionalSegment<T>([NotNull] BinaryReader reader, [NotNull] Func<byte[], int, T> converter, [NotNull] out T[] results)
        {
            byte[] data = InitializeArray(reader);

            int vectorIndex = BitConverter.ToInt32(data, 0 * sizeof(int));

            results = new T[(data.Length - 7 * sizeof(int)) / sizeof(int)];

            for (int i = 0; i < results.Length; i++)
            {
                results[i] = converter(data, (i + 7) * sizeof(int));
            }

            return vectorIndex > 1;
        }
    }
}