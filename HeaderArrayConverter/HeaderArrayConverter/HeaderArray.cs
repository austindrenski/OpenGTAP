using System;
using JetBrains.Annotations;

namespace HeaderArrayConverter
{
    /// <summary>
    /// Represents a Header Array (HAR) file.
    /// </summary>
    [PublicAPI]
    public class HeaderArray
    {
        /// <summary>
        /// Represents a Header Array (HAR) file.
        /// </summary>
        /// <param name="file">
        /// The file path from which to read a HAR file.
        /// </param>
        public HeaderArray(string file)
        {
            if (file is null)
            {
                throw new ArgumentNullException(nameof(file));
            }


        }
    }
}