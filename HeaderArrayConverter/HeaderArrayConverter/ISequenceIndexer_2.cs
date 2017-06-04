using System.Collections.Generic;
using JetBrains.Annotations;

namespace HeaderArrayConverter
{
    [PublicAPI]
    public interface ISequenceIndexer<TKey, TValue> : ISequenceIndexer<TKey>, IEnumerable<KeyValuePair<KeySequence<TKey>, TValue>>
    {
        new IEnumerable<KeyValuePair<KeySequence<TKey>, TValue>> this[params TKey[] keys] { get; }
    }
}