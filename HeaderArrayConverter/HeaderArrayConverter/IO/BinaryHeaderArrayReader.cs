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

// ReSharper disable UnusedVariable

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

            //using (BinaryReader reader = new BinaryReader(new MemoryStream(File.ReadAllBytes(file))))
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
            (string description, string header, bool sparse, HeaderArrayType type, IEnumerable<int> dimensions) = GetDescription(reader);

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
                    (string coefficient, float[] floats, IImmutableList<KeyValuePair<string, IImmutableList<string>>> sets) = GetRealArrayWithSetLabels(reader, sparse);

                    KeySequence<string>[] expandedSets = sets.AsExpandedSet().ToArray();
                    
                    KeyValuePair<KeySequence<string>, float>[] items = new KeyValuePair<KeySequence<string>, float>[floats.Length];

                    for (int i = 0; i < items.Length; i++)
                    {
                        items[i] = new KeyValuePair<KeySequence<string>, float>(expandedSets[i], floats[i]);
                    }

                    return new HeaderArray<float>(header, coefficient, description, type, items, dimensions, sets);
                }
                case HeaderArrayType.I2:
                {
                    int[] ints = GetTwoDimensionalNumericArray(reader, BitConverter.ToInt32);

                    ImmutableArray<KeyValuePair<string, IImmutableList<string>>> sets =
                        new KeyValuePair<string, IImmutableList<string>>[]
                        {
                            new KeyValuePair<string, IImmutableList<string>>(
                                "INDEX",
                                Enumerable.Range(0, ints.Length).Select(x => x.ToString()).ToImmutableArray())
                        }.ToImmutableArray();

                    KeyValuePair<KeySequence<string>, int>[] items = new KeyValuePair<KeySequence<string>, int>[ints.Length];

                    for (int i = 0; i < items.Length; i++)
                    {
                        items[i] = new KeyValuePair<KeySequence<string>, int>(i.ToString(), ints[i]);
                    }
                    
                    return new HeaderArray<int>(header, header, description, type, items, dimensions, sets);
                }
                case HeaderArrayType.R2:
                {
                    float[] floats = GetTwoDimensionalNumericArray(reader, BitConverter.ToSingle);

                    ImmutableArray<KeyValuePair<string, IImmutableList<string>>> sets =
                        new KeyValuePair<string, IImmutableList<string>>[]
                        {
                            new KeyValuePair<string, IImmutableList<string>>(
                                "INDEX",
                                Enumerable.Range(0, floats.Length).Select(x => x.ToString()).ToImmutableArray())
                        }.ToImmutableArray();

                    KeyValuePair<KeySequence<string>, float>[] items = new KeyValuePair<KeySequence<string>, float>[floats.Length];

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

        private static (string Description, string Header, bool Sparse, HeaderArrayType Type, IImmutableList<int> Dimensions) GetDescription(BinaryReader reader)
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

            return (description, header, sparse, type, dimensions.ToImmutableArray());
        }

        private static (string Coefficient, float[] Data, IImmutableList<KeyValuePair<string, IImmutableList<string>>> Sets) GetRealArrayWithSetLabels(BinaryReader reader, bool sparse)
        {
            // read dimension array
            byte[] dimensions = InitializeArray(reader);

            // number of labels?
            int a = BitConverter.ToInt32(dimensions, 0 * sizeof(int));

            int possibleSpacer0 = BitConverter.ToInt32(dimensions, 1 * sizeof(int));

            if (Spacer != possibleSpacer0)
            {
                if (a != 0 && BitConverter.ToInt32(dimensions, 1 * sizeof(int)) != 1)
                {
                    throw new DataValidationException("Binary spacer check", Spacer, possibleSpacer0);
                }
            }

            const int offset = 12;

            // number of labels...again?
            int c = BitConverter.ToInt32(dimensions, 2 * sizeof(int));

            // Read coefficient
            string coefficient = Encoding.ASCII.GetString(dimensions, offset, offset).Trim();

            int possibleSpacer1 = BitConverter.ToInt32(dimensions, 6 * sizeof(int));

            if (Spacer != possibleSpacer1)
            {
                if (a != 0 && BitConverter.ToInt32(dimensions, 1 * sizeof(int)) != 1)
                {
                    throw new DataValidationException("Binary spacer check", Spacer, possibleSpacer1);
                }
            }

            // Read set names
            string[] setNames = new string[a];
            for (int i = 0; i < a; i++)
            {
                setNames[i] = Encoding.ASCII.GetString(dimensions, 4 * sizeof(int) + i * offset, offset).Trim();
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

            KeyValuePair<string, IImmutableList<string>>[] sets = new KeyValuePair<string, IImmutableList<string>>[setNames.Length];

            for (int i = 0; i < setNames.Length; i++)
            {
                sets[i] = new KeyValuePair<string, IImmutableList<string>>(setNames[i], labelStrings[i].ToImmutableArray());
            }

            float[] data = sparse ? GetReSparseArray(reader, sets.Aggregate(1, (current, next) => current * next.Value.Count)) : GetReFullArray(reader);

            if (!sets.Any())
            {
                sets = new KeyValuePair<string, IImmutableList<string>>[]
                {
                    new KeyValuePair<string, IImmutableList<string>>(coefficient, new string[] { coefficient }.ToImmutableArray())
                };
            }

            return (coefficient, data, sets.ToImmutableArray());
        }

        [NotNull]
        private static float[] GetReFullArray(BinaryReader reader)
        {
            byte[] meta = InitializeArray(reader);
            int recordsFromEndOfThisHeaderArray = BitConverter.ToInt32(meta, 0 * sizeof(int));
            int dimensionLimit = BitConverter.ToInt32(meta, 1 * sizeof(int));
            int[] dimensions = new int[dimensionLimit];

            const int offset = 8;

            for (int i = 0; i < dimensions.Length; i++)
            {
                dimensions[i] = BitConverter.ToInt32(meta, offset + i * sizeof(int));
            }
            int count = dimensions.Aggregate(1, (current, next) => current * next);

            float[] results = new float[count];
            bool test = true;
            int counter = 0;
            while (test)
            {
                test = CalculateNextArraySegment(out float[] floats);
                Array.Copy(floats, 0, results, counter, floats.Length);
                counter += floats.Length;
            }

            return results;

            bool CalculateNextArraySegment(out float[] segment)
            {
                byte[] dimDefinitons = InitializeArray(reader);
                int x0 = dimDefinitons.Length / sizeof(int);
                int[] dimDescriptions = new int[x0];
                for (int i = 0; i < x0; i++)
                {
                    dimDescriptions[i] = BitConverter.ToInt32(dimDefinitons, i * sizeof(int));
                }
                int[] dimLengths = new int[x0 / 2];
                for (int i = 0; i < x0 / 2; i++)
                {
                    dimLengths[i] = dimDescriptions[2 + 2 * i] - dimDescriptions[1 + 2 * i] + 1;
                }

                byte[] data = InitializeArray(reader);
                int dataDim = BitConverter.ToInt32(data, 0 * sizeof(int));

                segment = new float[dimLengths.Aggregate(1, (current, next) => current * next)];

                for (int i = 0; i < segment.Length; i++)
                {
                    segment[i] = BitConverter.ToSingle(data, i * sizeof(int) + sizeof(int));
                }

                return dimDescriptions[0] != 2;
            }
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

        [NotNull]
        private static string[] GetStringArray(BinaryReader reader)
        {
            byte[] data = InitializeArray(reader);

            int x0 = BitConverter.ToInt32(data, 0 * sizeof(int));
            int x1 = BitConverter.ToInt32(data, 1 * sizeof(int));
            int x2 = BitConverter.ToInt32(data, 2 * sizeof(int));

            const int offset = 12;

            int elementSize = (data.Length - offset) / x2;

            byte[][] record = new byte[x1][];
            string[] strings = new string[x1];

            for (int i = 0; i < x0; i++)
            {
                if (i > 0)
                {
                    data = InitializeArray(reader);
                }
                for (int j = 0; j < x2; j++)
                {
                    int item = i * x2 + j;
                    if (item >= x1)
                    {
                        break;
                    }
                    record[item] = data.Skip(offset).Skip(j * elementSize).Take(elementSize).ToArray();
                    strings[item] = Encoding.ASCII.GetString(record[item]).Trim();
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
        /// <returns>
        /// A tuple with a <typeparamref name="TValue"/> array and the number of binary vectors from which the array was read.
        /// </returns>
        private static TValue[] GetTwoDimensionalNumericArray<TValue>(BinaryReader reader, Func<byte[], int, TValue> converter)
        {
            byte[] data = InitializeArray(reader);

            int vectors = BitConverter.ToInt32(data, 0 * sizeof(int));
            int totalCount = BitConverter.ToInt32(data, 1 * sizeof(int));
            int maxPerVector = BitConverter.ToInt32(data, 2 * sizeof(int));

            int vectors2 = BitConverter.ToInt32(data, 3 * sizeof(int));
            int totalCount2 = BitConverter.ToInt32(data, 4 * sizeof(int));
            int maxPerVector2 = BitConverter.ToInt32(data, 5 * sizeof(int));

            int vectorNumber = BitConverter.ToInt32(data, 6 * sizeof(int));

            const int offset = 7 * sizeof(int);

            TValue[] results = new TValue[totalCount];

            int counter = 0;
            bool test = true;
            while (test)
            {
                if (counter > 0)
                {
                    data = InitializeArray(reader);
                }

                TValue[] temp = new TValue[(data.Length - offset) / sizeof(int)];

                for (int i = 0; i < temp.Length; i++)
                {
                    temp[i] = converter(data, offset + i * sizeof(int));
                }

                Array.Copy(temp, 0, results, counter, temp.Length);
                counter += temp.Length;

                test = BitConverter.ToInt32(data, 0) != 1;
            }

            //for (int i = vectorNumber; i > 0; i--)
            //{
            //    if (i < vectorNumber)
            //    {
            //        data = InitializeArray(reader);
            //    }

            //    TValue[] temp = new TValue[(data.Length - offset) / sizeof(int)];

            //    for (int j = 0; j < temp.Length; j++)
            //    {
            //        temp[j] = converter(data, offset + j * sizeof(int));
            //    }

            //    Array.Copy(temp, 0, results, counter, temp.Length);

            //    counter += temp.Length;
            //}

            return results;
        }
    }
}