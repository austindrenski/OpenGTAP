using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace HeaderArrayConverter
{
    public class ImmutableOrderedDictionary<TKey, TValue> : IImmutableDictionary<TKey, TValue>
    {
        private IImmutableDictionary<TKey, TValue> _dictionary;

        private IImmutableList<KeyValuePair<TKey, TValue>> _items;

        public int Count => _dictionary.Count;

        public TValue this[TKey key] => _dictionary[key];

        public IEnumerable<TKey> Keys => _dictionary.Keys;

        public IEnumerable<TValue> Values => _dictionary.Values;

        public ImmutableOrderedDictionary(IEnumerable<KeyValuePair<TKey, TValue>> source)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            _items = source.ToImmutableArray();
            _dictionary = _items.ToImmutableDictionary(x => x.Key, x => x.Value);
        }

        public static ImmutableOrderedDictionary<TKey, TValue> Create(IEnumerable<KeyValuePair<TKey, TValue>> source)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return new ImmutableOrderedDictionary<TKey, TValue>(source);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _items.GetEnumerator();
        }
        
        public bool ContainsKey(TKey key)
        {
            return _dictionary.ContainsKey(key);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return _dictionary.TryGetValue(key, out value);
        }

        public IImmutableDictionary<TKey, TValue> Clear()
        {
            return _dictionary.Any() ? Create(Enumerable.Empty<KeyValuePair<TKey, TValue>>()) : this;
        }

        public bool TryGetKey(TKey equalKey, out TKey actualKey)
        {
            return _dictionary.TryGetKey(equalKey, out actualKey);
        }

        public IImmutableDictionary<TKey, TValue> Add(TKey key, TValue value)
        {
            return _dictionary.Add(key, value);
        }

        public IImmutableDictionary<TKey, TValue> AddRange(IEnumerable<KeyValuePair<TKey, TValue>> pairs)
        {
            return _dictionary.AddRange(pairs);
        }

        public IImmutableDictionary<TKey, TValue> SetItem(TKey key, TValue value)
        {
            return _dictionary.SetItem(key, value);
        }

        public IImmutableDictionary<TKey, TValue> SetItems(IEnumerable<KeyValuePair<TKey, TValue>> items)
        {
            return _dictionary.SetItems(items);
        }

        public IImmutableDictionary<TKey, TValue> RemoveRange(IEnumerable<TKey> keys)
        {
            return _dictionary.RemoveRange(keys);
        }

        public IImmutableDictionary<TKey, TValue> Remove(TKey key)
        {
            return _dictionary.Remove(key);
        }

        public bool Contains(KeyValuePair<TKey, TValue> pair)
        {
            return _dictionary.Contains(pair);
        }
    }
}