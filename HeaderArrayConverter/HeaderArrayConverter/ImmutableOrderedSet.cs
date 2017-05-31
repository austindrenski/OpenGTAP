using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace HeaderArrayConverter
{
    /// <summary>
    /// Extension methods for <see cref="ImmutableOrderedSet{T}"/>.
    /// </summary>
    [PublicAPI]
    public static class ImmutableOrderedSet
    {
        /// <summary>
        /// Creates an <see cref="ImmutableOrderedSet{T}"/> from an existing <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <typeparam name="T">
        /// The element type of the enumerable collection.
        /// </typeparam>
        /// <param name="source">
        /// The collection from which to create the <see cref="ImmutableOrderedSet{T}"/>.
        /// </param>
        /// <returns>
        /// An <see cref="ImmutableOrderedSet{T}"/> containing the distinct items from the enumerable collection.
        /// </returns>
        public static ImmutableOrderedSet<T> ToImmutableOrderedSet<T>([NotNull] this IEnumerable<T> source)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.ToImmutableOrderedSet(EqualityComparer<T>.Default);
        }

        /// <summary>
        /// Creates an <see cref="ImmutableOrderedSet{T}"/> from an existing <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <typeparam name="T">
        /// The element type of the enumerable collection.
        /// </typeparam>
        /// <param name="source">
        /// The collection from which to create the <see cref="ImmutableOrderedSet{T}"/>.
        /// </param>
        /// <param name="equalityComparer">
        /// 
        /// </param>
        /// <returns>
        /// An <see cref="ImmutableOrderedSet{T}"/> containing the distinct items from the enumerable collection.
        /// </returns>
        public static ImmutableOrderedSet<T> ToImmutableOrderedSet<T>([NotNull] this IEnumerable<T> source, [NotNull] IEqualityComparer<T> equalityComparer)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (equalityComparer is null)
            {
                throw new ArgumentNullException(nameof(equalityComparer));
            }

            return ImmutableOrderedSet<T>.Create(source, equalityComparer);
        }
    }
}