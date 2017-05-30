using System.Collections.Generic;
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
}
