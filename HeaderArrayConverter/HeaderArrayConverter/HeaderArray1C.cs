using System;
using System.Collections.Immutable;
using JetBrains.Annotations;

namespace HeaderArrayConverter
{
    public class HeaderArray1C : HeaderArray
    {
        /// <summary>
        /// The decoded form of <see cref="Array"/>
        /// </summary>
        public ImmutableArray<string> Strings { get; }
        
        public HeaderArray1C([NotNull] string header, [CanBeNull] string description, [NotNull] string type, int count, int size, bool sparse, int x0, int x1, int x2, [NotNull] string[] strings)
            : base(header, description, type, count, size, sparse, x0, x1, x2)
        {
            Strings = strings.ToImmutableArray();
        }
    }
}
