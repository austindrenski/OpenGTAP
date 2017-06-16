using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using AD.IO;
using HeaderArrayConverter.Types;
using JetBrains.Annotations;
using Newtonsoft.Json;

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

            IImmutableList<SetInformation> setInformation = BuildAllSets(arrayFile);

            IEnumerable<SolutionArray> solutionArrays = BuildSolutionArrays(arrayFile);

            IEnumerable<EndogenousArray> endogenousArrays =
                solutionArrays.Where(x => x.IsEndogenous)
                              .Select((x, i) => BuildNextArray(arrayFile, x, i).Result);

            IEnumerable<(EndogenousArray Array, IImmutableList<SetInformation> Sets)> matched =
                MatchVariableWithSets(arrayFile, setInformation, endogenousArrays);

            foreach (var a in matched)
            {
                Console.WriteLine(JsonConvert.SerializeObject(a, Formatting.Indented));
            }

            return null;
        }

        private IEnumerable<SolutionArray> BuildSolutionArrays(HeaderArrayFile arrayFile)
        {
            string[] names = arrayFile["VCNM"].As<string>().GetLogicalValuesEnumerable().ToArray();

            string[] descriptions = arrayFile["VCL0"].As<string>().GetLogicalValuesEnumerable().ToArray();

            ModelChangeType[] changeTypes = arrayFile["VCT0"].As<ModelChangeType>().GetLogicalValuesEnumerable().ToArray();

            ModelVariableType[] variableTypes = arrayFile["VCS0"].As<ModelVariableType>().GetLogicalValuesEnumerable().ToArray();

            string[] unitTypes = arrayFile["VCLE"].As<string>().GetLogicalValuesEnumerable().ToArray();

            int[] numberOfSets = arrayFile["VCNI"].As<int>().GetLogicalValuesEnumerable().ToArray();

            SolutionArray[] solutionArrays = new SolutionArray[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                solutionArrays[i] =
                    new SolutionArray(
                        i,
                        numberOfSets[i],
                        names[i],
                        descriptions[i],
                        unitTypes[i],
                        changeTypes[i],
                        variableTypes[i]);
            }

            return solutionArrays;
        }

        /// <summary>
        /// Constructs a <see cref="SetInformation"/> sequence from arrays in the <see cref="HeaderArrayFile"/>.
        /// </summary>
        /// <param name="arrayFile">
        /// The file from which set information is found.
        /// </param>
        /// <returns>
        /// A <see cref="SetInformation"/> sequence (ordered).
        /// </returns>
        /// <remarks>
        /// STNAM(NUMST) - names of the sets
        /// STLB(NUMST) - labelling information for the sets
        /// STTP(NUMST) - set types (n=nonintertemporal, i=intertemporal)
        /// SSZ(NUMST) - sizes of the sets
        /// STEL array (set elements from index position of the name in 'STNM' to value at the index position in 'STEL') 
        /// </remarks>
        private static IImmutableList<SetInformation> BuildAllSets(HeaderArrayFile arrayFile)
        {
            string[] names = arrayFile["STNM"].As<string>().GetLogicalValuesEnumerable().ToArray();

            string[] descriptions = arrayFile["STLB"].As<string>().GetLogicalValuesEnumerable().ToArray();

            bool[] temporal = arrayFile["STTP"].As<string>().GetLogicalValuesEnumerable().Select(x => x == "i").ToArray();

            int[] sizes = arrayFile["SSZ "].As<int>().GetLogicalValuesEnumerable().ToArray();

            string[] elements = arrayFile["STEL"].As<string>().GetLogicalValuesEnumerable().ToArray();
            
            SetInformation[] setInformation = new SetInformation[names.Length];

            int counter = 0;
            for (int i = 0; i < names.Length; i++)
            {
                setInformation[i] = 
                    new SetInformation(
                        names[i], 
                        descriptions[i], 
                        temporal[i], 
                        sizes[i], 
                        elements.Skip(counter).Take(sizes[i]).ToImmutableArray());

                counter += sizes[i];
            }

            return setInformation.ToImmutableArray();
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="arrayFile">
        /// 
        /// </param>
        /// <param name="setInformation">
        /// 
        /// </param>
        /// <param name="arrays">
        /// 
        /// </param>
        /// <remarks>
        /// VNCP - Number of components of variables at header VARS => [321] == 824
        /// 
        /// VCSP - VCSTNP(NUMVC) - pointers into VCSTN array for each variable => [355] == 224
        ///
        /// VCAR - VCSTN - arguments for variables(c + b) => [223] == 2
        ///
        /// VCSN - VCSTN(NVCSTN) - set numbers arguments range over var1, var2 etc => [223] == 4, [224] == 21
        ///
        /// STNM - STNAM(NUMST) - names of the sets => [3] == COM, [20] == SOURCE
        ///
        /// SSZ - SSZ(NUMST) - sizes of the sets => [3] == 412, [20] == 2
        ///
        /// STEL - STEL array =>
        ///    SSZ: [0] == 27, [1]==27, [2]==2 =>
        /// STEL[27 + 27 + 2]==[56...467]==(OilSeedFarm, ..., WatIntl)
        ///
        /// (A) Using NUMBVC get the count of total values in variable by VNCP[NUMBVC]
        /// (B) Using NUMVC get the pointer to VCSTN by VCSP[NUMVC]
        /// (C) Using((B) - 1) and get the number of arguments by VCAR[((B) - 1)]
        /// (D) Using(C) get((B) - 1) entries from VCSN[(C)...((C) + ((B) - 1))]
        /// (E) Using the vector((D):-1) get the set names by STNM[((D: -1)]
        /// (F) Using the vector((D):-1) get the set sizes by SSZ_[((D: -1)]
        /// (G) Do all of the above for all sets, then take the global mapping info set. Then index into STEL[previous_sum...(previous_sum + (F) - 1)]
        /// </remarks>
        private static IEnumerable<(EndogenousArray Array, IImmutableList<SetInformation> Sets)> MatchVariableWithSets(HeaderArrayFile arrayFile, IImmutableList<SetInformation> setInformation, IEnumerable<EndogenousArray> arrays)
        {
            //int[] numberOfValues = arrayFile["VNCP"].As<int>().GetLogicalValuesEnumerable().ToArray();

            int[] pointerIntoVcstn = arrayFile["VCSP"].As<int>().GetLogicalValuesEnumerable().ToArray();

            //int[] numberOfArguments = arrayFile["VCAR"].As<int>().GetLogicalValuesEnumerable().ToArray();

            int[] setPositions = arrayFile["VCSN"].As<int>().GetLogicalValuesEnumerable().ToArray();

            foreach (EndogenousArray array in arrays)
            {
                SetInformation[] arraySetInfo = new SetInformation[array.NumberOfSets];

                int pointer = pointerIntoVcstn[array.VariableIndex] - 1;

                for (int i = 0; i < array.NumberOfSets; i++)
                {
                    int setPosition= setPositions[pointer + i];

                    arraySetInfo[i] = setInformation[setPosition];
                }
                yield return (array, arraySetInfo.ToImmutableArray());
            }
        }


        private static async Task<EndogenousArray> BuildNextArray(HeaderArrayFile arrayFile, SolutionArray endogenous, int index)
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
            
            return await Task.FromResult(new EndogenousArray(endogenous, index));
        }
    }
}