using System;
using System.Collections.Generic;
using System.IO;
using HeaderArrayConverter;

namespace HeaderArrayConsole
{
    public static class Program
    {       
        /// <summary>
        /// Program entry point.
        /// </summary>
        public static void Main()
        {
            Test(Input[1], Output[0], Output[1], TestOptions.All);

            Console.WriteLine(HeaderArray<float>.JsonSchema);

            Console.ReadLine();
        }

        /// <summary>
        /// Input test files. 
        /// </summary>
        private static readonly IReadOnlyDictionary<int, string> Input =
            new Dictionary<int, string>
            {
                // Base
                [0] = $"{Directory.GetCurrentDirectory()}\\base\\sets original.har",
                [1] = $"{Directory.GetCurrentDirectory()}\\base\\basedata original.har",
                [2] = $"{Directory.GetCurrentDirectory()}\\base\\baserate original.har",
                [3] = $"{Directory.GetCurrentDirectory()}\\base\\baseview original.har",

                // LaborFD
                [4] = $"{Directory.GetCurrentDirectory()}\\laborfd\\laborfd (sl4) original.sl4",
                [5] = $"{Directory.GetCurrentDirectory()}\\laborfd\\laborfd (slc) original.slc",

                // reBAS11
                [6] = $"{Directory.GetCurrentDirectory()}\\reBAS11\\reBAS11 (sl4) original.sl4",
                [7] = $"{Directory.GetCurrentDirectory()}\\reBAS11\\reBAS11 (slc) original.slc"
            };

        /// <summary>
        /// Output test files.
        /// </summary>
        private static readonly IReadOnlyDictionary<int, string> Output =
            new Dictionary<int, string>
            {
                // Binary output
                [0] = $"{Directory.GetCurrentDirectory()}\\test6.har",

                // Json output
                [1] = $"{Directory.GetCurrentDirectory()}\\test6.harx"
            };

        /// <summary>
        /// Indicates the tests to perform.
        /// </summary>
        [Flags]
        private enum TestOptions
        {
            WriteBinary,
            WriteJson,
            ReadJson,
            ValidateSets,
            All = WriteBinary | WriteJson | ReadJson | ValidateSets
        }

        /// <summary>
        /// Executes the selected tests.
        /// </summary>
        /// <param name="input">
        /// The input file.
        /// </param>
        /// <param name="binaryOutput">
        /// The binary output file.
        /// </param>
        /// <param name="jsonOutput">
        /// The json output file.
        /// </param>
        /// <param name="option">
        /// The bit flags indicating which tests to perform.
        /// </param>
        private static void Test(string input, string binaryOutput, string jsonOutput, TestOptions option)
        {
            Console.WriteLine($"Reading {nameof(input)} with {nameof(HeaderArrayFile.Read)} at {DateTime.Now}.");

            HeaderArrayFile arrays = HeaderArrayFile.Read(input);
  
            if (option.HasFlag(TestOptions.WriteBinary))
            {
                Console.WriteLine($"Writing {nameof(arrays)} to {nameof(binaryOutput)} with {nameof(HeaderArrayFile.BinaryWriter)} at {DateTime.Now}.");
                HeaderArrayFile.BinaryWriter.Write(binaryOutput, arrays);
            }

            if (option.HasFlag(TestOptions.WriteJson))
            {
                Console.WriteLine($"Writing {nameof(arrays)} to {nameof(jsonOutput)} with {nameof(HeaderArrayFile.JsonWriter)} at {DateTime.Now}.");
                HeaderArrayFile.JsonWriter.Write(jsonOutput, arrays);
            }

            if (option.HasFlag(TestOptions.ReadJson))
            {
                Console.WriteLine($"Reading {nameof(jsonOutput)} with {nameof(HeaderArrayFile.JsonReader)} at {DateTime.Now}.");
                HeaderArrayFile.JsonReader.Read(jsonOutput);
            }

            if (option.HasFlag(TestOptions.ValidateSets))
            {
                Console.WriteLine($"Running {nameof(HeaderArray.ValidateSets)} on {nameof(arrays)} at {DateTime.Now}.");
                arrays.ValidateSets(Console.Out);
            }

            Console.WriteLine($"Completed test at {DateTime.Now}");
        }
    }
}