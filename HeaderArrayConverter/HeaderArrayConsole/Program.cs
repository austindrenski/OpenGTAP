using System;
using HeaderArrayConverter;

namespace HeaderArrayConsole
{
    public static class Program
    {
        public static void Main()
        {
            const string directory = "c:\\users\\adren\\desktop\\gtap source";
            //const string directory = "g:\\data\\austin d\\gtap source code";

            //string harFile = $"{directory}\\sets.har";
            //string harFile = $"{directory}\\basedata.har";
            string harFile = $"{directory}\\isep.har";
            //string harFile = $"{directory}\\gsddat.har";
            //string harFile = $"{directory}\\samdata.har";

            string harxFile = $"{directory}\\test3.harx";
            
            HeaderArrayFile arrays = HeaderArray.ReadHarFile(harFile);

            Console.WriteLine(arrays);
            
            arrays.WriteHarx(harxFile);

            Console.WriteLine(HeaderArray.ReadHarxFile(harxFile));

            Console.ReadLine();
        }
    }
}