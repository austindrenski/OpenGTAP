﻿using System;
using System.Linq;
using JetBrains.Annotations;

namespace HeaderArrayConverter.Branches
{
    /// <summary>
    /// Extension methods to branch, operate, and join sequences.
    /// </summary>
    [PublicAPI]
    public static class ParallelBranchMerge
    {
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
        public static ParallelQuery<TResult> BranchMerge<TResult>(this (ParallelQuery<TResult> Left, ParallelQuery<TResult> Right) source)
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
        public static ParallelQuery<TResult> BranchMerge<TLeft, TRight, TResult>(this (ParallelQuery<TLeft> Left, ParallelQuery<TRight> Right) source, Func<ParallelQuery<TLeft>, ParallelQuery<TResult>> left, Func<ParallelQuery<TRight>, ParallelQuery<TResult>> right)
        {
            if (left is null)
            {
                throw new ArgumentNullException(nameof(left));
            }
            if (right is null)
            {
                throw new ArgumentNullException(nameof(right));
            }

            return left(source.Left).Concat(right(source.Right));
        }
    }
}