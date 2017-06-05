using System;
using System.Collections.Generic;
using System.Linq;
using HeaderArrayConverter;
// ReSharper disable UnusedVariable

namespace HeaderArrayConsole
{
    public static class Program
    {
        public static void Main()
        {
            //const string file = "C:\\Users\\adren\\Desktop\\GTAP source\\US_3x3_BaseData.har";
            //const string file = "C:\\Users\\adren\\Desktop\\GTAP source\\sets.har";
            const string file = "G:\\data\\Austin D\\GTAP source code\\basedata.har";
            //const string file = "G:\\data\\Austin D\\GTAP source code\\sets.har";

            // Reads the full contents of a HAR file into memory.
            HeaderArrayFile arrays = HeaderArrayFile.Read(file);
            
            // The HeaderArrayFile type handles the formatting of internal objects. Just pass it to the standard output stream.
            Console.WriteLine(arrays);

            // Header arrays are indexed by the 4-character header.
            IHeaderArray tvom = arrays["TVOM"];

            // This array treats its entries as floats. If TVOM is a string array, an error is thrown.
            IHeaderArray<float> tvomWithType = arrays["TVOM"].As<float>();

            // This subset contains the untyped entries of TVOM where the first set element is "AGR".
            ImmutableSequenceDictionary<string, object> subset = tvom["AGR"];

            // This subset contains the typed entries of TVOM where the first set element is "AGR", and the second set element is "USA".
            ImmutableSequenceDictionary<string, float> subsetWithType = tvomWithType["AGR", "USA"];

            // Subsets handle their own formatting, so just pass one to the standard output to print to the console.
            Console.WriteLine(subset);
            Console.WriteLine(subsetWithType);

            // This will subset the first subset for entries where the first set element is "AGR" and the second is "USA".
            ImmutableSequenceDictionary<string, object> subsetFromSubset = subset["AGR", "USA"];

            // This just creates a clone of subsetWithType because it only contains elements defined by "AGR" and "USA".
            ImmutableSequenceDictionary<string, float> subsetFromSubsetWithType = subsetWithType["AGR", "USA"];

            // If we know that the subset is fully specified, then .Single() returns a KeyValuePair<string, float> and .Value accesses the float value.
            float a = (float)subsetFromSubset.Single().Value;
            float b = subsetFromSubsetWithType.Single().Value;

            // The preceding steps can be combined into a chained call.
            float c = (float) arrays["TVOM"]["AGR"]["AGR", "USA"].Single().Value;
            float d = arrays["TVOM"].As<float>()["AGR", "USA"].Single().Value;


            Console.WriteLine(arrays["TVOM"]["AGR", "USA"]);

            Console.WriteLine(arrays["TVOM"]["AGR"]["AGR", "ROW"]);

            Console.WriteLine(arrays["TVOM"]["AGR"]);

            foreach (KeyValuePair<KeySequence<string>, object> item in arrays["TVOM"]["AGR", "USA"])
            {
                Console.WriteLine(item);
            }

            foreach (KeyValuePair<KeySequence<string>, object> item in arrays["TVOM"]["AGR", "ROW"])
            {
                Console.WriteLine(item);
            }

            foreach (KeyValuePair<KeySequence<string>, object> item in arrays["TVOM"]["AGR"])
            {
                Console.WriteLine(item);
            }

            foreach (KeyValuePair<KeySequence<string>, float> item in arrays["TVOM"].As<float>()["AGR", "USA"])
            {
                Console.WriteLine(item);
            }

            foreach (KeyValuePair<KeySequence<string>, float> item in arrays["TVOM"].As<float>()["AGR", "ROW"])
            {
                Console.WriteLine(item);
            }

            foreach (KeyValuePair<KeySequence<string>, float> item in arrays["TVOM"].As<float>()["AGR"])
            {
                Console.WriteLine(item);
            }

            Console.ReadLine();
        }
    }
}