using System;
using HeaderArrayConverter;

namespace HeaderArrayConsole
{
    public static class Program
    {
        public static void Main()
        {
            //const string file = "c:\\users\\adren\\desktop\\gtap source\\new2.har";
            const string file = "C:\\Users\\adren\\Desktop\\GTAP source\\US_3x3_BaseData.har";
            //const string file = "C:\\Users\\adren\\Desktop\\GTAP source\\sets.har";
            //const string file = "G:\\data\\Austin D\\GTAP source code\\basedata.har";
            //const string file = "G:\\data\\Austin D\\GTAP source code\\sets.har";

            HeaderArrayFile arrays = HeaderArrayFile.ReadHarFile(file);
            
            Console.WriteLine(arrays);

            //Console.WriteLine(arrays["TVOM"]["AGR"]);

            //Console.WriteLine(arrays["TVOM"]["AGR", "USA"]);

            //Console.WriteLine(arrays["TVOM"]["AGR"]["AGR", "ROW"]);
            
            //foreach (KeyValuePair<KeySequence<string>, object> item in arrays["TVOM"]["AGR"])
            //{
            //    Console.WriteLine(item);
            //}
            //foreach (KeyValuePair<KeySequence<string>, object> item in arrays["TVOM"]["AGR", "USA"])
            //{
            //    Console.WriteLine(item);
            //}
            //foreach (KeyValuePair<KeySequence<string>, object> item in arrays["TVOM"]["AGR"]["AGR", "USA"])
            //{
            //    Console.WriteLine(item);
            //}

            //foreach (KeyValuePair<KeySequence<string>, float> item in arrays["TVOM"].As<float>()["AGR"])
            //{
            //    Console.WriteLine(item);
            //}
            //foreach (KeyValuePair<KeySequence<string>, float> item in arrays["TVOM"].As<float>()["AGR", "USA"])
            //{
            //    Console.WriteLine(item);
            //}
            //foreach (KeyValuePair<KeySequence<string>, float> item in arrays["TVOM"].As<float>()["AGR"]["AGR", "USA"])
            //{
            //    Console.WriteLine(item);
            //}

            HeaderArrayFile.WriteHarx("c:\\users\\adren\\desktop\\test2.harx", arrays);

            Console.WriteLine(HeaderArrayFile.ReadHarxFile("c:\\users\\adren\\desktop\\test2.harx"));

            Console.ReadLine();
        }
    }
}