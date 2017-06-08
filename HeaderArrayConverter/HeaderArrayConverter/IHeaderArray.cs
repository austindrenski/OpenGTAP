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
        IImmutableList<IImmutableList<string>> Sets { get; }

        /// <summary>
        /// Returns the value with the key defined by the key components or throws an exception if the key is not found.
        /// </summary>
        /// <param name="keys">
        /// The components that define the key whose value is returned.
        /// </param>
        /// <returns>
        /// The value stored by the given key.
        /// </returns>
        [NotNull]
        new ImmutableSequenceDictionary<string, object> this[params string[] keys] { get; }

        /// <summary>
        /// Casts the <see cref="IHeaderArray"/> as an <see cref="IHeaderArray{TResult}"/>.
        /// </summary>
        /// <typeparam name="TResult">
        /// The type of the array.
        /// </typeparam>
        /// <returns>
        /// An <see cref="IHeaderArray{TResult}"/>.
        /// </returns>
        [Pure]
        [NotNull]
        IHeaderArray<TResult> As<TResult>();

        /// <summary>
        /// Returns a JSON representation of this <see cref="IHeaderArray{T}"/>.
        /// </summary>
        [Pure]
        [NotNull]
        string ToJson();
    }
}