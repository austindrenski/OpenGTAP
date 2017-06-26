using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using AD.IO;
using HeaderArrayConverter.Collections;
using HeaderArrayConverter.Extensions;
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

            return ReadArraysAsync(file).Select(x => x.Result);
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

            return BuildHeaderArrays(file).Select(Task.FromResult);
        }

        /// <summary>
        /// Builds an <see cref="IHeaderArray"/> sequence from the solution file.
        /// </summary>
        /// <param name="file">
        /// A solution file (SL4).
        /// </param>
        /// <returns>
        /// An enumerable collection of tasks that when completed return an <see cref="IHeaderArray"/> from file.
        /// </returns>
        [NotNull]
        private IEnumerable<IHeaderArray> BuildHeaderArrays(FilePath file)
        {
            HeaderArrayFile arrayFile = HeaderArrayFile.BinaryReader.Read(file);

            // Variable information.
            IReadOnlyList<int> countOfComponentsInVariable = arrayFile["VNCP"].As<int>().GetLogicalValuesEnumerable().ToArray();

            // Endogenous variable components and information.
            IReadOnlyList<int> pointersToCumulative = arrayFile["PCUM"].As<int>().GetLogicalValuesEnumerable().ToArray();
            IReadOnlyList<int> countInCumulative = arrayFile["CMND"].As<int>().GetLogicalValuesEnumerable().ToArray();
            IReadOnlyList<float> cumulativeResults = arrayFile["CUMS"].As<float>().GetLogicalValuesEnumerable().ToArray();

            // Exogenous variable components and list of positions (where OREX != array.Length).
            IReadOnlyList<int> countOfExogenous = arrayFile["OREX"].As<int>().GetLogicalValuesEnumerable().ToArray();
            IReadOnlyList<int> positionsOfExogenous = arrayFile["OREL"].As<int>().GetLogicalValuesEnumerable().ToArray();

            // Shocked variable information
            IReadOnlyList<int> numberOfShockedComponents = arrayFile["SHCK"].As<int>().GetLogicalValuesEnumerable().ToArray();
            IReadOnlyList<int> pointersToShockValues = arrayFile["PSHK"].As<int>().GetLogicalValuesEnumerable().ToArray();
            IReadOnlyList<int> positionsOfShockValues = arrayFile["SHCL"].As<int>().GetLogicalValuesEnumerable().ToArray();
            IReadOnlyList<float> shockValues = arrayFile["SHOC"].As<float>().GetLogicalValuesEnumerable().ToArray();

            return
                BuildSolutionArrays(arrayFile).Where(x => x.IsBacksolvedOrCondensed)
                                              .OrderBy(x => x.VariableIndex)
                                              .Select(BuildNextArray);

            // Local method here to limit passing arrays as parameters.
            IHeaderArray BuildNextArray(SolutionArray array, int index)
            {
                float[] values = new float[array.Count];
                
                // When the array is condensed/backsolved and the pointer is empty, its exogenous.
                int pointer = pointersToCumulative[index] - 1;
                if (pointer != -1)
                {
                    Array.Copy((float[])cumulativeResults, pointer, values, 0, countInCumulative[index]);
                }
                
                // Not pure -- value is modified!
                ShiftExogenous(values);

                // Not pure -- value is modified!
                Shocks(values);

                IImmutableList<KeyValuePair<string, IImmutableList<string>>> set =
                    array.Sets
                         .Select(x => new KeyValuePair<string, IImmutableList<string>>(x.Name, x.Elements))
                         .ToImmutableArray();

                IEnumerable<KeyValuePair<KeySequence<string>, float>> entries =
                    set.AsExpandedSet()
                       .Zip(values, (k, v) => new KeyValuePair<KeySequence<string>, float>(k, v));

                return
                    new HeaderArray<float>(
                        array.Name,
                        array.Name,
                        array.Description,
                        HeaderArrayType.RE,
                        entries,
                        array.Sets.Select(x => x.Count).Concat(Enumerable.Repeat(1, 7)).Take(7),
                        set);


                // Shifts existing entries to their appropriate positions to create gaps for exogenous values.
                void ShiftExogenous(float[] inputArray)
                {
                    if (array.Count == countOfExogenous[index])
                    {
                        Array.Clear(inputArray, 0, inputArray.Length);
                        return;
                    }

                    int nextValidPosition =
                        countOfExogenous.Take(index)
                                        .Where((x, i) => x != countOfComponentsInVariable[i])
                                        .Sum();

                    for (int i = 0; i < countOfExogenous[index]; i++)
                    {
                        int position = positionsOfExogenous[nextValidPosition + i] - 1;

                        Array.Copy(inputArray, position, inputArray, position + 1, inputArray.Length - position - 1);

                        inputArray[position] = default(float);
                    }
                }

                // Adds shocks to open positions to a copy of the input array.
                void Shocks(float[] inputArray)
                {
                    int shockedCount = numberOfShockedComponents[index];

                    if (shockedCount == 0)
                    {
                        return;
                    }

                    int shockPointer = pointersToShockValues[index] - 1;

                    int nextValidPosition =
                        numberOfShockedComponents.Take(index - 1)
                                                 .Where((x, i) => x > 1 && x != numberOfShockedComponents[i])
                                                 .Sum();
                    
                    for (int i = 0; i < shockedCount; i++)
                    {
                        int position = array.Count == shockedCount ? i : position = positionsOfShockValues[nextValidPosition + i];
                        float value = shockValues[shockPointer + i];
                        inputArray[position] = value;
                    }
                }
            }
        }

        [Pure]
        [NotNull]
        private static ParallelQuery<SolutionArray> BuildSolutionArrays(HeaderArrayFile arrayFile)
        {
            IReadOnlyList<int> numberOfSets = arrayFile["VCNI"].As<int>().GetLogicalValuesEnumerable().ToArray();
            IReadOnlyList<string> names = arrayFile["VCNM"].As<string>().GetLogicalValuesEnumerable().ToArray();
            IReadOnlyList<string> descriptions = arrayFile["VCL0"].As<string>().GetLogicalValuesEnumerable().ToArray();
            IReadOnlyList<string> unitTypes = arrayFile["VCLE"].As<string>().GetLogicalValuesEnumerable().ToArray();
            IReadOnlyList<string> changeTypes = arrayFile["VCT0"].As<string>().GetLogicalValuesEnumerable().ToArray();
            IReadOnlyList<string> variableTypes = arrayFile["VCS0"].As<string>().GetLogicalValuesEnumerable().ToArray();

            IImmutableList<SetInformation>[] sets = VariableIndexedCollectionsOfSets(arrayFile).ToArray();

            return
                ParallelEnumerable.Range(0, names.Count)
                                  .Select(
                                      (x, i) => new SolutionArray(
                                          i,
                                          numberOfSets[i],
                                          names[i],
                                          descriptions[i],
                                          unitTypes[i],
                                          ModelChange.Parse(changeTypes[i]),
                                          ModelVariable.Parse(variableTypes[i]),
                                          sets[i]));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="arrayFile">
        /// 
        /// </param>
        /// <remarks>
        /// VCSP - VCSTNP(NUMVC) - pointers into VCSTN array for each variable
        /// VCNI - VCNIND(NUMVC) - how many arguments each variable has
        /// VCSN - VCSTN(NVCSTN) - set numbers arguments range over var1, var2 etc
        /// </remarks>
        [Pure]
        [NotNull]
        private static IEnumerable<IImmutableList<SetInformation>> VariableIndexedCollectionsOfSets(HeaderArrayFile arrayFile)
        {
            SetInformation[] setInformation = BuildAllSets(arrayFile).ToArray();
            IReadOnlyList<int> pointerIntoVcstn = arrayFile["VCSP"].As<int>().GetLogicalValuesEnumerable().ToArray();
            IReadOnlyList<int> setsPerVariable = arrayFile["VCNI"].As<int>().GetLogicalValuesEnumerable().ToArray();
            IReadOnlyList<int> setPositions = arrayFile["VCSN"].As<int>().GetLogicalValuesEnumerable().ToArray();

            for (int i = 0; i < pointerIntoVcstn.Count; i++)
            {
                SetInformation[] arraySetInfo = new SetInformation[setsPerVariable[i]];

                int pointer = pointerIntoVcstn[i] - 1;

                for (int j = 0; j < arraySetInfo.Length; j++)
                {
                    int setPosition= setPositions[pointer + j] - 1;

                    arraySetInfo[j] = setInformation[setPosition];
                }

                yield return arraySetInfo.ToImmutableArray();
            }
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
        /// STEL array - set elements from index position of the name in 'STNM' to value at the index position in 'STEL'
        /// </remarks>
        [Pure]
        [NotNull]
        private static IEnumerable<SetInformation> BuildAllSets(HeaderArrayFile arrayFile)
        {
            IReadOnlyList<int> sizes = arrayFile["SSZ "].As<int>().GetLogicalValuesEnumerable().ToArray();
            IReadOnlyList<bool> intertemporal = arrayFile["STTP"].As<string>().GetLogicalValuesEnumerable().Select(x => x == "i").ToArray();
            IReadOnlyList<string> names = arrayFile["STNM"].As<string>().GetLogicalValuesEnumerable().ToArray();
            IReadOnlyList<string> descriptions = arrayFile["STLB"].As<string>().GetLogicalValuesEnumerable().ToArray();
            IReadOnlyList<string> elements = arrayFile["STEL"].As<string>().GetLogicalValuesEnumerable().ToArray();

            int counter = 0;
            for (int i = 0; i < names.Count; i++)
            {
                yield return
                    new SetInformation(
                        names[i],
                        descriptions[i],
                        intertemporal[i],
                        sizes[i],
                        new ArraySegment<string>((string[])elements, counter, sizes[i]));

                counter += sizes[i];
            }
        }
    }
}