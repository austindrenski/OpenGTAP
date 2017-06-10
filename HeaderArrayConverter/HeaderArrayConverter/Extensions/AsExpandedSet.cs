using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HeaderArrayConverter.Collections;
using JetBrains.Annotations;

namespace HeaderArrayConverter.Extensions
{
    /// <summary>
    /// Extension methods for operating on sequences of sets.
    /// </summary>
    [PublicAPI]
    public static class AsExpandedSetExtensions
    {
        /// <summary>
        /// Expands a sequence of sets ordered with standard HAR semantics. 
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
        public static IEnumerable<KeySequence<T>> AsExpandedSet<T>(this IEnumerable<KeyValuePair<string, IImmutableList<T>>> source, IEnumerable<int> indexes)
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
        /// Expands a sequence of sets ordered with standard HAR semantics. 
        /// </summary>
        /// <param name="source">
        /// The source collection.
        /// </param>
        /// <returns>
        /// A <see cref="KeySequence{TKey}"/> collection ordered with standard HAR semantics. 
        /// </returns>
        public static IEnumerable<KeySequence<T>> AsExpandedSet<T>(this IEnumerable<KeyValuePair<string, IImmutableList<T>>> source)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return
                source.Select(x => x.Value)
                      .Aggregate(
                          Enumerable.Empty<KeySequence<T>>().DefaultIfEmpty(),
                          (current, next) =>
                              next.SelectMany(x => current.Select(y => y.Combine(x))));
        }
    }
}