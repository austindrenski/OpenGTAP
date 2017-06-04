using JetBrains.Annotations;

namespace HeaderArrayConverter
{
    [PublicAPI]
    public interface IIndexerProvider<TKey, TValue>
    {
        KeyValueSequence<TKey, TValue> this[KeySequence<TKey> nextKeyComponent] { get; }
    }
}