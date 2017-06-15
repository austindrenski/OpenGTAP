using JetBrains.Annotations;

namespace HeaderArrayConverter.Types
{
    /// <summary>
    /// Represents how the variable is handled by the model.
    /// </summary>
    [PublicAPI]
    public enum ModelVariableType
    {
        /// <summary>
        /// Condensed.
        /// </summary>
        Condensed = 'c',

        /// <summary>
        /// Backsolved.
        /// </summary>
        Backsolved = 'b',

        /// <summary>
        /// Ommitted.
        /// </summary>
        Ommitted = 'o',

        /// <summary>
        /// Substituted out.
        /// </summary>
        Substituted = 's'
    }
}