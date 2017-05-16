using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using JetBrains.Annotations;

namespace HeaderArrayConverter
{
    /// <summary>
    /// Extension methods to find header arrays in Header Array (HAR) formatted files.
    /// </summary>
    [PublicAPI]
    public static class HeaderArrayHelper
    {
        /// <summary>
        /// Gets the next header from the reader.
        /// </summary>
        /// <param name="reader">
        /// The reader from which to return the next header.
        /// </param>
        /// <param name="headerLength">
        /// The length of the header identifier. Default is 4.
        /// </param>
        /// <returns>
        /// The byte representation of the header array beginning at the header.
        /// </returns>
        public static (string Header, byte[] Content) GetHeader(BinaryReader reader, byte headerLength = 4)
        {
            if (reader is null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            string header = Encoding.ASCII.GetString(GetContent(reader, headerLength));
            byte[] content = GetContent(reader, headerLength);
            
            return (header, content);
        }

        private static byte[] GetContent(BinaryReader reader, byte headerLength = 4)
        {
            if (reader is null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            ImmutableArray<byte> headerIdentifier =
                new byte[] { headerLength, 0x00, 0x00, 0x00 }.ToImmutableArray();
            
            Queue<byte> buffer = new Queue<byte>(headerIdentifier.Length);
            Queue<byte> result = new Queue<byte>();

            if (reader.BaseStream.Position == 0)
            {
                while (!buffer.SequenceEqual(headerIdentifier))
                {
                    if (reader.BaseStream.Position == reader.BaseStream.Length)
                    {
                        break;
                    }
                    if (buffer.Count == headerIdentifier.Length)
                    {
                        result.Enqueue(buffer.Dequeue());
                    }
                    buffer.Enqueue(reader.ReadByte());
                }

                buffer.Clear();
            }

            while (!buffer.SequenceEqual(headerIdentifier))
            {
                if (reader.BaseStream.Position == reader.BaseStream.Length)
                {
                    break;
                }
                if (buffer.Count == headerIdentifier.Length)
                {
                    result.Enqueue(buffer.Dequeue());
                }
                buffer.Enqueue(reader.ReadByte());
            }

            return result.ToArray();
        }

        public static (int Count, int Length, byte[] Content) GetDescriptionFromContent(byte[] content)
        {
            if (content is null)
            {
                throw new ArgumentNullException(nameof(content));
            }
            if (!content.Any())
            {
                return (0, 0, new byte[0]);
            }
            
            ImmutableArray<byte> marker =
                new byte[] { 0x5C, 0x00, 0x00, 0x00 }.ToImmutableArray();
            
            Queue<byte> buffer = new Queue<byte>(marker.Length);
            Stack<byte> result = new Stack<byte>();

            bool insideDescription = false;
            for (int i = 0; i < content.Length; i++)
            {
                if (buffer.Count == marker.Length)
                {
                    result.Push(buffer.Dequeue());
                }
                if (result.Take(marker.Length).Reverse().SequenceEqual(marker))
                {
                    for (int j = 0; j < marker.Length; j++)
                    {
                        result.Pop();
                    }
                    if (insideDescription)
                    {
                        break;
                    }
                    insideDescription = true;
                }
                buffer.Enqueue(content[i]);
            }

            if (result.Count == 0)
            {
                return (0, 0, new byte[0]);
            }

            int count = BitConverter.ToInt32(result.Take(4).Reverse().ToArray(), 0);
            int size = BitConverter.ToInt32(result.Skip(4).Take(4).Reverse().ToArray(), 0);
            byte[] final = result.Skip(8).Reverse().ToArray();

            return (size, count, final);
        }
    }
}