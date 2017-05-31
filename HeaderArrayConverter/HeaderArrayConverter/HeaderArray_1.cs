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
    public class HeaderArray<T> : HeaderArray
    {
        /// <summary>
        /// The decoded form of <see cref="Array"/>
        /// </summary>
        public ImmutableArray<T> Records { get; }

        /// <summary>
        /// An enumerable collection of the records in the array with set labels.
        /// </summary>
        public IEnumerable<(string Label, T Record)> SetRecords => Records.Zip(SetRecordLabels, (x, y) => (y, x));

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
        public HeaderArray([NotNull] string header, [CanBeNull] string description, [NotNull] string type, int[] dimensions, [NotNull] T[] records, [NotNull] IEnumerable<HeaderArraySet<string>> sets)
            : base(header, description, type, dimensions, sets)
        {
            Records = records.ToImmutableArray();
        }

        /// <summary>
        /// Returns a string representation of the contents of this <see cref="HeaderArray"/>.
        /// </summary>
        public override string ToString()
        {
            int length = SetRecordLabels.DefaultIfEmpty().Max(x => x?.Length ?? 0);

            if (typeof(T) == typeof(string))
            {
                return
                    Records.Aggregate(
                        new StringBuilder(base.ToString()),
                        (current, next) =>
                            current.AppendLine($"[{next}]"),
                        x =>
                            x.ToString());
            }

            return
                SetRecords.Aggregate(
                    new StringBuilder(base.ToString()),
                    (current, next) =>
                        current.AppendLine($"[{next.Label.PadRight(length)}]: {next.Record}"),
                    x =>
                        x.ToString());
        }
    }
}