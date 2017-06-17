using System;
using HeaderArrayConverter;

namespace HeaderArrayConsole
{
    public static class Program
    {
        public static void Main()
        {
            const string directory = "c:\\users\\adren\\desktop\\gtap source\\har testing files";
            //const string directory = "g:\\data\\austin d\\gtap source code\\har testing files";

            //string input = $"{directory}\\..\\other\\sets original.har";
            //string input = $"{directory}\\..\\other\\basedata original.har";

            //string input = $"{directory}\\laborfd\\laborfd (sl4) original.sl4";
            //string input = $"{directory}\\laborfd\\laborfd (sol) original.sol";
            //string input = $"{directory}\\laborfd\\laborfd (slc) original.slc";

            //string input = $"{directory}\\reBAS11\\reBAS11 (har) original.har";
            //string input = $"{directory}\\reBAS11\\reBAS11 (slc) original.slc";
            string input = $"{directory}\\reBAS11\\reBAS11 (sl4) original.sl4";

            string jsonOutput = $"{directory}\\test6.harx";
            string binaryOutput = $"{directory}\\test6.har";

            //Console.Out.WriteLineAsync($"Reading {nameof(input)} with {nameof(HeaderArrayFile.BinaryReader)}.");
            //HeaderArrayFile arrays = HeaderArrayFile.BinaryReader.Read(input);

            Console.Out.WriteLineAsync($"Reading {nameof(input)} with {nameof(SolutionFile.BinaryReader)}.");
            HeaderArrayFile arrays = SolutionFile.BinaryReader.Read(input);

            //Console.Out.WriteLineAsync($"Writing {nameof(arrays)} to {nameof(Console)}.");
            //Console.WriteLine(arrays)

            Console.Out.WriteLineAsync($"Writing {nameof(arrays)} to {nameof(jsonOutput)}.");
            HeaderArrayFile.JsonWriter.Write(jsonOutput, arrays);

            Console.Out.WriteLineAsync($"Writing {nameof(arrays)} to {nameof(binaryOutput)}.");
            HeaderArrayFile.BinaryWriter.Write(binaryOutput, arrays);

            //Console.Out.WriteLineAsync($"Reading {nameof(jsonOutput)} with {nameof(HeaderArrayFile.JsonReader)} and writing to {nameof(Console)}.");
            //Console.WriteLine(HeaderArrayFile.JsonReader.Read(jsonOutput));

            //Console.Out.WriteLineAsync($"Running {nameof(HeaderArray.ValidateSets)} on {nameof(arrays)}.");
            //arrays.ValidateSets(Console.Out);

            Console.ReadLine();
        }
    }
}