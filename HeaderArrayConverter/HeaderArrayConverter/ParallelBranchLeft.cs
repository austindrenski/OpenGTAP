using System;
using System.Linq;
using JetBrains.Annotations;

namespace HeaderArrayConverter
{
    /// <summary>
    /// Extension methods to branch, operate, and join sequences.
    /// </summary>
    [PublicAPI]
    public static class ParallelBranchLeft
    {
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
        public static (ParallelQuery<TLeft> Left, ParallelQuery<TSource> Right) BranchLeft<TSource, TLeft>(this (ParallelQuery<TSource> Left, ParallelQuery<TSource> Right) source, Func<ParallelQuery<TSource>, ParallelQuery<TLeft>> left)
        {
            if (left is null)
            {
                throw new ArgumentNullException(nameof(left));
            }

            return (left(source.Left), source.Right);
        }
    }
}