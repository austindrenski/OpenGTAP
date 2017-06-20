using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace HeaderArrayConverter.Branches
{
    /// <summary>
    /// Extension methods to branch, operate, and join sequences.
    /// </summary>
    [PublicAPI]
    public static class BranchRightExtensions
    {
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
        public static (IEnumerable<TLeft> Left, IEnumerable<TRight> Right) BranchRight<TSource, TLeft, TRight>(this (IEnumerable<TLeft> Left, IEnumerable<TSource> Right) source, Func<IEnumerable<TSource>, IEnumerable<TRight>> right)
        {
            if (right is null)
            {
                throw new ArgumentNullException(nameof(right));
            }

            return (source.Left, right(source.Right));
        }
    }
}