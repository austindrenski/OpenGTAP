using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace HeaderArrayConverter.Types
{
    /// <summary>
    /// Represents how to interpret the variable changes in the model.
    /// </summary>
    [PublicAPI]
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ModelChangeType
    {
        /// <summary>
        /// Level changes.
        /// </summary>
        Change = 'c',

        /// <summary>
        /// Percent changes.
        /// </summary>
        PercentChange = 'p'
    }
}