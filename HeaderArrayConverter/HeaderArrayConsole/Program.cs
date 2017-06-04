using System;
using System.Collections.Generic;
using HeaderArrayConverter;

namespace HeaderArrayConsole
{
    public static class Program
    {
        public static void Main()
        {
            const string file = "C:\\Users\\adren\\Desktop\\GTAP source\\US_3x3_BaseData.har";
            //const string file = "C:\\Users\\adren\\Desktop\\GTAP source\\sets.har";
            //const string file = "G:\\data\\Austin D\\GTAP source code\\basedata.har";
            //const string file = "G:\\data\\Austin D\\GTAP source code\\sets.har";

            HeaderArrayFile arrays = HeaderArrayFile.Read(file);

            Console.WriteLine(arrays);

            Console.WriteLine(arrays["TVOM"]["AGR", "USA"]);

            Console.WriteLine(arrays["TVOM"]["AGR", "ROW"]);

            Console.WriteLine(arrays["TVOM"]["AGR"]);

            foreach (object item in arrays["TVOM"]["AGR", "USA"])
            {
                Console.WriteLine(item);
            }

            foreach (object item in arrays["TVOM"]["AGR", "ROW"])
            {
                Console.WriteLine(item);
            }

            foreach (object item in arrays["TVOM"]["AGR"])
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