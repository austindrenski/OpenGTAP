using System;
using System.IO;
using System.Linq;
using System.Text;
using HeaderArrayConverter;

namespace HeaderArrayConsole
{
    public static class Program
    {
        public static void Main()
        {
            //const string file = "C:\\Users\\adren\\Desktop\\GTAP source\\sets.har";
            const string file = "G:\\data\\Austin D\\GTAP source code\\sets.har";

            byte[] bytes = File.ReadAllBytes(file);

            Console.WriteLine(nameof(Encoding.ASCII));
            Console.WriteLine(Encoding.ASCII.GetString(bytes));
            Console.WriteLine("END");
            Console.WriteLine();

            using (BinaryReader reader = new BinaryReader(File.Open(file, FileMode.Open, FileAccess.Read), Encoding.ASCII))
            {
                while (reader.BaseStream.Position != reader.BaseStream.Length)
                {
                    Console.WriteLine("-----------------------------------------------");

                    string header = GetHeader(reader);

                    HeaderArrayInfo info = GetDescription(reader);

                    HeaderArray headerArray = GetArray(reader, info, header);

                    Console.WriteLine(headerArray);
                    
                    for (int i = 0; i < headerArray.Array.Length; i++)
                    {
                        Console.Write($"[{i}]: ");
                        if (info.Type == "1C")
                        {
                            Console.WriteLine(Encoding.ASCII.GetString(headerArray.Array[i].ToArray()));
                            continue;
                        }
                        Console.WriteLine(BitConverter.ToSingle(headerArray.Array[i].ToArray(), 0));
                    }
                }
            }

            Console.ReadLine();
        }

        private static string GetHeader(BinaryReader reader)
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

            return header;
        }

        private static HeaderArrayInfo GetDescription(BinaryReader reader)
        {
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
            string description = Encoding.ASCII.GetString(descriptionBuffer, 10, 74);

            // Read how many items are in the array
            int count = BitConverter.ToInt32(descriptionBuffer, 84);

            // Read how long each element is
            int size = BitConverter.ToInt32(descriptionBuffer, 88);

            return new HeaderArrayInfo(count, description, size, sparse, type);
        }

        private static (byte[][] Array, int X0, int X1, int X2) GetArray(BinaryReader reader, bool real)
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

            // Read item dimensions
            int x0 = BitConverter.ToInt32(data, 4);
            int x1 = BitConverter.ToInt32(data, 8);
            int x2 = data.Length > 12 ? BitConverter.ToInt32(data, 12) : 1;

            int chunkSize = (arrayLengthInBytes - (real ? 8 : 16)) / (x2 > 0 ? x2 : 1);

            byte[][] record = new byte[x2][];

            // Read records
            for (int i = 0; i < x2; i++)
            {
                record[i] = data.Skip(real ? 8 : 16).Skip(i * chunkSize).Take(chunkSize).ToArray();
            }

            return (record, x0, x1, x2);
        }

        private static HeaderArray GetArray(BinaryReader reader, HeaderArrayInfo info, string header)
        {
            (byte[][] array, int x0, int x1, int x2) = GetArray(reader, info.Type == "RE");

            while (reader.PeekChar() != 4 && reader.BaseStream.Position != reader.BaseStream.Length)
            {
                array = array.Concat(GetArray(reader, info.Type == "RE").Array).ToArray();
            }

            return new HeaderArray(header, info, x0, x1, x2, array);
        }
    }
}