using System;
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
            
            HeaderArrayFile arrays = HeaderArrayFile.Read(file);

            Console.WriteLine(arrays);

            Console.WriteLine(arrays["TVOM"]["AGR"]["USA"]);

            Console.WriteLine(arrays["TVOM"]["AGR", "ROW"]);

            Console.ReadLine();
        }
    }
}