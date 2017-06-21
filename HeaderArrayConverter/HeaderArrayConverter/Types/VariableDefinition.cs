using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace HeaderArrayConverter.Types
{
    /// <summary>
    /// Defines a <see cref="VariableDefinition"/>.
    /// </summary>
    [PublicAPI]
    [JsonObject(MemberSerialization.OptIn)]
    public class VariableDefinition
    {
        /// <summary>
        /// Gets the name of the variable.
        /// </summary>
        [NotNull]
        [JsonProperty]
        public string Name { get; }

        /// <summary>
        /// True if the variable is fully exogenous; otherwise false.
        /// </summary>
        [JsonProperty]
        public bool IsExogenous { get; }

        /// <summary>
        /// Gets the indexes for this definition.
        /// </summary>
        [NotNull]
        [JsonProperty]
        public IImmutableList<string> Indexes { get; }

        /// <summary>
        /// True if this definition includes indexes; otherwise false.
        /// </summary>
        public bool HasIndexes => Indexes.Any();

        /// <summary>
        /// Constructs a <see cref="VariableDefinition"/>.
        /// </summary>
        /// <param name="name">
        /// The name of the variable.
        /// </param>
        /// <param name="isExogenous">
        /// True if the variable is fully exogenous; otherwise false.
        /// </param>
        public VariableDefinition([NotNull] string name, bool isExogenous) : this(name, isExogenous, Enumerable.Empty<string>())
        {
        }

        /// <summary>
        /// Constructs a <see cref="VariableDefinition"/>.
        /// </summary>
        /// <param name="name">
        /// The name of the variable.
        /// </param>
        /// <param name="isExogenous">
        /// True if the variable is fully exogenous; otherwise false.
        /// </param>
        /// <param name="indexes">
        /// The indexes for this definition.
        /// </param>
        public VariableDefinition([NotNull] string name, bool isExogenous, [NotNull] IEnumerable<string> indexes)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (indexes is null)
            {
                throw new ArgumentNullException(nameof(indexes));
            }

            Name = name;
            IsExogenous = isExogenous;
            Indexes = indexes as IImmutableList<string> ?? indexes.ToImmutableArray();
        }

        /// <summary>
        /// Returns a JSON string representation of the object.
        /// </summary>
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
}