using System;
using System.Collections.Immutable;
using System.Text;
using JetBrains.Annotations;

namespace HeaderArrayConverter
{
    public class HeaderArrayRE : HeaderArray
    {
        /// <summary>
        /// The decoded form of <see cref="Array"/>
        /// </summary>
        public ImmutableArray<float> Floats { get; }

        public HeaderArrayRE([NotNull] string header, [CanBeNull] string description, [NotNull] string type, int count, int size, bool sparse, int x0, int x1, int x2, [NotNull] float[] floats)
            : base(header, description, type, count, size, sparse, x0, x1, x2)
        {
            Floats = floats.ToImmutableArray();
        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder(base.ToString());

            for (int i = 0; i < Floats.Length; i++)
            {
                stringBuilder.AppendLine($"[{i}]: {Floats[i]}");
            }
            return stringBuilder.ToString();
        }
    }
}
