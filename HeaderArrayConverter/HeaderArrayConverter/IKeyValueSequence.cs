using System.Collections;
using JetBrains.Annotations;

namespace HeaderArrayConverter
{
    [PublicAPI]
    public interface KeyValueSequence : IEnumerable, IIndexerProvider
    {
    }
}