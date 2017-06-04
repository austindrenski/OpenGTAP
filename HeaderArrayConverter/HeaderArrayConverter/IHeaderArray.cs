using System.Collections.Immutable;
using JetBrains.Annotations;

namespace HeaderArrayConverter
{
    [PublicAPI]
    public interface IHeaderArray : ISequenceIndexer<string>
    {
        string Header { get; }

        string Description { get; }

        string Type { get; }

        IImmutableList<int> Dimensions { get; }

        IImmutableList<ImmutableOrderedSet<string>> Sets { get; }

        IHeaderArray<T> As<T>();
    }
}