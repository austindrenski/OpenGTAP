using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace HeaderArrayConverter
{
    /// <summary>
    /// Extension methods for <see cref="ImmutableSequenceDictionary{TKey, TValue}"/>.
    /// </summary>
    [PublicAPI]
    public static class ImmutableSequenceDictionary
    {
        /// <summary>
        /// Creates an <see cref="ImmutableSequenceDictionary{TKey, TValue}"/> from an existing <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <typeparam name="TKey">
        /// The type of the keys in the collection.
        /// </typeparam>
        /// <typeparam name="TValue">
        /// The type of the values in the collection.
        /// </typeparam>
        /// <param name="source">
        /// The collection from which to create the <see cref="ImmutableSequenceDictionary{TKey, TValue}"/>.
        /// </param>
        /// <returns>
        /// An <see cref="ImmutableSequenceDictionary{TKey, TValue}"/> containing the distinct items from the enumerable collection.
        /// </returns>
        public static ImmutableSequenceDictionary<TKey, TValue> ToImmutableOrderedDictionary<TKey, TValue>([NotNull] this IEnumerable<KeyValuePair<KeySequence<TKey>, TValue>> source)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return ImmutableSequenceDictionary<TKey, TValue>.Create(source);
        }

        /// <summary>
        /// Creates an <see cref="ImmutableSequenceDictionary{TKey, TValue}"/> from an existing <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <typeparam name="TKey">
        /// The type of the keys in the collection.
        /// </typeparam>
        /// <typeparam name="TValue">
        /// The type of the values in the collection.
        /// </typeparam>
        /// <typeparam name="T">
        /// The type of the items in the source collection.
        /// </typeparam>
        /// <param name="source">
        /// The collection from which to create the <see cref="ImmutableSequenceDictionary{TKey, TValue}"/>.
        /// </param>
        /// <param name="keySelector">
        /// A selector function returning a key.
        /// </param>
        /// <param name="valueSelector">
        /// A selector function returning a value.
        /// </param>
        /// <returns>
        /// An <see cref="ImmutableSequenceDictionary{TKey, TValue}"/> containing the distinct items from the enumerable collection.
        /// </returns>
        public static ImmutableSequenceDictionary<TKey, TValue> ToImmutableOrderedDictionary<T, TKey, TValue>([NotNull] this IEnumerable<T> source, Func<T, TKey> keySelector, Func<T, TValue> valueSelector)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return ImmutableSequenceDictionary<TKey, TValue>.Create(source.Select(x => new KeyValuePair<KeySequence<TKey>, TValue>(keySelector(x), valueSelector(x))));
        }

        /// <summary>
        /// Creates an <see cref="ImmutableSequenceDictionary{TKey, TValue}"/> from an existing <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <typeparam name="TKey">
        /// The type of the keys in the collection.
        /// </typeparam>
        /// <typeparam name="TValue">
        /// The type of the values in the collection.
        /// </typeparam>
        /// <typeparam name="TLeft">
        /// The type of the left item in each tuple of the source collection.
        /// </typeparam>
        /// <typeparam name="TRight">
        /// The type of the right item in each tuple of the source collection.
        /// </typeparam>
        /// <param name="source">
        /// The collection from which to create the <see cref="ImmutableSequenceDictionary{TKey, TValue}"/>.
        /// </param>
        /// <param name="keySelector">
        /// A selector function returning a key.
        /// </param>
        /// <param name="valueSelector">
        /// A selector function returning a value.
        /// </param>
        /// <returns>
        /// An <see cref="ImmutableSequenceDictionary{TKey, TValue}"/> containing the distinct items from the enumerable collection.
        /// </returns>
        public static ImmutableSequenceDictionary<TKey, TValue> ToImmutableOrderedDictionary<TLeft, TRight, TKey, TValue>([NotNull] this IEnumerable<(TLeft Left, TRight Right)> source, Func<(TLeft Left, TRight Right), IEnumerable<TKey>> keySelector, Func<(TLeft Left, TRight Right), TValue> valueSelector)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return ImmutableSequenceDictionary<TKey, TValue>.Create(source.Select(x => new KeyValuePair<KeySequence<TKey>, TValue>(new KeySequence<TKey>(keySelector(x)), valueSelector(x))));
        }
    }
}