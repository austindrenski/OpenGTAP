using System;
using System.Collections.Immutable;
using System.Linq;
using JetBrains.Annotations;

namespace HeaderArrayConverter
{
    [PublicAPI]
    public struct HeaderArray
    {
        public string Header { get; }
            
        public int X0 { get; }

        public int X1 { get; }

        public int X2 { get; }

        public HeaderArrayInfo Info { get; }

        public ImmutableArray<ImmutableArray<byte>> Array { get; }

        public HeaderArray([NotNull] string header, HeaderArrayInfo info, int x0,  int x1, int x2, [NotNull][ItemNotNull] byte[][] array)
        {
            if (header is null)
            {
                throw new ArgumentNullException(nameof(header));
            }
            if (array is null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            Header = header;
            Info = info;
            X0 = x0;
            X1 = x1;
            X2 = x2;
            Array = array.Select(x => x.ToImmutableArray()).ToImmutableArray();
        }

        public override string ToString()
        {
            return $"{nameof(Header)}: {Header}\r\n" +
                    $"Dimensions: [{X0}][{X1}][{X2}]\r\n" +
                    $"{Info}\r\n" +
                    $"{nameof(Header)}: {Header}";
        }
    }
}