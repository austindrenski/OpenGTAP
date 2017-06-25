using System.Collections.Generic;
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

    /// <summary>
    /// Provides parsing for <see cref="ModelChangeType"/>.
    /// </summary>
    [PublicAPI]
    public static class ModelChange
    {
        /// <summary>
        /// Parses a <see cref="ModelChangeType"/> from a string.
        /// </summary>
        public static ModelChangeType Parse(string value)
        {
            switch (value)
            {
                case "c":
                {
                    return ModelChangeType.Change;
                }
                case "p":
                {
                    return ModelChangeType.PercentChange;
                }
                default:
                {
                    throw new KeyNotFoundException();
                }
            }
        }
    }
}