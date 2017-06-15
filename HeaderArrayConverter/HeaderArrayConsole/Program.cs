using HeaderArrayConverter;

namespace HeaderArrayConsole
{
    public static class Program
    {
        public static void Main()
        {
            const string directory = "c:\\users\\adren\\desktop\\gtap source\\har testing files";
            //const string directory = "g:\\data\\austin d\\gtap source code\\har testing files";

            //string input = $"{directory}\\sets original.har";
            //string input = $"{directory}\\basedata original.har";
            //string input = $"{directory}\\laborfd original.sl4";
            //string input = $"{directory}\\laborfd original.sol";
            //string input = $"{directory}\\reBAS11\\reBAS11 (har) original.har";
            //string input = $"{directory}\\reBAS11\\reBAS11 (slc) original.slc";
            string input = $"{directory}\\reBAS11\\reBAS11 (sl4) original.sl4";

            string jsonOutput = $"{directory}\\test6.harx";
            string binaryOutput = $"{directory}\\test6.har";

            HeaderArrayFile arrays = HeaderArrayFile.BinaryReader.Read(input);

            //Console.WriteLine(arrays);

            //HeaderArrayFile.JsonWriter.Write(jsonOutput, arrays);

            HeaderArrayFile.BinaryWriter.Write(binaryOutput, arrays);

            //Console.WriteLine(HeaderArrayFile.JsonReader.Read(jsonOutput));

            //arrays.ValidateSets(Console.Out);

            //Console.ReadLine();
        }
    }
}