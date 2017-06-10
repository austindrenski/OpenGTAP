using System.Collections;
using JetBrains.Annotations;

namespace HeaderArrayConverter.Collections
{
    /// <summary>
    /// Represents an untyped sequence that can be indexed by multiple keys.
    /// </summary>
    /// <typeparam name="TKey">
    /// The type of key in the sequence.
    /// </typeparam>
    [PublicAPI]
    public interface ISequenceIndexer<in TKey> : IEnumerable
    {
        /// <summary>
        /// Gets an <see cref="IEnumerable"/> for the given keys.
        /// </summary>
        /// <param name="keys">
        /// The collection of keys.
        /// </param>
        /// <returns>
        /// An <see cref="IEnumerable"/> for the given keys.
        /// </returns>
        [NotNull]
        IEnumerable this[params TKey[] keys] { get; }
    }
}