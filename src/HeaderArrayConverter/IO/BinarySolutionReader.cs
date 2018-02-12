using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using AD.IO.Paths;
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

            return new HeaderArrayFile(ReadArrays(file));
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

            return await Task.FromResult(Read(file));
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

            return BuildHeaderArrays(file);
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

            return ReadArrays(file).Select(Task.FromResult);
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
        private IEnumerable<IHeaderArray> BuildHeaderArrays([NotNull] FilePath file)
        {
            HeaderArrayFile arrayFile = HeaderArrayFile.BinaryReader.Read(file);

            // Variable information.
            int[] countOfComponentsInVariable = arrayFile["VNCP"].As<int>().Values.ToArray();

            // Endogenous variable components and information.
            int[] pointersToCumulative = arrayFile["PCUM"].As<int>().Values.ToArray();
            int[] countInCumulative = arrayFile["CMND"].As<int>().Values.ToArray();
            float[] cumulativeResults = arrayFile["CUMS"].As<float>().Values.ToArray();

            // Exogenous variable components and list of positions (where OREX != array.Length).
            int[] countOfExogenous = arrayFile["OREX"].As<int>().Values.ToArray();
            int[] positionsOfExogenous = arrayFile["OREL"].As<int>().Values.ToArray();

            // Shocked variable information
            int[] numberOfShockedComponents = arrayFile["SHCK"].As<int>().Values.ToArray();
            int[] pointersToShockValues = arrayFile["PSHK"].As<int>().Values.ToArray();
            int[] positionsOfShockValues = arrayFile["SHCL"].As<int>().Values.ToArray();
            float[] shockValues = arrayFile["SHOC"].As<float>().Values.ToArray();

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
                    Array.Copy(cumulativeResults, pointer, values, 0, countInCumulative[index]);
                }
                
                ShiftExogenous(values);

                Shocks(values);

                return
                    HeaderArray<float>.Create(
                        array.Name,
                        array.Name,
                        array.Description,
                        HeaderArrayType.RE,
                        array.Sets.Select(x => x.Value.Count).Concat(Enumerable.Repeat(1, 7)).Take(7),
                        values,
                        array.Sets);

                // Shifts existing entries to their appropriate positions to create gaps for exogenous values.
                void ShiftExogenous(float[] inputArray)
                {
                    if (array.Count == countOfExogenous[index])
                    {
                        Array.Clear(inputArray, 0, inputArray.Length);
                        return;
                    }

                    float[] withGaps = inputArray;

                    int nextValidPosition = 0;
                    for (int i = 0; i < index; i++)
                    {
                        if (countOfExogenous[i] != countOfComponentsInVariable[i])
                        {
                            nextValidPosition += countOfExogenous[i];
                        }
                    }

                    for (int i = 0; i < countOfExogenous[index]; i++)
                    {
                        int position = positionsOfExogenous[nextValidPosition + i] - 1;
                        Array.Copy(withGaps, position, withGaps, position + 1, withGaps.Length - position - 1);
                        withGaps[position] = default(float);
                    }
                }

                // Adds shocks to open positions to a copy of the input array.
                void Shocks(float[] inputArray)
                {
                    float[] withShocks = inputArray;

                    int shockedCount = numberOfShockedComponents[index];

                    if (shockedCount == 0)
                    {
                        return;
                    }

                    int shockPointer = pointersToShockValues[index] - 1;

                    int nextValidPosition = 0;
                    for (int i = 0; i < index - 1; i++)
                    {
                        if (numberOfShockedComponents[i] != countOfComponentsInVariable[i])
                        {
                            nextValidPosition += numberOfShockedComponents[i];
                        }
                    }
                    
                    for (int i = 0; i < shockedCount; i++)
                    {
                        int position = array.Count == shockedCount ? i : positionsOfShockValues[nextValidPosition + i] - 1;
                        float value = shockValues[shockPointer + i];
                        withShocks[position] = value;
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="arrayFile"></param>
        /// <returns></returns>
        [Pure]
        [NotNull]
        private static ParallelQuery<SolutionArray> BuildSolutionArrays([NotNull] HeaderArrayFile arrayFile)
        {
            int[] numberOfSets = arrayFile["VCNI"].As<int>().Values.ToArray();
            string[] names = arrayFile["VCNM"].As<string>().Values.ToArray();
            string[] descriptions = arrayFile["VCL0"].As<string>().Values.ToArray();
            string[] unitTypes = arrayFile["VCLE"].As<string>().Values.ToArray();
            char[] changeTypes = arrayFile["VCT0"].As<char>().Values.ToArray();
            char[] variableTypes = arrayFile["VCS0"].As<char>().Values.ToArray();

            KeyValuePair<string, IImmutableList<string>>[][] sets = VariableIndexedCollectionsOfSets(arrayFile);

            return
                ParallelEnumerable.Range(0, names.Length)
                                  .Select(
                                      x =>
                                          new SolutionArray(
                                              x,
                                              numberOfSets[x],
                                              names[x],
                                              descriptions[x],
                                              unitTypes[x],
                                              (ModelChangeType) changeTypes[x],
                                              (ModelVariableType) variableTypes[x],
                                              sets[x]));
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
        private static KeyValuePair<string, IImmutableList<string>>[][] VariableIndexedCollectionsOfSets([NotNull] HeaderArrayFile arrayFile)
        {
            SetInformation[] setInformation = BuildAllSets(arrayFile).ToArray();
            int[] pointerIntoVcsn = arrayFile["VCSP"].As<int>().Values.ToArray();
            int[] setsPerVariable = arrayFile["VCNI"].As<int>().Values.ToArray();
            int[] setPositions = arrayFile["VCSN"].As<int>().Values.ToArray();

            KeyValuePair<string, IImmutableList<string>>[][] arraySetInfo = new KeyValuePair<string, IImmutableList<string>>[pointerIntoVcsn.Length][];

            for (int i = 0; i < pointerIntoVcsn.Length; i++)
            {
                arraySetInfo[i] = new KeyValuePair<string, IImmutableList<string>>[setsPerVariable[i]];

                int pointer = pointerIntoVcsn[i] - 1;

                for (int j = 0; j < arraySetInfo[i].Length; j++)
                {
                    SetInformation set = setInformation[setPositions[pointer + j] - 1];

                    arraySetInfo[i][j] = new KeyValuePair<string, IImmutableList<string>>(set.Name, set.Elements);
                }
            }

            return arraySetInfo;
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
        private static SetInformation[] BuildAllSets([NotNull] HeaderArrayFile arrayFile)
        {
            int[] sizes = arrayFile["SSZ "].As<int>().Values.ToArray();
            char[] intertemporal = arrayFile["STTP"].As<char>().Values.ToArray();
            string[] names = arrayFile["STNM"].As<string>().Values.ToArray();
            string[] descriptions = arrayFile["STLB"].As<string>().Values.ToArray();
            string[] elements = arrayFile["STEL"].As<string>().Values.ToArray();

            SetInformation[] result = new SetInformation[names.Length];

            int counter = 0;
            for (int i = 0; i < names.Length; i++)
            {
                result[i] =
                    new SetInformation(
                        names[i],
                        descriptions[i],
                        intertemporal[i] == 'i',
                        sizes[i],
                        new ArraySegment<string>(elements, counter, sizes[i]));

                counter += sizes[i];
            }

            return result;
        }
    }
}