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

            //string input = $"{directory}\\sets.har";
            string input = $"{directory}\\basedata.har";
            //string input = $"{directory}\\isep.har";
            //string input = $"{directory}\\gsddat.har";
            //string input = $"{directory}\\samdata.har";

            string output = $"{directory}\\test3.harx";
            
            HeaderArrayFile arrays = HeaderArrayFile.BinaryReader.Read(input);

            Console.WriteLine(arrays);
          
            HeaderArrayFile.JsonWriter.Write(output, arrays);

            Console.WriteLine(HeaderArrayFile.JsonReader.Read(output));

            arrays.ValidateSets(Console.Out);

            Console.ReadLine();
        }
    }
}