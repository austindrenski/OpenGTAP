using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HeaderArrayConverter.Collections;
using JetBrains.Annotations;
// ReSharper disable PossibleInfiniteInheritance

namespace HeaderArrayConverter.IO
{
    /// <summary>
    /// Represents a partitioned collection supporting enumeration.
    /// </summary>
    [PublicAPI]
    public sealed class Partition<T> : IEnumerable<(int VectorIndex, IReadOnlyList<(int Lower, int Upper)> Ranges, IReadOnlyList<T> Values)> where T : IEquatable<T>
    {
        /// <summary>
        /// The internal limitation on array length in the Fortran routines used by Gempack.
        /// </summary>
        private static readonly int GempackArrayLimit = 1_999_991;

        /// <summary>
        /// The source <see cref="IHeaderArray{TValue}"/>.
        /// </summary>
        [NotNull]
        private readonly IHeaderArray<T> _headerArray;
        
        /// <summary>
        /// Gets the number of partitions.
        /// </summary>
        public int Partitions { get; }

        /// <summary>
        /// Gets the number of items in the collection.
        /// </summary>
        public int Total => _headerArray.Count;

        /// <summary>
        /// Gets the size limit of each partition.
        /// </summary>
        public int Size { get; }

        /// <summary>
        /// Gets the size of the last partition.
        /// </summary>
        public int LastSize => Total / Size;
        
        /// <summary>
        /// Constructs a <see cref="Partition{TValue}"/>.
        /// </summary>
        /// <param name="source">
        /// The source collection.
        /// </param>
        public Partition([NotNull] IHeaderArray<T> source)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            _headerArray = source;

            int test = 1;
            foreach (int dimension in source.Dimensions)
            {
                if ((test *= dimension) > GempackArrayLimit)
                {
                    break;
                }
                Size = test;
            }

            if (Size == 0)
            {
                Size = 1;
            }

            Partitions = Total / Size;

            if (Total % Size > 0)
            {
                Partitions++;
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// An enumerator that can be used to iterate through the collection.
        /// </returns>
        [Pure]
        [NotNull]
        public IEnumerator<(int VectorIndex, IReadOnlyList<(int Lower, int Upper)> Ranges, IReadOnlyList<T> Values)> GetEnumerator()
        {
            int dimensions = _headerArray.Dimensions.Count;

            (int, (int, int)[], T[]) cornerCase = (1, new(int, int)[dimensions], new T[0]);

            // Corner case where a valid header does not have any data, such as marker headers.
            if (Partitions == 0)
            {
                yield return cornerCase;
                yield break;
            }
            
            KeyValuePair<KeySequence<string>, T>[] items = _headerArray.ToArray();

            if (items.Length == 0)
            {
                yield return cornerCase;
                yield break;
            }

            IImmutableList<KeyValuePair<string, IImmutableList<string>>> sets = _headerArray.Sets;
            
            for (int i = 0; i < Partitions; i++)
            {
                ArraySegment<KeyValuePair<KeySequence<string>, T>> temp = new ArraySegment<KeyValuePair<KeySequence<string>, T>>(items, i * Size, Size);

                int[][] indexes =
                    temp.Select(x => x.Key)
                        .Select(
                            x =>
                                x.Select((y, j) => sets[j].Value.IndexOf(y) + 1)
                                 .Concat(Enumerable.Repeat(1, dimensions))
                                 .Take(dimensions)
                                 .ToArray())
                        .ToArray();

                (int Lower, int Upper)[] ranges = new(int Lower, int Upper)[dimensions];

                for (int j = 0; j < dimensions; j++)
                {
                    int jClosure = j;
                    ranges[j] = (indexes.Select(x => x[jClosure]).Min(), indexes.Select(x => x[jClosure]).Max());
                }
                
                if (temp.Any() || Size == 0)
                {
                    yield return (Partitions - i, ranges, temp.Select(x => x.Value).ToArray());
                }
            }
        }

        [Pure]
        [NotNull]
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}