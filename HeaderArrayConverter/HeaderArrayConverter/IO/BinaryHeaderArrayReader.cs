using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AD.IO;
using JetBrains.Annotations;

namespace HeaderArrayConverter.IO
{
    /// <summary>
    /// Implements a <see cref="HeaderArrayReader"/> for reading Header Array (HAR) files in binary format.
    /// </summary>
    [PublicAPI]
    public sealed class BinaryHeaderArrayReader : HeaderArrayReader
    {
        /// <summary>
        /// The padding sequence used in binary HAR files.
        /// </summary>
        private static readonly int Padding = 0x20_20_20_20;

        /// <summary>
        /// Reads <see cref="IHeaderArray"/> collections from file..
        /// </summary>
        /// <param name="file">
        /// The file to read.
        /// </param>
        /// <return>
        /// A <see cref="HeaderArrayFile"/> representing the contents of the file.
        /// </return>
        public override HeaderArrayFile Read(FilePath file)
        {
            if (file is null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            return new HeaderArrayFile(ReadArrays(file));
        }

        /// <summary>
        /// Asynchronously reads <see cref="IHeaderArray"/> collections from file..
        /// </summary>
        /// <param name="file">
        /// The file to read.
        /// </param>
        /// <return>
        /// A task that upon completion returns a <see cref="HeaderArrayFile"/> representing the contents of the file.
        /// </return>
        public override async Task<HeaderArrayFile> ReadAsync(FilePath file)
        {
            if (file is null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            return new HeaderArrayFile(await Task.Run(() => ReadArraysAsync(file).AsParallel().Select(x => x.Result)));
        }

        /// <summary>
        /// Enumerates the <see cref="IHeaderArray"/> collection from file.
        /// </summary>
        /// <param name="file">
        /// The file from which to read arrays.
        /// </param>
        /// <returns>
        /// A <see cref="IHeaderArray"/> collection from the file.
        /// </returns>
        public override IEnumerable<IHeaderArray> ReadArrays(FilePath file)
        {
            if (file is null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            using (BinaryReader reader = new BinaryReader(File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                long length = reader.BaseStream.Length;
                while (reader.BaseStream.Position < length)
                {
                    yield return ReadNext(reader);
                }
            }
        }

        /// <summary>
        /// Asynchronously enumerates the arrays from file.
        /// </summary>
        /// <param name="file">
        /// The file from which to read arrays.
        /// </param>
        /// <returns>
        /// An enumerable collection of tasks that when completed return an <see cref="IHeaderArray"/> from file.
        /// </returns>
        public override IEnumerable<Task<IHeaderArray>> ReadArraysAsync(FilePath file)
        {
            if (file is null)
            {
                throw new ArgumentNullException(nameof(file));
            }
            
            using (BinaryReader reader = new BinaryReader(File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                long length = reader.BaseStream.Length;
                while (reader.BaseStream.Position < length)
                {
                    yield return Task.FromResult(ReadNext(reader));
                }
            }
        }

        /// <summary>
        /// Reads one header array from the <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader">
        /// The reader from which the next header array is read.
        /// </param>
        /// <returns>
        /// An <see cref="IHeaderArray"/>.
        /// </returns>
        [NotNull]
        private static IHeaderArray ReadNext([NotNull] BinaryReader reader)
        {
            string header = GetHeader(reader);

            (string description, HeaderArrayStorage storage, HeaderArrayType type, int[] dimensions, int count) = GetMetadata(reader);

            switch (type)
            {
                case HeaderArrayType.C1:
                {
                    string[] values = GetOneDimensionalArray(reader, storage, count, (data, index, length) => Encoding.ASCII.GetString(data, index, length).Trim('\u0000', '\u0002', '\u0020'));

                    return HeaderArray<string>.Create(header, header, description, type, dimensions, values);
                }
                case HeaderArrayType.I2:
                {
                    int[] values = GetTwoDimensionalArray(reader, storage, count, BitConverter.ToInt32);

                    return HeaderArray<int>.Create(header, header, description, type, dimensions, values);
                }
                case HeaderArrayType.R2:
                {
                    float[] values = GetTwoDimensionalArray(reader, storage, count, BitConverter.ToSingle);

                    return HeaderArray<float>.Create(header, header, description, type, dimensions, values);
                }
                case HeaderArrayType.RE:
                {
                    (string coefficient, KeyValuePair<string, IImmutableList<string>>[] sets) = ReadSets(reader, dimensions);

                    float[] results = GetArrayWithSets(reader, storage, count, BitConverter.ToSingle);

                    return HeaderArray<float>.Create(header, coefficient, description, type, dimensions, results, sets.ToImmutableArray());
                }
                default:
                {
                    throw new DataValidationException("An unknown header array type was encountered.", "1C, RE, 2I, 2R", type);
                }
            }
        }

        /// <summary>
        /// Calculates the next 1-dimensional array of data.
        /// </summary>
        /// <param name="reader">
        /// The reader from which the array is read.
        /// </param>
        ///  <param name="storage">
        /// The storage type of the array.
        /// </param>
        /// <param name="count">
        /// The total count of elements.
        /// </param>
        /// <param name="converter">
        /// A delegate returning a value from an index inside a byte array.
        /// </param>
        /// <returns>
        /// An array of data.
        /// </returns>
        /// <remarks>
        /// The array is composed of one or more:
        /// 
        ///     [segment] = <see cref="GetNextOneDimensionalSegment{TValue}(BinaryReader, Func{byte[], int, int, TValue}, out TValue[])"/>.
        /// 
        /// </remarks>
        [NotNull]
        private static TValue[] GetOneDimensionalArray<TValue>([NotNull] BinaryReader reader, HeaderArrayStorage storage, int count, [NotNull] Func<byte[], int, int, TValue> converter)
        {
            TValue[] results = new TValue[count];

            switch (storage)
            {
                case HeaderArrayStorage.Full:
                {
                    int index = int.MaxValue;
                    int counter = 0;
                    while (index > 1)
                    {
                        index = GetNextOneDimensionalSegment(reader, converter, out TValue[] values);

                        Array.Copy(values, 0, results, counter, values.Length);

                        counter += values.Length;
                    }

                    return results;
                }
                case HeaderArrayStorage.Sparse:
                {
                    throw new NotSupportedException("The storage SPSE is not supported for 2-dimensional arrays");
                }
                default:
                {
                    throw new DataValidationException("An unknown header array storage was encountered.", "FULL, SPSE", storage);
                }
            }
        }

        /// <summary>
        /// Calculates the next 2-dimensional array of data.
        /// </summary>
        /// <param name="reader">
        /// The reader from which the data is read.
        /// </param>
        ///  <param name="storage">
        /// The storage type of the array.
        /// </param>
        /// <param name="count">
        /// The number of items in the segment.
        /// </param>
        /// <param name="converter">
        /// A delegate returning a value from an index inside a byte array.
        /// </param>
        /// <returns>
        ///  An array of data.
        /// </returns>
        /// <remarks>
        /// The array is composed of one or more:
        /// 
        ///     [segment] = <see cref="GetNextTwoDimensionalSegment{TValue}(BinaryReader, Func{byte[], int, TValue}, out TValue[])"/>.
        /// 
        /// </remarks>
        [NotNull]
        private static TValue[] GetTwoDimensionalArray<TValue>([NotNull] BinaryReader reader, HeaderArrayStorage storage, int count, [NotNull] Func<byte[], int, TValue> converter)
        {
            TValue[] results = new TValue[count];

            switch (storage)
            {
                case HeaderArrayStorage.Full:
                {
                    int index = int.MaxValue;
                    int counter = 0;
                    while (index > 1)
                    {
                        index = GetNextTwoDimensionalSegment(reader, converter, out TValue[] values);

                        Array.Copy(values, 0, results, counter, values.Length);

                        counter += values.Length;
                    }

                    return results;
                }
                case HeaderArrayStorage.Sparse:
                {
                    throw new NotSupportedException("The storage SPSE is not supported for 2-dimensional arrays");
                }
                default:
                {
                    throw new DataValidationException("An unknown header array storage was encountered.", "FULL, SPSE", storage);
                }
            }
        }

        ///  <summary>
        ///  Calculates the next array with sets stored as either non-sparse or sparse.
        ///  </summary>
        ///  <param name="reader">
        ///  The reader from which the data is read.
        ///  </param>
        ///  <param name="storage">
        /// The storage type of the array.
        /// </param>
        ///  <param name="count">
        ///  The total count of values in the logical array.
        ///  </param>
        /// <param name="converter">
        ///  A delegate returning a value from an index inside a byte array.
        ///  </param>
        /// <returns>
        ///  An array of data.
        ///  </returns>
        ///  <remarks>
        ///  If the array is non-sparse, then it is composed of:
        ///  
        ///          [dimensions] = <see cref="ReadDimensions(BinaryReader)"/>.
        /// 
        ///      Followed by one or more:
        ///  
        ///          [extents] = <see cref="ReadExtents(BinaryReader)"/>.
        ///          [segment] = <see cref="GetNextFullSegment{T}(BinaryReader, Func{byte[], int, T}, out T[])"/>.
        ///  
        ///  If the array is sparse, then it is composed of:
        ///  
        ///          [metadata] = <see cref="GetExtraMetadata(BinaryReader)"/>.
        ///  
        ///      Followed by one or more:
        ///  
        ///          [segment] = <see cref="GetNextSparseSegment{TValue}(BinaryReader, Func{byte[], int, TValue}, out TValue[], out int[])"/>.
        ///  
        ///   </remarks>
        [NotNull]
        private static TValue[] GetArrayWithSets<TValue>([NotNull] BinaryReader reader, HeaderArrayStorage storage, int count, [NotNull] Func<byte[], int, TValue> converter)
        {
            TValue[] results = new TValue[count];

            switch (storage)
            {
                case HeaderArrayStorage.Full:
                {
                    (int index, int _, int[] _) = ReadDimensions(reader);

                    while (index > 1)
                    {
                        (int _, int offset, int[] _) = ReadExtents(reader);

                        index = GetNextFullSegment(reader, converter, out TValue[] floats);

                        Array.Copy(floats, 0, results, offset, floats.Length);
                    }

                    return results;
                }
                case HeaderArrayStorage.Sparse:
                {
                    (int _, int _, int _, string _) = GetExtraMetadata(reader);

                    int index = int.MaxValue;

                    while (index > 1)
                    {
                        index = GetNextSparseSegment(reader, converter, out TValue[] values, out int[] pointers);

                        for (int i = 0; i < pointers.Length; i++)
                        {
                            results[pointers[i]] = values[i];
                        }
                    }

                    return results;
                }
                default:
                {
                    throw new DataValidationException("An unknown header array storage was encountered.", "FULL, SPSE", storage);
                }
            }
        }

        #region Segment constructors

        /// <summary>
        /// Calculates the next value segment for a non-sparse one-dimensional array.
        /// </summary>
        /// <typeparam name="TValue">
        /// The type of data in the array.
        /// </typeparam>
        /// <param name="reader">
        /// The reader from which the segment is read.
        /// </param>
        /// <param name="converter">
        /// A delegate returning a value from an index inside a byte array.
        /// </param>
        /// <param name="results">
        /// The values of the array segment.
        /// </param>
        /// <returns>
        /// The index of this segment.
        /// </returns>
        /// <remarks>
        /// For a 1-dimensional arrays, the next segment is composed of:
        /// 
        ///     [0 * sizeof(int)] = vector index.
        ///     [1 * sizeof(int)] = the number of elements in total.
        ///     [2 * sizeof(int)] = the number of elements in the segment.
        ///     [3 * sizeof(int) + i * count] = the i-th data value in the segment where count is the byte-length of a given item.
        /// 
        /// </remarks>
        private static int GetNextOneDimensionalSegment<TValue>([NotNull] BinaryReader reader, [NotNull] Func<byte[], int, int, TValue> converter, [NotNull] out TValue[] results)
        {
            byte[] data = InitializeArray(reader);

            int index = BitConverter.ToInt32(data, 0 * sizeof(int));
            int extent1 = BitConverter.ToInt32(data, 2 * sizeof(int));

            int count = (data.Length - 3 * sizeof(int)) / extent1;

            results = new TValue[count];

            for (int i = 0; i < count; i++)
            {
                results[i] = converter(data, 3 * sizeof(int) + i * count, count);
            }

            return index;
        }

        /// <summary>
        /// Calculates the next value segment for a non-sparse two-dimensional array.
        /// </summary>
        /// <typeparam name="TValue">
        /// The type of data in the array.
        /// </typeparam>
        /// <param name="reader">
        /// The reader from which the segment is read.
        /// </param>
        /// <param name="converter">
        /// A delegate returning a value from an index inside a byte array.
        /// </param>
        /// <param name="results">
        /// The values of the array segment.
        /// </param>
        /// <returns>
        /// The index of this segment.
        /// </returns>
        /// <remarks>
        /// For a 2-dimensional arrays, the next segment is composed of:
        /// 
        ///     [0 * sizeof(int)] = vector index.
        ///     [1 * sizeof(int)] = the number of elements in the first dimension.
        ///     [2 * sizeof(int)] = the number of elements in the second dimension.
        ///     [3 * sizeof(int)] = the 1-based starting index of the first dimension in the segment on the logical array.
        ///     [4 * sizeof(int)] = the 1-based ending index of the first dimension in the segment on the logical array.
        ///     [5 * sizeof(int)] = the 1-based starting index of the second dimension in the segment on the logical array.
        ///     [6 * sizeof(int)] = the 1-based ending index of the second dimension in the segment on the logical array.
        ///     [(7 + i) * sizeof(int)] = the i-th data value in the segment. 
        /// 
        /// </remarks>
        private static int GetNextTwoDimensionalSegment<TValue>([NotNull] BinaryReader reader, [NotNull] Func<byte[], int, TValue> converter, [NotNull] out TValue[] results)
        {
            byte[] data = InitializeArray(reader);

            int index = BitConverter.ToInt32(data, 0 * sizeof(int));

            results = new TValue[(data.Length - 7 * sizeof(int)) / sizeof(int)];

            for (int i = 0; i < results.Length; i++)
            {
                results[i] = converter(data, (i + 7) * sizeof(int));
            }

            return index;
        }

        /// <summary>
        /// Calculates the next full value segment that starts with a 32-bit integer representing the segment index and values beginning at the specified offset.
        /// </summary>
        /// <typeparam name="TValue">
        /// The type of data in the array.
        /// </typeparam>
        /// <param name="reader">
        /// The reader from which the segment is read.
        /// </param>
        /// <param name="converter">
        /// A delegate returning a value from an index inside a byte array.
        /// </param>
        /// <param name="results">
        /// The values of the array segment.
        /// </param>
        /// <returns>
        /// The index of this segment.
        /// </returns>
        /// <remarks>
        /// For 7-dimensional arrays, the next segment is composed of:
        /// 
        ///     [0 * sizeof(int)] = 1-based index of the current vector from the end of the record.
        ///     [(1 + i) * sizeof(int)] = the i-th value of the segment. 
        /// 
        /// </remarks>
        private static int GetNextFullSegment<TValue>([NotNull] BinaryReader reader, [NotNull] Func<byte[], int, TValue> converter, [NotNull] out TValue[] results)
        {
            byte[] data = InitializeArray(reader);

            int vectorIndex = BitConverter.ToInt32(data, 0 * sizeof(int));

            results = new TValue[(data.Length - 1 * sizeof(int)) / sizeof(int)];

            for (int i = 0; i < results.Length; i++)
            {
                results[i] = converter(data, (i + 1) * sizeof(int));
            }

            return vectorIndex;
        }

        /// <summary>
        /// Calculates the next sparse value segment that starts with a 32-bit integer representing the segment index and holds pointers followed by values.
        /// </summary>
        /// <typeparam name="TValue">
        /// The type of data in the array.
        /// </typeparam>
        /// <param name="reader">
        /// The reader from which the segment is read.
        /// </param>
        /// <param name="converter">
        /// A delegate returning a value from an index inside a byte array.
        /// </param>
        /// <param name="results">
        /// The values of the array segment.
        /// </param>
        /// <param name="pointers">
        /// The pointers into the main array for values in the segment.
        /// </param>
        /// <returns>
        /// The index of this segment.
        /// </returns>
        /// <remarks>
        /// For sparse 7-dimensional arrays, the next segment is composed of:
        /// 
        ///     [0 * sizeof(int)] = 1-based index of the current vector from the end of the record.
        ///     [1 * sizeof(int)] = the total number of non-zero values stored in the sparse header array.
        ///     [2 * sizeof(int)] = the number of positions and values stored in this segment.
        ///     [(3 + i) * sizeof(int)] = the i-th pointer in this segment.
        ///     [2 * (3 + i) * sizeof(int)] = the i-th value in this segment.  
        /// 
        /// </remarks>
        private static int GetNextSparseSegment<TValue>([NotNull] BinaryReader reader, [NotNull] Func<byte[], int, TValue> converter, [NotNull] out TValue[] results, [NotNull] out int[] pointers)
        {
            byte[] data = InitializeArray(reader);
            int index = BitConverter.ToInt32(data, 0 * sizeof(int));
            int segmentCount = BitConverter.ToInt32(data, 2 * sizeof(int));

            pointers = new int[segmentCount];
            results = new TValue[segmentCount];

            for (int j = 0; j < segmentCount; j++)
            {
                pointers[j] = BitConverter.ToInt32(data, 3 * sizeof(int) + j * sizeof(int)) - 1;

                results[j] = converter(data, 3 * sizeof(int) + segmentCount * sizeof(int) + j * sizeof(int));
            }

            return index;
        }

        #endregion Segment constructors

        #region Common helper methods

        /// <summary>
        /// Reads the next array from the <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader">
        /// The <see cref="BinaryReader"/> from which the array is read.
        /// </param>
        /// <returns>
        /// The next array from the <see cref="BinaryReader"/> positioned after the initial padding (e.g. <see cref="Padding"/>).
        /// </returns>
        [NotNull]
        private static byte[] InitializeArray([NotNull] BinaryReader reader)
        {
            int length = reader.ReadInt32();
            int padding = reader.ReadInt32();

            byte[] data = reader.ReadBytes(length - sizeof(int));

            int closingLength = reader.ReadInt32();

            if (length != closingLength)
            {
                throw new DataValidationException("Binary length check", length, closingLength);
            }

            if (Padding != padding)
            {
                throw new DataValidationException("Binary padding check", Padding, padding);
            }

            return data;
        }

        /// <summary>
        /// Reads a character string that is pre- and post-fixed by 32-bit integers of its length.
        /// </summary>
        /// <param name="reader">
        /// The reader from which the header is returned.
        /// </param>
        /// <returns>
        /// A character string uniquely naming the <see cref="IHeaderArray"/>.
        /// </returns>
        [NotNull]
        private static string GetHeader([NotNull] BinaryReader reader)
        {
            int length = reader.ReadInt32();

            string header = Encoding.ASCII.GetString(reader.ReadBytes(length));

            int closingLength = reader.ReadInt32();

            if (length != closingLength)
            {
                throw new DataValidationException("Binary length check", length, closingLength);
            }

            return header;
        }

        /// <summary>
        /// Reads the metadata entry and returns a long-name description, whether or not the full matrix is stored, the data type, and the dimensions.
        /// </summary>
        /// <param name="reader">
        /// The reader from which the metadata is returned.
        /// </param>
        /// <returns>
        /// A long-name description, a sparse indicator, the data type, the dimensions, and the count of values.
        /// </returns>
        private static (string Description, HeaderArrayStorage Storage, HeaderArrayType Type, int[] Dimensions, int Count) GetMetadata([NotNull] BinaryReader reader)
        {
            byte[] descriptionBuffer = InitializeArray(reader);

            HeaderArrayType type = (HeaderArrayType) BitConverter.ToInt16(descriptionBuffer, default(short));

            HeaderArrayStorage storage = (HeaderArrayStorage) BitConverter.ToInt32(descriptionBuffer, sizeof(short));

            string description = Encoding.ASCII.GetString(descriptionBuffer, sizeof(short) + sizeof(int), 70).Trim('\u0000', '\u0002', '\u0020');

            int[] dimensions = new int[BitConverter.ToInt32(descriptionBuffer, sizeof(short) + sizeof(int) + 70)];

            int count = 1;
            for (int i = 0; i < dimensions.Length; i++)
            {
                dimensions[i] = BitConverter.ToInt32(descriptionBuffer, sizeof(short) + sizeof(int) + 70 + sizeof(int) + i * sizeof(int));
                count *= dimensions[i];
            }

            if (type == HeaderArrayType.C1)
            {
                // 1C is actually a vector of character vectors.
                // So just take the first dimension.
                count = dimensions[0];
            }
            
            return (description, storage, type, dimensions, count);
        }

        /// <summary>
        /// Gets the extra metadata segment.
        /// </summary>
        /// <param name="reader">
        /// The reader from which the segment is read.
        /// </param>
        /// <returns>
        /// The count of non-zero entries, the byte size of integers, the byte size of reals, and a description.
        /// </returns>
        /// <remarks>
        /// For 7-dimensional real arrays, the extra metadata is composed of:
        /// 
        ///     [0 * sizeof(int)] = the count of non-zero values.
        ///     [1 * sizeof(int)] = the byte-size of integer values.
        ///     [2 * sizeof(int)] = the byte-size of real values. 
        ///     [3 * sizeof(int)] = an 80-character description.
        ///
        ///  </remarks>
        private static (int NonZeroCount, int SizeOfInteger, int SizeOfReal, string Description) GetExtraMetadata([NotNull] BinaryReader reader)
        {
            byte[] data = InitializeArray(reader);

            int nonZeroCount = BitConverter.ToInt32(data, 0 * sizeof(int));
            int sizeOfInteger = BitConverter.ToInt32(data, 1 * sizeof(int));
            int sizeOfReal = BitConverter.ToInt32(data, 2 * sizeof(int));
            string description = Encoding.ASCII.GetString(data, 3 * sizeof(int), 80);

            return (nonZeroCount, sizeOfInteger, sizeOfReal, description);
        }

        /// <summary>
        /// Calculates the coefficient and set names.
        /// </summary>
        /// <param name="reader">
        /// The reader from which the dimension definitions are read.
        /// </param>
        /// <returns>
        /// The coefficient and set names.
        /// </returns>
        /// <remarks>
        /// The sets segment is composed of:
        /// 
        ///     [0 * sizeof(int)] = the number of distinct sets stored.
        ///     [1 * sizeof(int)] = true if the dimensions are known.
        ///     [2 * sizeof(int)] = the number of sets that define the coefficient.
        ///     [3 * sizeof(int)] = the 12-character coefficient.
        ///     [3 * sizeof(int) + 12] = true if the sets are known.
        ///     [4 * sizeof(int) + 12 + i * 12] = the i-th 12-character set name.
        /// 
        /// </remarks>
        private static (int DistinctSetCount, string Coefficient, string[] SetNames) GetCoefficientAndSetNames([NotNull] BinaryReader reader)
        {
            byte[] setsArray = InitializeArray(reader);

            int distinctSets = BitConverter.ToInt32(setsArray, 0 * sizeof(int));

            bool dimensionsKnown = BitConverter.ToBoolean(setsArray, 1 * sizeof(int));

            if (!dimensionsKnown && distinctSets != 0)
            {
                throw new DataValidationException("Binary boolean check. If the dimensions are unknown, there should be no distinct sets recorded.", true, false);
            }

            int sets = BitConverter.ToInt32(setsArray, 2 * sizeof(int));

            string coefficient = Encoding.ASCII.GetString(setsArray, 3 * sizeof(int), 12).Trim();

            bool setsKnown = BitConverter.ToBoolean(setsArray, 3 * sizeof(int) + 12);

            if (!setsKnown && distinctSets != 0)
            {
                throw new DataValidationException("Binary boolean check. If the sets are unknown, there should be no distinct sets recorded.", true, false);
            }

            // Read set names
            string[] setNames = new string[sets];
            for (int i = 0; i < setNames.Length; i++)
            {
                setNames[i] = Encoding.ASCII.GetString(setsArray, 4 * sizeof(int) + 12 + i * 12, 12).Trim();
            }

            return (distinctSets, coefficient, setNames);
        }

        /// <summary>
        /// Calculates the sets and set elements in the record.
        /// </summary>
        /// <param name="reader">
        /// The reader from which the dimension definitions are read.
        /// </param>
        /// <param name="dimensions">
        /// An array of dimension lengths.
        /// </param>
        /// <returns>
        /// The coefficient name and sets.
        /// </returns>
        /// <remarks>
        /// The sets segment is composed of:
        /// 
        ///     [set metadata] = <see cref="GetCoefficientAndSetNames(BinaryReader)"/>.
        /// 
        /// Followed by one or more:
        ///     
        ///     [set] = <see cref="GetOneDimensionalArray{TValue}(BinaryReader, HeaderArrayStorage, int, Func{byte[], int, int, TValue})"/>.     
        /// 
        /// </remarks>
        private static (string Coefficient, KeyValuePair<string, IImmutableList<string>>[] Sets) ReadSets([NotNull] BinaryReader reader, [NotNull] int[] dimensions)
        {
            (int distinctSetCount, string coefficient, string[] setNames) = GetCoefficientAndSetNames(reader);

            string[][] setItems = new string[setNames.Length][];
            for (int i = 0; i < distinctSetCount; i++)
            {
                setItems[i] = 
                    GetOneDimensionalArray(
                        reader, 
                        HeaderArrayStorage.Full, 
                        dimensions[i], 
                        (data, index, length) => Encoding.ASCII.GetString(data, index, length).Trim('\u0000', '\u0002', '\u0020'));
            }

            if (distinctSetCount > 0 && setNames.Length > 0 && setNames.Length - distinctSetCount == 1)
            {
                setItems[setItems.Length - 1] = setItems[setItems.Length - 2];
            }

            KeyValuePair<string, IImmutableList<string>>[] sets = new KeyValuePair<string, IImmutableList<string>>[setNames.Length];
            for (int i = 0; i < setNames.Length; i++)
            {
                sets[i] = new KeyValuePair<string, IImmutableList<string>>(setNames[i], setItems[i].ToImmutableArray());
            }
            if (setNames.Length == 0)
            {
                sets = new KeyValuePair<string, IImmutableList<string>>[]
                {
                    new KeyValuePair<string, IImmutableList<string>>(coefficient, ImmutableArray.Create(coefficient))
                };
            }

            return (coefficient, sets);
        }

        /// <summary>
        /// Calculates the count of elements in the record based on the dimensions.
        /// </summary>
        /// <param name="reader">
        /// The reader from which the dimension definitions are read.
        /// </param>
        /// <returns>
        /// The segment index, the product of the dimension counts, and an array of dimension counts.
        /// </returns>
        /// <remarks>
        /// The dimension array contains the following:
        /// 
        ///     [0 * sizeof(int)] = 1-based index of the current vector from the end of the record.
        ///     [1 * sizeof(int)] = the count of dimensions for the record.
        ///     [(2 + i) * sizeof(int)] = the count of elements in the 0-based i-th dimension of the record.
        /// 
        /// </remarks>
        private static (int Index, int Count, int[] Dimensions) ReadDimensions([NotNull] BinaryReader reader)
        {
            byte[] dimensionArray = InitializeArray(reader);

            int index = BitConverter.ToInt32(dimensionArray, 0 * sizeof(int));

            int[] dimensions = new int[BitConverter.ToInt32(dimensionArray, 1 * sizeof(int))];

            int count = 1;
            for (int i = 0; i < dimensions.Length; i++)
            {
                dimensions[i] = BitConverter.ToInt32(dimensionArray, (2 + i) * sizeof(int));
                count *= dimensions[i];
            }

            return (index, count, dimensions);
        }

        /// <summary>
        /// Calculates the extents of each dimension represented on the next vector.
        /// </summary>
        /// <param name="reader">
        /// The reader from which the extent definitions are read.
        /// </param>
        /// <returns>
        /// An integer array indicating the extents of each dimension represented on the next vector.
        /// </returns>
        /// <remarks>
        /// The extents array contains the following:
        ///
        ///     [0 * sizeof(int)] = 1-based index of the current vector from the end of the record.
        ///     [(1 + i) * sizeof(int)] = the 0-based index indicating the first entry in the i-th set represented in this vector.
        ///     [(2 + i) * sizeof(int)] = the 0-based index indicating the last entry in the i-th set represented in this vector.
        /// 
        /// The count of dimensions can be determined in-line as the length of the extents array divided by the byte-size of
        /// an integer. The array length is actually the count of the dimensions + 1, but integer division rounds down.
        /// 
        /// Note that the extents must be deincremented by one to move from a 1-based index to a 0-based index. 
        /// 
        /// </remarks>
        private static (int Index, int Offset, int[] Extents) ReadExtents([NotNull] BinaryReader reader)
        {
            byte[] extentsArray = InitializeArray(reader);

            int[] extents = new int[(extentsArray.Length - sizeof(int)) / 2 / sizeof(int)];

            int index = BitConverter.ToInt32(extentsArray, 0 * sizeof(int));

            int offset = 1;
            for (int i = 0; i < extents.Length; i++)
            {
                int start = BitConverter.ToInt32(extentsArray, (1 + i) * sizeof(int)) - 1;
                extents[i] = BitConverter.ToInt32(extentsArray, (2 + i) * sizeof(int)) - start;
                offset *= start;
            }

            return (index, offset, extents);
        }

        #endregion Common helper methods
    }
}