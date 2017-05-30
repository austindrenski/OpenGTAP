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
        public static IEnumerable<string> AsEnumerable(this IEnumerable<HarSet> source)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.OuterCrossJoin();
        }

        private static IEnumerable<string> OuterCrossJoin(this IEnumerable<HarSet> source)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            IEnumerable<HarSet> sets = source as HarSet[] ?? source.ToArray();

            if (!sets.Any())
            {
                return Enumerable.Empty<string>();
            }

            return
                sets.Skip(1)
                    .OuterCrossJoin()
                    .DefaultIfEmpty()
                    .SelectMany(
                        x =>
                            sets.FirstOrDefault()
                                .Items
                                .Select(
                                    y =>
                                        x is null
                                            ? y
                                            : string.Join(" * ", y, x)));
        }
    }
}