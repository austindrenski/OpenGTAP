using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using JetBrains.Annotations;

namespace HeaderArrayConverter
{
    /// <summary>
    /// Represents a single entry from a Header Array (HAR) file.
    /// </summary>
    /// <typeparam name="T">
    /// The type of data in the array.
    /// </typeparam>
    [PublicAPI]
    public class HeaderArray<T> : IHeaderArray<T>
    {
        /// <summary>
        /// The four character identifier for this <see cref="HeaderArray{T}"/>.
        /// </summary>
        [NotNull]
        public string Header { get; }

        /// <summary>
        /// The long name description of the <see cref="HeaderArray{T}"/>.
        /// </summary>
        [CanBeNull]
        public string Description { get; }

        /// <summary>
        /// The type of element stored in the array.
        /// </summary>
        [NotNull]
        public string Type { get; }

        /// <summary>
        /// The dimensions of the array.
        /// </summary>
        [NotNull]
        public IImmutableList<int> Dimensions { get; }

        /// <summary>
        /// The sets defined on the array.
        /// </summary>
        [NotNull]
        public IImmutableList<ImmutableOrderedSet<string>> Sets { get; }

        /// <summary>
        /// An immutable dictionary of the records with set labels.
        /// </summary>
        [NotNull]
        private ImmutableOrderedDictionary<string, T> Records { get; }

        /// <summary>
        /// Returns the value with the key defined by the key components or throws an exception if the key is not found.
        /// </summary>
        /// <param name="key">
        /// The components that define the key whose value is returned.
        /// </param>
        /// <returns>
        /// The value stored by the given key.
        /// </returns>
        public KeyValueSequence<string, T> this[KeySequence<string> key] => new KeyValueSequence<string, T>(key, Records[key]);

        KeyValueSequence IIndexerProvider.this[KeySequence<object> key] => this[new KeySequence<string>(key.Cast<string>())];

        /// <summary>
        /// Represents one entry from a Header Array (HAR) file.
        /// </summary>
        /// <param name="header">
        /// The four character identifier for this <see cref="HeaderArray"/>.
        /// </param>
        /// <param name="description">
        /// The long name description of the <see cref="HeaderArray"/>.
        /// </param>
        /// <param name="type">
        /// The type of element stored in the array.
        /// </param>
        /// <param name="dimensions">
        /// The dimensions of the array.
        /// </param>
        /// <param name="records">
        /// The data in the array.
        /// </param>
        /// <param name="sets">
        /// The sets defined on the array.
        /// </param>
        public HeaderArray([NotNull] string header, [CanBeNull] string description, [NotNull] string type, int[] dimensions, [NotNull] IEnumerable<T> records, [NotNull] IEnumerable<ImmutableOrderedSet<string>> sets)
        {
            if (records is null)
            {
                throw new ArgumentNullException(nameof(records));
            }
            if (header is null)
            {
                throw new ArgumentNullException(nameof(header));
            }
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            if (dimensions is null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            if (sets is null)
            {
                throw new ArgumentNullException(nameof(sets));
            }

            Header = header;
            Description = description?.Trim('\u0000', '\u0002', '\u0020');
            Dimensions = dimensions.ToImmutableArray();
            Sets = sets.ToImmutableArray();
            Type = type;

            Records =
                Sets.AsExpandedSet()
                    .FullOuterZip(
                        records,
                        x => x.ToString())
                    .ToImmutableOrderedDictionary(
                        x => x.Left.Split('*') as IEnumerable<string>,
                        x => x.Right);
        }

        /// <summary>
        /// Returns a string representation of the contents of this <see cref="HeaderArray"/>.
        /// </summary>
        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.AppendLine($"{nameof(Header)}: {Header}");
            stringBuilder.AppendLine($"{nameof(Description)}: {Description}");
            stringBuilder.AppendLine($"{nameof(Type)}: {Type}");
            stringBuilder.AppendLine($"{nameof(Sets)}: {string.Join(" * ", Sets.Select(x => $"{{ {string.Join(", ", x)} }}"))}");
            //stringBuilder.AppendLine($"{nameof(Dimensions)}: {Dimensions.Aggregate(string.Empty, (current, next) => $"{current}[{next}]")}");
            stringBuilder.AppendLine($"{nameof(Dimensions)}: {Sets.Select(x => x.Count).Aggregate(string.Empty, (current, next) => $"{current}[{next}]")}");

            int length = Records.Keys.Max(x => x.ToString().Length);

            return
                Records.Aggregate(
                    stringBuilder,
                    (current, next) =>
                        current.AppendLine($"{next.Key.ToString().PadRight(length)}: {next.Value}"),
                    x =>
                        x.ToString());
        }
    }
}