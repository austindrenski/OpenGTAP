﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using JetBrains.Annotations;

namespace HeaderArrayConverter
{
    [PublicAPI]
    public struct KeySequence<T> : IEnumerable<T>, IEquatable<KeySequence<T>>
    {
        public static KeySequence<T> Empty { get; } = new KeySequence<T>(new T[0]);

        private readonly IImmutableList<T> _values;

        public int Count => _values.Count;

        public KeySequence(params T[] keys)
        {
            _values = keys.ToImmutableArray();
        }

        public static implicit operator KeySequence<T>(T value)
        {
            return new KeySequence<T>(value);
        }

        public static explicit operator T(KeySequence<T> values)
        {
            return values.Count == 1 ? values.First() : throw new InvalidCastException($"{nameof(KeySequence<T>)} contains more than one {nameof(T)}.");
        }

        public override string ToString()
        {
            return _values.Aggregate(string.Empty, (current, next) => $"{current}[{next}]");
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _values?.GetEnumerator() ?? Enumerable.Empty<T>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool Equals(KeySequence<T> other)
        {
            return _values.SequenceEqual(other);
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
    }
}