using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using JetBrains.Annotations;
// ReSharper disable PossibleInfiniteInheritance

namespace HeaderArrayConverter.IO
{
    /// <summary>
    /// Represents a partitioned collection supporting enumeration.
    /// </summary>
    [PublicAPI]
    public sealed class Partition<T> : IEnumerable<(int VectorIndex, IReadOnlyList<int> Min, IReadOnlyList<int> Max, IReadOnlyCollection<T> Values)>
    {
        /// <summary>
        /// The internal limitation on array length in the Fortran routines used by Gempack.
        /// </summary>
        private static readonly int GempackArrayLimit = 1_999_991;

        /// <summary>
        /// The source collection.
        /// </summary>
        [NotNull]
        private readonly (int[] Indexes, T Value)[] _items;

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
        public int Count => _items.Length;

        /// <summary>
        /// Gets the size limit of each partition.
        /// </summary>
        public int Size { get; }

        /// <summary>
        /// Gets the size of the last partition.
        /// </summary>
        public int LastSize => Count / Size;
        
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

            _items =
                source.GetLogicalEnumerable()
                      .Select(x => (x.Key.Select((y, i) => _headerArray.Sets[i].Value.IndexOf(y) + 1).ToArray(), x.Value))
                      .ToArray();

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

            Partitions = Count / Size;

            if (Count % Size > 0)
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
        public IEnumerator<(int VectorIndex, IReadOnlyList<int> Min, IReadOnlyList<int> Max, IReadOnlyCollection<T> Values)> GetEnumerator()
        {
            // Corner case where a valid header does not have any data, such as marker headers.
            if (Partitions == 0)
            {
                yield return (1, new int[0], new int[0], new T[0]);
            }

            for (int i = 0; i < Partitions; i++)
            {
                ArraySegment<(int[] position, T value)> temp = new ArraySegment<(int[] position, T value)>(_items, i * Size, Size);

                int[][] indexes = temp.Select(x => x.position.Concat(Enumerable.Repeat(1, 7)).Take(7).ToArray()).ToArray();

                int[] min = new int[_headerArray.Dimensions.Count];
                int[] max = new int[_headerArray.Dimensions.Count];
                for (int j = 0; j < _headerArray.Dimensions.Count; j++)
                {
                    min[j] = indexes.Select(x => x[j]).Min();
                    max[j] = indexes.Select(x => x[j]).Max();
                }
                
                if (temp.Any() || Size == 0)
                {
                    yield return (Partitions - i, min, max, temp.Select(x => x.value).ToArray());
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