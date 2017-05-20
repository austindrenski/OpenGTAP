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

                    HeaderArray headerArray =
                        HeaderArray.Read(reader);

                    Console.WriteLine(headerArray);

                    if (headerArray is HeaderArray1C stringHeaderArray)
                    {
                        for (int i = 0; i < stringHeaderArray.Strings.Length; i++)
                        {
                            Console.Write($"[{i}]: ");
                            Console.WriteLine(stringHeaderArray.Strings[i]);
                        }
                    }
                    else if (headerArray is HeaderArrayRE realHeaderArray)
                    {
                        for (int i = 0; i < realHeaderArray.Floats.Length; i++)
                        {
                            Console.Write($"[{i}]: ");
                            Console.WriteLine(string.Join(", ", realHeaderArray.Floats[i]));
                        }
                    }
                }

                Console.ReadLine();
            }
        }
    }
}