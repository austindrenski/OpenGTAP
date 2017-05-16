using System;
using System.Collections.Generic;
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

            //using (BinaryReader reader = new BinaryReader(File.Open(file, FileMode.Open, FileAccess.Read)))
            //{
            //    while (reader.BaseStream.Position != reader.BaseStream.Length)
            //    {
            //        (string Header, byte[] Content) v = HeaderArrayHelper.GetHeader(reader);
            //        (int Count, int Length, byte[] Content) t = HeaderArrayHelper.GetDescriptionFromContent(v.Content);
            //        Console.WriteLine(v.Header);
            //        Console.WriteLine(t.Count);
            //        Console.WriteLine(t.Length);
            //        Console.WriteLine(Encoding.ASCII.GetString(t.Content));
            //    }
            //}

            using (BinaryReader reader = new BinaryReader(File.Open(file, FileMode.Open, FileAccess.Read)))
            {
                while (reader.BaseStream.Position != reader.BaseStream.Length)
                {
                    Console.WriteLine("-----------------------------------------------");

                    // Skip header length
                    reader.ReadBytes(4);

                    // Read header
                    char[] header = reader.ReadChars(4);

                    // Skip header terminating length
                    reader.ReadBytes(4);

                    if (new string(header) == "DVER")
                    {
                        Queue<byte> buffer = new Queue<byte>(4);
                        while (!buffer.SequenceEqual(new byte[] { 04, 00, 00, 00 }))
                        {
                            if (buffer.Count == 4)
                            {
                                buffer.Dequeue();
                            }
                            buffer.Enqueue(reader.ReadByte());
                        }
                        reader.BaseStream.Seek(-4, SeekOrigin.Current);
                        continue;
                    }
                    // Skip separator or length => '5C 00 00 00'
                    reader.ReadBytes(4);

                    // Skip 4 spaces => '20 20 20 20'
                    reader.ReadBytes(4);

                    // Read type => '1C', 'RE', etc
                    char[] type = reader.ReadChars(2);

                    // Read length type => 'FULL'
                    char[] lengthType = reader.ReadChars(4);

                    // Read longer name description with limit of 70 characters
                    char[] name = reader.ReadChars(70);

                    // Skip terminating marker '02 00 00 00'
                    reader.ReadBytes(4);

                    // Read how many items are in the array
                    int count = reader.ReadInt32();

                    // Read how long each element is
                    int size = reader.ReadInt32();

                    // Skip separator => '5C 00 00 00'
                    reader.ReadBytes(4);

                    // Read how long each record is => '17 00 00 00'
                    int recordLength = reader.ReadInt32();

                    // Skip padding => '20 20 20 20'
                    reader.ReadBytes(4);

                    // Read item dimensions
                    int dimension0 = reader.ReadInt32();
                    int dimension1 = reader.ReadInt32();
                    int dimension2 = reader.ReadInt32();

                    // Read record
                    byte[][] record = new byte[count][];
                    for (int i = 0; i < count; i++)
                    {
                        record[i] = reader.ReadBytes(size);
                    }
                        
                        


                    
                    // Skip terminating value length => equal to 'lengthValue'
                    reader.ReadInt32();

                    Console.WriteLine($"Header = '{new string(header)}'");
                    Console.WriteLine($"Name = '{new string(name).Trim()}'");
                    Console.WriteLine($"Type = '{new string(type)}'");
                    Console.WriteLine($"LengthType = '{new string(lengthType)}':");
                    Console.WriteLine($"LengthValue = '{recordLength}'");
                    Console.WriteLine($"ItemCount: '{count}'");
                    Console.WriteLine($"ItemSize: '{size}'");
                    Console.WriteLine($"ArrayDimensions: '[{dimension0}][{dimension1}][{dimension2}]'");

                    for (int i = 0; i < count; i++)
                    {
                        Console.Write($"[{i}]: ");
                        Console.WriteLine(Encoding.ASCII.GetString(record[i]));
                    }
                }
            }

            Console.ReadLine();
        }
    }
}