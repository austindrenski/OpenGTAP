using System;
using System.Collections.Generic;
using HeaderArrayConverter.Collections;
using JetBrains.Annotations;

namespace HeaderArrayConverter
{
    /// <summary>
    /// Represents an <see cref="IHeaderArray{TValue}"/>.
    /// </summary>
    /// <typeparam name="TValue">
    /// The type of element in the array.
    /// </typeparam>
    [PublicAPI]
    public interface IHeaderArray<TValue> : IHeaderArray, ISequenceIndexer<string, TValue> where TValue : IEquatable<TValue>
    {
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
        IImmutableSequenceDictionary<string, TValue> this[KeySequence<string> keys] { get; }

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
        new IImmutableSequenceDictionary<string, TValue> this[params string[] keys] { get; }

        /// <summary>
        /// Returns the value with the key defined by the key components or throws an exception if the key is not found.
        /// </summary>
        /// <param name="key">
        /// The components that define the key whose value is returned.
        /// </param>
        /// <returns>
        /// The value stored by the given key.
        /// </returns>
        [NotNull]
        new TValue this[int key] { get; }

        /// <summary>
        /// Returns a copy of this <see cref="IHeaderArray"/> with the header modified.
        /// </summary>
        /// <param name="header">
        /// The new header.
        /// </param>
        /// <returns>
        /// A copy of this <see cref="IHeaderArray"/> with a new name.
        /// </returns>
        [Pure]
        [NotNull]
        new IHeaderArray<TValue> With([NotNull] string header);

        /// <summary>
        /// Returns an enumerable that iterates through the logical collection as defined by the <see cref="IHeaderArray.Sets"/>.
        /// </summary>
        /// <returns>
        /// An enumerable that can be used to iterate through the logical collection as defined by the <see cref="IHeaderArray.Sets"/>.
        /// </returns>
        [Pure]
        [NotNull]
        new IEnumerable<KeyValuePair<KeySequence<string>, TValue>> GetLogicalEnumerable();

        /// <summary>
        /// Returns an enumerable that iterates through the logical values collection as defined by the <see cref="IHeaderArray.Sets"/>.
        /// </summary>
        /// <returns>
        /// An enumerable that can be used to iterate through the logical values collection as defined by the <see cref="IHeaderArray.Sets"/>.
        /// </returns>
        [Pure]
        [NotNull]
        new IEnumerable<TValue> GetLogicalValuesEnumerable();
        
        /// <summary>
        /// Returns an enumerable that iterates through the logical values collection as defined by the <see cref="IHeaderArray.Sets"/>.
        /// </summary>
        /// <returns>
        /// An enumerable that can be used to iterate through the logical values collection as defined by the <see cref="IHeaderArray.Sets"/>.
        /// </returns>
        [Pure]
        [NotNull]
        new IEnumerable<TValue> GetLogicalValuesEnumerable([NotNull] IComparer<KeySequence<string>> keyComparer);
    }
}