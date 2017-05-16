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
            const string file = "C:\\Users\\adren\\Desktop\\GTAP source\\sets.har";
            //const string file = "G:\\data\\Austin D\\GTAP source code\\sets.har";

            byte[] headerToTypeSeparator = new byte[]
            {
                0x04,
                0x00,
                0x00,
                0x00,
                0x5C,
                0x00,
                0x00,
                0x00,
                0x20,
                0x20,
                0x20,
                0x20
            };

            byte[] bytes = File.ReadAllBytes(file);

            Console.WriteLine(nameof(Encoding.ASCII));
            Console.WriteLine(Encoding.ASCII.GetString(bytes));
            Console.WriteLine("END");
            Console.WriteLine();

            using (BinaryReader reader = new BinaryReader(File.Open(file, FileMode.Open, FileAccess.Read)))
            {
                Console.WriteLine("Read four bytes of padding:");
                Console.WriteLine(BitConverter.ToString(reader.ReadBytes(4)));

                Console.WriteLine("Read four char header:");
                Console.WriteLine(reader.ReadChars(4));

                Console.WriteLine("Read eight bytes of padding:");
                Console.WriteLine(BitConverter.ToString(reader.ReadBytes(8)));
                
                Console.WriteLine("Read header type:");
                Console.WriteLine(reader.ReadChars(2));

                Console.WriteLine("Read header length:");
                Console.WriteLine(reader.ReadChars(4));

                Console.WriteLine("Read header long description:");
                Console.WriteLine(reader.ReadChars(70));

                Console.WriteLine("Read twelve bytes of separator:");
                Console.WriteLine(BitConverter.ToString(reader.ReadBytes(12)));

                Console.WriteLine("Read four bytes of separator:");
                Console.WriteLine(BitConverter.ToString(reader.ReadBytes(4)));
                
                Console.WriteLine("Read four bytes of separator:");
                Console.WriteLine(BitConverter.ToString(reader.ReadBytes(4)));
                
                Console.WriteLine("Read four bytes of spacing:");
                Console.WriteLine(BitConverter.ToString(reader.ReadBytes(4)));

                Console.WriteLine("Read four bytes of separator:");
                Console.WriteLine(BitConverter.ToString(reader.ReadBytes(12)));
            }


            Console.ReadLine();
        }
    }
}