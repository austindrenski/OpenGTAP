﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using JetBrains.Annotations;

namespace HeaderArrayConverter
{
    /// <summary>
    /// Represents an immutable dictionary in which the insertion order is preserved.
    /// </summary>
    /// <typeparam name="TKey">
    /// The key type.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The item type.
    /// </typeparam>
    [PublicAPI]
    public class ImmutableOrderedDictionary<TKey, TValue> : IImmutableDictionary<KeySequence<TKey>, TValue>
    {
        /// <summary>
        /// The collection stored as an <see cref="ImmutableDictionary{TKey, TValue}"/>.
        /// </summary>
        [NotNull]
        private readonly IImmutableDictionary<KeySequence<TKey>, TValue> _dictionary;

        /// <summary>
        /// The collection stored as an <see cref="IImmutableList{T}"/> where T is <see cref="KeyValuePair{TKey, TValue}"/>.
        /// </summary>
        [NotNull]
        private readonly IImmutableList<KeyValuePair<KeySequence<TKey>, TValue>> _items;

        /// <summary>
        /// Gets the number of elements in the collection.
        /// </summary>
        public int Count => _dictionary.Count;

        /// <summary>
        /// Gets the element that has the specified key in the read-only dictionary.
        /// </summary>
        public TValue this[KeySequence<TKey> key] => _dictionary[key];

        /// <summary>
        /// Gets the element that has the specified key in the read-only dictionary.
        /// </summary>
        public IKeyValueSequence<TKey, TValue> this[params TKey[] keyComponents]
        {
            get
            {
                KeySequence<TKey> key = new KeySequence<TKey>(keyComponents);
                return 
                    _dictionary.ContainsKey(key) 
                    ? new KeyValueSequence<TKey, TValue>(key, _dictionary[key]) 
                    : new KeyValueSequence<TKey, TValue>(key, _dictionary.Where(x => x.Key.Take(key.Count).SequenceEqual(key)));
            }
        } 

        /// <summary>
        /// Gets an enumerable collection that contains the keys in the read-only dictionary.
        /// </summary>
        [NotNull]
        public IEnumerable<KeySequence<TKey>> Keys => _dictionary.Keys;

        /// <summary>
        /// Gets an enumerable collection that contains the values in the read-only dictionary.
        /// </summary>
        [NotNull]
        public IEnumerable<TValue> Values => _dictionary.Values;

        /// <summary>
        /// Gets an immutable collection of the immutable sets defined on the header array.
        /// </summary>
        [NotNull]
        [ItemNotNull]
        public IImmutableList<IImmutableSet<TKey>> Sets { get; }

        /// <summary>
        /// Constructs an <see cref="ImmutableOrderedDictionary{TKey, TValue}"/> in which the insertion order is preserved.
        /// </summary>
        /// <param name="source">
        /// The collection from which to create the 
        /// </param>
        public ImmutableOrderedDictionary([NotNull] IEnumerable<KeyValuePair<KeySequence<TKey>, TValue>> source)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            _items = source.ToImmutableArray();
            _dictionary = _items.ToImmutableDictionary();
            Sets = _dictionary.Keys.Select(x => (IImmutableSet<TKey>) x.ToImmutableHashSet()).ToImmutableArray();
        }

