using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace HeaderArrayConverter
{
    /// <summary>
    /// Represents one entry from a Header Array (HAR) file.
    /// </summary>
    [PublicAPI]
    public abstract class HeaderArray
    {
        /// <summary>
        /// The four character identifier for this <see cref="HeaderArray"/>.
        /// </summary>
        [NotNull]
        public string Header { get; }

        /// <summary>
        /// The long name description of the <see cref="HeaderArray"/>.
        /// </summary>
        [CanBeNull]
        public string Description { get; }
       
        /// <summary>
        /// The type of element stored in the array.
        /// </summary>
        [NotNull]
        public string Type { get; }

        /// <summary>
        /// The dimensions of the array.
        /// </summary>
        public ImmutableArray<int> Dimensions { get; }

        /// <summary>
        /// The sets defined on the array.
        /// </summary>
        public (string Labels, string[] Names)[] Sets { get; }

        /// <summary>
        /// Represents one entry from a Header Array (HAR) file.
        /// </summary>
        /// <param name="header">
        /// The four character identifier for this <see cref="HeaderArray"/>.
        /// </param>
        /// <param name="description">
        /// The long name description of the <see cref="HeaderArray"/>.
        /// </param>
        /// <param name="type">
        /// The type of element stored in the array.
        /// </param>
        /// <param name="dimensions">
        /// The dimensions of the array.
        /// </param>
        /// <param name="sets">
        /// The sets defined on the array.
        /// </param>
        protected HeaderArray([NotNull] string header, [CanBeNull] string description, [NotNull] string type, [NotNull] int[] dimensions, [NotNull] (string, string[])[] sets)
        {
            if (header is null)
            {
                throw new ArgumentNullException(nameof(header));
            }
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            if (dimensions is null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            if (sets is null)
            {
                throw new ArgumentNullException(nameof(sets));
            }

            Header = header;
            Description = description?.Trim('\u0000', '\u0002', '\u0020');
            Dimensions = dimensions.ToImmutableArray();
            Sets = sets;
            Type = type;
        }
        
        /// <summary>
        /// Returns a string representation of the contents of this <see cref="HeaderArray"/>.
        /// </summary>
        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.AppendLine($"{nameof(Header)}: {Header}");
            stringBuilder.AppendLine($"{nameof(Description)}: {Description}");
            stringBuilder.AppendLine($"{nameof(Type)}: {Type}");
            stringBuilder.AppendLine($"{nameof(Sets)}: {string.Join(" * ", Sets.Where(x => x.Item1 != null).Select(x => $"{{ {string.Join(", ", x.Item2)} }}"))}");

            Console.WriteLine(string.Join("; ", OuterCrossJoin(Sets)));

            //stringBuilder.AppendLine($"{nameof(Dimensions)}: {Dimensions.Aggregate(string.Empty, (current, next) => $"{current}[{next}]")}");
            return stringBuilder.ToString();
        }

        private static IEnumerable<string> OuterCrossJoin(IEnumerable<(string Labels, string[] Names)> source)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            source = source as (string Labels, string[] Names)[] ?? source.ToArray();

            if (!source.Any())
            {
                yield break;
            }

            if (source.Count() == 2)
            {
                foreach (string item1 in source.Skip(1).First().Names)
                {
                    foreach (string item0 in source.First().Names)
                    {
                        yield return $"{item0} * {item1}";
                    }
                }
            }

            if (source.Count() == 3)
            {
                foreach (string item2 in source.Skip(2).First().Names)
                {
                    foreach (string item1 in source.Skip(1).First().Names)
                    {
                        foreach (string item0 in source.First().Names)
                        {
                            yield return $"{item0} * {item1} * {item2}";
                        }
                    }
                }
            }

            if (source.Count() == 4)
            {
                foreach (string item3 in source.Skip(2).First().Names)
                {
                    foreach (string item2 in source.Skip(2).First().Names)
                    {
                        foreach (string item1 in source.Skip(1).First().Names)
                        {
                            foreach (string item0 in source.First().Names)
                            {
                                yield return $"{item0} * {item1} * {item2} * {item3}";
                            }
                        }
                    }
                }
            }
        }

        private static IEnumerable<string> OuterCrossJoinHelper(IEnumerable<(string Labels, string[] Names)> source)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (source.Count() > 1)
            {
                foreach (string item in OuterCrossJoinHelper(source.Skip(1)))
                {
                    yield return item;
                }
            }
            else
            {
                foreach (string item in source.Skip(1).FirstOrDefault().Names ?? Enumerable.Empty<string>())
                {
                    yield return item;
                }
            }
        }


        /// <summary>
        /// Reads one entry from a Header Array (HAR) file.
        /// </summary>
        [NotNull]
        public static HeaderArray Read(BinaryReader reader)
        {
            (string description, string header, bool sparse, string type, int[] dimensions) = GetDescription(reader);

            switch (type)
            {
                case "1C":
                {
                    string[] strings = GetStringArray(reader);
                    return new HeaderArray<string>(header, description, type, dimensions, strings, new(string, string[])[0]);
                    }
                case "RE":
                {
                    (float[] floats, (string Labels, string[] Names)[] sets) = GetReArray(reader, sparse);
                    return new HeaderArray<float>(header, description, type, dimensions, floats, sets);
                }
                case "RL":
                {
                    float[] floats = GetRlArray(reader);
                    return new HeaderArray<float>(header, description, type, dimensions, floats, new (string, string[])[0]);
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
        public static async Task<HeaderArray> ReadAsync(BinaryReader reader)
        {
            return await Task.FromResult(Read(reader));
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

        private static (float[] Data, (string Label, string[] Names)[] Sets) GetReArray(BinaryReader reader, bool sparse)
        {
            // read dimension array
            byte[] dimensions = InitializeArray(reader);

            // number of labels?
            int a = BitConverter.ToInt32(dimensions, 0);

            if (BitConverter.ToInt32(dimensions, 4) != -1)
            {
                throw new InvalidDataException("Expected 0xFF_FF_FF_FF .");
            }

            // number of labels...again?
            int c = BitConverter.ToInt32(dimensions, 8);

            // Read coefficient
            string coefficient = Encoding.ASCII.GetString(dimensions, 12, 12);

            if (BitConverter.ToInt32(dimensions, 24) != -1)
            {
                throw new InvalidDataException("Expected 0xFF_FF_FF_FF .");
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

            if (c - a == 1)
            {
                setNames = setNames.Append(setNames.LastOrDefault()).ToArray();
                labelStrings = labelStrings.Append(labelStrings.LastOrDefault()).ToArray();
            }

            (string, string[])[] sets = new (string, string[])[setNames.Length];

            for (int i = 0; i < setNames.Length; i++)
            {
                sets[i] = (setNames[i], labelStrings[i]);
            }
            
            float[] data = sparse ? GetReSparseArray(reader) : GetReFullArray(reader);

            return (data, sets);
        }

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
                    strings[item] = Encoding.ASCII.GetString(record[item]);
                }
            }

            return strings;
        }
    }
}