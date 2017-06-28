using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace HeaderArrayConverter.Collections
{
    /// <summary>
    /// Represents a sequence of zero or more keys. This type is suitable for use in a <see cref="IDictionary{TKey, TValue}"/>.
    /// </summary>
    /// <typeparam name="TKey">
    /// The type of key in the sequence.
    /// </typeparam>
    [PublicAPI]
    public struct KeySequence<TKey> : IReadOnlyList<TKey>, IEquatable<KeySequence<TKey>>, IEquatable<TKey>, IStructuralEquatable
    {
        /// <summary>
        /// An empty <see cref="KeySequence{TKey}"/> with the specified type argument.
        /// </summary>
        [NotNull]
        private static readonly TKey[] EmptyArray = new TKey[0];

        /// <summary>
        /// An empty <see cref="KeySequence{TKey}"/> with the specified type argument.
        /// </summary>
        private static readonly KeySequence<TKey> Empty = new KeySequence<TKey>(EmptyArray);

        /// <summary>
        /// Compares sequences with <see cref="StringComparison.OrdinalIgnoreCase"/> semantics.
        /// </summary>
        [NotNull]
        public static IComparer<KeySequence<TKey>> ForwardComparer { get; } = new Comparer(StringComparer.OrdinalIgnoreCase.Compare);

        /// <summary>
        /// Compares sequences with reverse <see cref="StringComparison.OrdinalIgnoreCase"/> semantics.
        /// </summary>
        [NotNull]
        public static IComparer<KeySequence<TKey>> ReverseComparer { get; } = new Comparer(StringComparer.OrdinalIgnoreCase.Compare, Enumerable.Reverse);

        /// <summary>
        /// Compares sequences with <see cref="StringComparison.OrdinalIgnoreCase"/> semantics.
        /// </summary>
        [NotNull]
        public static IEqualityComparer<KeySequence<string>> OrdinalIgnoreCaseEquality { get; } = new StringKeyEqualityComparer();

        /// <summary>
        /// The sequence values.
        /// </summary>
        [CanBeNull]
        private readonly TKey[] _keys;

        /// <summary>
        /// Gets the number of items contained in the sequence.
        /// </summary>
        public int Count => _keys?.Length ?? 0;

        /// <summary>
        /// Returns the values at the specified index.
        /// </summary>
        [NotNull]
        public TKey this[int index] => _keys is null ? throw new IndexOutOfRangeException() : _keys[index];
        
        /// <summary>
        /// Constructs a <see cref="KeySequence{TKey}"/> from the collection.
        /// </summary>
        /// <param name="keys">
        /// The key collection.
        /// </param>
        public KeySequence(TKey keys)
        {
            _keys = new TKey[] { keys };
        }

        /// <summary>
        /// Constructs a <see cref="KeySequence{TKey}"/> from the collection.
        /// </summary>
        /// <param name="key">
        /// The key collection.
        /// </param>
        public KeySequence(KeySequence<TKey> key)
        {
            _keys = key._keys ?? EmptyArray;
        }

        /// <summary>
        /// Constructs a <see cref="KeySequence{TKey}"/> from the collection.
        /// </summary>
        /// <param name="keys">
        /// The key collection.
        /// </param>
        public KeySequence(params TKey[] keys)
        {
            _keys = keys.ToArray();
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

            _keys = keys is KeySequence<TKey> sequence ? sequence._keys :  keys.ToArray();
        }
        
        /// <summary>
        /// Constructs a <see cref="KeySequence{TKey}"/> from the collection.
        /// </summary>
        /// <param name="a">
        /// The key collection.
        /// </param>
        /// <param name="b">
        /// A second key collection to combine.
        /// </param>
        public KeySequence(KeySequence<TKey> a, KeySequence<TKey> b)
        {
            if (a._keys is null && b._keys is null)
            {
                _keys = EmptyArray;
            }
            else if (a._keys is null)
            {
                _keys = b._keys;
            }
            else if (b._keys is null)
            {
                _keys = a._keys;
            }
            else
            {
                _keys = new TKey[a._keys.Length + b._keys.Length];
                Array.Copy(a._keys, 0, _keys, 0, a._keys.Length);
                Array.Copy(b._keys, 0, _keys, a._keys.Length, b._keys.Length);
            }
        }

        /// <summary>
        /// Constructs a <see cref="KeySequence{TKey}"/> from the collection.
        /// </summary>
        /// <param name="keys">
        /// The key collection.
        /// </param>
        /// <param name="other">
        /// A second key collection to combine.
        /// </param>
        public KeySequence([CanBeNull] IEnumerable<TKey> keys, [CanBeNull] IEnumerable<TKey> other)
        {
            if (keys is null && other is null)
            {
                _keys = EmptyArray;
            }
            else if (keys is null)
            {
                _keys = other is KeySequence<TKey> test ? test._keys : other.ToArray();
            }
            else if (other is null)
            {
                _keys = keys is KeySequence<TKey> test ? test._keys : keys.ToArray();
            }
            else if (keys is KeySequence<TKey> a && other is KeySequence<TKey> b)
            {
                _keys = new TKey[a._keys.Length + b._keys.Length];
                Array.Copy(a._keys, 0, _keys, 0, a._keys.Length);
                Array.Copy(b._keys, 0, _keys, a._keys.Length, b._keys.Length);
            }
            else if (keys is KeySequence<TKey> c)
            {
                TKey[] right = other.ToArray();
                _keys = new TKey[c._keys.Length + right.Length];
                Array.Copy(c._keys, 0, _keys, 0, c._keys.Length);
                Array.Copy(right, 0, _keys, c._keys.Length, right.Length);
            }
            else if (other is KeySequence<TKey> d)
            {
                TKey[] left = other.ToArray();
                _keys = new TKey[left.Length + d._keys.Length];
                Array.Copy(left, 0, _keys, 0, left.Length);
                Array.Copy(d._keys, 0, _keys, left.Length, d._keys.Length);
            }
            else
            {
                TKey[] left = keys as TKey[] ?? keys.ToArray();
                TKey[] right = other as TKey[] ?? other.ToArray();
                _keys = new TKey[left.Length + right.Length];
                Array.Copy(left, 0, _keys, 0, left.Length);
                Array.Copy(right, 0, _keys, left.Length, right.Length);
            }
        }

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
            return new KeySequence<TKey>(value._keys.Cast<TKey>());
        }

        /// <summary>
        /// Implicitly casts the <see cref="KeySequence{TKey}"/> to a string.
        /// </summary>
        /// <param name="value">
        /// The sequence create a string.
        /// </param>
        public static explicit operator string(KeySequence<TKey> value)
        {
            return value.ToString();
        }

        /// <summary>
        /// Parses a string in the form of '[AGR][USA][ROW]' to to a <see cref="KeySequence{TKey}"/>.
        /// </summary>
        /// <param name="value">
        /// The sequence create a string.
        /// </param>
        public static KeySequence<string> Parse(string value)
        {
            return value.Split(new string[] { "[", "]" }, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Returns a string representation of this sequence.
        /// </summary>
        public override string ToString()
        {
            return _keys?.Aggregate(default(string), (current, next) => $"{current}[{next}]");
        }

        /// <summary>
        /// Returns a string representation of this sequence with the transform function applied.
        /// </summary>
        public string ToString(Func<IEnumerable<TKey>, IEnumerable<TKey>> transform)
        {
            return _keys is null ? null : transform(_keys).Aggregate(default(string), (current, next) => $"{current}[{next}]");
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
            return _keys?.AsEnumerable().GetEnumerator() ?? Empty.GetEnumerator();
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
            if (_keys is null || other._keys is null || _keys.Length != other._keys.Length)
            {
                return false;
            }

            for (int i = 0; i < _keys.Length; i++)
            {
                if (!_keys[i].Equals(other._keys[i]))
                {
                    return false;
                }
            }

            return true;
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
            return _keys?.Length == 1 && _keys[0].Equals(other);
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
        /// Equality member.
        /// </summary>
        public static bool operator ==(KeySequence<TKey> left, KeySequence<TKey> right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Inequality member.
        /// </summary>
        public static bool operator !=(KeySequence<TKey> left, KeySequence<TKey> right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>
        /// A 32-bit signed integer that is the hash code for this instance.
        /// </returns>
        public override int GetHashCode()
        {
            if (_keys is null)
            {
                return 0;
            }
            if (_keys.Length == 0)
            {
                return -1;
            }

            unchecked
            {
                int hashCode = 17;
                for (int i = 0; i < _keys.Length; i++)
                {
                    hashCode = 31 * hashCode + _keys[i].GetHashCode();
                }
                return hashCode;
            }
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

        /// <summary>
        /// Compares two <see cref="KeySequence{TKey}"/> objects.
        /// </summary>
        private sealed class Comparer : IComparer<KeySequence<TKey>>
        {
            /// <summary>
            /// The function applied to compare keys.
            /// </summary>
            [NotNull]
            private readonly Func<string, string, int> _comparer;

            /// <summary>
            /// A transform function applied to the keys before comparison.
            /// </summary>
            [CanBeNull]
            private readonly Func<IEnumerable<TKey>, IEnumerable<TKey>> _transform;

            /// <summary>
            /// Constructs a <see cref="IComparable{TKey}"/>.
            /// </summary>
            /// <param name="comparer"></param>
            /// <param name="transform"></param>
            public Comparer([NotNull] Func<string, string, int> comparer, [CanBeNull] Func<IEnumerable<TKey>, IEnumerable<TKey>> transform = null)
            {
                _comparer = comparer;
                _transform = transform;
            }

            /// <summary>
            /// Compares two sequences.
            /// </summary>
            [Pure]
            public int Compare(KeySequence<TKey> x, KeySequence<TKey> y)
            {
                return
                    _transform is null
                        ? _comparer(x.ToString(), y.ToString())
                        : _comparer(x.ToString(_transform), y.ToString(_transform));
            }
        }

        private sealed class StringKeyEqualityComparer : IEqualityComparer<KeySequence<string>>
        {
            public bool Equals(KeySequence<string> x, KeySequence<string> y)
            {
                using (IEnumerator<string> xEnumerator = x.GetEnumerator())
                {
                    using (IEnumerator<string> yEnumerator = y.GetEnumerator())
                    {
                        while (xEnumerator.MoveNext() && yEnumerator.MoveNext())
                        {
                            if (!xEnumerator.Current.Equals(yEnumerator.Current, StringComparison.OrdinalIgnoreCase))
                            {
                                return false;
                            }
                        }
                        return true;
                    }
                }
            }

            public int GetHashCode(KeySequence<string> obj)
            {
                return obj.GetHashCode();
            }
        }
    }
}