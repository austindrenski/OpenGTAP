using System.Collections.Generic;
using System.Collections.Immutable;
using HeaderArrayConverter.Collections;
using JetBrains.Annotations;

namespace HeaderArrayConverter
{
    /// <summary>
    /// Represents a header array.
    /// </summary>
    [PublicAPI]
    public interface IHeaderArray : ISequenceIndexer<string>
    {
        /// <summary>
        /// The header of the array.
        /// </summary>
        [NotNull]
        string Header { get; }

        /// <summary>
        /// An optional description of the array.
        /// </summary>
        [NotNull]
        string Description { get; }

        /// <summary>
        /// The type of the array.
        /// </summary>
        [NotNull]
        string Type { get; }

        /// <summary>
        /// The dimensions of the array.
        /// </summary>
        [NotNull]
        IImmutableList<int> Dimensions { get; }

        /// <summary>
        /// The sets of the array.
        /// </summary>
        [NotNull]
        IImmutableList<KeyValuePair<string, IImmutableList<string>>> Sets { get; }
        
        /// <summary>
        /// Gets the total number of entries in the array.
        /// </summary>
        int Total { get; }

        /// <summary>
        /// Returns the value with the key defined by the key components or throws an exception if the key is not found.
        /// </summary>
        /// <param name="keys">
        /// The components that define the key whose value is returned.
        /// </param>
        /// <returns>
        /// The value stored by the given key.
        /// </returns>
        [NotNull]
        new IImmutableSequenceDictionary<string> this[params string[] keys] { get; }

        /// <summary>
        /// Casts the <see cref="IHeaderArray"/> as an <see cref="IHeaderArray{TResult}"/>.
        /// </summary>
        /// <typeparam name="TResult">
        /// The type of the array.
        /// </typeparam>
        /// <returns>
        /// An <see cref="IHeaderArray{TResult}"/>.
        /// </returns>
        [Pure]
        [NotNull]
        IHeaderArray<TResult> As<TResult>();

        /// <summary>
        /// Returns the stored value or the default value. Throws <see cref="KeyNotFoundException"/> if the key is not valid.
        /// </summary>
        [Pure]
        object Return(KeySequence<string> key);
        
        /// <summary>
        /// Returns a JSON representation of the contents of this <see cref="HeaderArray{TValue}"/>.
        /// </summary>
        [Pure]
        [NotNull]
        string Serialize(bool indent);

        /// <summary>
        /// Returns an enumerable that iterates through the logical collection as defined by the <see cref="Sets"/>.
        /// </summary>
        /// <returns>
        /// An enumerable that can be used to iterate through the logical collection as defined by the <see cref="Sets"/>.
        /// </returns>
        [Pure]
        [NotNull]
        IEnumerable<KeyValuePair<KeySequence<string>, object>> GetLogicalEnumerable();
    }
}