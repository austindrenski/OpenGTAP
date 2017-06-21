using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace HeaderArrayConverter.Types
{
    [PublicAPI]
    public class ModelCommandFile
    {
        /// <summary>
        /// 
        /// </summary>
        public static Regex VariableRegex { get; } = new Regex("\\b(\\w+)\\([,\"]{0,2}(\\w+)[,\"]{0,2}(?:[,\"]{0,2}(\\w+)[,\"]{0,2})*\\)\\B", RegexOptions.Compiled | RegexOptions.ExplicitCapture);

        public ModelCommandFile(IHeaderArray<string> commandFile)
        {
            IEnumerable<Match> s =
                commandFile.GetLogicalValuesEnumerable()
                           .AsParallel()
                           .AsOrdered()
                           .SelectMany(
                               x =>
                                   VariableRegex.Matches(x).Cast<Match>());

            foreach (Match i in s)
            {
                foreach (Group j in i.Groups)
                {
                    foreach (Capture c in j.Captures)
                    {
                        Console.WriteLine(c.Value);
                    }
                }
            }
        }
    }
}
