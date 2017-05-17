using System;
using System.Collections.Immutable;
using System.Linq;
using JetBrains.Annotations;

namespace HeaderArrayConverter
{
    /// <summary>
    /// Represents one entry from a Header Array (HAR) file.
    /// </summary>
    [PublicAPI]
    public struct HeaderArray
    {
        /// <summary>
        /// The four character identifier for this <see cref="HeaderArray"/>.
        /// </summary>
        public string Header { get; }
            
        /// <summary>
        /// The first dimension of the <see cref="HeaderArray"/>.
        /// This represents the number of arrays used to store this <see cref="HeaderArray"/> by Fortran.
        /// </summary>
        public int X0 { get; }

        /// <summary>
        /// The second dimension of the <see cref="HeaderArray"/>.
        /// This represents the total number of elements in the logical array.
        /// </summary>
        public int X1 { get; }

        /// <summary>
        /// The third dimension of the <see cref="HeaderArray"/>.
        /// This represents the maximum number of elements in any of the arrays used to store this <see cref="HeaderArray"/> by Fortran.
        /// </summary>
        public int X2 { get; }

        /// <summary>
        /// Represents metadata on the <see cref="HeaderArray"/> located after the identifier and before the array contents.
        /// </summary>
        public HeaderArrayInfo Info { get; }

        /// <summary>
        /// The immutable byte array for each record in the logical array.
        /// </summary>
        public ImmutableArray<ImmutableArray<byte>> Array { get; }

        /// <summary>
        /// Represents one entry from a Header Array (HAR) file.
        /// </summary>
        /// <param name="header">
        /// The four character identifier for this <see cref="HeaderArray"/>.
        /// </param>
        /// <param name="info">
        /// Represents metadata on the <see cref="HeaderArray"/> located after the identifier and before the array contents.
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
        /// <param name="array">
        /// The immutable byte array for each record in the logical array.
        /// </param>
        public HeaderArray([NotNull] string header, HeaderArrayInfo info, int x0,  int x1, int x2, [NotNull][ItemNotNull] byte[][] array)
        {
            if (header is null)
            {
                throw new ArgumentNullException(nameof(header));
            }
            if (array is null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            Header = header;
            Info = info;
            X0 = x0;
            X1 = x1;
            X2 = x2;
            Array = array.Select(x => x.ToImmutableArray()).ToImmutableArray();
        }

        /// <summary>
        /// Returns a string representation of the contents of this <see cref="HeaderArray"/>.
        /// </summary>
        public override string ToString()
        {
            return $"{nameof(Header)}: {Header}\r\n" +
                    $"Dimensions: [{X0}][{X1}][{X2}]\r\n" +
                    $"{Info}";
        }
    }
}