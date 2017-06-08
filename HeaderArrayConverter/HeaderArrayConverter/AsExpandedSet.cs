using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace HeaderArrayConverter
{
    /// <summary>
    /// Extension methods for operating on sequences of <see cref="ImmutableOrderedSet{T}"/> objects.
    /// </summary>
    [PublicAPI]
    public static class AsExpandedSetExtensions
    {
        /// <summary>
        /// Expands a <see cref="ImmutableOrderedSet{T}"/> collection ordered with standard HAR semantics. 
        /// </summary>
        /// <param name="source">
        /// The source collection.
        /// </param>
        /// <param name="indexes">
        /// The collection of index positions that the source collection represents in the expanded set.
        /// </param>
        /// <returns>
        /// A <see cref="KeySequence{TKey}"/> collection ordered with standard HAR semantics. 
        /// </returns>
        public static IEnumerable<KeySequence<T>> AsExpandedSet<T>(this IEnumerable<IEnumerable<T>> source, IEnumerable<int> indexes)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (indexes is null)
            {
                throw new ArgumentNullException(nameof(indexes));
            }

            indexes = indexes as int[] ?? indexes.ToArray();

            return source.AsExpandedSet().Where((x, i) => indexes.Contains(i));
        }

        /// <summary>
        /// Expands a <see cref="ImmutableOrderedSet{T}"/> collection ordered with standard HAR semantics. 
        /// </summary>
        /// <param name="source">
        /// The source collection.
        /// </param>
        /// <returns>
        /// A <see cref="KeySequence{TKey}"/> collection ordered with standard HAR semantics. 
        /// </returns>
        public static IEnumerable<KeySequence<T>> AsExpandedSet<T>(this IEnumerable<IEnumerable<T>> source)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return
                source.Aggregate(
                    Enumerable.Empty<KeySequence<T>>().DefaultIfEmpty(),
                    (current, next) =>
                        next.SelectMany(x => current.Select(y => y.Combine(x))));
        }
    }
}