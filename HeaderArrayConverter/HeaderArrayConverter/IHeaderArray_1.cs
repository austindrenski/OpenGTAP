using JetBrains.Annotations;

namespace HeaderArrayConverter
{
    [PublicAPI]
    public interface IHeaderArray<T> : ISequenceIndexer<string, T>, IHeaderArray { }
}