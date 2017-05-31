﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using JetBrains.Annotations;

namespace HeaderArrayConverter
{
    /// <summary>
    /// Represents an immutable set that preserves the initial order of the source collection.
    /// </summary>
    /// <typeparam name="T">
    /// The element type of the set.
    /// </typeparam>
    /// <remarks>
    /// This collection maintains an <see cref="IImmutableList{T}"/> of distinct items for enumeration 
    /// and an <see cref="IImmutableSet{T}"/> for set comparisons. As a result, the memory footprint
    /// will be larger than an alternative set implementation, such as <see cref="ImmutableHashSet{T}"/>.
    /// </remarks>
    [PublicAPI]
    public class ImmutableOrderedSet<T> : IImmutableSet<T>
    {
        /// <summary>
        /// The distinct items of the initial collection stored in the initial order.
        /// </summary>
        [NotNull]
        private readonly IImmutableList<T> _enumerable;

        /// <summary>
        /// The collection stored as an unordered set.
        /// </summary>
        [NotNull]
        private readonly IImmutableSet<T> _set;

        /// <summary>
        /// The equality comparer provided at construction; otherwise the default comparer for T.
        /// </summary>
        [NotNull]
        private readonly IEqualityComparer<T> _equalityComparer;

        /// <summary>
        /// Gets the number of elements in the collection.
        /// </summary>
        /// <returns>
        /// The number of elements in the collection.
        /// </returns>
        public int Count => _set.Count;
        
        /// <summary>
        /// Constructs an <see cref="ImmutableOrderedSet{T}"/> from the <see cref="IEnumerable{T}"/> and <see cref="IEqualityComparer{T}"/>.
        /// </summary>
        /// <param name="source">
        /// The source collection.
        /// </param>
        /// <param name="equalityComparer">
        /// The equality comparer to determine uniqueness for the source collection. 
        /// </param>
        protected ImmutableOrderedSet([NotNull] IEnumerable<T> source, [NotNull] IEqualityComparer<T> equalityComparer)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (equalityComparer is null)
            {
                throw new ArgumentNullException(nameof(equalityComparer));
            }

            _enumerable = source.Distinct(equalityComparer).ToImmutableArray();
            _set = _enumerable.ToImmutableHashSet(equalityComparer);
            _equalityComparer = equalityComparer;
        }

        /// <summary>
        /// Creates an <see cref="ImmutableOrderedSet{T}"/> from the <see cref="IEnumerable{T}"/> and optional <see cref="IEqualityComparer{T}"/>.
        /// </summary>
        /// <param name="source">
        /// The source collection.
        /// </param>
        /// <param name="equalityComparer">
        /// An optional equality comparer for the source collection. 
        /// </param>
        /// <returns>
        /// A new <see cref="ImmutableOrderedSet{T}"/> containing the distinct items of the source collection.
        /// </returns>
        [Pure]
        [NotNull]
        public static ImmutableOrderedSet<T> Create([NotNull] IEnumerable<T> source, [CanBeNull] IEqualityComparer<T> equalityComparer = null)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return new ImmutableOrderedSet<T>(source, equalityComparer ?? EqualityComparer<T>.Default);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// An enumerator that can be used to iterate through the collection.
        /// </returns>
        [Pure]
        [NotNull]
        public IEnumerator<T> GetEnumerator()
        {
            return _enumerable.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        [Pure]
        [NotNull]
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_enumerable).GetEnumerator();
        }
        
        /// <summary>
        /// Adds the specified value to this set.
        /// </summary>
        /// <param name="value">
        /// The value to add.
        /// </param>
        /// <returns>
        /// A new set with the element added, or this set if the element is already in this set.
        /// </returns>
        [Pure]
        [NotNull]
        public IImmutableSet<T> Add(T value)
        {
            return _set.Contains(value) ? this : Create(_enumerable.Append(value), _equalityComparer);
        }

        /// <summary>
        /// Gets an empty set that retains the same sort or unordered semantics that this instance has.
        /// </summary>
        [Pure]
        [NotNull]
        public IImmutableSet<T> Clear()
        {
            return _set.Any() ? Create(Enumerable.Empty<T>(), _equalityComparer) : this;
        }

        /// <summary>
        /// Removes a given set of items from this set.
        /// </summary>
        /// <param name="other">
        /// The items to remove from this set.
        /// </param>
        /// <returns>
        /// The new set with the items removed; or the original set if none of the items were in the set.
        /// </returns>
        [Pure]
        [NotNull]
        public IImmutableSet<T> Except(IEnumerable<T> other)
        {
            if (other is null)
            {
                return this;
            }

            other = other as T[] ?? other.ToArray();

            return _set.Overlaps(other) ? Create(_enumerable.Except(other, _equalityComparer), _equalityComparer) : this;
        }

        /// <summary>
        /// Produces a set that contains elements that exist in both this set and the specified set.
        /// </summary>
        /// <param name="other">
        /// The set to intersect with this one.
        /// </param>
        /// <returns>
        /// A new set that contains any elements that exist in both sets.
        /// </returns>
        [Pure]
        [NotNull]
        public IImmutableSet<T> Intersect(IEnumerable<T> other)
        {
            if (other is null)
            {
                return this;
            }

            other = other as T[] ?? other.ToArray();

            return _set.Overlaps(other) ? Create(_enumerable.Intersect(other, _equalityComparer), _equalityComparer) : this;
        }

        /// <summary>
        /// Removes the specified value from this set.
        /// </summary>
        /// <param name="value">
        /// The value to remove.
        /// </param>
        /// <returns>
        /// A new set with the element removed, or this set if the element is not in this set.
        /// </returns>
        [Pure]
        [NotNull]
        public IImmutableSet<T> Remove(T value)
        {
            return _set.Contains(value) ? Create(_enumerable.Where(x => !_equalityComparer.Equals(x, value)), _equalityComparer) : this;
        }

        [Pure]
        [NotNull]
        public IImmutableSet<T> SymmetricExcept(IEnumerable<T> other)
        {
            return _set.SymmetricExcept(other);
        }

        [Pure]
        [NotNull]
        public IImmutableSet<T> Union(IEnumerable<T> other)
        {
            return _set.Union(other);
        }

        [Pure]
        public bool Contains(T value)
        {
            return _set.Contains(value);
        }

        [Pure]
        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            return _set.IsProperSubsetOf(other);
        }

        [Pure]
        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            return _set.IsProperSupersetOf(other);
        }

        [Pure]
        public bool IsSubsetOf(IEnumerable<T> other)
        {
            return _set.IsSubsetOf(other);
        }

        [Pure]
        public bool IsSupersetOf(IEnumerable<T> other)
        {
            return _set.IsSupersetOf(other);
        }

        [Pure]
        public bool Overlaps(IEnumerable<T> other)
        {
            return _set.Overlaps(other);
        }

        [Pure]
        public bool SetEquals(IEnumerable<T> other)
        {
            return _set.SetEquals(other);
        }

        [Pure]
        public bool TryGetValue(T equalValue, out T actualValue)
        {
            return _set.TryGetValue(equalValue, out actualValue);
        }
    }
}