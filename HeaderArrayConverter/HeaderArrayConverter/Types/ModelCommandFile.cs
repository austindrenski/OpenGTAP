using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace HeaderArrayConverter.Types
{
    /// <summary>
    /// Represents the contents of a command (CMF) file.
    /// </summary>
    [PublicAPI]
    public class ModelCommandFile
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
        public static Regex ShockedVariables { get; } = new Regex("(?<=shock)([^;]+)*", RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);

        /// <summary>
        /// 
        /// </summary>
        [NotNull]
        public static Regex VariableRegex { get; } = new Regex("\\b(?<variable>\\w+)\\(?((?<indexes>\"?\\w+\"?)*(?:,)*)*\\)?", RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);

        /// <summary>
        /// 
        /// </summary>
        [NotNull]
        public IImmutableList<VariableDefinition> VariableDefinitions { get; }

        /// <summary>
        /// 
        /// </summary>
        [NotNull]
        public IEnumerable<VariableDefinition> ExogenousDefinitions => VariableDefinitions.Where(x => x.IsExogenous);

        /// <summary>
        /// 
        /// </summary>
        public string CommandText { get; }

        /// <summary>
        /// 
        /// </summary>
        public string ExogenousCommands { get; }

        /// <summary>
        /// Constructs a command file from the command file header.
        /// </summary>
        /// <param name="commandFile">
        /// A header (CMDF) containing the lines of a command file.
        /// </param>
        /// <param name="sets">
        /// 
        /// </param>
        public ModelCommandFile(IHeaderArray<string> commandFile, IEnumerable<KeyValuePair<string, IImmutableList<string>>> sets)
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

            ExogenousCommands =
                ExogenousVariableSection.Matches(CommandText)
                                        .Cast<Match>()
                                        .Aggregate(
                                            new StringBuilder(),
                                            (current, next) => current.Append(next.Value + ' '),
                                            result => result.ToString());

            VariableDefinitions = SetExogenousVariables(ExogenousCommands, sets).ToImmutableArray();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="exogenous"
        /// 
        /// ></param>
        /// <param name="sets">
        /// 
        /// </param>
        /// <returns>
        /// 
        /// </returns>
        private static IEnumerable<VariableDefinition> SetExogenousVariables(string exogenous, IEnumerable<KeyValuePair<string, IImmutableList<string>>> sets)
        {
            sets = sets as KeyValuePair<string, IImmutableList<string>>[] ?? sets.ToArray();

            foreach (Match match in VariableRegex.Matches(exogenous))
            {
                VariableDefinition definition =
                    new VariableDefinition(
                        match.Groups["variable"].Value,
                        true,
                        !match.Value.Contains('(')
                            ? Enumerable.Empty<string>()
                            : match.Groups["indexes"]
                                   .Captures
                                   .Cast<Capture>()
                                   .Where(y => y.Length > 0)
                                   .Select(y => y.Value));

                if (definition.Indexes.All(x => x.StartsWith("\"")))
                {
                    yield return
                        new VariableDefinition(
                            definition.Name,
                            definition.IsExogenous,
                            definition.Indexes.Select(x => x.Replace("\"", null).Replace(",", null)));

                    continue;
                }

                foreach (KeyValuePair<string, IImmutableList<string>> item in sets.Where(x => definition.Indexes.Contains(x.Key)))
                {
                    foreach (string entry in item.Value)
                    {
                        yield return new VariableDefinition(definition.Name, true, definition.Indexes.Replace(item.Key, entry).Select(x => x.Replace("\"", null).Replace(",", null)));
                    }
                }
            }
        }

        private static IEnumerable<VariableDefinition> SetShockedVariables(string shocks)
        {
            return null;
        }
    }
}