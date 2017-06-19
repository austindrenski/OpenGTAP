using System;
using HeaderArrayConverter;
using HeaderArrayConverter.IO;

namespace HeaderArrayConsole
{
    public static class Program
    {
        public static void Main()
        {
            //const string directory = "c:\\users\\adren\\desktop\\gtap source\\har testing files";
            const string directory = "g:\\data\\austin d\\gtap source code\\har testing files";

            //string input = $"{directory}\\base\\sets original.har";
            //string input = $"{directory}\\base\\basedata original.har";
            //string input = $"{directory}\\base\\baserate original.har";
            //string input = $"{directory}\\base\\baseview original.har";

            //string input = $"{directory}\\laborfd\\laborfd (sol) original.sol";
            string input = $"{directory}\\laborfd\\laborfd (sl4) original.sl4";
            //string input = $"{directory}\\laborfd\\laborfd (slc) original.slc";

            //string input = $"{directory}\\reBAS11\\reBAS11 (har) original.har";
            //string input = $"{directory}\\reBAS11\\reBAS11 (sl4) original.sl4";
            //string input = $"{directory}\\reBAS11\\reBAS11 (slc) original.slc";

            string jsonOutput = $"{directory}\\test6.harx";
            string binaryOutput = $"{directory}\\test6.har";

            //// 1. Read the input file with HAR semantics.
            //// 2. Write to binary and json.
            //// 3. Do not validate sets.
            //Test(
            //    input: input,
            //    binaryOutput: binaryOutput,
            //    jsonOutput: jsonOutput,
            //    reader: HeaderArrayFile.BinaryReader,
            //    writeBinary: true,
            //    writeJson: true,
            //    validateSets: false);

            //// 1. Read the input file with HARX semantics.
            //// 2. Write to binary and json.
            //// 3. Do not validate sets.
            //Test(
            //    input: jsonOutput,
            //    binaryOutput: binaryOutput,
            //    jsonOutput: jsonOutput,
            //    reader: HeaderArrayFile.JsonReader,
            //    writeBinary: true,
            //    writeJson: true,
            //    validateSets: false);

            // 1. Read the input file with SL4 semantics.
            // 2. Write to binary and json.
            // 3. Do not validate sets.
            Test(
                input: input,
                binaryOutput: binaryOutput,
                jsonOutput: jsonOutput,
                reader: SolutionFile.BinaryReader,
                writeBinary: true,
                writeJson: true,
                validateSets: false);

            //Console.ReadLine();
        }

        private static void Test(string input, string binaryOutput, string jsonOutput, HeaderArrayReader reader, bool writeBinary, bool writeJson, bool validateSets)
        {
            Console.Out.WriteLineAsync($"Reading {nameof(input)} with {reader.GetType().Name}.");

            HeaderArrayFile arrays = reader.Read(input);
  
            if (writeBinary)
            {
                Console.Out.WriteLineAsync($"Writing {nameof(arrays)} to {nameof(binaryOutput)} with {nameof(HeaderArrayFile.BinaryWriter)}.");
                HeaderArrayFile.BinaryWriter.Write(binaryOutput, arrays);
            }

            if (writeJson)
            {
                Console.Out.WriteLineAsync($"Writing {nameof(arrays)} to {nameof(jsonOutput)} with {nameof(HeaderArrayFile.JsonWriter)}.");
                HeaderArrayFile.JsonWriter.Write(jsonOutput, arrays);
            }

            if (validateSets)
            {
                Console.Out.WriteLineAsync($"Running {nameof(HeaderArray.ValidateSets)} on {nameof(arrays)}.");
                arrays.ValidateSets(Console.Out);
            }
        }
    }
}