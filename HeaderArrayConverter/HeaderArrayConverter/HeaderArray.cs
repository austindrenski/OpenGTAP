using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AD.IO;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// ReSharper disable UnusedVariable

namespace HeaderArrayConverter
{
    /// <summary>
    /// Utility methods for reading and writing Header Array (HAR) files.
    /// </summary>
    [PublicAPI]
    public static class HeaderArray
    {
        #region Read HARX files

        /// <summary>
        /// Enumerates the arrays from the HARX file.
        /// </summary>
        /// <param name="file">
        /// The HARX file from which to read arrays.
        /// </param>
        /// <returns>
        /// An enumerable collection of the arrays in the file.
        /// </returns>
        [NotNull]
        [ItemNotNull]
        public static IEnumerable<IHeaderArray> ReadHarxArrays(FilePath file)
        {
            foreach (Task<IHeaderArray> array in ReadHarxArraysAsync(file))
            {
                yield return array.Result;
            }
        }

        /// <summary>
        /// Asynchronously enumerates the arrays from the HAR file.
        /// </summary>
        /// <param name="file">
        /// The HAR file from which to read arrays.
        /// </param>
        /// <returns>
        /// An enumerable collection of tasks that when completed return the arrays in the file.
        /// </returns>
        [NotNull]
        [ItemNotNull]
        public static IEnumerable<Task<IHeaderArray>> ReadHarxArraysAsync(FilePath file)
        {
            using (ZipArchive archive = new ZipArchive(File.Open(file, FileMode.Open, FileAccess.Read)))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    yield return ReadHarxArrayAsync(entry);
                }
            }
        }

        /// <summary>
        /// Reads one entry from a HARX file.
        /// </summary>
        [NotNull]
        [ItemNotNull]
        private static async Task<IHeaderArray> ReadHarxArrayAsync(ZipArchiveEntry entry)
        {
            string json = new StreamReader(entry.Open()).ReadToEnd();

            switch (json.Contains("\"Type\":\"1C\""))
            {
                case true:
                {
                    return await Task.FromResult<IHeaderArray>(JsonConvert.DeserializeObject<IHeaderArray<string>>(json, new HeaderArrayJsonConverter<string>()));
                }
                case false:
                {
                    return await Task.FromResult<IHeaderArray>(JsonConvert.DeserializeObject<IHeaderArray<float>>(json, new HeaderArrayJsonConverter<float>()));
                }
                default:
                {
                    throw new NotSupportedException();
                }
            }
        }

        /// <summary>
        /// Reads the contents of a HAR file.
        /// </summary>
        /// <param name="file">
        /// The file to read.
        /// </param>
        /// <returns>
        /// The contents of the HAR file.
        /// </returns>
        [NotNull]
        public static HeaderArrayFile ReadHarFile([NotNull] FilePath file)
        {
            if (file is null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            return new HeaderArrayFile(ReadHarArrays(file));
        }

        /// <summary>
        /// Reads the contents of a HARX file.
        /// </summary>
        /// <param name="file">
        /// The file to read.
        /// </param>
        /// <returns>
        /// The contents of the HARX file.
        /// </returns>
        [NotNull]
        public static HeaderArrayFile ReadHarxFile([NotNull] FilePath file)
        {
            if (file is null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            return new HeaderArrayFile(ReadHarxArrays(file));
        }

        #endregion

        #region Write HARX files

        /// <summary>
        /// Asynchronously writes the <see cref="IHeaderArray"/> collection to a zipped archive of JSON files.
        /// </summary>
        /// <param name="source">
        /// The array collection to write.
        /// </param>
        /// <param name="file">
        /// The output file.
        /// </param>
        public static async Task WriteHarxAsync([NotNull] this IEnumerable<IHeaderArray> source, [NotNull] string file)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (file is null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            using (ZipArchive archive = new ZipArchive(new FileStream(file, FileMode.Create), ZipArchiveMode.Create))
            {
                foreach (IHeaderArray item in source)
                {
                    ZipArchiveEntry entry = archive.CreateEntry($"{item.Header}.json", CompressionLevel.Optimal);
                    using (StreamWriter writer = new StreamWriter(entry.Open()))
                    {
                        await writer.WriteAsync(item.SerializeJson());
                    }
                }
            }
        }

        /// <summary>
        /// Asynchronously writes the <see cref="IHeaderArray"/> to a zipped archive of JSON files.
        /// </summary>
        /// <param name="source">
        /// The array to write.
        /// </param>
        /// <param name="file">
        /// The output file.
        /// </param>
        public static async Task WriteHarxAsync([NotNull] this IHeaderArray source, [NotNull] string file)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (file is null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            await new IHeaderArray[] { source }.WriteHarxAsync(file);
        }

        /// <summary>
        /// Writes the <see cref="IHeaderArray"/> collection to a zipped archive of JSON files.
        /// </summary>
        /// <param name="source">
        /// The arrays to write.
        /// </param>
        /// <param name="file">
        /// The output file.
        /// </param>
        public static void WriteHarx([NotNull] this IEnumerable<IHeaderArray> source, [NotNull] string file)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (file is null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            source.WriteHarxAsync(file).Wait();
        }

        /// <summary>
        /// writes the <see cref="IHeaderArray"/> to a zipped archive of JSON files.
        /// </summary>
        /// <param name="source">
        /// The array to write.
        /// </param>
        /// <param name="file">
        /// The output file.
        /// </param>
        public static void WriteHarx([NotNull] IHeaderArray source, [NotNull] string file)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (file is null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            source.WriteHarxAsync(file).Wait();
        }
        
        #endregion

        #region Read HAR files

        /// <summary>
        /// Enumerates the arrays from the HAR file.
        /// </summary>
        /// <param name="file">
        /// The HAR file from which to read arrays.
        /// </param>
        /// <returns>
        /// An enumerable collection of the arrays in the file.
        /// </returns>
        [NotNull]
        [ItemNotNull]
        public static IEnumerable<IHeaderArray> ReadHarArrays(FilePath file)
        {
            using (BinaryReader reader = new BinaryReader(File.Open(file, FileMode.Open, FileAccess.Read)))
            {
                while (reader.BaseStream.Position != reader.BaseStream.Length)
                {
                    yield return ReadNext(reader);
                }
            }
        }

        /// <summary>
        /// Asynchronously enumerates the arrays from the HAR file.
        /// </summary>
        /// <param name="file">
        /// The HAR file from which to read arrays.
        /// </param>
        /// <returns>
        /// An enumerable collection of tasks that when completed return the arrays in the file.
        /// </returns>
        [NotNull]
        [ItemNotNull]
        public static IEnumerable<Task<IHeaderArray>> ReadHarArraysAsync(FilePath file)
        {
            using (BinaryReader reader = new BinaryReader(File.Open(file, FileMode.Open, FileAccess.Read)))
            {
                while (reader.BaseStream.Position != reader.BaseStream.Length)
                {
                    yield return ReadNextAsync(reader);
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

                    return new HeaderArray<string>(header, description, type, dimensions, items, Enumerable.Empty<IEnumerable<string>>());
                }
                case "RE":
                {
                    (float[] floats, IEnumerable<string>[] sets) = GetReArray(reader, sparse);

                    IEnumerable<KeySequence<string>> expandedSets =
                        sets.AsExpandedSet()
                            .ToArray();

                    IEnumerable<KeyValuePair<KeySequence<string>, float>> items =
                            expandedSets.Zip(floats, (k, v) => new KeyValuePair<KeySequence<string>, float>(k, v));

                    return new HeaderArray<float>(header, description, type, dimensions, items, sets);
                }
                case "RL":
                {
                    float[] floats = GetRlArray(reader);

                    IEnumerable<KeyValuePair<KeySequence<string>, float>> items =
                        floats.Select(
                            (x, i) =>
                                new KeyValuePair<KeySequence<string>, float>(i.ToString(), x));

                    return new HeaderArray<float>(header, description, type, dimensions, items, Enumerable.Empty<IEnumerable<string>>());
                }
                default:
                {
                    throw new InvalidDataException($"Unknown array type encountered: {type}");
                }
            }
        }

        /// <summary>
        /// Asynchronously reads one entry from a Header Array (HAR) file.
        /// </summary>
        [NotNull]
        [ItemNotNull]
        private static async Task<IHeaderArray> ReadNextAsync(BinaryReader reader)
        {
            return await Task.FromResult(ReadNext(reader));
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

            // Verify array length
            if (reader.ReadInt32() != length)
            {
                throw new InvalidDataException("Initiating and terminating lengths do not match.");
            }

            // Verify the padding
            if (BitConverter.ToInt32(data, 0) != 0x20_20_20_20)
            {
                throw new InvalidDataException("Failed to find expected padding of '0x20_20_20_20'");
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

            // Verify the length of the header
            if (length != reader.ReadInt32())
            {
                throw new InvalidDataException("Initiating and terminating lengths do not match.");
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
            
            //// Read how many items are in the array
            //int count = BitConverter.ToInt32(descriptionBuffer, description.Length - 4 - 4);

            //// Read how long each element is
            //int size = BitConverter.ToInt32(descriptionBuffer, description.Length - 4);

            return (description, header, sparse, type, dimensions);
        }

        private static (float[] Data, string[][] Sets) GetReArray(BinaryReader reader, bool sparse)
        {
            // read dimension array
            byte[] dimensions = InitializeArray(reader);

            // number of labels?
            int a = BitConverter.ToInt32(dimensions, 0);

            if (BitConverter.ToInt32(dimensions, 4) != -1)
            {
                if (a != 0 && BitConverter.ToInt32(dimensions, 4) != 1)
                {
                    throw new InvalidDataException("Expected 0xFF_FF_FF_FF.");
                }
            }

            // number of labels...again?
            int c = BitConverter.ToInt32(dimensions, 8);

            // Read coefficient
            string coefficient = Encoding.ASCII.GetString(dimensions, 12, 12).Trim();

            if (BitConverter.ToInt32(dimensions, 24) != -1)
            {
                if (a != 0 && BitConverter.ToInt32(dimensions, 4) != 1)
                {
                    throw new InvalidDataException("Expected 0xFF_FF_FF_FF.");
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

            string[][] sets = new string[setNames.Length][];

            for (int i = 0; i < setNames.Length; i++)
            {
                sets[i] = labelStrings[i];
            }
            
            float[] data = sparse ? GetReSparseArray(reader, sets.Aggregate(1, (current, next) => current * next.Length)) : GetReFullArray(reader);

            if (!sets.Any())
            {
                sets = new string[][]
                {
                    new string[] { coefficient }
                };
            }

            return (data, sets);
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
                test = CalculateNextArraySegment(reader, out float[] floats);
                Array.Copy(floats, 0, results, counter, floats.Length);
                counter += floats.Length;
            }

            return results;
        }

        private static bool CalculateNextArraySegment(BinaryReader reader, out float[] segment)
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
        private static float[] GetRlArray(BinaryReader reader)
        {
            // read dimension array
            byte[] dims = InitializeArray(reader);
            int countFromEndOfThisHeaderArray = BitConverter.ToInt32(dims, 0);
            int dimensionLimit = BitConverter.ToInt32(dims, 4);
            int[] dimensions = new int[dimensionLimit];
            for (int i = 0; i < dimensions.Length; i++)
            {
                dimensions[i] = BitConverter.ToInt32(dims, 8 + 4 * i);
            }

            byte[] dimDefinitons = InitializeArray(reader);
            int x0 = dimensions.Aggregate(1, (current, next) => current * next);
            int[][] dimDescriptions = new int[x0][];
            for (int i = 0; i < x0; i++)
            {
                dimDescriptions[i] = new int[dimDefinitons.Length / 4];
                for (int j = 4; j < dimDefinitons.Length / 4; j++)
                {
                    dimDescriptions[i][j] = BitConverter.ToInt32(dimDefinitons, j);
                }
            }

            byte[] data = InitializeArray(reader);
            int dataDim = BitConverter.ToInt32(data, 0);
            byte[][] record = new byte[2][];

            record[0] = dims;
            record[1] = data.Skip(4).ToArray();

            float[] floats = new float[x0];

            // Read records
            for (int i = 0; i < x0; i++)
            {
                floats[i] = BitConverter.ToSingle(record[1], i * 4);
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

        #endregion

        /// <summary>
        /// Custom converter from JSON <see cref="IHeaderArray{TValue}"/>.
        /// </summary>
        /// <typeparam name="TValue">
        /// The type of data in the array.
        /// </typeparam>
        internal sealed class HeaderArrayJsonConverter<TValue> : JsonConverter
        {
            /// <summary>
            /// True if the type implements <see cref="IHeaderArray"/>; otherwise false.
            /// </summary>
            /// <param name="objectType">
            /// The type to compare.
            /// </param>
            public override bool CanConvert(Type objectType)
            {
                return typeof(IHeaderArray).GetTypeInfo().IsAssignableFrom(objectType);
            }
            
            /// <summary>
            /// Reads the JSON representation of the object.
            /// </summary>
            /// <param name="reader">
            /// The <see cref="JsonReader"/> to read from.
            /// </param>
            /// <param name="objectType">
            /// Type of the object.
            /// </param>
            /// <param name="existingValue">
            /// The existing value of object being read.
            /// </param>
            /// <param name="serializer">
            /// The calling serializer.
            /// </param>
            /// <returns>
            /// The object value.
            /// </returns>
            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                JObject jObject = JObject.Load(reader);

                IEnumerable<KeyValuePair<KeySequence<string>, TValue>> items =
                    jObject["Entries"].Values<JToken>()
                                       .Select(
                                           x => new
                                           {
                                               Key = KeySequence<string>.Parse(x),
                                               Value = x.Single().Value<TValue>()
                                           })
                                       .Select(
                                           x =>
                                               new KeyValuePair<KeySequence<string>, TValue>(x.Key, x.Value));

                IHeaderArray<TValue> array =
                    new HeaderArray<TValue>(
                        jObject["Header"].Value<string>(),
                        jObject["Description"].Value<string>(),
                        jObject["Type"].Value<string>(),
                        jObject["Dimensions"].Values<int>(),
                        items,
                        jObject["Sets"].Values<JToken>().Select(x => x.Values<string>()));

                return array;
            }

            /// <summary>
            /// Writes the JSON representation of the object.
            /// </summary>
            /// <param name="writer">
            /// The <see cref="JsonWriter"/> to write to.
            /// </param>
            /// <param name="value">
            /// The value.
            /// </param>
            /// <param name="serializer">
            /// The calling serializer.
            /// </param>
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                throw new NotSupportedException();
            }
        }
    }
}