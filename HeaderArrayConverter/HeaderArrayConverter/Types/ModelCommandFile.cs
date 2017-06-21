using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace HeaderArrayConverter.Types
{
    /// <summary>
    /// 
    /// </summary>
    [PublicAPI]
    public partial class ModelCommandFile
    {        
        /// <summary>
        /// 
        /// </summary>
        [NotNull]
        public static Regex RemoveComments { get; } = new Regex("(!.*)", RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);

        /// <summary>
        /// 
        /// </summary>
        [NotNull]
        public static Regex ExogenousVariableSection { get; } = new Regex("(?<=exogenous)([^;]+)*", RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);

        /// <summary>
        /// 
        /// </summary>
        [NotNull]
        public static Regex VariableRegex { get; } = new Regex("\\b(?<variable>\\w+)\\(?(?<indexes>,?\"?\\w+,?\"?)+\\)?\\b", RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);

        /// <summary>
        /// 
        /// </summary>
        [NotNull]
        public IImmutableList<VariableDefinition> VariableDefinitions { get; }

        /// <summary>
        /// 
        /// </summary>
        public string CommandText { get; }

        /// <summary>
        /// 
        /// </summary>
        public string ExogenousCommands { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="commandFile">
        /// 
        /// </param>
        public ModelCommandFile(IHeaderArray<string> commandFile)
        {
            CommandText =
                commandFile.GetLogicalValuesEnumerable()
                           .AsParallel()
                           .AsOrdered()
                           .Select(x => RemoveComments.Replace(x, string.Empty))
                           .Aggregate(
                               new StringBuilder(),
                               (current, next) => current.Append(next + ' '),
                               result => result.ToString());

            ExogenousCommands = ExogenousVariableSection.Match(CommandText).Value;

            VariableDefinitions = SetExogenousVariables(ExogenousCommands).ToImmutableArray();

            foreach (VariableDefinition definition in VariableDefinitions)
            {
                Console.WriteLine(definition);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="exogenous"
        /// 
        /// ></param>
        /// <returns>
        /// 
        /// </returns>
        private static IEnumerable<VariableDefinition> SetExogenousVariables(string exogenous)
        {
            return
                VariableRegex.Matches(exogenous)
                             .Cast<Match>()
                             .Select(
                                 x =>
                                     new VariableDefinition(
                                         x.Groups["variable"].Value,
                                         true,
                                         x.Value.Contains('(')
                                             ? x.Groups["indexes"]
                                                .Captures
                                                .Cast<Capture>()
                                                .Where(y => y.Length > 0)
                                                .Select(y => y.Value.Replace(",", null).Replace("\"", null))
                                             : Enumerable.Empty<string>()));
        }
    }
}