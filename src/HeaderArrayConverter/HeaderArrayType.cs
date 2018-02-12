using System.Runtime.Serialization;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace HeaderArrayConverter
{
    /// <summary>
    /// Represents the type of a Header Array when persisted to a binary HAR file.
    /// </summary>
    /// <remarks>
    /// This enums has <see cref="short"/> values which are composed of two bytes. 
    /// 
    /// These short values are the reverse character representation of the type codes. For instance, the code '1C' is composed of bytes 0x31 and 0x43. 
    /// 
    /// In a sequential read these are flipped, so the assigned value is flipped in the hexadecimal short representation.
    /// 
    /// This works out so that readers can read a <see cref="short"/> and cast to a <see cref="HeaderArrayType"/>, and writers can cast to a <see cref="short"/> and write.
    /// </remarks>
    [PublicAPI]
    [JsonConverter(typeof(StringEnumConverter))]
    public enum HeaderArrayType : short
    {
        /// <summary>
        /// Represents a 1-dimensional array of <see cref="string"/> values.
        /// </summary>
        [EnumMember(Value = "1C")]
        C1 = 0x43_31,

        /// <summary>
        /// Represents a multi-dimensional array of <see cref="float"/> values and item labels.
        /// </summary>
        [EnumMember(Value = "RE")]
        // ReSharper disable once InconsistentNaming
        RE = 0x45_52,

        /// <summary>
        /// Represents a 2-dimensional array of <see cref="float"/> values.
        /// </summary>
        [EnumMember(Value = "2R")]
        R2 = 0x52_32,

        /// <summary>
        /// Represents a 2-dimensional array of <see cref="int"/> values.
        /// </summary>
        [EnumMember(Value = "2I")]
        I2 = 0x49_32
    }
}