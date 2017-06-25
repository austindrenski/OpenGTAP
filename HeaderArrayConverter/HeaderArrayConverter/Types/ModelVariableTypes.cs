using System.Collections.Generic;
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

    /// <summary>
    /// Provides parsing for <see cref="ModelVariableType"/>.
    /// </summary>
    [PublicAPI]
    public static class ModelVariable
    {
        /// <summary>
        /// Parses a <see cref="ModelVariableType"/> from a string.
        /// </summary>
        public static ModelVariableType Parse(string value)
        {
            switch (value)
            {
                case "c":
                {
                    return ModelVariableType.Condensed;
                }
                case "b":
                {
                    return ModelVariableType.Backsolved;
                }
                case "o":
                {
                    return ModelVariableType.Ommitted;
                }
                case "s":
                {
                    return ModelVariableType.Substituted;
                }
                default:
                {
                    throw new KeyNotFoundException();
                }
            }
        }
    }
}