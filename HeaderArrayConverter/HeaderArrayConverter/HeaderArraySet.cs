using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using JetBrains.Annotations;

namespace HeaderArrayConverter
{
    /// <summary>
    /// Extension methods for operating on sequences of <see cref="HeaderArraySet{T}"/> objects.
    /// </summary>
    [PublicAPI]
    public static class HeaderArraySet
    {
        /// <summary>
        /// Expands a <see cref="HeaderArraySet{T}"/> collection ordered with standard HAR semantics. 
        /// </summary>
        /// <param name="source">
        /// The source collection.
        /// </param>
        /// <returns>
        /// A set of asterisk-delimited strings ordered with standard HAR semantics. 
        /// </returns>
        public static IImmutableSet<string> AsExpandedSet<T>(this IEnumerable<HeaderArraySet<T>> source)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return
                source.Aggregate(
                          Enumerable.Empty<string>(),
                          (current, next) =>
                              next.SelectMany(
                                  outer =>
                                      current.DefaultIfEmpty()
                                             .Select(
                                                 inner =>
                                                     inner is null
                                                         ? $"{outer}"
                                                         : $"{inner}*{outer}")))
                      .ToImmutableOrderedSet();
        }
    }
}