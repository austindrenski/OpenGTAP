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
        [NotNull]
        private readonly IImmutableList<TKey> _keys;

        /// <summary>
        /// Returns an empty <see cref="KeySequence{TKey}"/> with the specified type argument.
        /// </summary>
        public static KeySequence<TKey> Empty { get; } = new KeySequence<TKey>(new TKey[0]);
        
        /// <summary>
        /// Gets the number of items contained in the sequence.
        /// </summary>
        public int Count => _keys.Count;

        /// <summary>
        /// Returns the value at the specified index.
        /// </summary>
        public TKey this[int index] => _keys[index];

        /// <summary>
        /// Returns the values at the specified index.
        /// </summary>
        [NotNull]
        public IEnumerable<TKey> this[params int[] index]
        {
            get
            {
                foreach (int i in index)
                {
                    yield return this[i];
                }
            }
        }

        /// <summary>
        /// Constructs a <see cref="KeySequence{TKey}"/> from the collection.
        /// </summary>
        /// <param name="keys">
        /// The key collection.
        /// </param>
        public KeySequence([NotNull] IEnumerable<TKey> keys) 
        {
            if (keys is null)
            {
                throw new ArgumentNullException(nameof(keys));
            }

            _keys = keys.ToImmutableArray();
        }

        /// <summary>
        /// Constructs a <see cref="KeySequence{TKey}"/> from the collection.
        /// </summary>
        /// <param name="keys">
        /// The key collection.
        /// </param>
        public KeySequence(params TKey[] keys) : this(keys as IEnumerable<TKey>) { }

        /// <summary>
        /// Implicitly casts the value to a <see cref="KeySequence{TKey}"/>.
        /// </summary>
        /// <param name="value">
        /// The value used to construct the <see cref="KeySequence{TKey}"/>.
        /// </param>
        public static implicit operator KeySequence<TKey>(TKey value)
        {
            return new KeySequence<TKey>(value);
        }

        /// <summary>
        /// Implicitly casts the values to a <see cref="KeySequence{TKey}"/>.
        /// </summary>
        /// <param name="value">
        /// The values used to construct the <see cref="KeySequence{TKey}"/>.
        /// </param>
        public static implicit operator KeySequence<TKey>(TKey[] value)
        {
            return new KeySequence<TKey>(value);
        }

        /// <summary>
        /// Implicitly casts the sequence to a <see cref="KeySequence{TKey}"/>.
        /// </summary>
        /// <param name="value">
        /// The sequence used to construct the <see cref="KeySequence{TKey}"/>.
        /// </param>
        public static implicit operator KeySequence<TKey>(KeySequence<object> value)
        {
            return new KeySequence<TKey>((IEnumerable<TKey>)value._keys);
        }

        /// <summary>
        /// Implicitly casts the <see cref="KeySequence{TKey}"/> to a string.
        /// </summary>
        /// <param name="value">
        /// The sequence create a string.
        /// </param>
        public static implicit operator string(KeySequence<TKey> value)
        {
            return value.ToString();
        }

        /// <summary>
        /// Returns a new <see cref="KeySequence{TKey}"/> that is a combination of this and the next sequence.
        /// </summary>
        /// <param name="next">
        /// The next keys to combine.
        /// </param>
        /// <returns>
        /// Returns a new <see cref="KeySequence{TKey}"/> that is a combination of this and the next sequence.
        /// </returns>
        public KeySequence<TKey> Combine([NotNull] IEnumerable<TKey> next)
        {
            if (next is null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            return new KeySequence<TKey>(_keys.Concat(next));
        }

        /// <summary>
        /// Returns a string representation of this instance.
        /// </summary>
        public override string ToString()
        {
            return _keys.Aggregate(string.Empty, (current, next) => $"{current}[{next}]");
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// An enumerator that can be used to iterate through the collection.
        /// </returns>
        [Pure]
        [NotNull]
        public IEnumerator<TKey> GetEnumerator()
        {
            return _keys.GetEnumerator() ?? Enumerable.Empty<TKey>().GetEnumerator();
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
            return _keys.SequenceEqual(other);
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
            return _keys.SequenceEqual(Enumerable.Empty<TKey>().Append(other));
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

        /// <summary>
        /// Indicates whether this instance and a specified object are equal.
        /// </summary>
        /// <param name="obj">
        /// The object to compare with the current instance.
        /// </param>
        /// <returns>
        /// True if <paramref name="obj" /> and this instance are the same type and represent the same value; otherwise, false. 
        /// </returns>
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

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>
        /// A 32-bit signed integer that is the hash code for this instance.
        /// </returns>
        public override int GetHashCode()
        {
            return _keys.GetHashCode();
        }

        /// <summary>
        /// Returns a hash code for the current instance.
        /// </summary>
        /// <param name="comparer">
        /// An object that computes the hash code of the current object.
        /// </param>
        /// <returns>
        /// The hash code for the current instance.
        /// </returns>
        public int GetHashCode(IEqualityComparer comparer)
        {
            return comparer.GetHashCode(this);
        }
    }
}