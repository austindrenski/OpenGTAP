using JetBrains.Annotations;

namespace HeaderArrayConverter
{
    [PublicAPI]
    public interface IIndexerProvider
    {
        KeyValueSequence this[KeySequence<object> nextKeyComponent] { get; }
    }
}