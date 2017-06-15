using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace HeaderArrayConverter.Types
{
    /// <summary>
    /// Represents the result values for a variable in a <see cref="SolutionFile"/> where the variable is <see cref="ModelVariableType.Condensed"/> or <see cref="ModelVariableType.Backsolved"/>.
    /// </summary>
    [PublicAPI]
    public class CondensedOrBacksolvedSolutionDataObject : SolutionDataObject
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
        /// Constructs a <see cref="CondensedOrBacksolvedSolutionDataObject"/> from a base <see cref="SolutionDataObject"/> where the variable is <see cref="ModelVariableType.Condensed"/> or <see cref="ModelVariableType.Backsolved"/>.
        /// </summary>
        /// <param name="solutionDataObject">
        /// The base definition.
        /// </param>
        /// <param name="index">
        /// The index position of the variable among variables that are <see cref="ModelVariableType.Condensed"/> or <see cref="ModelVariableType.Backsolved"/>.
        /// </param>
        public CondensedOrBacksolvedSolutionDataObject(SolutionDataObject solutionDataObject, int index) : base(solutionDataObject)
        {
            Index = index;
        }

        /// <summary>
        /// Returns a <see cref="CondensedOrBacksolvedSolutionDataObject"/> sequence from a <see cref="SolutionDataObject"/> sequence.
        /// </summary>
        /// <param name="source">
        /// The <see cref="SolutionDataObject"/> sequence from which valid entries are found.
        /// </param>
        /// <returns>
        /// A <see cref="CondensedOrBacksolvedSolutionDataObject"/> sequence.
        /// </returns>
        public static IEnumerable<CondensedOrBacksolvedSolutionDataObject> Create(IEnumerable<SolutionDataObject> source)
        {
            return
                source.Where(x => x.VariableType == ModelVariableType.Condensed || x.VariableType == ModelVariableType.Backsolved)
                      .OrderBy(x => x.VariableIndex)
                      .Select((x, i) => new CondensedOrBacksolvedSolutionDataObject(x, i));
        }

        /// <summary>
        /// Marshals values to build an immutable <see cref="CondensedOrBacksolvedSolutionDataObject"/>.
        /// </summary>
        [PublicAPI]
        public new class Builder
        {
            /// <summary>
            /// Gets the base properties of a variable in a <see cref="SolutionFile"/>.
            /// </summary>
            public SolutionDataObject SolutionDataObject { get; set; }

            /// <summary>
            /// Gets the index among variables that are <see cref="ModelVariableType.Condensed"/> or <see cref="ModelVariableType.Backsolved"/>. 
            /// Equivalent to the Gempack parameter 'NUMBVC'.
            /// </summary>
            /// <remarks>
            /// This value is derived from the index order in 'VCLE' of only those entries equal to 'c' or 'b'. This defines the Gempack parameter 'NUMBVC'.
            /// </remarks>
            public int Index { get; set; }

            /// <summary>
            /// Constructs a <see cref="CondensedOrBacksolvedSolutionDataObject"/> from the <see cref="Builder"/>. 
            /// </summary>
            /// <returns>
            /// A <see cref="CondensedOrBacksolvedSolutionDataObject"/> containing the properties set by this <see cref="Builder"/>.
            /// </returns>
            public CondensedOrBacksolvedSolutionDataObject Build()
            {
                return new CondensedOrBacksolvedSolutionDataObject(SolutionDataObject, Index);
            }
        }
    }
}