using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace HeaderArrayConverter.Types
{
    /// <summary>
    /// Represents how the variable is handled by the model.
    /// </summary>
    [PublicAPI]
    [JsonConverter(typeof(StringEnumConverter))]
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