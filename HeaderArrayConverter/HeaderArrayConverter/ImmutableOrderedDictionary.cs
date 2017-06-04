using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace HeaderArrayConverter
{
    [PublicAPI]
    public static class ImmutableOrderedDictionary
    {
        /// <summary>
        /// Creates an <see cref="ImmutableOrderedSet{T}"/> from an existing <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <typeparam name="T">
        /// The element type of the enumerable collection.
        /// </typeparam>
        /// <param name="source">
        /// The collection from which to create the <see cref="ImmutableOrderedSet{T}"/>.
        /// </param>
        /// <returns>
        /// An <see cref="ImmutableOrderedSet{T}"/> containing the distinct items from the enumerable collection.
        /// </returns>
        public static ImmutableOrderedDictionary<TKey, TValue> ToImmutableOrderedDictionary<TKey, TValue>([NotNull] this IEnumerable<KeyValuePair<KeySequence<TKey>, TValue>> source)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return ImmutableOrderedDictionary<TKey, TValue>.Create(source);
        }

        public static ImmutableOrderedDictionary<TKey, TValue> ToImmutableOrderedDictionary<T, TKey, TValue>([NotNull] this IEnumerable<T> source, Func<T, TKey> keySelector, Func<T, TValue> valueSelector)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return ImmutableOrderedDictionary<TKey, TValue>.Create(source.Select(x => new KeyValuePair<KeySequence<TKey>, TValue>((KeySequence<TKey>)keySelector(x), valueSelector(x))));
        }

        public static ImmutableOrderedDictionary<TKey, TValue> ToImmutableOrderedDictionary<TLeft, TRight, TKey, TValue>([NotNull] this IEnumerable<(TLeft Left, TRight Right)> source, Func<(TLeft Left, TRight Right), IEnumerable<TKey>> keySelector, Func<(TLeft Left, TRight Right), TValue> valueSelector)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return ImmutableOrderedDictionary<TKey, TValue>.Create(source.Select(x => new KeyValuePair<KeySequence<TKey>, TValue>(new KeySequence<TKey>(keySelector(x)), valueSelector(x))));
        }
    }
}