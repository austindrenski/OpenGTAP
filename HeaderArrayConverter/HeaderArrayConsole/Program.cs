using System;
using HeaderArrayConverter;

namespace HeaderArrayConsole
{
    public static class Program
    {
        public static void Main()
        {
            //const string harFile = "C:\\Users\\adren\\Desktop\\GTAP source\\sets.har";
            //const string harFile = "C:\\Users\\adren\\Desktop\\GTAP source\\US_3x3_BaseData.har";
            //const string harFile = "c:\\users\\adren\\desktop\\gtap source\\isep.har";
            //const string harFile = "c:\\users\\adren\\desktop\\gtap source\\samdata.har";

            //const string harFile = "G:\\data\\Austin D\\GTAP source code\\sets.har";
            //const string harFile = "G:\\data\\Austin D\\GTAP source code\\basedata.har";
            //const string harFile = "G:\\data\\Austin D\\GTAP source code\\isep.har";
            const string harFile = "G:\\data\\Austin D\\GTAP source code\\gsddat.har";

            //const string harxFile = "c:\\users\\adren\\desktop\\test3.harx";
            const string harxFile = "c:\\users\\austin.drenski\\desktop\\test3.harx";
            
            HeaderArrayFile arrays = HeaderArray.ReadHarFile(harFile);

            Console.WriteLine(arrays);

            arrays.WriteHarx(harxFile);

            Console.WriteLine(HeaderArray.ReadHarxFile(harxFile));

            Console.ReadLine();
        }
    }
}