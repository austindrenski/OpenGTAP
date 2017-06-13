using System;
using HeaderArrayConverter;

namespace HeaderArrayConsole
{
    public static class Program
    {
        public static void Main()
        {
            //const string directory = "c:\\users\\adren\\desktop\\gtap source";
            const string directory = "g:\\data\\austin d\\gtap source code";

            //string input = $"{directory}\\sets.har";
            //string input = $"{directory}\\basedata.har";
            //string input = $"{directory}\\isep.har";
            string input = $"{directory}\\gsddat.har";
            //string input = $"{directory}\\laborfd.sl4";
            //string input = $"{directory}\\reBAS11.sl4";

            string jsonOutput = $"{directory}\\test4.harx";
            string binaryOutput = $"{directory}\\test4.har";

            HeaderArrayFile arrays = HeaderArrayFile.BinaryReader.Read(input);

            //Console.WriteLine(arrays);

            //HeaderArrayFile.JsonWriter.Write(jsonOutput, arrays);

            HeaderArrayFile.BinaryWriter.Write(binaryOutput, arrays);

            //Console.WriteLine(HeaderArrayFile.JsonReader.Read(jsonOutput));

            //arrays.ValidateSets(Console.Out);

            Console.ReadLine();
        }
    }
}