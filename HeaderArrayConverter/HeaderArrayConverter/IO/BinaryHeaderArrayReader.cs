using System;
using System.Collections.Concurrent;
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

            return ReadArraysAsync(file).Select(x => x.Result);
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

            //using (BinaryReader reader = new BinaryReader(File.Open(file, FileMode.Open)))
            //{
            //    long length = reader.BaseStream.Length;
            //    while (reader.BaseStream.Position < length)
            //    {
            //        yield return Task.FromResult(ReadNext(reader));
            //    }
            //}
            //yield break;

            byte[] buffer = File.ReadAllBytes(file);
            using (Stream stream = new MemoryStream(buffer))
            {
                List<byte> temp = new List<byte>();

                while (stream.Position < stream.Length)
                {
                    byte[] data = new byte[sizeof(int) + BitConverter.ToInt32(buffer, (int)stream.Position) + sizeof(int)];

                    Buffer.BlockCopy(buffer, (int) stream.Position, data, 0, data.Length);

                    stream.Seek(data.Length, SeekOrigin.Current);

                    if (!temp.Any())
                    {
                        temp.AddRange(data);
                        if (stream.Position < stream.Length)
                        {
                            continue;
                        }
                    }

                    if (data[4] == 0x20 && data[5] == 0x20 && data[6] == 0x20 && data[7] == 0x20)
                    {
                        temp.AddRange(data);
                        if (stream.Position < stream.Length)
                        {
                            continue;
                        }
                    }

                    byte[] bytes = temp.ToArray();

                    yield return Task.Factory.StartNew(() => ReadNext(new BinaryReader(new MemoryStream(bytes))));

                    temp.Clear();
                    temp.AddRange(data);
                  
                    if (stream.Position == stream.Length)
                    {
                        break;
                    }
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
                    (string coefficient, float[] floats, IReadOnlyList<KeyValuePair<string, IImmutableList<string>>> sets) = 
                            GetNumericArrayWithSetLabels(reader, sparse);

                    KeySequence<string>[] expandedSets = 
                            sets.AsExpandedSet().ToArray();
                    
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
                    int[] ints =
                            GetFullNumericArray(reader, BitConverter.ToInt32, 7, count);

                    ImmutableArray<KeyValuePair<string, IImmutableList<string>>> sets = 
                            ImmutableArray.Create(new KeyValuePair<string, IImmutableList<string>>("INDEX", Enumerable.Range(0, ints.Length).Select(x => x.ToString()).ToImmutableArray()));

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
                    float[] floats =
                            GetFullNumericArray(reader, BitConverter.ToSingle, 7, count);

                    ImmutableArray<KeyValuePair<string, IImmutableList<string>>> sets = 
                            ImmutableArray.Create(new KeyValuePair<string, IImmutableList<string>>("INDEX", Enumerable.Range(0, floats.Length).Select(x => x.ToString()).ToImmutableArray()));

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

        private static (string Coefficient, float[] Data, IReadOnlyList<KeyValuePair<string, IImmutableList<string>>> Sets) GetNumericArrayWithSetLabels(BinaryReader reader, bool sparse)
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

            float[] data = sparse ? GetReSparseArray(reader, dimensions) : GetFullRealArrayWithSets(reader, BitConverter.ToSingle, 1);

            return (coefficient, data, sets);
        }

        private static (string Coefficient, string[] SetNames, string[][] LabelStrings) ReadSets([NotNull] BinaryReader reader)
        {
            // read dimension array
            byte[] setDimensions = InitializeArray(reader);

            // number of labels?
            int a = BitConverter.ToInt32(setDimensions, 0 * sizeof(int));

            bool knownSets = BitConverter.ToBoolean(setDimensions, 1 * sizeof(int));

            if (!knownSets && a != 0)
            {
                throw new DataValidationException("Binary boolean check", true, false);
            }

            // number of labels...again?
            int c = BitConverter.ToInt32(setDimensions, 2 * sizeof(int));

            // Read coefficient
            string coefficient = Encoding.ASCII.GetString(setDimensions, 3 * sizeof(int), 3 * sizeof(int)).Trim();

            bool check = BitConverter.ToBoolean(setDimensions, 6 * sizeof(int));

            if (!check && a != 0)
            {
                throw new DataValidationException("Binary boolean check", true, false);
            }

            // Read set names
            string[] setNames = new string[a];
            for (int i = 0; i < a; i++)
            {
                setNames[i] = Encoding.ASCII.GetString(setDimensions, 4 * sizeof(int) + i * 3 * sizeof(int), 3 * sizeof(int)).Trim();
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
                    labelStrings[h][i] = Encoding.ASCII.GetString(labels, 3 * sizeof(int) + i * 3 * sizeof(int), 3 * sizeof(int)).Trim();
                }
            }

            if (a > 0 && c > 0 && c - a == 1)
            {
                setNames = setNames.Append(setNames.LastOrDefault()).ToArray();
                labelStrings = labelStrings.Append(labelStrings.LastOrDefault()).ToArray();
            }

            return (coefficient, setNames, labelStrings);
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
        /// <param name="offset">
        /// The offset to move from the zero-th position to the first value position.
        /// </param>
        /// <returns>
        ///  An array of data.
        ///  </returns>
        ///  <remarks>
        ///  The array is composed of one or more:
        ///  
        ///      [extents] = <see cref="ReadExtents(BinaryReader)"/>.
        ///      [segment] = <see cref="GetNextSegment{T}(BinaryReader, Func{byte[], int, T}, int, out T[])"/>.
        /// 
        ///  </remarks>
        [NotNull]
        private static TValue[] GetFullRealArrayWithSets<TValue>([NotNull] BinaryReader reader, [NotNull] Func<byte[], int, TValue> converter, int offset)
        {
            int count = ReadDimensions(reader);
            TValue[] results = new TValue[count];
            int counter = 0;
            bool test = true;
            while (test)
            {
                ReadExtents(reader);
                test = GetNextSegment(reader, converter, offset, out TValue[] floats);
                Array.Copy(floats, 0, results, counter, floats.Length);
                counter += floats.Length;
            }

            return results;
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
        /// <param name="offset">
        /// The offset to move from the zero-th position to the first value position.
        /// </param>
        /// <param name="count">
        /// The number of items in the segment.
        /// </param>
        /// <returns>
        ///  An array of data.
        ///  </returns>
        ///  <remarks>
        ///  The array is composed of one or more:
        ///  
        ///      [segment] = <see cref="GetNextSegment{T}(BinaryReader, Func{byte[], int, T}, int, out T[])"/>.
        /// 
        ///  </remarks>
        [NotNull]
        private static TValue[] GetFullNumericArray<TValue>([NotNull] BinaryReader reader, [NotNull] Func<byte[], int, TValue> converter, int offset, int count)
        {
            TValue[] results = new TValue[count];
            int counter = 0;
            bool test = true;
            while (test)
            {
                test = GetNextSegment(reader, converter, offset, out TValue[] floats);
                Array.Copy(floats, 0, results, counter, floats.Length);
                counter += floats.Length;
            }

            return results;
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

            int dimensions = BitConverter.ToInt32(dimensionArray, 1 * sizeof(int));

            int count = 1;
            for (int i = 0; i < dimensions; i++)
            {
                count *= BitConverter.ToInt32(dimensionArray, (2 + i) * sizeof(int));
            }

            return count;
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
        private static IReadOnlyList<int> ReadExtents([NotNull] BinaryReader reader)
        {
            byte[] extentsArray = InitializeArray(reader);

            int[] extents = new int[(extentsArray.Length - sizeof(int)) / 2 / sizeof(int)];

            for (int i = 0; i < extents.Length; i++)
            {
                extents[i] = BitConverter.ToInt32(extentsArray, (2 + i) * sizeof(int)) - BitConverter.ToInt32(extentsArray, (1 + i) * sizeof(int));
            }

            return extents;
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

        ///  <summary>
        ///  Calculates the next value segment.
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
        /// <param name="offset">
        /// The offset to move from the zero-th position to the first value position.
        /// </param>
        /// <param name="results">
        ///  The values of the array segment.
        ///  </param>
        ///  <returns>
        ///  True if there are additional segments; otherwise false.
        ///  </returns>
        ///  <remarks>
        ///  For two dimensional arrays, the next segment is composed of:
        /// 
        ///       [0 * sizeof(int)] = vector index.
        ///       [1 * sizeof(int)] = the number of elements in the first dimension.
        ///       [2 * sizeof(int)] = the number of elements in the second dimension.
        ///       [3 * sizeof(int)] = the starting index of the first dimension in the segment on the logical array.
        ///       [4 * sizeof(int)] = the ending index of the first dimension in the segment on the logical array.
        ///       [5 * sizeof(int)] = the starting index of the second dimension in the segment on the logical array.
        ///       [6 * sizeof(int)] = the ending index of the second dimension in the segment on the logical array.
        ///       [(7 + i) * sizeof(int)] = the i-th data value in the segment. 
        ///  
        ///  For seven dimensional arrays, the next segment is composed of:
        /// 
        ///      [extents] = <see cref="ReadExtents(BinaryReader)"/>.
        ///      [0 * sizeof(int)] = 1-based index of the current vector from the end of the record.
        ///      [(1 + i) * sizeof(int)] = the i-th value of the segment. 
        /// 
        ///  </remarks>
        private static bool GetNextSegment<TValue>([NotNull] BinaryReader reader, [NotNull] Func<byte[], int, TValue> converter, int offset, [NotNull] out TValue[] results)
        {
            byte[] data = InitializeArray(reader);

            int vectorIndex = BitConverter.ToInt32(data, 0 * sizeof(int));

            results = new TValue[(data.Length - offset * sizeof(int)) / sizeof(int)];

            for (int i = 0; i < results.Length; i++)
            {
                results[i] = converter(data, (i + offset) * sizeof(int));
            }

            return vectorIndex > 1;
        }
    }
}