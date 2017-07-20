using System;
using System.Collections.Generic;
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
    public interface IImmutableSequenceDictionary<TKey, TValue> : IImmutableSequenceDictionary<TKey>, ISequenceIndexer<TKey, TValue>, IReadOnlyDictionary<KeySequence<TKey>, TValue>, IDictionary<KeySequence<TKey>, TValue> where TKey : IEquatable<TKey> where TValue : IEquatable<TValue>
    {
        /// <summary>
        /// Gets the number of logical entries in the dictionary.
        /// </summary>
        new int Count { get; }

        /// <summary>
        /// Gets the element that has the specified key.
        /// </summary>
        new TValue this[KeySequence<TKey> key] { get; }

        /// <summary>
        /// Gets the entry that has the specified key or the entries that begin with the specified key.
        /// </summary>
        [NotNull]
        new IImmutableSequenceDictionary<TKey, TValue> this[params TKey[] keys] { get; }
        
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

        /// <summary>
        /// Determines whether the read-only dictionary contains an element that has the specified key.
        /// </summary>
        /// <param name="key">
        /// The key to locate.
        /// </param>
        /// <returns>
        /// True if the read-only dictionary contains an element that has the specified key; otherwise, false.
        /// </returns> 
        [Pure]
        new bool ContainsKey(KeySequence<TKey> key);

        /// <summary>
        /// Gets the value that is associated with the specified key.
        /// </summary>
        /// <param name="key">
        /// The key to locate.
        /// </param>
        /// <param name="value">
        /// When this method returns, the value associated with the specified key, if the key is found; otherwise, the default value for the type of the <paramref name="value" /> parameter. This parameter is passed uninitialized.
        /// </param>
        /// <returns>
        /// True if the object that implements the <see cref="System.Collections.Generic.IReadOnlyDictionary{TKey, TValue}"/> interface contains an element that has the specified key; otherwise, false.
        /// </returns>
        [Pure]
        new bool TryGetValue(KeySequence<TKey> key, out TValue value);
    }
}