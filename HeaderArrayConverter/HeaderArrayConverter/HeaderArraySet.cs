using System.Collections.Generic;
using JetBrains.Annotations;

namespace HeaderArrayConverter
{
    /// <summary>
    /// 
    /// </summary>
    [PublicAPI]
    public struct HeaderArraySet
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
        public HeaderArraySet(string name, params string[] items)
        {
            Name = name;
            Items = items;
        }
    }
}
