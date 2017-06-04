using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using JetBrains.Annotations;

namespace HeaderArrayConverter
{
    [PublicAPI]
    public struct ValueSequence<TKey, TValue> : IEnumerable<KeyValuePair<KeySequence<TKey>, TValue>>, IEquatable<IEnumerable<KeyValuePair<KeySequence<TKey>, TValue>>>, IEquatable<KeyValuePair<KeySequence<TKey>, TValue>>
    {
        [NotNull]
        private readonly IImmutableDictionary<KeySequence<TKey>, TValue> _items;
        
        public static ValueSequence<TKey, TValue> Empty { get; } = new ValueSequence<TKey, TValue>(new KeyValuePair<KeySequence<TKey>, TValue>[0]);

        public ValueSequence(IEnumerable<KeyValuePair<KeySequence<TKey>, TValue>> source)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            _items = source.ToImmutableDictionary();
        }

        public ValueSequence(KeySequence<TKey> key, params TValue[] source)
            : this(source.Select(x => new KeyValuePair<KeySequence<TKey>, TValue>(key, x))) { }
        
        public ValueSequence(params KeyValuePair<TKey, TValue>[] source)
            : this(source.Select(x => new KeyValuePair<KeySequence<TKey>, TValue>(x.Key, x.Value))) { }

        public override string ToString()
        {
            return 
                _items.Aggregate(
                    new StringBuilder(), 
                    (current, next) => current.AppendLine($"{next.Key}: {next.Value}"), 
                    result => result.ToString());
        }

        public static implicit operator ValueSequence<TKey, TValue>(KeyValuePair<TKey, TValue> value)
        {
            return new ValueSequence<TKey, TValue>(value);
        }

        public bool Equals(IEnumerable<KeyValuePair<KeySequence<TKey>, TValue>> other)
        {
            return _items.SequenceEqual(other);
        }

        public bool Equals(KeyValuePair<KeySequence<TKey>, TValue> other)
        {
            return _items.Count == 1 && _items.Single().Equals(other);
        }

        public override bool Equals(object obj)
        {
            return obj is IEnumerable<KeyValuePair<KeySequence<TKey>, TValue>> sequence && Equals(sequence);
        }
        
        public override int GetHashCode()
        {
            return _items.GetHashCode();
        }

        public IEnumerator<KeyValuePair<KeySequence<TKey>, TValue>> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}