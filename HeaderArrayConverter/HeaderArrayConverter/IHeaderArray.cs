using System.Collections.Immutable;
using JetBrains.Annotations;

namespace HeaderArrayConverter
{
    /// <summary>
    /// Represents a header array.
    /// </summary>
    [PublicAPI]
    public interface IHeaderArray : ISequenceIndexer<string>
    {
        /// <summary>
        /// The header of the array.
        /// </summary>
        [NotNull]
        string Header { get; }

        /// <summary>
        /// An optional description of the array.
        /// </summary>
        [CanBeNull]
        string Description { get; }

        /// <summary>
        /// The type of the array.
        /// </summary>
        [NotNull]
        string Type { get; }

        /// <summary>
        /// The dimensions of the array.
        /// </summary>
        [NotNull]
        IImmutableList<int> Dimensions { get; }

        /// <summary>
        /// The sets of the array.
        /// </summary>
        [NotNull]
        IImmutableList<ImmutableOrderedSet<string>> Sets { get; }

        /// <summary>
        /// Casts the <see cref="IHeaderArray"/> as an <see cref="IHeaderArray{T}"/>.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the array.
        /// </typeparam>
        /// <returns>
        /// An <see cref="IHeaderArray{T}"/>.
        /// </returns>
        [Pure]
        [NotNull]
        IHeaderArray<T> As<T>();
    }
}