        /// <summary>
        /// Creates an <see cref="ImmutableOrderedDictionary{TKey, TValue}"/> from the collection.
        /// </summary>
        /// <param name="source">
        /// The source collection.
        /// </param>
        /// <returns>
        /// An <see cref="ImmutableOrderedDictionary{TKey, TValue}"/> created from the collection.
        /// </returns>
        [Pure]
        [NotNull]
        public static ImmutableOrderedDictionary<TKey, TValue> Create(IEnumerable<KeyValuePair<KeySequence<TKey>, TValue>> source)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return new ImmutableOrderedDictionary<TKey, TValue>(source);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// An enumerator that can be used to iterate through the collection.
        /// </returns>
        [Pure]
        [NotNull]
        public IEnumerator<KeyValuePair<KeySequence<TKey>, TValue>> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// An enumerator that can be used to iterate through the collection.
        /// </returns>
        [Pure]
        [NotNull]
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _items.GetEnumerator();
        }

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
        public bool ContainsKey(KeySequence<TKey> key)
        {
            return _dictionary.ContainsKey(key);
        }

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
        public bool TryGetValue(KeySequence<TKey> key, out TValue value)
        {
            return _dictionary.TryGetValue(key, out value);
        }

        /// <summary>
        /// Gets an empty dictionary with equivalent ordering and key/value comparison rules.
        /// </summary>
        [Pure]
        [NotNull]
        public IImmutableDictionary<KeySequence<TKey>, TValue> Clear()
        {
            return _dictionary.Any() ? Create(Enumerable.Empty<KeyValuePair<KeySequence<TKey>, TValue>>()) : this;
        }

        /// <summary>
        /// Searches the dictionary for a given key and returns the equal key it finds, if any.
        /// </summary>
        /// <param name="equalKey">
        /// The key to search for.
        /// </param>
        /// <param name="actualKey">
        /// The key from the dictionary that the search found, or <paramref name="equalKey" /> if the search yielded no match.
        /// </param>
        /// <returns>
        /// A value indicating whether the search was successful.
        /// </returns>
        [Pure]
        public bool TryGetKey(KeySequence<TKey> equalKey, out KeySequence<TKey> actualKey)
        {
            return _dictionary.TryGetKey(equalKey, out actualKey);
        }

        /// <summary>
        /// Adds the specified key and value to the dictionary.
        /// </summary>
        /// <param name="key">
        /// The key of the entry to add.
        /// </param>
        /// <param name="value">
        /// The value of the entry to add.
        /// </param>
        /// <returns>
        /// The new dictionary containing the additional key-value pair.
        /// </returns>
        [Pure]
        [NotNull]
        public IImmutableDictionary<KeySequence<TKey>, TValue> Add(KeySequence<TKey> key, TValue value)
        {
            return _dictionary.Add(key, value);
        }

        /// <summary>
        /// Adds the specified key-value pairs to the dictionary.
        /// </summary>
        /// <param name="pairs"
        /// >The pairs to add.
        /// </param>
        /// <returns>
        /// The new dictionary containing the additional key-value pairs.
        /// </returns>
        [Pure]
        [NotNull]
        public IImmutableDictionary<KeySequence<TKey>, TValue> AddRange(IEnumerable<KeyValuePair<KeySequence<TKey>, TValue>> pairs)
        {
            return _dictionary.AddRange(pairs);
        }

        /// <summary>
        /// Sets the specified key and value to the dictionary, possibly overwriting an existing value for the given key.
        /// </summary>
        /// <param name="key">
        /// The key of the entry to add.
        /// </param>
        /// <param name="value">
        /// The value of the entry to add.
        /// </param>
        /// <returns>
        /// The new dictionary containing the additional key-value pair.
        /// </returns>
        [Pure]
        [NotNull]
        public IImmutableDictionary<KeySequence<TKey>, TValue> SetItem(KeySequence<TKey> key, TValue value)
        {
            return _dictionary.SetItem(key, value);
        }

        /// <summary>
        /// Applies a given set of key=value pairs to an immutable dictionary, replacing any conflicting keys in the resulting dictionary.
        /// </summary>
        /// <param name="items">
        /// The key=value pairs to set on the dictionary.  Any keys that conflict with existing keys will overwrite the previous values.
        /// </param>
        /// <returns>
        /// An immutable dictionary.
        /// </returns>
        [Pure]
        [NotNull]
        public IImmutableDictionary<KeySequence<TKey>, TValue> SetItems(IEnumerable<KeyValuePair<KeySequence<TKey>, TValue>> items)
        {
            return _dictionary.SetItems(items);
        }

        /// <summary>
        /// Removes the specified keys from the dictionary with their associated values.
        /// </summary>
        /// <param name="keys">
        /// The keys to remove.
        /// </param>
        /// <returns>
        /// A new dictionary with those keys removed; or this instance if those keys are not in the dictionary.
        /// </returns>
        [Pure]
        [NotNull]
        public IImmutableDictionary<KeySequence<TKey>, TValue> RemoveRange(IEnumerable<KeySequence<TKey>> keys)
        {
            return _dictionary.RemoveRange(keys);
        }

        /// <summary>
        /// Removes the specified key from the dictionary with its associated value.
        /// </summary>
        /// <param name="key">
        /// The key to remove.
        /// </param>
        /// <returns>
        /// A new dictionary with the matching entry removed; or this instance if the key is not in the dictionary.
        /// </returns>
        [Pure]
        [NotNull]
        public IImmutableDictionary<KeySequence<TKey>, TValue> Remove(KeySequence<TKey> key)
        {
            return _dictionary.Remove(key);
        }

        /// <summary>
        /// Determines whether this dictionary contains the specified key-value pair.
        /// </summary>
        /// <param name="pair">
        /// The key value pair.
        /// </param>
        /// <returns>
        /// True if this dictionary contains the key-value pair; otherwise, false.
        /// </returns>
        [Pure]
        public bool Contains(KeyValuePair<KeySequence<TKey>, TValue> pair)
        {
            return _dictionary.Contains(pair);
        }
    }
}