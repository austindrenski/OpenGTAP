using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AD.IO;
using JetBrains.Annotations;

namespace HeaderArrayConverter.IO
{
    /// <summary>
    /// Implements a <see cref="HeaderArrayReader"/> for reading Header Array (HARX) files in zipped JSON format.
    /// </summary>
    [PublicAPI]
    public class BinarySolutionReader : HeaderArrayReader
    {
        private static HeaderArrayReader BinaryReader { get; } = new BinaryHeaderArrayReader();

        /// <summary>
        /// Reads <see cref="IHeaderArray"/> collections from file..
        /// </summary>
        /// <param name="file">
        /// The file to read.
        /// </param>
        /// <return>
        /// A <see cref="HeaderArrayFile"/> representing the contents of the file.
        /// </return>
        public override HeaderArrayFile Read(FilePath file)
        {
            if (file is null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            return ReadAsync(file).Result;
        }

        /// <summary>
        /// Asynchronously reads <see cref="IHeaderArray"/> collections from file..
        /// </summary>
        /// <param name="file">
        /// The file to read.
        /// </param>
        /// <return>
        /// A task that upon completion returns a <see cref="HeaderArrayFile"/> representing the contents of the file.
        /// </return>
        public override async Task<HeaderArrayFile> ReadAsync(FilePath file)
        {
            if (file is null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            return new HeaderArrayFile(await Task.WhenAll(ReadArraysAsync(file)));
        }

        /// <summary>
        /// Enumerates the <see cref="IHeaderArray"/> collection from file.
        /// </summary>
        /// <param name="file">
        /// The file from which to read arrays.
        /// </param>
        /// <returns>
        /// A <see cref="IHeaderArray"/> collection from the file.
        /// </returns>
        public override IEnumerable<IHeaderArray> ReadArrays(FilePath file)
        {
            if (file is null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            foreach (Task<IHeaderArray> array in ReadArraysAsync(file))
            {
                yield return array.Result;
            }
        }

        /// <summary>
        /// Asynchronously enumerates the arrays from file.
        /// </summary>
        /// <param name="file">
        /// The file from which to read arrays.
        /// </param>
        /// <returns>
        /// An enumerable collection of tasks that when completed return an <see cref="IHeaderArray"/> from file.
        /// </returns>
        public override IEnumerable<Task<IHeaderArray>> ReadArraysAsync(FilePath file)
        {
            if (file is null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            return BuildHeaderArraysAsync(file);
        }

        /// <summary>
        /// Builds an <see cref="IHeaderArray"/> sequence from the <see cref="SolutionFile"/>.
        /// </summary>
        /// <param name="file">
        /// A solution file (SL4).
        /// </param>
        /// <returns>
        /// An enumerable collection of tasks that when completed return an <see cref="IHeaderArray"/> from file.
        /// </returns>
        [NotNull]
        private IEnumerable<Task<IHeaderArray>> BuildHeaderArraysAsync(FilePath file)
        {
            HeaderArrayFile arrayFile = BinaryReader.Read(file);
            
            // VCNM - VCNAM(numvc) - names of all the variables
            IEnumerable<string> names =
                arrayFile["VCNM"].As<string>().GetLogicalValuesEnumerable().ToArray();

            //VCT0 - VCTP(numvc) p =% -change, c = change
            //    [355] = "p"
            IEnumerable<string> changeOrPercentChange =
                arrayFile["VCT0"].As<string>().GetLogicalValuesEnumerable().ToArray();

            // VCS0 - VCSTAT(numvc) - c = condensed b = backsolved o = omitted s = subbed out
            IEnumerable<string> variableResultType =
                arrayFile["VCS0"].As<string>().GetLogicalValuesEnumerable().ToArray();

            //VCL0 - VCLB(numvc) - Labelling information for variables
            //    [355] = "Purchasers prices by commodities and source to households"
            IEnumerable<string> variableDefinitions =
                arrayFile["VCL0"].As<string>().GetLogicalValuesEnumerable().ToArray();

            //VCLE - Info about levels value of variables lv = levels var, ol = ORIG_LEV
            //    [355] = "ln p3cs"
            IEnumerable<string> levelsOrLogs =
                arrayFile["VCLE"].As<string>().GetLogicalValuesEnumerable().ToArray();

            //VCNI - VCNIND(numvc) - how many arguments each variable has
            //    [355] = 2
            IEnumerable<int> numberOfDefiningSets =
                arrayFile["VCNI"].As<int>().GetLogicalValuesEnumerable().ToArray();

            IEnumerable<object> condensedOrBacksolved =
                names.Zip(
                         changeOrPercentChange,
                         (x, y) =>
                             (name: x, changeType: y))
                     .Zip(
                         variableResultType,
                         (x, y) =>
                             (name: x.name, changeType: x.changeType, type: y))
                     .Zip(
                         variableDefinitions,
                         (x, y) =>
                             (name: x.name, changeType: x.changeType, type: x.type, definition: y))
                     .Zip(
                         levelsOrLogs,
                         (x, y) =>
                             (name: x.name, changeType: x.changeType, type: x.type, definition: x.definition, levelLogTypes: y))
                     .Zip(
                         numberOfDefiningSets,
                         (x, y) => new SolutionDataObject<float>
                         {
                             Name = x.name,
                             ChangeType = x.changeType,
                             VariableType = (ModelVariableType)Enum.Parse(typeof(ModelVariableType), x.type),
                             Description = x.definition,
                             VariableUnitType = x.type,
                             //LevelOrLogs = levelsOrLogs
                         })
                     .Where(
                         x =>
                             x.VariableType is ModelVariableType.Condensed || x.VariableType is ModelVariableType.Backsolved)
                     .ToArray();

            foreach (SolutionFile item in condensedOrBacksolved)
            {
                yield return BuildNextArray(arrayFile, item.Name, type, index);
            }
        }

        [PublicAPI]
        private class SolutionDataObject<T>
        {
            /// <summary>
            /// Gets the name of the variable. [VCNM, VCNAM(numvc)].
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Gets the variable description. [VCL0, VCLB(numvc)].
            /// </summary>
            public string Description { get; set; }

            /// <summary>
            /// True if percentage change; otherwise false. [VCT0, VCTP(numvc)].
            /// </summary>
            public string ChangeType { get; set; }

            /// <summary>
            /// Gets the <see cref="ModelVariableType"/> for this object. [VCS0, VCSTAT(numvc)].
            /// </summary>
            public ModelVariableType VariableType { get; set; }

            /// <summary>
            /// Gets the unit type of the variable (e.g. ln, lv = levels var, ol = ORIG_LEV). [VCLE].
            /// </summary>
            public string VariableUnitType { get; set; }

            /// <summary>
            /// Gets the number of sets defined on this array. [VCNI, VCNIND(numvc)].
            /// </summary>
            public int NumberOfSets { get; set; }

            /// <summary>
            /// Gets information about levels value of variables lv = levels var, ol = ORIG_LEV. [VCLA]
            /// </summary>
            public string LevelOrLogs { get; set; }

            /// <summary>
            /// Gets this objects entry among C or B.
            /// </summary>
            public int Index { get; set; }
        }

        [PublicAPI]
        public enum ModelVariableType
        {
            /// <summary>
            /// c = condensed.
            /// </summary>
            Condensed,

            /// <summary>
            /// b = backsolved.
            /// </summary>
            Backsolved,

            /// <summary>
            /// o = ommitted.
            /// </summary>
            Ommitted,

            /// <summary>
            /// s = substituted out.
            /// </summary>
            Substituted
        }

        private async Task<IHeaderArray> BuildNextArray(HeaderArrayFile arrayFile, string name, string type, int index)
        {
            // VCLB - VCLB - labelling information for variables(condensed + backsolved)
            string description = arrayFile["VCLB"].As<string>()[$"{index}"].SingleOrDefault().Value;

            // VCTP - BVCTP(numbvc) - p =% -change, c = change[condensed + backsolved var only]
            string changeOrPercentChange = arrayFile["VCTP"].As<string>()[$"{index}"].SingleOrDefault().Value;

            // VCLE - Info about levels value of variables lv = levels var, ol = ORIG_LEV
            string valueDescription = arrayFile["VCLE"].As<string>()[$"{index}"].SingleOrDefault().Value;

            // VCNA - VCNIND - number of arguments for variables (condensed+backsolved)
            int numberOfDefiningSets = arrayFile["VCNA"].As<int>()[$"{index}"].SingleOrDefault().Value;

            // VNCP - number of components of variables at header VARS
            int numberOfValues = arrayFile["VNCP"].As<int>()[$"{index}"].SingleOrDefault().Value;


            return null;
        }
    }
}