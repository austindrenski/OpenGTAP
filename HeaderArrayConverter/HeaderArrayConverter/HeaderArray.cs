using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using AD.IO;
using Newtonsoft.Json;
// ReSharper disable UnusedVariable

namespace HeaderArrayConverter
{
    /// <summary>
    /// Utility methods for reading and writing Header Array (HAR) files.
    /// </summary>
    [PublicAPI]
    public static class HeaderArray
    {
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
                    return new HeaderArray<string>(header, description, type, dimensions, strings, Enumerable.Empty<ImmutableOrderedSet<string>>());
                    }
                case "RE":
                {
                    (float[] floats, IEnumerable<ImmutableOrderedSet<string>> sets) = GetReArray(reader, sparse);
                    return new HeaderArray<float>(header, description, type, dimensions, floats, sets);
                }
                case "RL":
                {
                    float[] floats = GetRlArray(reader);
                    return new HeaderArray<float>(header, description, type, dimensions, floats, Enumerable.Empty<ImmutableOrderedSet<string>>());
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

        private static (float[] Data, ImmutableOrderedSet<string>[] Sets) GetReArray(BinaryReader reader, bool sparse)
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

            ImmutableOrderedSet<string>[] sets = new ImmutableOrderedSet<string>[setNames.Length];

            for (int i = 0; i < setNames.Length; i++)
            {
                sets[i] = ImmutableOrderedSet<string>.Create(setNames[i], null, labelStrings[i]);
            }
            
            float[] data = sparse ? GetReSparseArray(reader) : GetReFullArray(reader);

            if (!sets.Any())
            {
                sets = new ImmutableOrderedSet<string>[]
                {
                    ImmutableOrderedSet<string>.Create(coefficient, null, coefficient)
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
            int d0 = BitConverter.ToInt32(meta, 8);
            int d1 = BitConverter.ToInt32(meta, 12);
            int d2 = BitConverter.ToInt32(meta, 16);
            int d3 = BitConverter.ToInt32(meta, 20);
            int d4 = BitConverter.ToInt32(meta, 24);
            int d5 = BitConverter.ToInt32(meta, 28);
            int d6 = BitConverter.ToInt32(meta, 32);

            int count = d0 * d1 * d2 * d3 * d4 * d5 * d6;

            if (count > 0)
            {
                byte[] dimDefinitons = InitializeArray(reader);
                int x0 = BitConverter.ToInt32(dimDefinitons, 0);
                int[][] dimDescriptions = new int[x0][];
                for (int i = 0; i < x0; i++)
                {
                    dimDescriptions[i] = new int[7];
                    dimDescriptions[i][0] = BitConverter.ToInt32(dimDefinitons, 4);
                    dimDescriptions[i][1] = BitConverter.ToInt32(dimDefinitons, 8);
                    dimDescriptions[i][2] = BitConverter.ToInt32(dimDefinitons, 12);
                    dimDescriptions[i][3] = BitConverter.ToInt32(dimDefinitons, 16);
                    dimDescriptions[i][4] = BitConverter.ToInt32(dimDefinitons, 20);
                    dimDescriptions[i][5] = BitConverter.ToInt32(dimDefinitons, 24);
                    dimDescriptions[i][6] = BitConverter.ToInt32(dimDefinitons, 28);
                }
            }

            byte[] data = InitializeArray(reader);
            int dataDim = BitConverter.ToInt32(data, 0);

            float[] floats = new float[count];

            // Read records
            for (int i = 0; i < count; i++)
            {
                floats[i] = BitConverter.ToSingle(data, 4 + i * 4);
            }

            return floats;
        }

        [NotNull]
        private static float[] GetReSparseArray(BinaryReader reader)
        {
            byte[] meta = InitializeArray(reader);

            int valueCount = BitConverter.ToInt32(meta, 0);
            int idk0 = BitConverter.ToInt32(meta, 4);
            int idk1 = BitConverter.ToInt32(meta, 8);
            
            byte[] data = InitializeArray(reader);
            int numberOfVectors = BitConverter.ToInt32(data, 0);
            int totalCountOfEntries = BitConverter.ToInt32(data, 4);
            int maxEntriesPerVector= BitConverter.ToInt32(data, 8);

            int[] indices = new int[totalCountOfEntries];
            for (int i = 0; i < totalCountOfEntries; i++)
            {
                indices[i] = BitConverter.ToInt32(data, 12 + i * 4) - 1;
            }

            byte[] record = data.Skip(12 + totalCountOfEntries * 4).ToArray();

            float[] floats = new float[valueCount * valueCount + idk0 + idk1];

            // Read records
            for (int i = 0; i < totalCountOfEntries; i++)
            {
                floats[indices[i]] = BitConverter.ToSingle(record, i * 4);
            }

            return floats;
        }

        [NotNull]
        private static float[] GetRlArray(BinaryReader reader)
        {
            // read dimension array
            byte[] dimensions = InitializeArray(reader);
            int countFromEndOfThisHeaderArray = BitConverter.ToInt32(dimensions, 0);
            int dimensionLimit = BitConverter.ToInt32(dimensions, 4);
            int d0 = BitConverter.ToInt32(dimensions, 8);
            int d1 = BitConverter.ToInt32(dimensions, 12);
            int d2 = BitConverter.ToInt32(dimensions, 16);
            int d3 = BitConverter.ToInt32(dimensions, 20);
            int d4 = BitConverter.ToInt32(dimensions, 24);
            int d5 = BitConverter.ToInt32(dimensions, 28);
            int d6 = BitConverter.ToInt32(dimensions, 32);

            byte[] dimDefinitons = InitializeArray(reader);
            int x0 = d0 * d1 * d2 * d3 * d4 * d5 * d6; 
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

            record[0] = dimensions;
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
    }
}