using System.Collections.Immutable;
using JetBrains.Annotations;

namespace HeaderArrayConverter
{
    [PublicAPI]
    public interface IHeaderArray
    {
        string Header { get; }

        string Description { get; }

        string Type { get; }

        IImmutableList<int> Dimensions { get; }

        IImmutableList<ImmutableOrderedSet<string>> Sets { get; }

        object this[KeySequence<string> key] { get; }

        IKeyValueSequence this[params string[] keyComponents] { get; }
    }
}