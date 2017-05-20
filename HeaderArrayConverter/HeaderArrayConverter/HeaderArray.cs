using System;
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
    public class HeaderArray
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
        /// The total count of elements in the array.
        /// </summary>
        public int Count { get; }
        
        /// <summary>
        /// The size in bytes of each element in the array.
        /// </summary>
        public int Size { get; }

        /// <summary>
        /// True if the array is sparsely populated; otherwise false.
        /// </summary>
        public bool Sparse { get; }

        /// <summary>
        /// The type of element stored in the array.
        /// </summary>
        [NotNull]
        public string Type { get; }

        /// <summary>
        /// The first dimension of the <see cref="HeaderArray"/>.
        /// This represents the number of arrays used to store this <see cref="HeaderArray"/> by Fortran.
        /// </summary>
        public int X0 { get; }

        /// <summary>
        /// The second dimension of the <see cref="HeaderArray"/>.
        /// This represents the total number of elements in the logical array.
        /// </summary>
        public int X1 { get; }

        /// <summary>
        /// The third dimension of the <see cref="HeaderArray"/>.
        /// This represents the maximum number of elements in any of the arrays used to store this <see cref="HeaderArray"/> by Fortran.
        /// </summary>
        public int X2 { get; }

        /// <summary>
        /// The immutable byte array for each record in the logical array.
        /// </summary>
        public ImmutableArray<ImmutableArray<byte>> Array { get; }

        /// <summary>
        /// The decoded form of <see cref="Array"/>
        /// </summary>
        public ImmutableArray<ImmutableArray<float>> Floats { get; }
        
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
        /// <param name="count">
        /// The total count of elements in the array.
        /// </param>
        /// <param name="size">
        /// The size in bytes of each element in the array.
        /// </param>
        /// <param name="sparse">
        /// True if the array is sparsely populated; otherwise false.
        /// </param>
        /// <param name="x0">
        /// The first dimension of the <see cref="HeaderArray"/>.
        /// This represents the number of arrays used to store this <see cref="HeaderArray"/> by Fortran.
        /// </param>
        /// <param name="x1">
        /// The second dimension of the <see cref="HeaderArray"/>.
        /// This represents the total number of elements in the logical array.
        /// </param>
        /// <param name="x2">
        /// The third dimension of the <see cref="HeaderArray"/>.
        /// This represents the maximum number of elements in any of the arrays used to store this <see cref="HeaderArray"/> by Fortran.
        /// </param>
        /// <param name="array">
        /// The immutable byte array for each record in the logical array.
        /// </param>
        public HeaderArray([NotNull] string header, [CanBeNull] string description, [NotNull] string type, int count, int size, bool sparse, int x0, int x1, int x2, [NotNull][ItemNotNull] byte[][] array)
        {
            if (header is null)
            {
                throw new ArgumentNullException(nameof(header));
            }
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            if (array is null)
            {
                throw new ArgumentNullException(nameof(array));
            }
            if (count != x1)
            {
                Console.Error.WriteLineAsync($"Warning => Dimension mismatch: {nameof(count)} is not equal to {nameof(x1)}.");
            }
            if (x0 * x2 < count)
            {
                Console.Error.WriteLineAsync($"Warning => Dimension mismatch: {nameof(count)} is greater than the product of {nameof(x0)} and {nameof(x2)}.");
            }

            Header = header;
            Description = description?.Trim('\u0000', '\u0002', '\u0020');
            Size = size;
            Sparse = sparse;
            Type = type;
            X0 = x0;
            X1 = x1;
            X2 = x2;
            Array = array.Select(x => x.ToImmutableArray()).ToImmutableArray();
        }

        public HeaderArray([NotNull] string header, [CanBeNull] string description, [NotNull] string type, int count, int size, bool sparse, int x0, int x1, int x2, [ItemNotNull, NotNull] byte[][] array, float[][] floats) : this(header, description, type, count, size, sparse, x0, x1, x2, array)
        {
            Floats = floats.Select(x => x.ToImmutableArray()).ToImmutableArray();
        }
        
        /// <summary>
        /// Returns a string representation of the contents of this <see cref="HeaderArray"/>.
        /// </summary>
        public override string ToString()
        {
            return 
                $"{nameof(Header)}: {Header}\r\n" +
                $"{nameof(Description)}: {Description}\r\n" +
                $"{nameof(Type)}: {Type}\r\n" +
                $"{nameof(Count)}: {Count}\r\n" +
                $"{nameof(Size)}: {Size}\r\n" +
                $"{nameof(Sparse)}: {Sparse}\r\n" +
                $"Array: [{X0}][{X1}][{X2}]";
        }

        /// <summary>
        /// Reads one entry from a Header Array (HAR) file.
        /// </summary>
        [NotNull]
        public static HeaderArray Read(BinaryReader reader)
        {
            (int count, string description, string header, int size, bool sparse, string type) = GetDescription(reader);

            int x0;
            int x1;
            int x2;
            byte[][] array;
            float[][] floats = new float[0][];

            if (type == "1C")
            {
                (x0, x1, x2, array) = GetStringArray(reader);
            }
            else if (type == "RE")
            {
                (x0, x1, x2, array, floats) = sparse ? GetReSparseArray(reader) : GetReFullArray(reader);
            }
            else
            {
                (x0, x1, x2, array, floats) = GetRlArray(reader);
            }

            return new HeaderArray(header, description, type, count, size, sparse, x0, x1, x2, array, floats);
        }

        /// <summary>
        /// Asynchronously reads one entry from a Header Array (HAR) file.
        /// </summary>
        [NotNull]
        public static async Task<HeaderArray> ReadAsync(BinaryReader reader)
        {
            return await Task.FromResult(Read(reader));
        }

        private static (int Count, string Description, string Header, int Size, bool Sparse, string Type) GetDescription(BinaryReader reader)
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

            // Read the length of the description
            int descriptionLength = reader.ReadInt32();

            byte[] descriptionBuffer = reader.ReadBytes(descriptionLength);

            // Verify length of the description
            if (reader.ReadInt32() != descriptionLength)
            {
                throw new InvalidDataException("Initiating and terminating lengths do not match.");
            }

            // Skip 4 spaces
            if (BitConverter.ToInt32(descriptionBuffer, 0) != 0x20_20_20_20)
            {
                throw new InvalidDataException("Failed to find expected padding of '0x20_20_20_20'");
            }

            // Read type => '1C', 'RE', etc
            string type = Encoding.ASCII.GetString(descriptionBuffer, 4, 2);

            // Read length type => 'FULL'
            bool sparse = Encoding.ASCII.GetString(descriptionBuffer, 6, 4) != "FULL";

            // Read longer name description with limit of 70 characters
            string description = Encoding.ASCII.GetString(descriptionBuffer, 10, 70);

            // Read how many items are in the array
            int count = BitConverter.ToInt32(descriptionBuffer, descriptionLength - 4 - 4);

            // Read how long each element is
            int size = BitConverter.ToInt32(descriptionBuffer, descriptionLength - 4);

            return (count, description, header, size, sparse, type);
        }

        private static (int X0, int X1, int X2, byte[][] Array, float[][]) GetReFullArray(BinaryReader reader)
        {
            // read dimension array
            byte[] dimensions = InitializeArray(reader);
            // number of labels?
            int a = BitConverter.ToInt32(dimensions, 0);
            // 0xFF_FF_FF_FF == -1
            int b = BitConverter.ToInt32(dimensions, 4);
            // number of labels...again?
            int c = BitConverter.ToInt32(dimensions, 8);
            // this looks like the header for this array
            string setHeader = Encoding.ASCII.GetString(dimensions, 12, 8);
            // this looks like the set name used for this array
            string setName = Encoding.ASCII.GetString(dimensions, 24, dimensions.Length - 24);

            byte[][] labels = new byte[Math.Max(a, 1)][];
            string[][] labelStrings = new string[labels.Length][];
            for (int h = 0; h < labels.Length; h++)
            {
                labels[h] = InitializeArray(reader);
                // get label dimensions
                int labelX0 = BitConverter.ToInt32(labels[h], 0);
                int labelX1 = BitConverter.ToInt32(labels[h], 4);
                int labelX2 = BitConverter.ToInt32(labels[h], 8);
                labelStrings[h] = new string[labelX1];
                for (int i = 0; i < labelX2; i++)
                {
                    labelStrings[h][i] = Encoding.ASCII.GetString(labels[h], i * 12 + 12, 12);
                }
            }

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

            if (a > 0 && labels.Length > 0 && count > 0)
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
            byte[][] record = new byte[3][];

            record[0] = dimensions;
            record[1] = labels.SelectMany(x => x.Select(y => y)).ToArray();
            record[2] = data.Skip(4).ToArray();

            float[][] floats = new float[dataDim][];

            // Read records
            floats[0] = new float[count];
            for (int i = 0; i < count; i++)
            {
                floats[0][i] = BitConverter.ToSingle(record[2], i * 4);
            }

            return (0, 0, 0, record, floats);
        }

        private static (int X0, int X1, int X2, byte[][] Array, float[][]) GetReSparseArray(BinaryReader reader)
        {
            // read dimension array
            byte[] dimensions = InitializeArray(reader);
            // number of labels?
            int a = BitConverter.ToInt32(dimensions, 0);
            // 0xFF_FF_FF_FF == -1
            int b = BitConverter.ToInt32(dimensions, 4);
            // number of labels...again?
            int c = BitConverter.ToInt32(dimensions, 8);
            // this looks like the header for this array
            string setHeader = Encoding.ASCII.GetString(dimensions, 12, 8);
            // this looks like the set name used for this array
            string setName = Encoding.ASCII.GetString(dimensions, 24, dimensions.Length - 24);

            byte[][] labels = new byte[Math.Max(a, 1)][];
            string[][] labelStrings = new string[labels.Length][];
            for (int h = 0; h < labels.Length; h++)
            {
                labels[h] = InitializeArray(reader);
                // get label dimensions
                int labelX0 = BitConverter.ToInt32(labels[h], 0);
                int labelX1 = BitConverter.ToInt32(labels[h], 4);
                int labelX2 = BitConverter.ToInt32(labels[h], 8);
                labelStrings[h] = new string[labelX1];
                for (int i = 0; i < labelX2; i++)
                {
                    labelStrings[h][i] = Encoding.ASCII.GetString(labels[h], i * 12 + 12, 12);
                }
            }

            byte[] meta = InitializeArray(reader);

            int valueCount = BitConverter.ToInt32(meta, 0);
            int idk0 = BitConverter.ToInt32(meta, 4);
            int idk1 = BitConverter.ToInt32(meta, 8);
            
            byte[] data = InitializeArray(reader);
            int dataDim = BitConverter.ToInt32(data, 0);
            int idk3 = BitConverter.ToInt32(data, 4);
            int countOfIndices = BitConverter.ToInt32(data, 8);

            int[] indices = new int[countOfIndices];
            for (int i = 0; i < countOfIndices; i++)
            {
                indices[i] = BitConverter.ToInt32(data, 12 + i * 4) - 1;
            }

            byte[][] record = new byte[3][];
            
            record[0] = dimensions;
            record[1] = labels.SelectMany(x => x.Select(y => y)).ToArray();
            record[2] = data.Skip(12 + countOfIndices * 4).ToArray();

            float[][] floats = new float[dataDim][];

            // Read records
            floats[0] = new float[a * a * c];
            for (int i = 0; i < countOfIndices; i++)
            {
                floats[0][indices[i]] = BitConverter.ToSingle(record[2], i * 4);
            }

            return (0, 0, 0, record, floats);
        }


        private static (int X0, int X1, int X2, byte[][] Array, float[][]) GetRlArray(BinaryReader reader)
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
            int x0 = d0 * d1 * d2 * d3 * d4 * d5 * d6; // BitConverter.ToInt32(dimDefinitons, 0);
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

            float[][] floats = new float[dataDim][];

            // Read records
            floats[0] = new float[x0];
            for (int i = 0; i < x0; i++)
            {
                floats[0][i] = BitConverter.ToSingle(record[1], i * 4);
            }

            return (x0, 0, 0, record, floats);
        }
        
        private static byte[] InitializeArray(BinaryReader reader)
        {
            int length = reader.ReadInt32();

            byte[] data = reader.ReadBytes(length);

            // Verify section length
            if (reader.ReadInt32() != length)
            {
                throw new InvalidDataException("Initiating and terminating lengths do not match.");
            }

            if (BitConverter.ToInt32(data, 0) != 0x20_20_20_20)
            {
                throw new InvalidDataException("Failed to find expected padding of '0x20_20_20_20'");
            }

            return data.Skip(4).ToArray();
        }

        private static (int X0, int X1, int X2, byte[][] Array) GetStringArray(BinaryReader reader)
        {
            // Read the number of bytes stored in each sub-array
            int arrayLengthInBytes = reader.ReadInt32();

            // Buffer data
            byte[] data = reader.ReadBytes(arrayLengthInBytes);

            // Verify section length
            if (reader.ReadInt32() != arrayLengthInBytes)
            {
                throw new InvalidDataException("Initiating and terminating lengths do not match.");
            }

            // Skip 4 spaces
            if (BitConverter.ToInt32(data, 0) != 0x20_20_20_20)
            {
                throw new InvalidDataException("Failed to find expected padding of '0x20_20_20_20'");
            }

            // Read item dimensions => [x0][x1][x2]
            int x0 = BitConverter.ToInt32(data, 4);
            int x1 = BitConverter.ToInt32(data, 8);
            int x2 = BitConverter.ToInt32(data, 12);

            // Find the 
            int elementSize = (arrayLengthInBytes - 16) / (x2 > 0 ? x2 : 1);

            byte[][] record = new byte[x2][];

            // Read records
            for (int i = 0; i < x2; i++)
            {
                record[i] = data.Skip(16).Skip(i * elementSize).Take(elementSize).ToArray();
            }

            return (x0, x1, x2, record);
        }
    }
}