using System;
using System.Threading.Tasks;
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
            
            HeaderArrayFile arrays = SolutionFile.BinaryReader.Read(input);

            //HeaderArrayFile arrays = HeaderArrayFile.BinaryReader.Read(input);

            Console.Out.WriteLineAsync(arrays.ToString());

            Task writeJ = HeaderArrayFile.JsonWriter.WriteAsync(jsonOutput, arrays);

            Task writeB = HeaderArrayFile.BinaryWriter.WriteAsync(binaryOutput, arrays);

            //Console.WriteLine(HeaderArrayFile.JsonReader.Read(jsonOutput));

            //arrays.ValidateSets(Console.Out);

            Task.WaitAll(writeJ, writeB);
            Console.Beep(3, 200);
            Console.ReadLine();
        }
    }
}