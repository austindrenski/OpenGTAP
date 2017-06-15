using JetBrains.Annotations;
using Newtonsoft.Json;

namespace HeaderArrayConverter.Types
{
    /// <summary>
    /// Represents the result values for a variable in a <see cref="SolutionFile"/> where the variable is <see cref="ModelVariableType.Condensed"/> or <see cref="ModelVariableType.Backsolved"/>.
    /// </summary>
    [PublicAPI]
    [JsonObject]
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
        /// Returns a JSON representation of the current object.
        /// </summary>
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
}