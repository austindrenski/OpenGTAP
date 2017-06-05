using System;
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
        /// Gets the name of this set.
        /// </summary>
        [CanBeNull]
        public string Name { get; }

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
        /// <param name="name">
        /// An optional name for this set.
        /// </param>
        protected ImmutableOrderedSet([NotNull] IEnumerable<T> source, [NotNull] IEqualityComparer<T> equalityComparer, [CanBeNull] string name)
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
            Name = name;
        }

        /// <summary>
        /// Constructs an <see cref="ImmutableOrderedSet{T}"/> from the <see cref="IEnumerable{T}"/> and <see cref="IEqualityComparer{T}"/>.
        /// </summary>
        /// <param name="name">
        /// An optional name for this set.
        /// </param>
        /// <param name="equalityComparer">
        /// The equality comparer to determine uniqueness for the source collection. 
        /// </param>
        /// <param name="items">
        /// The source collection.
        /// </param>
        protected ImmutableOrderedSet([CanBeNull] string name, [NotNull] IEqualityComparer<T> equalityComparer, params T[] items) 
            : this(items, equalityComparer, name) { }

        /// <summary>
        /// Creates an <see cref="ImmutableOrderedSet{T}"/> from the <see cref="IEnumerable{T}"/> and optional <see cref="IEqualityComparer{T}"/>.
        /// </summary>
        /// <param name="source">
        /// The source collection.
        /// </param>
        /// <param name="equalityComparer">
        /// An optional equality comparer for the source collection. 
        /// </param>
        /// <param name="name">
        /// An optional name for this set.
        /// </param>
        /// <returns>
        /// A new <see cref="ImmutableOrderedSet{T}"/> containing the distinct items of the source collection.
        /// </returns>
        [Pure]
        [NotNull]
        public static ImmutableOrderedSet<T> Create([NotNull] IEnumerable<T> source, [CanBeNull] IEqualityComparer<T> equalityComparer = null, [CanBeNull] string name = null)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return new ImmutableOrderedSet<T>(source, equalityComparer ?? EqualityComparer<T>.Default, name);
        }

        /// <summary>
        /// Creates an <see cref="ImmutableOrderedSet{T}"/> from the <see cref="IEnumerable{T}"/> and optional <see cref="IEqualityComparer{T}"/>.
        /// </summary>

        /// <param name="equalityComparer">
        /// An optional equality comparer for the source collection. 
        /// </param>
        /// <param name="name">
        /// An optional name for this set.
        /// </param>
        /// <param name="items">
        /// The source collection.
        /// </param>
        /// <returns>
        /// A new <see cref="ImmutableOrderedSet{T}"/> containing the distinct items of the source collection.
        /// </returns>
        [Pure]
        [NotNull]
        public static ImmutableOrderedSet<T> Create([CanBeNull] string name, IEqualityComparer<T> equalityComparer = null, params T[] items)
        {
            return Create(items, equalityComparer, name);
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

        /// <summary>
        /// Produces a set that contains elements either in this set or a given sequence, but not both.
        /// </summary>
        /// <param name="other">
        /// The other sequence of items.
        /// </param>
        /// <returns>
        /// The new set.
        /// </returns>
        [Pure]
        [NotNull]
        public IImmutableSet<T> SymmetricExcept(IEnumerable<T> other)
        {
            return _set.SymmetricExcept(other);
        }

        /// <summary>
        /// Adds a given set of items to this set.
        /// </summary>
        /// <param name="other">
        /// The items to add.
        /// </param>
        /// <returns>
        /// The new set with the items added; or the original set if all the items were already in the set.
        /// </returns>
        [Pure]
        [NotNull]
        public IImmutableSet<T> Union(IEnumerable<T> other)
        {
            return _set.Union(other);
        }
        
        /// <summary>
        /// Determines whether this set contains the specified value.
        /// </summary>
        /// <param name="value">
        /// The value.
        /// </param>
        /// <returns>
        /// True if the set contains the specified value; otherwise, false.
        /// </returns>
        [Pure]
        public bool Contains(T value)
        {
            return _set.Contains(value);
        }

        /// <summary>
        /// Determines whether the current set is a proper (strict) subset of a specified collection.
        /// </summary>
        /// <param name="other">
        /// The collection to compare to the current set.
        /// </param>
        /// <returns>
        /// True if the current set is a correct subset of other; otherwise, false.
        /// </returns>
        [Pure]
        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            return _set.IsProperSubsetOf(other);
        }

        /// <summary>
        /// Determines whether the current set is a proper superset of a specified collection.
        /// </summary>
        /// <param name="other">
        /// The collection to compare to the current set.
        /// </param>
        /// <returns>
        /// True if the current set is a correct superset of other; otherwise, false.
        /// </returns>
        [Pure]
        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            return _set.IsProperSupersetOf(other);
        }

        /// <summary>
        /// Determines whether a set is a subset of a specified collection.
        /// </summary>
        /// <param name="other">
        /// The collection to compare to the current set.
        /// </param>
        /// <returns>
        /// True if the current set is a subset of other; otherwise, false.
        /// </returns>
        [Pure]
        public bool IsSubsetOf(IEnumerable<T> other)
        {
            return _set.IsSubsetOf(other);
        }

        /// <summary>
        /// Determines whether the current set is a superset of a specified collection.
        /// </summary>
        /// <param name="other">
        /// The collection to compare to the current set.
        /// </param>
        /// <returns>
        /// True if the current set is a superset of other; otherwise, false.
        /// </returns>
        [Pure]
        public bool IsSupersetOf(IEnumerable<T> other)
        {
            return _set.IsSupersetOf(other);
        }

        /// <summary>
        /// Determines whether the current set overlaps with the specified collection.
        /// </summary>
        /// <param name="other">
        /// The collection to compare to the current set.
        /// </param>
        /// <returns>
        /// True if the current set and other share at least one common element; otherwise, false.
        /// </returns>
        [Pure]
        public bool Overlaps(IEnumerable<T> other)
        {
            return _set.Overlaps(other);
        }

        /// <summary>
        /// Checks whether a given sequence of items entirely describe the contents of this set.
        /// </summary>
        /// <param name="other">
        /// The sequence of items to check against this set.
        /// </param>
        /// <returns>
        /// A value indicating whether the sets are equal.
        /// </returns>
        [Pure]
        public bool SetEquals(IEnumerable<T> other)
        {
            return _set.SetEquals(other);
        }

        /// <summary>
        /// Searches the set for a given value and returns the equal value it finds, if any.
        /// </summary>
        /// <param name="equalValue">
        /// The value to search for.
        /// </param>
        /// <param name="actualValue">
        /// The value from the set that the search found, or <paramref name="equalValue"/> if the search yielded no match.
        /// </param>
        /// <returns>
        /// A value indicating whether the search was successful.
        /// </returns>
        /// <remarks>
        /// This can be useful when you want to reuse a previously stored reference instead of
        /// a newly constructed one (so that more sharing of references can occur) or to look up
        /// a value that has more complete data than the value you currently have, although their
        /// comparer functions indicate they are equal.
        /// </remarks>
        [Pure]
        public bool TryGetValue(T equalValue, out T actualValue)
        {
            return _set.TryGetValue(equalValue, out actualValue);
        }
    }
}