using System.Collections;
using JetBrains.Annotations;

namespace HeaderArrayConverter
{
    [PublicAPI]
    public interface IKeyValueSequence : IEnumerable
    {
        IKeyValueSequence this[params object[] keyComponents] { get; }
    }
}