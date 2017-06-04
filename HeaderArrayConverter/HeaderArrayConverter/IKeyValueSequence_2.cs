using System.Collections.Generic;
using JetBrains.Annotations;

namespace HeaderArrayConverter
{
    [PublicAPI]
    public interface IKeyValueSequence<TKey, TValue> : IKeyValueSequence, IEnumerable<KeyValuePair<KeySequence<TKey>, TValue>>
    {
        TValue this[KeySequence<TKey> key] { get; }

        IKeyValueSequence<TKey, TValue> this[params TKey[] keyComponents] { get; }
    }
}
