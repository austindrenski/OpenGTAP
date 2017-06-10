using System.Collections.Generic;
using JetBrains.Annotations;

namespace HeaderArrayConverter.Collections
{
    /// <summary>
    /// Represents a typed sequence that can be indexed by multiple keys.
    /// </summary>
    /// <typeparam name="TKey">
    /// The type of key in the sequence.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The type of value in the sequence.
    /// </typeparam>
    [PublicAPI]
    public interface ISequenceIndexer<TKey, TValue> : ISequenceIndexer<TKey>, IEnumerable<KeyValuePair<KeySequence<TKey>, TValue>>
    {
        /// <summary>
        /// Gets an <see cref="IEnumerable{T}"/> for the given keys.
        /// </summary>
        /// <param name="keys">
        /// The collection of keys.
        /// </param>
        /// <returns>
        /// An <see cref="IEnumerable{T}"/> for the given keys.
        /// </returns>
        [NotNull]
        new IEnumerable<KeyValuePair<KeySequence<TKey>, TValue>> this[params TKey[] keys] { get; }
    }
}