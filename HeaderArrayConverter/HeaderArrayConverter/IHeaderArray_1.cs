using JetBrains.Annotations;

namespace HeaderArrayConverter
{
    [PublicAPI]
    public interface IHeaderArray<T> : IHeaderArray
    {
        /// <summary>
        /// Returns the value with the specified key or throws an exception if the key is not found.
        /// </summary>
        /// <param name="key">
        /// The key whose value is returned.
        /// </param>
        /// <returns>
        /// The value stored by the given key.
        /// </returns>
        new T this[KeySequence<string> key] { get; }

        /// <summary>
        /// Returns the value with the key defined by the key components or throws an exception if the key is not found.
        /// </summary>
        /// <param name="keyComponents">
        /// The components that define the key whose value is returned.
        /// </param>
        /// <returns>
        /// The value stored by the given key.
        /// </returns>
        new IKeyValueSequence<string, T> this[params string[] keyComponents] { get; }
    }
}