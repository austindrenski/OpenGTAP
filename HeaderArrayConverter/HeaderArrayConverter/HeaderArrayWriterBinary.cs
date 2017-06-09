using System;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace HeaderArrayConverter
{
    /// <summary>
    /// Writes Header Array (HAR) files in binary format.
    /// </summary>
    [PublicAPI]
    public class HeaderArrayWriterBinary : HeaderArrayWriter
    {
        public override Task WriteAsync(string file, params IHeaderArray[] source)
        {
            throw new NotImplementedException();
        }

        public override void Write(string file, params IHeaderArray[] source)
        {
            throw new NotImplementedException();
        }
    }
}
