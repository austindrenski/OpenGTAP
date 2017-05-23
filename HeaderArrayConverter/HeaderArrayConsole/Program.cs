using System;
using System.IO;
using System.Text;
using HeaderArrayConverter;

namespace HeaderArrayConsole
{
    public static class Program
    {
        public static void Main()
        {
            const string file = "C:\\Users\\adren\\Desktop\\GTAP source\\US_3x3_BaseData.har";
            //const string file = "C:\\Users\\adren\\Desktop\\GTAP source\\sets.har";
            //const string file = "G:\\data\\Austin D\\GTAP source code\\basedata.har";
            //const string file = "G:\\data\\Austin D\\GTAP source code\\sets.har";

            byte[] bytes = File.ReadAllBytes(file);

            Console.WriteLine(nameof(Encoding.ASCII));
            Console.WriteLine(Encoding.ASCII.GetString(bytes));
            Console.WriteLine("END");
            Console.WriteLine();

            using (BinaryReader reader = new BinaryReader(File.Open(file, FileMode.Open, FileAccess.Read)))
            {
                while (reader.BaseStream.Position != reader.BaseStream.Length)
                {
                    Console.WriteLine("-----------------------------------------------");
                    Console.WriteLine(HeaderArray.Read(reader));
                }
                Console.ReadLine();
            }
        }
    }
}