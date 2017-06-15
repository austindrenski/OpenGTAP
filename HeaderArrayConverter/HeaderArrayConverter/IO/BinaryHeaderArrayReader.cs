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

            foreach (Task<IHeaderArray> array in ReadArraysAsync(file))
            {
                yield return array.Result;
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

            using (BinaryReader reader = new BinaryReader(File.Open(file, FileMode.Open, FileAccess.Read)))
            {
                while (reader.BaseStream.Position != reader.BaseStream.Length)
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
            (string description, string header, bool sparse, string type, int[] dimensions) = GetDescription(reader);

            switch (type)
            {
                case "1C":
                {
                    string[] strings = GetStringArray(reader);

                    IEnumerable<KeyValuePair<KeySequence<string>, string>> items =
                        strings.Select(
                            (x, i) =>
                                new KeyValuePair<KeySequence<string>, string>(i.ToString(), x));

                    ImmutableArray<KeyValuePair<string, IImmutableList<string>>> sets =
                        new KeyValuePair<string, IImmutableList<string>>[]
                        {
                            new KeyValuePair<string, IImmutableList<string>>(
                                "INDEX",
                                Enumerable.Range(0, strings.Length)
                                          .Select(x => x.ToString())
                                          .ToImmutableArray())
                        }.ToImmutableArray();

                    return new HeaderArray<string>(header, description, type, dimensions, items, sets, 1);
                }
                case "RE":
                {
                    (float[] floats, IImmutableList<KeyValuePair<string, IImmutableList<string>>> sets) = GetReArray(reader, sparse);

                    IEnumerable<KeySequence<string>> expandedSets =
                        sets.AsExpandedSet()
                            .ToArray();

                    IEnumerable<KeyValuePair<KeySequence<string>, float>> items =
                        expandedSets.Zip(floats, (k, v) => new KeyValuePair<KeySequence<string>, float>(k, v));

                    return new HeaderArray<float>(header, description, type, dimensions, items, sets, 1);
                }
                case "2I":
                {
                    (int serializedVectors, int[] ints) = Get2_Array(reader, BitConverter.ToInt32);

                    IEnumerable<KeyValuePair<KeySequence<string>, int>> items = 
                        ints.Select(
                            (x, i) =>
                                new KeyValuePair<KeySequence<string>, int>(i.ToString(), x));

                    ImmutableArray<KeyValuePair<string, IImmutableList<string>>> sets =
                        new KeyValuePair<string, IImmutableList<string>>[]
                        {
                            new KeyValuePair<string, IImmutableList<string>>(
                                "INDEX",
                                Enumerable.Range(0, ints.Length)
                                          .Select(x => x.ToString())
                                          .ToImmutableArray())
                        }.ToImmutableArray();

                    return new HeaderArray<int>(header, description, type, dimensions, items, sets, serializedVectors);
                }
                case "2R":
                {
                    (int serializedVectors, float[] floats) = Get2_Array(reader, BitConverter.ToSingle);

                    IEnumerable<KeyValuePair<KeySequence<string>, float>> items =
                        floats.Select(
                            (x, i) =>
                                new KeyValuePair<KeySequence<string>, float>(i.ToString(), x));

                    ImmutableArray<KeyValuePair<string, IImmutableList<string>>> sets =
                        new KeyValuePair<string, IImmutableList<string>>[]
                        {
                            new KeyValuePair<string, IImmutableList<string>>(
                                "INDEX", 
                                Enumerable.Range(0, floats.Length)
                                          .Select(x => x.ToString())
                                          .ToImmutableArray())
                        }.ToImmutableArray();

                    return new HeaderArray<float>(header, description, type, dimensions, items, sets, serializedVectors);
                }
                default:
                {
                    throw new DataValidationException("Type", "1C, RE, RL, 2I, 2R", type);
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

            // Read array
            byte[] data = reader.ReadBytes(length);

            int closingLength = reader.ReadInt32();

            // Verify array length
            if (length != closingLength)
            {
                throw new DataValidationException("Binary length check", length, closingLength);
            }

            int padding = BitConverter.ToInt32(data, 0);

            // Verify the padding
            if (Padding != padding)
            {
                throw new DataValidationException("Binary padding check", Padding, padding);
            }

            // Skip padding and return
            return data.Skip(4).ToArray();
        }

        private static (string Description, string Header, bool Sparse, string Type, int[] Dimensions) GetDescription(BinaryReader reader)
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
            string type = Encoding.ASCII.GetString(descriptionBuffer, 0, 2);

            // Read length type => 'FULL'
            bool sparse = Encoding.ASCII.GetString(descriptionBuffer, 2, 4) != "FULL";

            // Read longer name description with limit of 70 characters
            string description = Encoding.ASCII.GetString(descriptionBuffer, 6, 70);

            int[] dimensions = new int[BitConverter.ToInt32(descriptionBuffer, 76)];

            for (int i = 0; i < dimensions.Length; i++)
            {
                dimensions[i] = BitConverter.ToInt32(descriptionBuffer, 80 + 4 * i);
            }

            return (description, header, sparse, type, dimensions);
        }

        private static (float[] Data, IImmutableList<KeyValuePair<string, IImmutableList<string>>> Sets) GetReArray(BinaryReader reader, bool sparse)
        {
            // read dimension array
            byte[] dimensions = InitializeArray(reader);

            // number of labels?
            int a = BitConverter.ToInt32(dimensions, 0);

            int possibleSpacer0 = BitConverter.ToInt32(dimensions, 4);

            if (Spacer != possibleSpacer0)
            {
                if (a != 0 && BitConverter.ToInt32(dimensions, 4) != 1)
                {
                    throw new DataValidationException("Binary spacer check", Spacer, possibleSpacer0);
                }
            }

            // number of labels...again?
            int c = BitConverter.ToInt32(dimensions, 8);

            // Read coefficient
            string coefficient = Encoding.ASCII.GetString(dimensions, 12, 12).Trim();

            int possibleSpacer1 = BitConverter.ToInt32(dimensions, 24);

            if (Spacer != possibleSpacer1)
            {
                if (a != 0 && BitConverter.ToInt32(dimensions, 4) != 1)
                {
                    throw new DataValidationException("Binary spacer check", Spacer, possibleSpacer1);
                }
            }

            // Read set names
            string[] setNames = new string[a];
            for (int i = 0; i < a; i++)
            {
                setNames[i] = Encoding.ASCII.GetString(dimensions, 28 + i * 12, 12).Trim();
            }

            string[][] labelStrings = new string[setNames.Length][];
            for (int h = 0; h < setNames.Length; h++)
            {
                byte[] labels = InitializeArray(reader);
                // get label dimensions
                int labelX0 = BitConverter.ToInt32(labels, 0);
                int labelX1 = BitConverter.ToInt32(labels, 4);
                int labelX2 = BitConverter.ToInt32(labels, 8);
                labelStrings[h] = new string[labelX1];
                for (int i = 0; i < labelX2; i++)
                {
                    labelStrings[h][i] = Encoding.ASCII.GetString(labels, 12 + i * 12, 12).Trim();
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

            return (data, sets.ToImmutableArray());
        }

        [NotNull]
        private static float[] GetReFullArray(BinaryReader reader)
        {
            byte[] meta = InitializeArray(reader);
            int recordsFromEndOfThisHeaderArray = BitConverter.ToInt32(meta, 0);
            int dimensionLimit = BitConverter.ToInt32(meta, 4);
            int[] dimensions = new int[dimensionLimit];
            for (int i = 0; i < dimensions.Length; i++)
            {
                dimensions[i] = BitConverter.ToInt32(meta, 8 + 4 * i);
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
                int x0 = dimDefinitons.Length / 4;
                int[] dimDescriptions = new int[x0];
                for (int i = 0; i < x0; i++)
                {
                    dimDescriptions[i] = BitConverter.ToInt32(dimDefinitons, 4 * i);
                }
                int[] dimLengths = new int[x0 / 2];
                for (int i = 0; i < x0 / 2; i++)
                {
                    dimLengths[i] = dimDescriptions[2 + 2 * i] - dimDescriptions[1 + 2 * i] + 1;
                }

                byte[] data = InitializeArray(reader);
                int dataDim = BitConverter.ToInt32(data, 0);

                segment = new float[dimLengths.Aggregate(1, (current, next) => current * next)];

                for (int i = 0; i < segment.Length; i++)
                {
                    segment[i] = BitConverter.ToSingle(data, 4 + i * 4);
                }

                return dimDescriptions[0] != 2;
            }
        }

        [NotNull]
        private static float[] GetReSparseArray(BinaryReader reader, int count)
        {
            byte[] meta = InitializeArray(reader);

            int valueCount = BitConverter.ToInt32(meta, 0);
            int idk0 = BitConverter.ToInt32(meta, 4);
            int idk1 = BitConverter.ToInt32(meta, 8);

            byte[] data = InitializeArray(reader);
            int numberOfVectors = BitConverter.ToInt32(data, 0);
            int totalCountOfEntries = BitConverter.ToInt32(data, 4);
            int maxEntriesPerVector = BitConverter.ToInt32(data, 8);

            int[] indices = new int[totalCountOfEntries];
            float[] floats = new float[count];

            for (int i = 0; i < numberOfVectors; i++)
            {
                if (i > 0)
                {
                    data = InitializeArray(reader);
                }
                int length = i + 1 == numberOfVectors ? totalCountOfEntries - i * maxEntriesPerVector : maxEntriesPerVector;
                for (int j = 0; j < length; j++)
                {
                    indices[i * maxEntriesPerVector + j] = BitConverter.ToInt32(data, 12 + j * 4) - 1;
                }
                for (int j = 0; j < length; j++)
                {
                    floats[indices[i * maxEntriesPerVector + j]] = BitConverter.ToSingle(data, 12 + length * 4 + j * 4);
                }
            }

            return floats;
        }

        [NotNull]
        private static string[] GetStringArray(BinaryReader reader)
        {
            byte[] data = InitializeArray(reader);

            int x0 = BitConverter.ToInt32(data, 0);
            int x1 = BitConverter.ToInt32(data, 4);
            int x2 = BitConverter.ToInt32(data, 8);

            int elementSize = (data.Length - 12) / x2;

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
                    record[item] = data.Skip(12).Skip(j * elementSize).Take(elementSize).ToArray();
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
        private static (int SerializedVectors, TValue[] Values) Get2_Array<TValue>(BinaryReader reader, Func<byte[], int, TValue> converter)
        {
            byte[] data = InitializeArray(reader);

            int vectors = BitConverter.ToInt32(data, 0);
            int totalCount = BitConverter.ToInt32(data, 4);
            int maxPerVector = BitConverter.ToInt32(data, 8);

            int vectors2 = BitConverter.ToInt32(data, 12);
            int totalCount2 = BitConverter.ToInt32(data, 16);
            int maxPerVector2 = BitConverter.ToInt32(data, 20);

            int vectorNumber = BitConverter.ToInt32(data, 24);

            TValue[] results = new TValue[totalCount];
            bool test = true;
            int counter = 0;
            int serializedVectors = 0;
            while (test)
            {
                serializedVectors++;
                if (counter > 0)
                {
                    data = InitializeArray(reader);
                }
                TValue[] floats = new TValue[(data.Length - 28) / 4];

                for (int i = 0; i < floats.Length; i++)
                {
                    floats[i] = converter(data, 28 + 4 * i);
                }

                Array.Copy(floats, 0, results, counter, floats.Length);
                counter += floats.Length;

                test = BitConverter.ToInt32(data, 0) != 1;
            }

            return (serializedVectors, results);
        }
    }
}