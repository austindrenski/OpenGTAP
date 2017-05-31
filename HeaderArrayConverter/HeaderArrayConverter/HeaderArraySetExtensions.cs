using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace HeaderArrayConverter
{
    /// <summary>
    /// 
    /// </summary>
    [PublicAPI]
    public static class HeaderArraySetExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static IEnumerable<string> AsEnumerable(this IEnumerable<HeaderArraySet> source)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.OuterCrossJoin();
        }

        private static IEnumerable<string> OuterCrossJoin(this IEnumerable<HeaderArraySet> source)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            IEnumerable<HeaderArraySet> sets = source as HeaderArraySet[] ?? source.ToArray();

            if (!sets.Any())
            {
                return Enumerable.Empty<string>();
            }

            return
                sets.Skip(1)
                    .OuterCrossJoin()
                    .DefaultIfEmpty()
                    .SelectMany(
                        outer =>
                            sets.FirstOrDefault()
                                .Items
                                .Select(
                                    inner =>
                                        outer is null
                                            ? inner
                                            : $"{inner} * {outer}"));
        }
    }
}