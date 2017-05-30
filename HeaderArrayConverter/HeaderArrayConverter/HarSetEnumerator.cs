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
    public struct HarSet
    {
        /// <summary>
        /// 
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<string> Items { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="items"></param>
        public HarSet(string name, IEnumerable<string> items)
        {
            Name = name;
            Items = items;
        }
    }


    /// <summary>
    /// 
    /// </summary>
    [PublicAPI]
    public static class HarSetEnumerator
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
                                        string.Join(" * ", new string[] { y, x }.Where(z => z != null))));
        }
    }
}
