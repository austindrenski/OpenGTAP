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
    /// <typeparam name="T">
    /// The type of key in the sequence.
    /// </typeparam>
    [PublicAPI]
    public struct KeySequence<T> : IEnumerable<T>, IEquatable<KeySequence<T>>, IStructuralEquatable
    {
        /// <summary>
        /// The sequence values.
        /// </summary>
        private readonly IImmutableList<T> _values;

        /// <summary>
        /// Returns an empty <see cref="KeySequence{T}"/> with the specified type argument.
        /// </summary>
        public static KeySequence<T> Empty { get; } = new KeySequence<T>(new T[0]);
        
        /// <summary>
        /// Gets the number of items contained in the sequence.
        /// </summary>
        public int Count => _values.Count;

        /// <summary>
        /// Returns the value at the specified index.
        /// </summary>
        public T this[int index] => _values[index];

        /// <summary>
        /// Returns the values at the specified index.
        /// </summary>
        public IEnumerable<T> this[params int[] index] => Yield(index);

        public KeySequence(params T[] keys) : this(keys as IEnumerable<T>) { }

        public KeySequence(IEnumerable<T> keys) 
        {
            _values = keys.ToImmutableArray();
        }
        
        public static implicit operator KeySequence<T>(T value)
        {
            return new KeySequence<T>(value);
        }

        public static KeySequence<T> operator +(KeySequence<T> left, KeySequence<T> right)
        {
            return new KeySequence<T>(left.Concat(right));
        }

        public static KeySequence<T> operator +(KeySequence<T> left, T[] right)
        {
            return new KeySequence<T>(left.Concat(right));
        }

        private IEnumerable<T> Yield(IEnumerable<int> index)
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
        public IEnumerator<T> GetEnumerator()
        {
            return _values?.GetEnumerator() ?? Enumerable.Empty<T>().GetEnumerator();
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
        public bool Equals(KeySequence<T> other)
        {
            return _values.SequenceEqual(other);
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
                case T value:
                {
                    return Equals(value);
                }
                case KeySequence<T> value:
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
            return _values.Aggregate(0, (current, next) => unchecked(391 * current + (next?.GetHashCode() ?? 0)));
        }

        public int GetHashCode(IEqualityComparer comparer)
        {
            return comparer.GetHashCode(this);
        }
    }
}