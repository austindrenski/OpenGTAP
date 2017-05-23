using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using JetBrains.Annotations;

namespace HeaderArrayConverter
{
    /// <summary>
    /// Represents a single entry from a Header Array (HAR) file.
    /// </summary>
    /// <typeparam name="T">
    /// The type of data in the array.
    /// </typeparam>
    [PublicAPI]
    public class HeaderArray<T> : HeaderArray
    {
        /// <summary>
        /// The decoded form of <see cref="Array"/>
        /// </summary>
        public ImmutableArray<T> Records { get; }

        /// <summary>
        /// Represents one entry from a Header Array (HAR) file.
        /// </summary>
        /// <param name="header">
        /// The four character identifier for this <see cref="HeaderArray"/>.
        /// </param>
        /// <param name="description">
        /// The long name description of the <see cref="HeaderArray"/>.
        /// </param>
        /// <param name="type">
        /// The type of element stored in the array.
        /// </param>
        /// <param name="dimensions">
        /// The dimensions of the array.
        /// </param>
        /// <param name="x0">
        /// The first dimension of the <see cref="HeaderArray"/>.
        /// This represents the number of arrays used to store this <see cref="HeaderArray"/> by Fortran.
        /// </param>
        /// <param name="x1">
        /// The second dimension of the <see cref="HeaderArray"/>.
        /// This represents the total number of elements in the logical array.
        /// </param>
        /// <param name="x2">
        /// The third dimension of the <see cref="HeaderArray"/>.
        /// This represents the maximum number of elements in any of the arrays used to store this <see cref="HeaderArray"/> by Fortran.
        /// </param>
        /// <param name="records">
        /// The data in the array.
        /// </param>
        public HeaderArray([NotNull] string header, [CanBeNull] string description, [NotNull] string type, int[] dimensions, int x0, int x1, int x2, [NotNull] T[] records)
            : base(header, description, type, dimensions, x0, x1, x2)
        {
            Records = records.ToImmutableArray();
        }

        /// <summary>
        /// Returns a string representation of the contents of this <see cref="HeaderArray"/>.
        /// </summary>
        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.AppendLine(base.ToString());

            for (int i = 0; i < Records.Length; i++)
            {
                stringBuilder.AppendLine($"[{i}]: {Records[i]}");
            }

            return stringBuilder.ToString();
        }
    }
}