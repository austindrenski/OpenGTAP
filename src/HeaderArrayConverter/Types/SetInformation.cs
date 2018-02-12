using System.Collections.Generic;
using System.Collections.Immutable;
using JetBrains.Annotations;

namespace HeaderArrayConverter.Types
{
    // TODO: document SetInformation
    /// <summary>
    ///
    /// </summary>
    [PublicAPI]
    public class SetInformation
    {
        /// <summary>
        ///
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///
        /// </summary>
        public string Description { get; }

        /// <summary>
        ///
        /// </summary>
        public bool IsTemporal { get; }

        /// <summary>
        ///
        /// </summary>
        public int Count { get; }

        /// <summary>
        ///
        /// </summary>
        public IImmutableList<string> Elements { get; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="name"></param>
        /// <param name="description"></param>
        /// <param name="isTemporal"></param>
        /// <param name="count"></param>
        /// <param name="elements"></param>
        public SetInformation(string name, string description, bool isTemporal, int count, IEnumerable<string> elements)
        {
            Name = name;
            Description = description;
            IsTemporal = isTemporal;
            Count = count;
            Elements = elements.ToImmutableArray();
        }
    }
}