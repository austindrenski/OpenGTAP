using System.Collections.Generic;
using JetBrains.Annotations;

namespace HeaderArrayConverter
{
    /// <summary>
    /// 
    /// </summary>
    [PublicAPI]
    public class HeaderArraySet<T> : ImmutableOrderedSet<T>
    {
        /// <summary>
        /// The name of this set.
        /// </summary>
        public string Name { get; }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="items"></param>
        public HeaderArraySet(string name, params T[] items) : this(name, EqualityComparer<T>.Default, items) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="equalityComparer"></param>
        /// <param name="items"></param>
        public HeaderArraySet(string name, IEqualityComparer<T> equalityComparer, params T[] items) : base(items, equalityComparer)
        {
            Name = name;
        }
    }
}