using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace HeaderArrayConverter.Types
{
    /// <summary>
    /// Represents the result values for a variable in a <see cref="SolutionFile"/> where the variable is <see cref="ModelVariableType.Condensed"/> or <see cref="ModelVariableType.Backsolved"/>.
    /// </summary>
    [PublicAPI]
    public class EndogenousSolutionObject : SolutionDataObject
    {
        /// <summary>
        /// Gets the index among variables that are <see cref="ModelVariableType.Condensed"/> or <see cref="ModelVariableType.Backsolved"/>. 
        /// Equivalent to the Gempack parameter 'NUMBVC'.
        /// </summary>
        /// <remarks>
        /// This value is derived from the index order in 'VCLE' of only those entries equal to 'c' or 'b'. This defines the Gempack parameter 'NUMBVC'.
        /// </remarks>
        public int Index { get; }

        /// <summary>
        /// Constructs a <see cref="EndogenousSolutionObject"/> from a base <see cref="SolutionDataObject"/> where the variable is <see cref="ModelVariableType.Condensed"/> or <see cref="ModelVariableType.Backsolved"/>.
        /// </summary>
        /// <param name="solutionDataObject">
        /// The base definition.
        /// </param>
        /// <param name="index">
        /// The index position of the variable among variables that are <see cref="ModelVariableType.Condensed"/> or <see cref="ModelVariableType.Backsolved"/>.
        /// </param>
        public EndogenousSolutionObject(SolutionDataObject solutionDataObject, int index) : base(solutionDataObject)
        {
            Index = index;
        }

        /// <summary>
        /// Returns a <see cref="EndogenousSolutionObject"/> sequence from a <see cref="SolutionDataObject"/> sequence.
        /// </summary>
        /// <param name="source">
        /// The <see cref="SolutionDataObject"/> sequence from which valid entries are found.
        /// </param>
        /// <returns>
        /// A <see cref="EndogenousSolutionObject"/> sequence.
        /// </returns>
        public static IEnumerable<EndogenousSolutionObject> Create(IEnumerable<SolutionDataObject> source)
        {
            return
                source.Where(x => x.VariableType == ModelVariableType.Condensed || x.VariableType == ModelVariableType.Backsolved)
                      .OrderBy(x => x.VariableIndex)
                      .Select((x, i) => new EndogenousSolutionObject(x, i));
        }
    }
}