using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;

namespace HeaderArrayConverter
{
    [PublicAPI]
    public struct KeyValueSequence<TKey, TValue> : KeyValueSequence, IIndexerProvider<TKey, TValue>, IEnumerable<KeyValuePair<KeySequence<TKey>, TValue>>
    {
        private readonly ImmutableOrderedDictionary<TKey, TValue> _dictionary;
        
        public KeySequence<TKey> Key { get; }

        public KeyValueSequence<TKey, TValue> this[KeySequence<TKey> nextKeyComponent] => Index(nextKeyComponent);

        KeyValueSequence IIndexerProvider.this[KeySequence<object> nextKeyComponent] => Index(new KeySequence<TKey>(nextKeyComponent.Cast<TKey>()));

        private KeyValueSequence<TKey, TValue> Index(KeySequence<TKey> nextKeyComponent)
        {
            KeySequence<TKey> newKey = new KeySequence<TKey>(Key.Combine(nextKeyComponent));
            return
                _dictionary.ContainsKey(newKey)
                    ? new KeyValueSequence<TKey, TValue>(newKey, _dictionary[newKey])
                    : new KeyValueSequence<TKey, TValue>(Key, _dictionary.Where(x => x.Key.Take(newKey.Count).SequenceEqual(newKey)));
        }

        public KeyValueSequence(KeySequence<TKey> key, IEnumerable<KeyValuePair<KeySequence<TKey>, TValue>> source)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            Key = key;
            _dictionary = source.ToImmutableOrderedDictionary();
        }

        public KeyValueSequence(KeySequence<TKey> key, params TValue[] values)
            : this (key, values.Select(x => new KeyValuePair<KeySequence<TKey>, TValue>(key, x))) { }

        public KeyValueSequence(KeySequence<TKey> key, params KeyValuePair<KeySequence<TKey>, TValue>[] values) 
            : this(key, values as IEnumerable<KeyValuePair<KeySequence<TKey>, TValue>>) { }

        /// <summary>
        /// Returns a string representation of this instance.
        /// </summary>
        public override string ToString()
        {
            int length = _dictionary.Keys.Max(x => x.ToString().Length);

            return
                _dictionary.Aggregate(
                    new StringBuilder(),
                    (current, next) =>
                        current.AppendLine($"{next.Key.ToString().PadRight(length)}: {next.Value}"),
                    x =>
                        x.ToString());
        }

        public IEnumerator<KeyValuePair<KeySequence<TKey>, TValue>> GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}