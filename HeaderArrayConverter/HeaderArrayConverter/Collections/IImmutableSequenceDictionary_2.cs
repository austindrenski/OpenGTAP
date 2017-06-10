using System.Collections.Generic;
using System.Collections.Immutable;
using JetBrains.Annotations;

namespace HeaderArrayConverter.Collections
{
    /// <summary>
    /// Represents an immutable dictionary using sequence keys and in which the insertion order is preserved.
    /// </summary>
    /// <typeparam name="TKey">
    /// The key type.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The item type.
    /// </typeparam>
    [PublicAPI]
    public interface IImmutableSequenceDictionary<TKey, TValue> : IImmutableSequenceDictionary<TKey>, ISequenceIndexer<TKey, TValue>, IImmutableDictionary<KeySequence<TKey>, TValue>
    {
        /// <summary>
        /// Gets the entry that has the specified key or the entries that begin with the specified key.
        /// </summary>
        [NotNull]
        new ImmutableSequenceDictionary<TKey, TValue> this[params TKey[] keys] { get; }

        /// <summary>
        /// Gets the number of entries stored in the dictionary.
        /// </summary>
        new int Count { get; }

        /// <summary>
        /// Gets an enumerable collection that contains the keys in the read-only dictionary.
        /// </summary>
        [NotNull]
        new IEnumerable<KeySequence<TKey>> Keys { get; }

        /// <summary>
        /// Gets an enumerable collection that contains the values in the read-only dictionary.
        /// </summary>
        [NotNull]
        new IEnumerable<TValue> Values { get; }
    }
}