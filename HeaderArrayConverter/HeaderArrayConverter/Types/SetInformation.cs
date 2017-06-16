using System.Collections.Immutable;
using JetBrains.Annotations;

namespace HeaderArrayConverter.Types
{
    [PublicAPI]
    public class SetInformation
    {
        public string Name { get; }

        public string Description { get; }

        public bool IsTemporal { get; }

        public int Count { get; }

        public IImmutableList<string> Elements { get; }

        public SetInformation(string name, string description, bool isTemporal, int count, IImmutableList<string> elements)
        {
            Name = name;
            Description = description;
            IsTemporal = isTemporal;
            Count = count;
            Elements = elements;
        }
    }
}