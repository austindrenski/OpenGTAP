using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace HeaderArrayConverter
{
    /// <summary>
    /// Extension methods to branch, operate, and join sequences.
    /// </summary>
    [PublicAPI]
    public static class ParallelBranch
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
        public static ParallelQuery<TResult> Branch<TSource, TResult>([NotNull] this ParallelQuery<TSource> source, [NotNull] Func<TSource, bool> predicate, [NotNull] Func<ParallelQuery<TSource>, ParallelQuery<TResult>> left, [NotNull] Func<ParallelQuery<TSource>, ParallelQuery<TResult>> right)
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
        public static ParallelQuery<TResult> Branch<TSource, TLeft, TRight, TResult>([NotNull] this ParallelQuery<TSource> source, [NotNull] Func<TSource, bool> predicate, [NotNull] Func<ParallelQuery<TSource>, ParallelQuery<TLeft>> left, [NotNull] Func<ParallelQuery<TSource>, ParallelQuery<TRight>> right, [NotNull] Func<ParallelQuery<TLeft>, ParallelQuery<TResult>> leftMerge, [NotNull] Func<ParallelQuery<TRight>, ParallelQuery<TResult>> rightMerge)
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
        public static (ParallelQuery<TSource> Left, ParallelQuery<TSource> Right) Branch<TSource>([NotNull] this ParallelQuery<TSource> source, [NotNull] Func<TSource, bool> predicate)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (predicate is null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            ConcurrentQueue<TSource> left = new ConcurrentQueue<TSource>();
            ConcurrentQueue<TSource> right = new ConcurrentQueue<TSource>();

            Parallel.ForEach(
                source,
                x =>
                {
                    if (predicate(x))
                    {
                        left.Enqueue(x);
                    }
                    else
                    {
                        right.Enqueue(x);
                    }
                });

            return (left.AsParallel(), right.AsParallel());
        }
    }
}