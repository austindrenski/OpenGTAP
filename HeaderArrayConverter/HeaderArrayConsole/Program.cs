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
            const string file = "C:\\Users\\adren\\Desktop\\GTAP source\\sets.har";
            //const string file = "G:\\data\\Austin D\\GTAP source code\\sets.har";

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

                    HeaderArray headerArray = 
                        HeaderArray.Read(reader);

                    Console.WriteLine(headerArray);
                    
                    for (int i = 0; i < headerArray.Array.Length; i++)
                    {
                        Console.Write($"[{i}]: ");
                        if (headerArray.Type == "1C")
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
    }
}