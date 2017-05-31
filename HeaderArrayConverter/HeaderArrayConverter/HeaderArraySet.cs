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
    public static class HeaderArraySet
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static IEnumerable<string> AsEnumerable<T>(this IEnumerable<HeaderArraySet<T>> source)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.OuterCrossJoin();
        }

        private static IEnumerable<string> OuterCrossJoin<T>(this IEnumerable<HeaderArraySet<T>> source)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            IEnumerable<HeaderArraySet<T>> sets = source as HeaderArraySet<T>[] ?? source.ToArray();

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
                                .Select(
                                    inner =>
                                        outer is null
                                            ? $"{inner}"
                                            : $"{inner} * {outer}"));
        }
    }
}