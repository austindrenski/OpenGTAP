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
        /// An immutable dictionary of the records with set labels.
        /// </summary>
        [NotNull]
        public IImmutableDictionary<string, T> Records { get; }

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
            if (records is null)
            {
                throw new ArgumentNullException(nameof(records));
            }

            Records =
                SetRecordLabels.FullOuterZip(
                                   records,
                                   x => x.ToString())
                               .ToImmutableOrderedDictionary(
                                   x => x.Left,
                                   x => x.Right);
        }

        /// <summary>
        /// Returns a string representation of the contents of this <see cref="HeaderArray"/>.
        /// </summary>
        public override string ToString()
        {
            int length = SetRecordLabels.DefaultIfEmpty().Max(x => x?.Length ?? 0);

            //if (typeof(T) == typeof(string))
            //{
            //    return
            //        Records.Aggregate(
            //            new StringBuilder(base.ToString()),
            //            (current, next) =>
            //                current.AppendLine($"[{next}]"),
            //            x =>
            //                x.ToString());
            //}

            //foreach (IImmutableSet<string> item in ((ImmutableOrderedDictionary<string, T>)Records).Sets)
            //{
            //    Console.WriteLine("---Printing sets---");
            //    Console.WriteLine(string.Join(Environment.NewLine, item));
            //    Console.WriteLine("-------------------");
            //}

            return
                Records.Aggregate(
                    new StringBuilder(base.ToString()),
                    (current, next) =>
                        current.AppendLine($"[{next.Key.PadRight(length)}]: {next.Value}"),
                    x =>
                        x.ToString());
        }
    }
}