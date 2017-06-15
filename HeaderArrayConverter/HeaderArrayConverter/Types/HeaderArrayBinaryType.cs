using System.ComponentModel;
using JetBrains.Annotations;

namespace HeaderArrayConverter.Types
{
    /// <summary>
    /// Represents the type of a Header Array when persisted to a binary HAR file.
    /// </summary>
    [PublicAPI]
    public enum HeaderArrayBinaryType : short
    {
        /// <summary>
        /// Represents an array of strings.
        /// </summary>
        [DisplayName("1C")]
        
        C1 = 0x31_43,

        /// <summary>
        /// Represents an array of <see cref="float"/> values and item labels.
        /// </summary>
        RE = 0x52_45,

        /// <summary>
        /// Represents an array of <see cref="float"/> values.
        /// </summary>
        RL = 'R' + 'L',

        /// <summary>
        /// Represents an array of <see cref="float"/> values.
        /// </summary>
        [DisplayName("2R")]
        R2 = 0x32_52,

        /// <summary>
        /// Represents an array of <see cref="int"/> values.
        /// </summary>
        [DisplayName("2I")]
        I2 = 0x32_49
    }
}
