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
        /// Compares dictionary entries based on the keys.
        /// </summary>
        [NotNull]
        private static KeyComparer DistinctKeyComparer { get; } = new KeyComparer();

        private readonly KeyComparison _comparer;

        /// <summary>
        /// The collection stored as an <see cref="ImmutableDictionary{TKey, TValue}"/>.
        /// </summary>
        [NotNull]
        [JsonProperty]
        private readonly IDictionary<KeySequence<TKey>, TValue> _dictionary;

        /// <summary>
        /// Gets the number of entries stored in the dictionary.
        /// </summary>
        public int Count => _dictionary.Count;

        /// <summary>
        /// Gets the total number of entries represented by the dictionary.
        /// </summary>
        public int Total => Math.Max(Sets.Aggregate(1, (current, next) => current * next.Value.Count), _dictionary.Count);

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

        IImmutableSequenceDictionary<TKey> IImmutableSequenceDictionary<TKey>.this[params TKey[] keys] => this[keys];

        IEnumerable<KeyValuePair<KeySequence<TKey>, TValue>> ISequenceIndexer<TKey, TValue>.this[params TKey[] keys] => this[keys];

        IEnumerable ISequenceIndexer<TKey>.this[params TKey[] keys] => this[keys];

        TValue IReadOnlyDictionary<KeySequence<TKey>, TValue>.this[KeySequence<TKey> key] => _dictionary[key];

        /// <summary>
        /// Gets an enumerable collection that contains the keys in the read-only dictionary.
        /// </summary>
        public IEnumerable<KeySequence<TKey>> Keys => _dictionary.Keys;

        /// <summary>
        /// Gets an enumerable collection that contains the values in the read-only dictionary.
        /// </summary>
        public IEnumerable<TValue> Values => _dictionary.Values;

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
        private ImmutableSequenceDictionary([NotNull] IEnumerable<KeyValuePair<string, IImmutableList<TKey>>> sets, [NotNull] IEnumerable<KeyValuePair<KeySequence<TKey>, TValue>> source)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            Sets = sets as IImmutableList<KeyValuePair<string, IImmutableList<TKey>>> ?? sets.ToImmutableArray();

            //_comparer = new KeyComparison(Sets);

            _dictionary = source.AsParallel().Where(x => !x.Value.Equals(default(TValue))).ToDictionary(x => x.Key, x => x.Value);
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

            return Create(sets, source as IEnumerable<KeyValuePair<KeySequence<TKey>, TValue>>);
        }

        /// <summary>
        /// Returns a string representation of the contents of the header array.
        /// </summary>
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
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
            return _dictionary.GetEnumerator();
        }

        [Pure]
        [NotNull]
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerable that iterates through the logical collection as defined by the <see cref="Sets"/>.
        /// </summary>
        /// <returns>
        /// An enumerable that can be used to iterate through the logical collection as defined by the <see cref="Sets"/>.
        /// </returns>
        [Pure]
        public IEnumerable<KeyValuePair<KeySequence<TKey>, TValue>> GetLogicalEnumerable()
        {
            if (Sets.Count == 0)
            {
                return _dictionary;
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
                        });
        }

        [Pure]
        IEnumerable<KeyValuePair<KeySequence<TKey>, object>> IImmutableSequenceDictionary<TKey>.GetLogicalEnumerable()
        {
            if (Sets.Count == 0)
            {
                return _dictionary.Select(x => new KeyValuePair<KeySequence<TKey>, object>(x.Key, x.Value));
            }

            return
                Sets.AsExpandedSet()
                    .AsParallel()
                    .AsOrdered()
                    .Select(
                        x =>
                        {
                            _dictionary.TryGetValue(x, out TValue value);
                            return new KeyValuePair<KeySequence<TKey>, object>(x, value);
                        });
        }

        /// <summary>
        /// Returns an enumerable that iterates through the logical value collection as defined by the <see cref="Sets"/>.
        /// </summary>
        /// <returns>
        /// An enumerable that can be used to iterate through the logical value collection as defined by the <see cref="Sets"/>.
        /// </returns>
        [Pure]
        public IEnumerable<TValue> GetLogicalValuesEnumerable()
        {

            if (Sets.Count == 0)
            {
                return _dictionary.Values;
            }

            return
                Sets.AsExpandedSet()
                    .AsParallel()
                    .AsOrdered()
                    .Select(
                        x =>
                        {
                            _dictionary.TryGetValue(x, out TValue value);
                            return value;
                        });
        }

        [Pure]
        IEnumerable IImmutableSequenceDictionary<TKey>.GetLogicalValuesEnumerable(IComparer<KeySequence<TKey>> keyComparer)
        {
            return GetLogicalValuesEnumerable(keyComparer);
        }

        /// <summary>
        /// Returns an enumerable that iterates through the logical value collection as defined by the <see cref="Sets"/>.
        /// </summary>
        /// <returns>
        /// An enumerable that can be used to iterate through the logical value collection as defined by the <see cref="Sets"/>.
        /// </returns>
        [Pure]
        public IEnumerable<TValue> GetLogicalValuesEnumerable(IComparer<KeySequence<TKey>> keyComparer)
        {
            if (Sets.Count == 0)
            {
                return _dictionary.Values;
            }

            return
                Sets.AsExpandedSet()
                    .DefaultIfEmpty()
                    .AsParallel()
                    .OrderBy(x => x, keyComparer)
                    .Select(
                        x =>
                        {
                            _dictionary.TryGetValue(x, out TValue value);
                            return value;
                        });
        }

        [Pure]
        IEnumerable IImmutableSequenceDictionary<TKey>.GetLogicalValuesEnumerable()
        {
            return GetLogicalValuesEnumerable();
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
        public bool Contains(KeyValuePair<KeySequence<TKey>, TValue> pair)
        {
            return _dictionary.Contains(pair);
        }

        #region IDictionary implementation for serialization

        bool ICollection<KeyValuePair<KeySequence<TKey>, TValue>>.IsReadOnly => throw new NotSupportedException();

        ICollection<TValue> IDictionary<KeySequence<TKey>, TValue>.Values => throw new NotSupportedException();

        ICollection<KeySequence<TKey>> IDictionary<KeySequence<TKey>, TValue>.Keys => throw new NotSupportedException();

        void IDictionary<KeySequence<TKey>, TValue>.Add(KeySequence<TKey> key, TValue value)
        {
            throw new NotSupportedException();
        }
        bool IDictionary<KeySequence<TKey>, TValue>.Remove(KeySequence<TKey> key)
        {
            throw new NotSupportedException();
        }
        TValue IDictionary<KeySequence<TKey>, TValue>.this[KeySequence<TKey> key]
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }
        void ICollection<KeyValuePair<KeySequence<TKey>, TValue>>.Add(KeyValuePair<KeySequence<TKey>, TValue> item)
        {
            throw new NotSupportedException();
        }

        void ICollection<KeyValuePair<KeySequence<TKey>, TValue>>.Clear()
        {
            throw new NotSupportedException();
        }

        void ICollection<KeyValuePair<KeySequence<TKey>, TValue>>.CopyTo(KeyValuePair<KeySequence<TKey>, TValue>[] array, int arrayIndex)
        {
            throw new NotSupportedException();
        }

        bool ICollection<KeyValuePair<KeySequence<TKey>, TValue>>.Remove(KeyValuePair<KeySequence<TKey>, TValue> item)
        {
            throw new NotSupportedException();
        }

        #endregion IDictionary implementation for serialization

        /// <summary>
        /// Provides comparisons between entries based on the keys.
        /// </summary>
        private sealed class KeyComparer : IEqualityComparer<KeyValuePair<KeySequence<TKey>, TValue>>
        {
            public bool Equals(KeyValuePair<KeySequence<TKey>, TValue> x, KeyValuePair<KeySequence<TKey>, TValue> y)
            {
                return x.Key.Equals(y.Key);
            }

            public int GetHashCode(KeyValuePair<KeySequence<TKey>, TValue> obj)
            {
                return obj.Key.GetHashCode();
            }
        }

        private sealed class KeyComparison : IComparer<KeyValuePair<KeySequence<TKey>, TValue>>
        {
            private readonly Dictionary<TKey, int>[] _sets;

            public KeyComparison(IImmutableList<KeyValuePair<string, IImmutableList<TKey>>> sets)
            {
                _sets = sets.Select(x => x.Value.Select((y, i) => new KeyValuePair<TKey, int>(y, i)).ToDictionary(y => y.Key, y => y.Value)).ToArray();
            }

            /// <summary>
            /// Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
            /// </summary>
            /// <returns>
            /// A signed integer that indicates the relative values of <paramref name="x" /> and <paramref name="y" />, as shown in the following table.
            /// Value Meaning Less than zero<paramref name="x" /> is less than <paramref name="y" />.
            /// Zero<paramref name="x" /> equals <paramref name="y" />.
            /// Greater than zero<paramref name="x" /> is greater than <paramref name="y" />.
            /// </returns>
            public int Compare(KeyValuePair<KeySequence<TKey>, TValue> x, KeyValuePair<KeySequence<TKey>, TValue> y)
            {
                Comparer<int> comparer = Comparer<int>.Default;
                int test = 0;
                for (int i = _sets.Length; i > 0; i--)
                {
                    test = comparer.Compare(_sets[i][x.Key[i]], _sets[i][y.Key[i]]);

                    if (test == 0)
                    {
                        continue;
                    }

                    return test;
                }
                return test;
            }
        }
    }
}