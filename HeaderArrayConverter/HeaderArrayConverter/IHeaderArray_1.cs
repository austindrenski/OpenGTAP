using JetBrains.Annotations;

namespace HeaderArrayConverter
{
    [PublicAPI]
    public interface IHeaderArray<T> : IHeaderArray, IIndexerProvider<string, T>
    {
    }
}