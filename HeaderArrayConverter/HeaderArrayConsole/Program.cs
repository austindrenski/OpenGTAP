using System;
using System.IO;
using System.Linq;
using System.Text;

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

                    byte[] description = reader.ReadBytes(descriptionLength);


                    // Verify length of the description
                    if (reader.ReadInt32() != descriptionLength)
                    {
                        throw new InvalidDataException("Initiating and terminating lengths do not match.");
                    }

                    // Skip 4 spaces
                    if (BitConverter.ToInt32(description, 0) != 0x20_20_20_20)
                    {
                        throw new InvalidDataException("Failed to find expected padding of '0x20_20_20_20'");
                    }

                    // Read type => '1C', 'RE', etc
                    string type = Encoding.ASCII.GetString(description, 4, 2);

                    // Read length type => 'FULL'
                    string lengthType = Encoding.ASCII.GetString(description, 6, 4);

                    // Read longer name description with limit of 70 characters
                    string name = Encoding.ASCII.GetString(description, 10, 74);

                    // Read how many items are in the array
                    int count = BitConverter.ToInt32(description, 84);

                    // Read how long each element is
                    int size = BitConverter.ToInt32(description, 88);

                    byte[][] record = GetArray(reader, type == "RE");

                    while (reader.PeekChar() != 4 && reader.BaseStream.Position != reader.BaseStream.Length)
                    {
                        record = record.Concat(GetArray(reader, type == "RE")).ToArray();
                    }

                    Console.WriteLine($"Header = '{header}'");
                    Console.WriteLine($"Description = '{name.Trim('\u0000', '\u0002', '\u0020')}'");
                    Console.WriteLine($"Type = '{type}'");
                    Console.WriteLine($"Fill = '{lengthType}':");
                    Console.WriteLine($"Elements: '{count}'");
                    Console.WriteLine($"Size (bytes): '{size}'");

                    for (int i = 0; i < record.Length; i++)
                    {
                        Console.Write($"[{i}]: ");
                        if (type == "1C")
                        {
                            Console.WriteLine(Encoding.ASCII.GetString(record[i]));
                            continue;
                        }
                        Console.WriteLine(BitConverter.ToSingle(record[i], 0));
                    }
                }
            }

            Console.ReadLine();
        }

        private static byte[][] GetArray(BinaryReader reader, bool real)
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
            int dimension0 = BitConverter.ToInt32(data, 4);
            int dimension1 = BitConverter.ToInt32(data, 8);
            int dimension2 = data.Length > 12 ? BitConverter.ToInt32(data, 12) : 1;

            int chunkSize = (arrayLengthInBytes - (real ? 8 : 16)) / (dimension2 > 0 ? dimension2 : 1);

            byte[][] record = new byte[dimension2][];

            // Read records
            for (int i = 0; i < dimension2; i++)
            {
                record[i] = data.Skip((real ? 8 : 16)).Skip(i * chunkSize).Take(chunkSize).ToArray();
            }

            return record;
        }
    }
}