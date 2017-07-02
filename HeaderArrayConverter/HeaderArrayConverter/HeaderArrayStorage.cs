using System.Runtime.Serialization;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace HeaderArrayConverter
{
    /// <summary>
    /// Represents the storage type of a Header Array when persisted to a binary HAR file.
    /// </summary>
    /// <remarks>
    /// In a sequential read these bytes are flipped, so the assigned value is flipped in the hexadecimal <see cref="int"/> representation.
    /// 
    /// This works out so that readers can read a <see cref="int"/> and cast to a <see cref="HeaderArrayStorage"/>, and writers can cast to a <see cref="int"/> and write.
    /// </remarks>
    [PublicAPI]
    [JsonConverter(typeof(StringEnumConverter))]
    public enum HeaderArrayStorage
    {
        /// <summary>
        /// Represents an array that is non-sparse.
        /// </summary>
        [EnumMember(Value = "FULL")]
        Full = 0x4C_4C_55_46,

        /// <summary>
        /// Represents an array that is sparse.
        /// </summary>
        [EnumMember(Value = "SPSE")]
        Sparse = 0x53_45_50_45
    }
}