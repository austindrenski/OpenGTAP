using System.Runtime.Serialization;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace HeaderArrayConverter.Types
{
    /// <summary>
    /// Represents the type of a Header Array when persisted to a binary HAR file.
    /// </summary>
    [PublicAPI]
    [JsonConverter(typeof(StringEnumConverter))]
    public enum HeaderArrayType : short
    {
        /// <summary>
        /// Represents an array of strings.
        /// </summary>
        [EnumMember(Value = "1C")]
        C1 = 0x43_31,

        /// <summary>
        /// Represents an array of <see cref="float"/> values and item labels.
        /// </summary>
        [EnumMember(Value = "RE")]
        RE = 0x45_52,

        /// <summary>
        /// Represents an array of <see cref="float"/> values.
        /// </summary>
        [EnumMember(Value = "2R")]
        R2 = 0x52_32,

        /// <summary>
        /// Represents an array of <see cref="int"/> values.
        /// </summary>
        [EnumMember(Value = "2I")]
        I2 = 0x49_32
    }
}