using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HeaderArrayConverter.Extensions;
using JetBrains.Annotations;
using Newtonsoft.Json;

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
    [JsonDictionary]
    public class ImmutableSequenceDictionary<TKey, TValue> : IImmutableSequenceDictionary<TKey, TValue> where TKey : IEquatable<TKey> where TValue : IEquatable<TValue>
    {
        /// <summary>
        /// Magic string marking an index set.
        /// </summary>
        [NotNull]
        private static readonly string Index = "INDEX";

        /// <summary>
        /// Compares dictionary entries based on the keys.
        /// </summary>
        [NotNull]
        private static KeyComparer DistinctKeyComparer { get; } = new KeyComparer();

        /// <summary>
        /// The collection stored as an <see cref="ImmutableDictionary{TKey, TValue}"/>.
        /// </summary>
        [NotNull]
        [JsonProperty]
        private readonly IReadOnlyDictionary<KeySequence<TKey>, TValue> _dictionary;

        /// <summary>
        /// Gets the number of logical entries in the dictionary.
        /// </summary>
        public int Count => Math.Max(Sets.Aggregate(1, (current, next) => current * next.Value.Count), _dictionary.Count);

        /// <summary>
        /// Gets the total number of entries represented by the dictionary.
        /// </summary>
        public int StoredCount => _dictionary.Count;

        /// <summary>
        /// Gets the element that has the specified key.
        /// </summary>
        public TValue this[KeySequence<TKey> key] => _dictionary[key];

        /// <summary>
        /// Gets the entry that has the specified key or the entries that begin with the specified key.
        /// </summary>
        public IImmutableSequenceDictionary<TKey, TValue> this[params TKey[] keys]
        {
            get
            {
                KeySequence<TKey> key = new KeySequence<TKey>(keys);

                if (_dictionary.ContainsKey(key))
                {
                    return Create(Sets, new KeyValuePair<KeySequence<TKey>, TValue>(key, _dictionary[key]));
                }

                if (key.Count != Sets.Count || Sets.Zip(key, (s, k) => s.Value.Contains(k)).Any(x => !x))
                {
                    throw new KeyNotFoundException(key.ToString());
                }

                return
                    key.Count == Sets.Count
                        ? Create(Sets, new KeyValuePair<KeySequence<TKey>, TValue>(key, default(TValue)))
                        : Create(Sets, _dictionary.Where(x => x.Key.Take(key.Count).SequenceEqual(key)));
            }
        }

        /// <summary>
        /// Gets the entry that has the specified key or the entries that begin with the specified key.
        /// </summary>
        IImmutableSequenceDictionary<TKey> IImmutableSequenceDictionary<TKey>.this[params TKey[] keys] => this[keys];

        /// <summary>
        /// Gets an <see cref="IEnumerable{T}"/> for the given keys.
        /// </summary>
        /// <param name="keys">
        /// The collection of keys.
        /// </param>
        /// <returns>
        /// An <see cref="IEnumerable{T}"/> for the given keys.
        /// </returns>
        IEnumerable<KeyValuePair<KeySequence<TKey>, TValue>> ISequenceIndexer<TKey, TValue>.this[params TKey[] keys] => this[keys];

        /// <summary>
        /// Gets an <see cref="IEnumerable"/> for the given keys.
        /// </summary>
        /// <param name="keys">
        /// The collection of keys.
        /// </param>
        /// <returns>
        /// An <see cref="IEnumerable"/> for the given keys.
        /// </returns>
        IEnumerable ISequenceIndexer<TKey>.this[params TKey[] keys] => this[keys];

        /// <summary>
        /// Gets or sets the element with the specified key.
        /// </summary>
        /// <param name="key">
        /// The key of the element to get or set.
        /// </param>
        /// <returns>
        /// The element with the specified key.
        /// </returns>
        TValue IDictionary<KeySequence<TKey>, TValue>.this[KeySequence<TKey> key]
        {
            get => _dictionary[key];
            set => throw new NotSupportedException();
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="ICollection{T}" /> is read-only.
        /// </summary>
        /// <returns>
        /// True if the <see cref="ICollection{T}"/> is read-only; otherwise, false.
        /// </returns>
        bool ICollection<KeyValuePair<KeySequence<TKey>, TValue>>.IsReadOnly => true;

        /// <summary>
        /// Gets an <see cref="ICollection{T}"/> containing the values in the <see cref="IDictionary{TKey, TValue}" />.
        /// </summary>
        /// <returns>
        /// An <see cref="ICollection{T}" /> containing the values in the object that implements <see cref="IDictionary{TKey, TValue}" />.
        /// </returns>
        [NotNull]
        ICollection<TValue> IDictionary<KeySequence<TKey>, TValue>.Values => Values.ToArray();

        /// <summary>
        /// Gets an <see cref="ICollection{T}" /> containing the keys of the <see cref="IDictionary{TKey, TValue}" />.
        /// </summary>
        /// <returns>
        /// An <see cref="ICollection{T}" /> containing the keys of the object that implements <see cref="IDictionary{TKey, TValue}" />.
        /// </returns>
        [NotNull]
        ICollection<KeySequence<TKey>> IDictionary<KeySequence<TKey>, TValue>.Keys => Keys.ToArray();

        /// <summary>
        /// Gets an enumerable collection that contains the keys in the read-only dictionary.
        /// </summary>
        public IEnumerable<KeySequence<TKey>> Keys => Sets.AsExpandedSet();

        /// <summary>
        /// Gets an enumerable collection that contains the values in the read-only dictionary.
        /// </summary>
        public IEnumerable<TValue> Values
        {
            get
            {
                if (Sets.Count == 0)
                {
                    return _dictionary.Values;
                }

                if (Sets.Count == 1 && Sets.Single().Key.Equals(Index, StringComparison.OrdinalIgnoreCase))
                {
                    return 
                        _dictionary.AsParallel()
                                   .OrderBy(x => int.Parse((string) x.Key))
                                   .Select(x => x.Value);
                }

                return
                    Sets.AsExpandedSet()
                        .DefaultIfEmpty()
                        .AsParallel()
                        .AsOrdered()
                        .Select(
                            x =>
                            {
                                _dictionary.TryGetValue(x, out TValue value);
                                return value;
                            });
            }
        }

        /// <summary>
        /// Gets the sets that define this dictionary.
        /// </summary>
        public IImmutableList<KeyValuePair<string, IImmutableList<TKey>>> Sets { get; }

        /// <summary>
        /// Constructs an <see cref="ImmutableSequenceDictionary{TKey, TValue}"/> in which the insertion order is preserved.
        /// </summary>
        /// <param name="sets">
        /// The collection of sets that define this dictionary.
        /// </param>
        /// <param name="source">
        /// The collection from which to create the 
        /// </param>
        public ImmutableSequenceDictionary([NotNull] IEnumerable<KeyValuePair<string, IImmutableList<TKey>>> sets, [NotNull] IEnumerable<KeyValuePair<KeySequence<TKey>, TValue>> source)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            Sets = sets as IImmutableList<KeyValuePair<string, IImmutableList<TKey>>> ?? sets.ToImmutableArray();

            if (source is IDictionary<KeySequence<TKey>, TValue> dictionary)
            {
                _dictionary = new Dictionary<KeySequence<TKey>, TValue>(dictionary);
            }
            else if (Sets.Count == 1 && Sets.First().Key.Equals(Index, StringComparison.OrdinalIgnoreCase))
            {
                _dictionary = source.AsParallel().ToDictionary(x => x.Key, x => x.Value);
            }
            else
            {
                _dictionary = source.AsParallel().Where(x => !x.Value.Equals(default(TValue))).ToDictionary(x => x.Key, x => x.Value);
            }
        }

        /// <summary>
        /// Creates an <see cref="ImmutableSequenceDictionary{TKey, TValue}"/> from the collection.
        /// </summary>
        /// <param name="sets">
        /// The collection of sets that define this dictionary.
        /// </param>
        /// <param name="source">
        /// The source collection.
        /// </param>
        /// <returns>
        /// An <see cref="ImmutableSequenceDictionary{TKey, TValue}"/> created from the collection.
        /// </returns>
        [Pure]
        [NotNull]
        public static ImmutableSequenceDictionary<TKey, TValue> Create([NotNull] IEnumerable<KeyValuePair<string, IImmutableList<TKey>>> sets, [NotNull] IEnumerable<KeyValuePair<KeySequence<TKey>, TValue>> source)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return new ImmutableSequenceDictionary<TKey, TValue>(sets, source);
        }

        /// <summary>
        /// Creates an <see cref="ImmutableSequenceDictionary{TKey, TValue}"/> from the collection.
        /// </summary>
        /// <param name="sets">
        /// The collection of sets that define this dictionary.
        /// </param>
        /// <param name="source">
        /// The source collection.
        /// </param>
        /// <returns>
        /// An <see cref="ImmutableSequenceDictionary{TKey, TValue}"/> created from the collection.
        /// </returns>
        [Pure]
        [NotNull]
        public static ImmutableSequenceDictionary<TKey, TValue> Create([NotNull] IEnumerable<KeyValuePair<string, IImmutableList<TKey>>> sets, params KeyValuePair<KeySequence<TKey>, TValue>[] source)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return new ImmutableSequenceDictionary<TKey, TValue>(sets, source);
        }

        /// <summary>
        /// Creates an <see cref="ImmutableSequenceDictionary{TKey, TValue}"/> from the collection.
        /// </summary>
        /// <param name="sets">
        /// The index set that define this dictionary.
        /// </param>
        /// <param name="source">
        /// The source collection.
        /// </param>
        /// <returns>
        /// An <see cref="ImmutableSequenceDictionary{TKey, TValue}"/> created from the collection.
        /// </returns>
        [Pure]
        [NotNull]
        public static ImmutableSequenceDictionary<TKey, TValue> Create([NotNull] IEnumerable<KeyValuePair<string, IImmutableList<TKey>>> sets, [NotNull] IEnumerable<TValue> source)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            TValue[] values = source as TValue[] ?? source.ToArray();

            KeyValuePair<string, IImmutableList<TKey>>[] setsArray = sets as KeyValuePair<string, IImmutableList<TKey>>[] ?? sets.ToArray();

            KeySequence<TKey>[] keys = setsArray.AsExpandedSet().ToArray();

            KeyValuePair<KeySequence<TKey>, TValue>[] items = new KeyValuePair<KeySequence<TKey>, TValue>[values.Length];
            for (int i = 0; i < items.Length; i++)
            {
                items[i] = new KeyValuePair<KeySequence<TKey>, TValue>(keys[i], values[i]);
            }

            return new ImmutableSequenceDictionary<TKey, TValue>(setsArray, items);
        }

        /// <summary>
        /// Creates an <see cref="ImmutableSequenceDictionary{TKey, TValue}"/> from the collection.
        /// </summary>
        /// <param name="source">
        /// The source collection.
        /// </param>
        /// <returns>
        /// An <see cref="ImmutableSequenceDictionary{TKey, TValue}"/> created from the collection.
        /// </returns>
        [Pure]
        [NotNull]
        public static ImmutableSequenceDictionary<string, TValue> Create([NotNull] IEnumerable<TValue> source)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            TValue[] values = source as TValue[] ?? source.ToArray();

            IImmutableList<KeyValuePair<string, IImmutableList<string>>> sets = 
                ImmutableArray.Create(
                    new KeyValuePair<string, IImmutableList<string>>(
                        Index, 
                        Enumerable.Range(0, values.Length).Select(x => x.ToString()).ToImmutableArray()));

            KeyValuePair<KeySequence<string>, TValue>[] items = new KeyValuePair<KeySequence<string>, TValue>[values.Length];
            for (int i = 0; i < items.Length; i++)
            {
                items[i] = new KeyValuePair<KeySequence<string>, TValue>(i.ToString(), values[i]);
            }
            
            return new ImmutableSequenceDictionary<string, TValue>(sets, items);
        }

        /// <summary>
        /// Returns a string representation of the contents of the header array.
        /// </summary>
        [Pure]
        [NotNull]
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }

        /// <summary>
        /// Returns an enumerable that iterates through the logical collection as defined by the <see cref="Sets"/>.
        /// </summary>
        /// <returns>
        /// An enumerator that can be used to iterate through the collection.
        /// </returns>
        [Pure]
        [NotNull]
        public IEnumerator<KeyValuePair<KeySequence<TKey>, TValue>> GetEnumerator()
        {
            if (Sets.Count == 0)
            {
                return _dictionary.GetEnumerator();
            }

            if (Sets.Count == 1 && Sets.Single().Key.Equals(Index, StringComparison.OrdinalIgnoreCase))
            {
                return _dictionary.AsParallel().OrderBy(x => int.Parse((string) x.Key)).GetEnumerator();
            }
            
            return
                Sets.AsExpandedSet()
                    .AsParallel()
                    .AsOrdered()
                    .Select(
                        x =>
                        {
                            _dictionary.TryGetValue(x, out TValue value);
                            return new KeyValuePair<KeySequence<TKey>, TValue>(x, value);
                        })
                    .GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerable that iterates through the logical collection as defined by the <see cref="Sets"/>.
        /// </summary>
        /// <returns>
        /// An enumerator that can be used to iterate through the collection.
        /// </returns>
        [Pure]
        [NotNull]
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
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
        /// Determines whether this dictionary contains the specified key-value pair.
        /// </summary>
        /// <param name="pair">
        /// The key value pair.
        /// </param>
        /// <returns>
        /// True if this dictionary contains the key-value pair; otherwise, false.
        /// </returns>
        [Pure]
        bool ICollection<KeyValuePair<KeySequence<TKey>, TValue>>.Contains(KeyValuePair<KeySequence<TKey>, TValue> pair)
        {
            return _dictionary.Contains(pair);
        }

        /// <summary>
        /// Provides comparisons between entries based on the keys.
        /// </summary>
        private sealed class KeyComparer : IEqualityComparer<KeyValuePair<KeySequence<TKey>, TValue>>
        {
            /// <summary>
            /// Determines whether the specified objects are equal.
            /// </summary>
            /// <param name="x">
            /// The first object to compare.
            /// </param>
            /// <param name="y">
            /// The second object to compare.
            /// </param>
            /// <returns>
            /// True if the specified objects are equal; otherwise, false.
            /// </returns>
            public bool Equals(KeyValuePair<KeySequence<TKey>, TValue> x, KeyValuePair<KeySequence<TKey>, TValue> y)
            {
                return x.Key.Equals(y.Key);
            }

            /// <summary>
            /// Returns a hash code for the specified object.
            /// </summary>
            /// <param name="obj">
            /// The <see cref="object"/> for which a hash code is to be returned.
            /// </param>
            /// <returns>
            /// A hash code for the specified object.
            /// </returns>
            public int GetHashCode(KeyValuePair<KeySequence<TKey>, TValue> obj)
            {
                return obj.Key.GetHashCode();
            }
        }

        #region IDictionary implementation for serialization

        void ICollection<KeyValuePair<KeySequence<TKey>, TValue>>.Clear()
        {
            throw new NotSupportedException();
        }

        void ICollection<KeyValuePair<KeySequence<TKey>, TValue>>.CopyTo(KeyValuePair<KeySequence<TKey>, TValue>[] array, int arrayIndex)
        {
            throw new NotSupportedException();
        }

        void ICollection<KeyValuePair<KeySequence<TKey>, TValue>>.Add(KeyValuePair<KeySequence<TKey>, TValue> item)
        {
            throw new NotSupportedException();
        }

        bool ICollection<KeyValuePair<KeySequence<TKey>, TValue>>.Remove(KeyValuePair<KeySequence<TKey>, TValue> item)
        {
            throw new NotSupportedException();
        }

        void IDictionary<KeySequence<TKey>, TValue>.Add(KeySequence<TKey> key, TValue value)
        {
            throw new NotSupportedException();
        }

        bool IDictionary<KeySequence<TKey>, TValue>.Remove(KeySequence<TKey> key)
        {
            throw new NotSupportedException();
        }

        #endregion IDictionary implementation for serialization
    }
}