using JetBrains.Annotations;

namespace HeaderArrayConverter
{
    /// <summary>
    /// Represents an <see cref="IHeaderArray{T}"/>.
    /// </summary>
    /// <typeparam name="T">
    /// The type of element in the array.
    /// </typeparam>
    [PublicAPI]
    public interface IHeaderArray<T> : IHeaderArray, ISequenceIndexer<string, T>
    {
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
        new ImmutableSequenceDictionary<string, T> this[params string[] keys] { get; }
    }
}