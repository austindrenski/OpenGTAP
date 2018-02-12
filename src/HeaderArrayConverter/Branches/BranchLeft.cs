using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace HeaderArrayConverter.Branches
{
    /// <summary>
    /// Extension methods to branch, operate, and join sequences.
    /// </summary>
    [PublicAPI]
    public static class BranchLeftExtensions
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
        public static (IEnumerable<TLeft> Left, IEnumerable<TSource> Right) BranchLeft<TSource, TLeft>(this (IEnumerable<TSource> Left, IEnumerable<TSource> Right) source, Func<IEnumerable<TSource>, IEnumerable<TLeft>> left)
        {
            if (left is null)
            {
                throw new ArgumentNullException(nameof(left));
            }

            return (left(source.Left), source.Right);
        }
    }
}