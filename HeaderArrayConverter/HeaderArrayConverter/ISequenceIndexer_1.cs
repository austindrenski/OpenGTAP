using System.Collections;
using JetBrains.Annotations;

namespace HeaderArrayConverter
{
    [PublicAPI]
    public interface ISequenceIndexer<in TKey> : IEnumerable
    {
        IEnumerable this[params TKey[] keys] { get; }
    }
}