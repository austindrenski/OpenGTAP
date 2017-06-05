using JetBrains.Annotations;

namespace HeaderArrayConverter
{
    /// <summary>
    /// Represents an <see cref="IHeaderArray{T}"/>.
    /// </summary>
    /// <typeparam name="T">
    /// The type of element in the array.
    /// </typeparam>
    [PublicAPI]
    public interface IHeaderArray<T> : ISequenceIndexer<string, T>, IHeaderArray { }
}