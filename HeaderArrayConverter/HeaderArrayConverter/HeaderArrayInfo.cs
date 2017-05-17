using System;
using JetBrains.Annotations;

namespace HeaderArrayConverter
{
    /// <summary>
    /// Represents metadata on the <see cref="HeaderArray"/> located after the identifier and before the array contents.
    /// </summary>
    [PublicAPI]
    public struct HeaderArrayInfo
    {
        /// <summary>
        /// The total count of elements in the array.
        /// </summary>
        public int Count { get; }

        /// <summary>
        /// The long name description of the <see cref="HeaderArray"/>.
        /// </summary>
        [CanBeNull]
        public string Description { get; }

        /// <summary>
        /// The size in bytes of each element in the array.
        /// </summary>
        public int Size { get; }

        /// <summary>
        /// True if the array is sparsely populated; otherwise false.
        /// </summary>
        public bool Sparse { get; }

        /// <summary>
        /// The type of element stored in the array.
        /// </summary>
        [NotNull]
        public string Type { get; }

        /// <summary>
        /// Represents metadata on the <see cref="HeaderArray"/> located after the identifier and before the array contents.
        /// </summary>
        /// <param name="count">
        /// The total count of elements in the array.
        /// </param>
        /// <param name="description">
        /// The long name description of the <see cref="HeaderArray"/>.
        /// </param>
        /// <param name="size">
        /// The size in bytes of each element in the array.
        /// </param>
        /// <param name="sparse">
        /// True if the array is sparsely populated; otherwise false.
        /// </param>
        /// <param name="type">
        /// The type of element stored in the array.
        /// </param>
        public HeaderArrayInfo(int count, [CanBeNull] string description, int size, bool sparse, [NotNull] string type)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            Description = description?.Trim('\u0000', '\u0002', '\u0020');
            Count = count;
            Size = size;
            Type = type;
            Sparse = sparse;
        }

        /// <summary>
        /// Returns a string representation of the contents of this <see cref="HeaderArrayInfo"/>.
        /// </summary>
        public override string ToString()
        {
            return $"{nameof(Count)}: {Count}\r\n" +
                   $"{nameof(Description)}: {Description}\r\n" +
                   $"{nameof(Size)}: {Size}\r\n" +
                   $"{nameof(Sparse)}: {Sparse}\r\n" +
                   $"{nameof(Type)}: {Type}";
        }
    }
}