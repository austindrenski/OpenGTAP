using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AD.IO;
using JetBrains.Annotations;

namespace HeaderArrayConverter
{
    [PublicAPI]
    public class HeaderArrayFile : IEnumerable<IHeaderArray>
    {
        private ImmutableOrderedDictionary<string, IHeaderArray> Arrays { get; }

        public int Count => Arrays.Count;

        public IHeaderArray this[KeySequence<string> key] => Arrays[key];

        public HeaderArrayFile(IEnumerable<IHeaderArray> arrays)
        {
            if (arrays is null)
            {
                throw new ArgumentNullException(nameof(arrays));
            }

            Arrays = arrays.ToImmutableOrderedDictionary(x => x.Header, x => x);
        }

        public static HeaderArrayFile Read(FilePath file)
        {
            return new HeaderArrayFile(HeaderArray.ReadArrays(file));
        }

        public override string ToString()
        {
            return
                Arrays.Aggregate(
                    new StringBuilder(),
                    (current, next) =>
                        current.AppendLine("-----------------------------------------------")
                               .AppendLine(next.Value.ToString()),
                    x =>
                        x.ToString());
        }

        public IEnumerator<IHeaderArray> GetEnumerator()
        {
            return Arrays.Select(x => x.Value).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}