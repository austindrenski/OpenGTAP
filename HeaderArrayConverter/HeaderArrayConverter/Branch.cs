using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace HeaderArrayConverter
{
    /// <summary>
    /// Extension methods to branch, operate, and join sequences.
    /// </summary>
    [PublicAPI]
    public static class BranchExtensions
    {
        /// <summary>
        /// Branches a sequence based on a predicate, applies transform functions to each, applies join transforms to each, then concatenates the results.
        /// </summary>
        /// <typeparam name="TSource">
        /// The type of the main branch.
        /// </typeparam>
        /// <typeparam name="TResult">
        /// The type of the resulting branch.
        /// </typeparam>
        /// <param name="source">
        /// The source collection.
        /// </param>
        /// <param name="predicate">
        /// The function differentiating between the left and the right sequences.
        /// </param>
        /// <param name="left">
        /// A transform function to be applied to the left branch.
        /// </param>
        /// <param name="right">
        /// A transform function to be applied to the right branch.
        /// </param>
        /// <returns>
        /// A sequence containing the left then the right sequences.
        /// </returns>
        [Pure]
        [NotNull]
        [LinqTunnel]
        [CollectionAccess(CollectionAccessType.Read)]
        public static IEnumerable<TResult> Branch<TSource, TResult>([NotNull] this IEnumerable<TSource> source, [NotNull] Func<TSource, bool> predicate, [NotNull] Func<TSource, TResult> left, [NotNull] Func<TSource, int, TResult> right)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (predicate is null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }
            if (left is null)
            {
                throw new ArgumentNullException(nameof(left));
            }
            if (right is null)
            {
                throw new ArgumentNullException(nameof(right));
            }

            return
                source.Branch(predicate)
                      .BranchLeft(left)
                      .BranchRight(right)
                      .BranchMerge();
        }

        /// <summary>
        /// Branches a sequence based on a predicate, applies transform functions to each, applies join transforms to each, then concatenates the results.
        /// </summary>
        /// <typeparam name="TSource">
        /// The type of the main branch.
        /// </typeparam>
        /// <typeparam name="TResult">
        /// The type of the resulting branch.
        /// </typeparam>
        /// <param name="source">
        /// The source collection.
        /// </param>
        /// <param name="predicate">
        /// The function differentiating between the left and the right sequences.
        /// </param>
        /// <param name="left">
        /// A transform function to be applied to the left branch.
        /// </param>
        /// <param name="right">
        /// A transform function to be applied to the right branch.
        /// </param>
        /// <returns>
        /// A sequence containing the left then the right sequences.
        /// </returns>
        [Pure]
        [NotNull]
        [LinqTunnel]
        [CollectionAccess(CollectionAccessType.Read)]
        public static IEnumerable<TResult> Branch<TSource, TResult>([NotNull] this IEnumerable<TSource> source, [NotNull] Func<TSource, bool> predicate, [NotNull] Func<TSource, int, TResult> left, [NotNull] Func<TSource, TResult> right)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (predicate is null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }
            if (left is null)
            {
                throw new ArgumentNullException(nameof(left));
            }
            if (right is null)
            {
                throw new ArgumentNullException(nameof(right));
            }

            return
                source.Branch(predicate)
                      .BranchLeft(left)
                      .BranchRight(right)
                      .BranchMerge();
        }

        /// <summary>
        /// Branches a sequence based on a predicate, applies transform functions to each, applies join transforms to each, then concatenates the results.
        /// </summary>
        /// <typeparam name="TSource">
        /// The type of the main branch.
        /// </typeparam>
        /// <typeparam name="TResult">
        /// The type of the resulting branch.
        /// </typeparam>
        /// <param name="source">
        /// The source collection.
        /// </param>
        /// <param name="predicate">
        /// The function differentiating between the left and the right sequences.
        /// </param>
        /// <param name="left">
        /// A transform function to be applied to the left branch.
        /// </param>
        /// <param name="right">
        /// A transform function to be applied to the right branch.
        /// </param>
        /// <returns>
        /// A sequence containing the left then the right sequences.
        /// </returns>
        [Pure]
        [NotNull]
        [LinqTunnel]
        [CollectionAccess(CollectionAccessType.Read)]
        public static IEnumerable<TResult> Branch<TSource, TResult>([NotNull] this IEnumerable<TSource> source, [NotNull] Func<TSource, bool> predicate, [NotNull] Func<TSource, int, TResult> left, [NotNull] Func<TSource, int, TResult> right)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (predicate is null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }
            if (left is null)
            {
                throw new ArgumentNullException(nameof(left));
            }
            if (right is null)
            {
                throw new ArgumentNullException(nameof(right));
            }

            return
                source.Branch(predicate)
                      .BranchLeft(left)
                      .BranchRight(right)
                      .BranchMerge();
        }

        /// <summary>
        /// Branches a sequence based on a predicate, applies transform functions to each, applies join transforms to each, then concatenates the results.
        /// </summary>
        /// <typeparam name="TSource">
        /// The type of the main branch.
        /// </typeparam>
        /// <typeparam name="TResult">
        /// The type of the resulting branch.
        /// </typeparam>
        /// <param name="source">
        /// The source collection.
        /// </param>
        /// <param name="predicate">
        /// The function differentiating between the left and the right sequences.
        /// </param>
        /// <param name="left">
        /// A transform function to be applied to the left branch.
        /// </param>
        /// <param name="right">
        /// A transform function to be applied to the right branch.
        /// </param>
        /// <returns>
        /// A sequence containing the left then the right sequences.
        /// </returns>
        [Pure]
        [NotNull]
        [LinqTunnel]
        [CollectionAccess(CollectionAccessType.Read)]
        public static IEnumerable<TResult> Branch<TSource, TResult>([NotNull] this IEnumerable<TSource> source, [NotNull] Func<TSource, bool> predicate, [NotNull] Func<TSource, TResult> left, [NotNull] Func<TSource, TResult> right)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (predicate is null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }
            if (left is null)
            {
                throw new ArgumentNullException(nameof(left));
            }
            if (right is null)
            {
                throw new ArgumentNullException(nameof(right));
            }

            return
                source.Branch(predicate)
                      .BranchLeft(left)
                      .BranchRight(right)
                      .BranchMerge();
        }

        /// <summary>
        /// Branches a sequence based on a predicate, applies transform functions to each, applies join transforms to each, then concatenates the results.
        /// </summary>
        /// <typeparam name="TSource">
        /// The type of the main branch.
        /// </typeparam>
        /// <typeparam name="TLeft">
        /// The type of the left branch.
        /// </typeparam>
        /// <typeparam name="TRight">
        /// The type of the right branch after the transform function is applied.
        /// </typeparam>
        /// <typeparam name="TResult">
        /// The type of the resulting branch.
        /// </typeparam>
        /// <param name="source">
        /// The source collection.
        /// </param>
        /// <param name="predicate">
        /// The function differentiating between the left and the right sequences.
        /// </param>
        /// <param name="left">
        /// A transform function to be applied to the left branch.
        /// </param>
        /// <param name="right">
        /// A transform function to be applied to the right branch.
        /// </param>
        /// <param name="leftMerge">
        /// A transform function to be applied to the right branch.
        /// </param>
        /// <param name="rightMerge">
        /// A transform function to be applied to the right branch.
        /// </param>
        /// <returns>
        /// A sequence containing the left then the right sequences.
        /// </returns>
        [Pure]
        [NotNull]
        [LinqTunnel]
        [CollectionAccess(CollectionAccessType.Read)]
        public static IEnumerable<TResult> Branch<TSource, TLeft, TRight, TResult>([NotNull] this IEnumerable<TSource> source, [NotNull] Func<TSource, bool> predicate, [NotNull] Func<TSource, TLeft> left, [NotNull] Func<TSource, TRight> right, [NotNull] Func<TLeft, TResult> leftMerge, [NotNull] Func<TRight, TResult> rightMerge)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (predicate is null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }
            if (left is null)
            {
                throw new ArgumentNullException(nameof(left));
            }
            if (right is null)
            {
                throw new ArgumentNullException(nameof(right));
            }
            if (leftMerge is null)
            {
                throw new ArgumentNullException(nameof(leftMerge));
            }
            if (rightMerge is null)
            {
                throw new ArgumentNullException(nameof(rightMerge));
            }

            return
                source.Branch(predicate)
                      .BranchLeft(left)
                      .BranchRight(right)
                      .BranchMerge(leftMerge, rightMerge);
        }

        /// <summary>
        /// Branches a sequence based on a predicate.
        /// </summary>
        /// <typeparam name="TSource">
        /// The type of item in the source collection.
        /// </typeparam>
        /// <param name="source">
        /// The source collection.
        /// </param>
        /// <param name="predicate">
        /// The function differentiating between the left and the right sequences.
        /// </param>
        /// <returns>
        /// A <see cref="ValueTuple{TSource, TSource}"/> where the left sequence contains the elements that returned true from the <paramref name="predicate"/>;
        /// the right sequence contains elements that return false from the predicate.
        /// </returns>
        [Pure]
        [CollectionAccess(CollectionAccessType.Read)]
        public static (IEnumerable<TSource> Left, IEnumerable<TSource> Right) Branch<TSource>([NotNull] this IEnumerable<TSource> source, [NotNull] Func<TSource, bool> predicate)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (predicate is null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            Queue<TSource> left = new Queue<TSource>();
            Queue<TSource> right = new Queue<TSource>();

            foreach (TSource item in source)
            {
                if (predicate(item))
                {
                    left.Enqueue(item);
                }
                else
                {
                    right.Enqueue(item);
                }
            }

            return (left, right);
        }

        /// <summary>
        /// Applies a transform function to the left branch.
        /// </summary>
        /// <typeparam name="TSource">
        /// The type of the main branch.
        /// </typeparam>
        /// <typeparam name="TLeft">
        /// The type of the left branch after the transform function is applied.
        /// </typeparam>
        /// <param name="source">
        /// A branched sequence.
        /// </param>
        /// <param name="left">
        /// A transform function to be applied to the left branch.
        /// </param>
        /// <returns>
        /// A <see cref="ValueTuple{TLeft, TSource}"/>.
        /// </returns>
        [Pure]
        [CollectionAccess(CollectionAccessType.Read)]
        public static (IEnumerable<TLeft> Left, IEnumerable<TSource> Right) BranchLeft<TSource, TLeft>(this (IEnumerable<TSource> Left, IEnumerable<TSource> Right) source, Func<TSource, TLeft> left)
        {
            if (left is null)
            {
                throw new ArgumentNullException(nameof(left));
            }

            return (source.Left.Select(left), source.Right);
        }

        /// <summary>
        /// Applies a transform function to the left branch.
        /// </summary>
        /// <typeparam name="TSource">
        /// The type of the main branch.
        /// </typeparam>
        /// <typeparam name="TLeft">
        /// The type of the left branch after the transform function is applied.
        /// </typeparam>
        /// <param name="source">
        /// A branched sequence.
        /// </param>
        /// <param name="left">
        /// A transform function to be applied to the left branch.
        /// </param>
        /// <returns>
        /// A <see cref="ValueTuple{TLeft, TSource}"/>.
        /// </returns>
        [Pure]
        [CollectionAccess(CollectionAccessType.Read)]
        public static (IEnumerable<TLeft> Left, IEnumerable<TSource> Right) BranchLeft<TSource, TLeft>(this (IEnumerable<TSource> Left, IEnumerable<TSource> Right) source, Func<TSource, int, TLeft> left)
        {
            if (left is null)
            {
                throw new ArgumentNullException(nameof(left));
            }

            return (source.Left.Select(left), source.Right);
        }

        /// <summary>
        /// Applies a transform function to the right branch.
        /// </summary>
        /// <typeparam name="TSource">
        /// The type of the main branch.
        /// </typeparam>
        /// <typeparam name="TLeft">
        /// The type of the left branch.
        /// </typeparam>
        /// <typeparam name="TRight">
        /// The type of the right branch after the transform function is applied.
        /// </typeparam>
        /// <param name="source">
        /// A branched sequence.
        /// </param>
        /// <param name="right">
        /// A transform function to be applied to the right branch.
        /// </param>
        /// <returns>
        /// A <see cref="ValueTuple{TLeft, TRight}"/>.
        /// </returns>
        [Pure]
        [CollectionAccess(CollectionAccessType.Read)]
        public static (IEnumerable<TLeft> Left, IEnumerable<TRight> Right) BranchRight<TSource, TLeft, TRight>(this (IEnumerable<TLeft> Left, IEnumerable<TSource> Right) source, Func<TSource, TRight> right)
        {
            if (right is null)
            {
                throw new ArgumentNullException(nameof(right));
            }

            return (source.Left, source.Right.Select(right));
        }


        /// <summary>
        /// Applies a transform function to the right branch.
        /// </summary>
        /// <typeparam name="TSource">
        /// The type of the main branch.
        /// </typeparam>
        /// <typeparam name="TLeft">
        /// The type of the left branch.
        /// </typeparam>
        /// <typeparam name="TRight">
        /// The type of the right branch after the transform function is applied.
        /// </typeparam>
        /// <param name="source">
        /// A branched sequence.
        /// </param>
        /// <param name="right">
        /// A transform function to be applied to the right branch.
        /// </param>
        /// <returns>
        /// A <see cref="ValueTuple{TLeft, TRight}"/>.
        /// </returns>
        [Pure]
        [CollectionAccess(CollectionAccessType.Read)]
        public static (IEnumerable<TLeft> Left, IEnumerable<TRight> Right) BranchRight<TSource, TLeft, TRight>(this (IEnumerable<TLeft> Left, IEnumerable<TSource> Right) source, Func<TSource, int, TRight> right)
        {
            if (right is null)
            {
                throw new ArgumentNullException(nameof(right));
            }

            return (source.Left, source.Right.Select(right));
        }

        /// <summary>
        /// Concatenates the two branches sequentially.
        /// </summary>
        /// <typeparam name="TResult">
        /// The type of the resulting branch.
        /// </typeparam>
        /// <param name="source">
        /// A branched sequence.
        /// </param>
        /// <returns>
        /// A sequence containing the left then the right sequences.
        /// </returns>
        [Pure]
        [NotNull]
        [LinqTunnel]
        [CollectionAccess(CollectionAccessType.Read)]
        public static IEnumerable<TResult> BranchMerge<TResult>(this (IEnumerable<TResult> Left, IEnumerable<TResult> Right) source)
        {
            return source.Left.Concat(source.Right);
        }

        /// <summary>
        /// Concatenates the two branches sequentially.
        /// </summary>
        /// <typeparam name="TLeft">
        /// The type of the left branch.
        /// </typeparam>
        /// <typeparam name="TRight">
        /// The type of the right branch after the transform function is applied.
        /// </typeparam>
        /// <typeparam name="TResult">
        /// The type of the resulting branch.
        /// </typeparam>
        /// <param name="source">
        /// A branched sequence.
        /// </param>
        /// <param name="left">
        /// A transform function to be applied to the right branch.
        /// </param>
        /// <param name="right">
        /// A transform function to be applied to the right branch.
        /// </param>
        /// <returns>
        /// A sequence containing the left then the right sequences.
        /// </returns>
        [Pure]
        [NotNull]
        [LinqTunnel]
        [CollectionAccess(CollectionAccessType.Read)]
        public static IEnumerable<TResult> BranchMerge<TLeft, TRight, TResult>(this (IEnumerable<TLeft> Left, IEnumerable<TRight> Right) source, Func<TLeft, TResult> left, Func<TRight, TResult> right)
        {
            if (left is null)
            {
                throw new ArgumentNullException(nameof(left));
            }
            if (right is null)
            {
                throw new ArgumentNullException(nameof(right));
            }

            return source.Left.Select(left).Concat(source.Right.Select(right));
        }

        /// <summary>
        /// Concatenates the two branches sequentially.
        /// </summary>
        /// <typeparam name="TLeft">
        /// The type of the left branch.
        /// </typeparam>
        /// <typeparam name="TRight">
        /// The type of the right branch after the transform function is applied.
        /// </typeparam>
        /// <typeparam name="TResult">
        /// The type of the resulting branch.
        /// </typeparam>
        /// <param name="source">
        /// A branched sequence.
        /// </param>
        /// <param name="left">
        /// A transform function to be applied to the right branch.
        /// </param>
        /// <param name="right">
        /// A transform function to be applied to the right branch.
        /// </param>
        /// <returns>
        /// A sequence containing the left then the right sequences.
        /// </returns>
        [Pure]
        [NotNull]
        [LinqTunnel]
        [CollectionAccess(CollectionAccessType.Read)]
        public static IEnumerable<TResult> BranchMerge<TLeft, TRight, TResult>(this (IEnumerable<TLeft> Left, IEnumerable<TRight> Right) source, Func<TLeft, int, TResult> left, Func<TRight, int, TResult> right)
        {
            if (left is null)
            {
                throw new ArgumentNullException(nameof(left));
            }
            if (right is null)
            {
                throw new ArgumentNullException(nameof(right));
            }

            return source.Left.Select(left).Concat(source.Right.Select(right));
        }
    }
}