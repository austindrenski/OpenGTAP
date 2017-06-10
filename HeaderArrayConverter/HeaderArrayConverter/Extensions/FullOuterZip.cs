using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace HeaderArrayConverter.Extensions
{
    /// <summary>
    /// Extension methods to produce a full zip of two sequences.
    /// </summary>
    [PublicAPI]
    public static class FullOuterZipExtensions
    {
        /// <summary>
        /// Returns an enumerable collection of pairwise tuples. Default values are used if the sequences are uneven.
        /// </summary>
        /// <typeparam name="TLeft">
        /// The type of the <paramref name="left"/> sequence.
        /// </typeparam>
        /// <typeparam name="TRight">
        /// The type of the <paramref name="right"/> sequence.
        /// </typeparam>
        /// <param name="left">
        /// The left sequence.
        /// </param>
        /// <param name="right">
        /// The right sequence.
        /// </param>
        /// <returns>
        /// An enumerable collection of pairwise tuples.
        /// </returns>
        [Pure]
        [NotNull]
        public static IEnumerable<(TLeft Left, TRight Right)> FullOuterZip<TLeft, TRight>([NotNull] this IEnumerable<TLeft> left, [NotNull] IEnumerable<TRight> right)
        {
            if (left is null)
            {
                throw new ArgumentNullException(nameof(left));
            }
            if (right is null)
            {
                throw new ArgumentNullException(nameof(right));
            }

            return left.FullOuterZip(right, x => default(TLeft), x => default(TRight));
        }

        /// <summary>
        /// Returns an enumerable collection of pairwise tuples. Default values are used if the sequences are uneven.
        /// </summary>
        /// <typeparam name="TLeft">
        /// The type of the <paramref name="left"/> sequence.
        /// </typeparam>
        /// <typeparam name="TRight">
        /// The type of the <paramref name="right"/> sequence.
        /// </typeparam>
        /// <param name="left">
        /// The left sequence.
        /// </param>
        /// <param name="right">
        /// The right sequence.
        /// </param>
        /// <param name="defaultLeft">
        /// 
        /// </param>
        /// <returns>
        /// An enumerable collection of pairwise tuples.
        /// </returns>
        [Pure]
        [NotNull]
        public static IEnumerable<(TLeft Left, TRight Right)> FullOuterZip<TLeft, TRight>([NotNull] this IEnumerable<TLeft> left, [NotNull] IEnumerable<TRight> right, Func<int, TLeft> defaultLeft)
        {
            if (left is null)
            {
                throw new ArgumentNullException(nameof(left));
            }
            if (right is null)
            {
                throw new ArgumentNullException(nameof(right));
            }

            return left.FullOuterZip(right, defaultLeft, x => default(TRight));
        }

        /// <summary>
        /// Returns an enumerable collection of pairwise tuples. Default values are used if the sequences are uneven.
        /// </summary>
        /// <typeparam name="TLeft">
        /// The type of the <paramref name="left"/> sequence.
        /// </typeparam>
        /// <typeparam name="TRight">
        /// The type of the <paramref name="right"/> sequence.
        /// </typeparam>
        /// <param name="left">
        /// The left sequence.
        /// </param>
        /// <param name="right">
        /// The right sequence.
        /// </param>
        /// <param name="leftDefault">
        /// 
        /// </param>
        /// <param name="rightDefault">
        /// 
        /// </param>
        /// <returns>
        /// An enumerable collection of pairwise tuples.
        /// </returns>
        [Pure]
        [NotNull]
        public static IEnumerable<(TLeft Left, TRight Right)> FullOuterZip<TLeft, TRight>([NotNull] this IEnumerable<TLeft> left, [NotNull] IEnumerable<TRight> right, [NotNull] Func<int, TLeft> leftDefault, [NotNull] Func<int, TRight> rightDefault)
        {
            if (left is null)
            {
                throw new ArgumentNullException(nameof(left));
            }
            if (right is null)
            {
                throw new ArgumentNullException(nameof(right));
            }
            if (leftDefault is null)
            {
                throw new ArgumentNullException(nameof(leftDefault));
            }
            if (rightDefault is null)
            {
                throw new ArgumentNullException(nameof(rightDefault));
            }

            using (IEnumerator<TLeft> leftEnumerator = left.GetEnumerator())
            {
                using (IEnumerator<TRight> rightEnumerator = right.GetEnumerator())
                {
                    int index = 0;
                    bool valid;
                    bool rightValid;
                    while ((valid = leftEnumerator.MoveNext()) | (rightValid = rightEnumerator.MoveNext()))
                    {
                        if (valid && rightValid)
                        {
                            yield return (leftEnumerator.Current, rightEnumerator.Current);
                        }
                        else if (valid)
                        {
                            yield return (leftEnumerator.Current, rightDefault(index));
                        }
                        else if (rightValid)
                        {
                            yield return (leftDefault(index), rightEnumerator.Current);
                        }
                        index++;
                    }
                }
            }
        }
    }
}
