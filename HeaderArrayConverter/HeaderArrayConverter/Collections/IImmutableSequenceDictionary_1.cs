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
    [PublicAPI]
    public interface IImmutableSequenceDictionary<TKey> : ISequenceIndexer<TKey>
    {
        /// <summary>
        /// Gets the number of entries stored in the dictionary.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Gets the total number of entries represented by the dictionary.
        /// </summary>
        int Total { get; }

        /// <summary>
        /// Gets an enumerable collection that contains the keys in the read-only dictionary.
        /// </summary>
        [NotNull]
        IEnumerable<KeySequence<TKey>> Keys { get; }

        /// <summary>
        /// Gets the sets that define this dictionary.
        /// </summary>
        [NotNull]
        IImmutableList<KeySequence<TKey>> Sets { get; }

        /// <summary>
        /// Gets the entry that has the specified key or the entries that begin with the specified key.
        /// </summary>
        [NotNull]
        new IImmutableSequenceDictionary<TKey> this[params TKey[] keys] { get; }
    }
}