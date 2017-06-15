using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AD.IO;
using HeaderArrayConverter.Types;
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
            
            string[] names = arrayFile["VCNM"].As<string>().GetLogicalValuesEnumerable().ToArray();

            string[] descriptions = arrayFile["VCL0"].As<string>().GetLogicalValuesEnumerable().ToArray();

            ModelChangeType[] changeTypes = arrayFile["VCT0"].As<ModelChangeType>().GetLogicalValuesEnumerable().ToArray();

            ModelVariableType[] variableTypes = arrayFile["VCS0"].As<ModelVariableType>().GetLogicalValuesEnumerable().ToArray();

            string[] unitTypes = arrayFile["VCLE"].As<string>().GetLogicalValuesEnumerable().ToArray();

            int[] numberOfSets = arrayFile["VCNI"].As<int>().GetLogicalValuesEnumerable().ToArray();
            
            SolutionDataObject[] solutionObjects = new SolutionDataObject[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                solutionObjects[i] =
                    new SolutionDataObject(
                        i,
                        numberOfSets[i],
                        names[i],
                        descriptions[i],
                        unitTypes[i],
                        changeTypes[i],
                        variableTypes[i]);
            }
            
            foreach ((SolutionDataObject solution, int index) in solutionObjects.Where(x => x.IsEndogenous).Select((x, i) => (x, i)))
            {
                yield return BuildNextArray(arrayFile, solution, index);
            }
        }

        private async Task<IHeaderArray> BuildNextArray(HeaderArrayFile arrayFile, SolutionDataObject endogenous, int index)
        {
            // VARS - names of variables(condensed+backsolved)
            string name = arrayFile["VARS"].As<string>()[index];

            // VCLB - VCLB - labelling information for variables(condensed + backsolved)
            string description = arrayFile["VCLB"].As<string>()[index];

            // VCTP - BVCTP(numbvc) - p =% -change, c = change[condensed + backsolved var only]
            ModelChangeType changeType = arrayFile["VCTP"].As<ModelChangeType>()[index];

            // VCNA - VCNIND - number of arguments for variables (condensed+backsolved)
            int numberOfSets = arrayFile["VCNA"].As<int>()[index];

            if (name != endogenous.Name)
            {
                throw DataValidationException.Create(endogenous, x => x.Name, name);
            }
            if (description != endogenous.Description)
            {
                throw DataValidationException.Create(endogenous, x => x.Description, description);
            }
            if (changeType != endogenous.ChangeType)
            {
                throw DataValidationException.Create(endogenous, x => x.ChangeType, changeType);
            }
            if (numberOfSets != endogenous.NumberOfSets)
            {
                throw DataValidationException.Create(endogenous, x => x.NumberOfSets, numberOfSets);
            }

            // VCLE - Info about levels value of variables lv = levels var, ol = ORIG_LEV
            string levelType = arrayFile["VCST"].As<string>()[index];

            // VNCP - number of components of variables at header VARS
            int numberOfValues = arrayFile["VNCP"].As<int>()[index];


            if (endogenous.Name == "p3cs")
            {
                Console.WriteLine(endogenous);
            }

            return null;
        }
    }
}