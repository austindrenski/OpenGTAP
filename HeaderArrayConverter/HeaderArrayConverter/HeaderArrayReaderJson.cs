using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AD.IO;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HeaderArrayConverter
{
    /// <summary>
    /// Reads Header Array (HARX) files in JSON format.
    /// </summary>
    [PublicAPI]
    public class HeaderArrayReaderJson : HeaderArrayReader
    {
        /// <summary>
        /// Static instance of a <see cref="HeaderArrayJsonConverter"/> to parse JSON objects into <see cref="IHeaderArray{TValue}"/>.
        /// </summary>
        private static readonly HeaderArrayJsonConverter HeaderArrayConverter = new HeaderArrayJsonConverter();

        /// <summary>
        /// Reads the contents of a HARX file.
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

            return new HeaderArrayFile(ReadHarxArrays(file));
        }

        /// <summary>
        /// Enumerates the arrays from the HARX file.
        /// </summary>
        /// <param name="file">
        /// The HARX file from which to read arrays.
        /// </param>
        /// <returns>
        /// An enumerable collection of the arrays in the file.
        /// </returns>
        [NotNull]
        [ItemNotNull]
        public static IEnumerable<IHeaderArray> ReadHarxArrays(FilePath file)
        {
            foreach (Task<IHeaderArray> array in ReadHarxArraysAsync(file))
            {
                yield return array.Result;
            }
        }

        /// <summary>
        /// Asynchronously enumerates the arrays from the HAR file.
        /// </summary>
        /// <param name="file">
        /// The HAR file from which to read arrays.
        /// </param>
        /// <returns>
        /// An enumerable collection of tasks that when completed return the arrays in the file.
        /// </returns>
        [NotNull]
        [ItemNotNull]
        public static IEnumerable<Task<IHeaderArray>> ReadHarxArraysAsync(FilePath file)
        {
            using (ZipArchive archive = new ZipArchive(File.Open(file, FileMode.Open, FileAccess.Read)))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    yield return ReadHarxArrayAsync(entry);
                }
            }
        }

        /// <summary>
        /// Reads one entry from a HARX file.
        /// </summary>
        [NotNull]
        [ItemNotNull]
        private static async Task<IHeaderArray> ReadHarxArrayAsync(ZipArchiveEntry entry)
        {
            string json = new StreamReader(entry.Open()).ReadToEnd();

            return await Task.FromResult(JsonConvert.DeserializeObject<IHeaderArray>(json, HeaderArrayConverter));
        }

        /// <summary>
        /// Custom converter from JSON <see cref="IHeaderArray{TValue}"/>.
        /// </summary>
        private sealed class HeaderArrayJsonConverter : JsonConverter
        {
            /// <summary>
            /// True if the type implements <see cref="IHeaderArray"/>; otherwise false.
            /// </summary>
            /// <param name="objectType">
            /// The type to compare.
            /// </param>
            public override bool CanConvert(Type objectType)
            {
                return typeof(IHeaderArray).GetTypeInfo().IsAssignableFrom(objectType);
            }

            /// <summary>
            /// Reads the JSON representation of the object.
            /// </summary>
            /// <param name="reader">
            /// The <see cref="JsonReader"/> to read from.
            /// </param>
            /// <param name="objectType">
            /// Type of the object.
            /// </param>
            /// <param name="existingValue">
            /// The existing value of object being read.
            /// </param>
            /// <param name="serializer">
            /// The calling serializer.
            /// </param>
            /// <returns>
            /// The object value.
            /// </returns>
            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                JObject jObject = JObject.Load(reader);

                return
                    jObject["Type"].Value<string>() == "1C"
                        ? Create<string>(jObject)
                        : Create<float>(jObject);
            }

            private static IHeaderArray Create<T>(JObject jObject)
            {
                return
                    new HeaderArray<T>(
                        jObject["Header"].Value<string>(),
                        jObject["Description"].Value<string>(),
                        jObject["Type"].Value<string>(),
                        jObject["Dimensions"].Values<int>(),
                        ParseEntries<T>(jObject["Entries"]),
                        ParseSets(jObject["Sets"]));
            }

            private static IEnumerable<KeyValuePair<KeySequence<string>, T>> ParseEntries<T>(JToken entries)
            {
                return
                    entries.Values<JToken>()
                           .Select(
                               x => new
                               {
                                   Key = KeySequence<string>.Parse(((JProperty)x).Name),
                                   Value = x.Single().Value<T>()
                               })
                           .Select(
                               x =>
                                   new KeyValuePair<KeySequence<string>, T>(x.Key, x.Value));
            }

            private static IEnumerable<IEnumerable<string>> ParseSets(JToken sets)
            {
                return sets.Values<JToken>().Select(x => x.Values<string>());
            }

            /// <summary>
            /// Writes the JSON representation of the object.
            /// </summary>
            /// <param name="writer">
            /// The <see cref="JsonWriter"/> to write to.
            /// </param>
            /// <param name="value">
            /// The value.
            /// </param>
            /// <param name="serializer">
            /// The calling serializer.
            /// </param>
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                throw new NotSupportedException();
            }
        }
    }
}