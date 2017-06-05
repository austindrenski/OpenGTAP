using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using JetBrains.Annotations;

namespace HeaderArrayConverter
{
    /// <summary>
    /// Represents a sequence of zero or more keys. This type is suitable for use in a <see cref="IDictionary{TKey, TValue}"/>.
    /// </summary>
    /// <typeparam name="TKey">
    /// The type of key in the sequence.
    /// </typeparam>
    [PublicAPI]
    public struct KeySequence<TKey> : IEnumerable<TKey>, IEquatable<TKey>, IEquatable<KeySequence<TKey>>, IStructuralEquatable
    {
        /// <summary>
        /// The sequence values.
        /// </summary>
        private readonly IImmutableList<TKey> _values;

        /// <summary>
        /// Returns an empty <see cref="KeySequence{T}"/> with the specified type argument.
        /// </summary>
        public static KeySequence<TKey> Empty { get; } = new KeySequence<TKey>(new TKey[0]);
        
        /// <summary>
        /// Gets the number of items contained in the sequence.
        /// </summary>
        public int Count => _values.Count;

        /// <summary>
        /// Returns the value at the specified index.
        /// </summary>
        public TKey this[int index] => _values[index];

        /// <summary>
        /// Returns the values at the specified index.
        /// </summary>
        public IEnumerable<TKey> this[params int[] index] => Yield(index);

        public KeySequence<TKey> Combine(TKey next)
        {
            return Create(this, next);
        }

        public KeySequence<TKey> Combine(params TKey[] next)
        {
            return Create(this, next);
        }

        public KeySequence<TKey> Combine(IEnumerable<TKey> next)
        {
            return Create(this, next);
        }

        public KeySequence(IEnumerable<TKey> keys) 
        {
            _values = keys.ToImmutableArray();
        }

        public KeySequence(params TKey[] keys) : this(keys as IEnumerable<TKey>) { }

        public static implicit operator KeySequence<TKey>(KeySequence<object> value)
        {
            return new KeySequence<TKey>((IEnumerable<TKey>) value._values);
        }

        public static implicit operator string(KeySequence<TKey> value)
        {
            return value.ToString();
        }

        public static implicit operator KeySequence<TKey>(TKey value)
        {
            return new KeySequence<TKey>(value);
        }

        public static explicit operator KeySequence<TKey>(TKey[] value)
        {
            return new KeySequence<TKey>(value);
        }

        public static KeySequence<TKey> Create(KeySequence<TKey> left, params KeySequence<TKey>[] right)
        {
            return new KeySequence<TKey>(left.Concat(right.SelectMany(x => x)));
        }

        public static KeySequence<TKey> Create(KeySequence<TKey> left, params TKey[] right)
        {
            return new KeySequence<TKey>(left.Concat(right));
        }

        public static KeySequence<TKey> Create(KeySequence<TKey> left, IEnumerable<TKey> right)
        {
            return new KeySequence<TKey>(left.Concat(right));
        }

        private IEnumerable<TKey> Yield(IEnumerable<int> index)
        {
            foreach (int i in index)
            {
                yield return this[i];
            }
        }

        /// <summary>
        /// Returns a string representation of this instance.
        /// </summary>
        public override string ToString()
        {
            return _values.Aggregate(string.Empty, (current, next) => $"{current}[{next}]");
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// An enumerator that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<TKey> GetEnumerator()
        {
            return _values?.GetEnumerator() ?? Enumerable.Empty<TKey>().GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">
        /// An object to compare with this object.
        /// </param>
        /// <returns>
        /// True if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        public bool Equals(KeySequence<TKey> other)
        {
            return _values.SequenceEqual(other);
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">
        /// An object to compare with this object.
        /// </param>
        /// <returns>
        /// True if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        public bool Equals(TKey other)
        {
            return _values.SequenceEqual(Enumerable.Empty<TKey>().Append(other));
        }

        /// <summary>
        /// Determines whether an object is structurally equal to the current instance.
        /// </summary>
        /// <param name="other">
        /// The object to compare with the current instance.
        /// </param>
        /// <param name="comparer">
        /// An object that determines whether the current instance and <paramref name="other"/> are equal.
        /// </param>
        /// <returns>
        /// True if the two objects are equal; otherwise, false.
        /// </returns>
        public bool Equals(object other, IEqualityComparer comparer)
        {
            return comparer.Equals(this, other);
        }

        public override bool Equals(object obj)
        {
            switch (obj)
            {
                case null:
                {
                    return Equals(Empty);
                }
                case TKey value:
                {
                    return Equals(value);
                }
                case KeySequence<TKey> value:
                {
                    return Equals(value);
                }
                default:
                {
                    return false;
                }
            }
        }

        public override int GetHashCode()
        {
            return _values.GetHashCode();
        }

        public int GetHashCode(IEqualityComparer comparer)
        {
            return comparer.GetHashCode(this);
        }
    }
}