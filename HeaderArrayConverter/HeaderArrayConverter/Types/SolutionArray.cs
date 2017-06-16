using System;
using System.Collections.Immutable;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace HeaderArrayConverter.Types
{
    /// <summary>
    /// Represents the base properties of a variable in a <see cref="SolutionFile"/>.
    /// </summary>
    /// <remarks>
    /// Encapsulates values from the headers in a Gempack solution file (SL4) that include the full range of model variables.
    /// </remarks>
    [PublicAPI]
    [JsonObject]
    public class SolutionArray
    {
        /// <summary>
        /// Gets the name of the variable.
        /// </summary>
        /// <remarks>
        /// This value is derived from the index position on header 'VCNM' and defines the value in variable 'VCNAM' at index 'NUMVC'.
        /// </remarks>
        public string Name { get; }

        /// <summary>
        /// Gets the variable description.
        /// </summary>
        /// <remarks>
        /// This value is derived from the index position on header 'VCL0' and defines the value on header 'VCLB' at index 'NUMVC'.
        /// </remarks>
        public string Description { get; }

        /// <summary>
        /// Gets the index number of this variable among all variables. Equivalent to the Gempack parameter 'NUMVC'.
        /// </summary>
        /// <remarks>
        /// This value is derived from the index order on header 'VCNM' and defines the Gempack parameter 'NUMVC'.
        /// </remarks>
        public int VariableIndex { get; }

        /// <summary>
        /// Gets the <see cref="ModelChangeType"/> for this object.
        /// </summary>
        /// <remarks>
        /// This value is derived from the index order on header 'VCT0' and defines the value on header 'VCTP' at index 'NUMVC'.
        /// This is also the value on header 'BVCTP' at index 'NUMBVC'.
        /// </remarks>
        public ModelChangeType ChangeType { get; }

        /// <summary>
        /// Gets the <see cref="ModelVariableType"/> for this object.
        /// </summary>
        /// <remarks>
        /// This value is derived from the index order on header 'VCS0' and defines the value in variable 'VCSTAT' at index 'NUMVC'.
        /// </remarks>
        public ModelVariableType VariableType { get; }

        /// <summary>
        /// Gets the unit type of the variable (e.g. ln, lv = levels var, ol = ORIG_LEV).
        /// </summary>
        /// <remarks>
        /// This value is derived from the index order on header 'VCLE'.
        /// </remarks>
        public string UnitType { get; }

        /// <summary>
        /// Gets the number of sets defined on this array.
        /// </summary>
        /// <remarks>
        /// This value is derived from the index order on header 'VCNI' and defines the value in variable 'VCNIND' at index 'NUMVC'.
        /// </remarks>
        public int NumberOfSets { get; }

        /// <summary>
        /// The defining sets for this endogenous array.
        /// </summary>
        public IImmutableList<SetInformation> Sets { get; }

        /// <summary>
        /// True if the <see cref="VariableType"/> is <see cref="ModelVariableType.Condensed"/> or <see cref="ModelVariableType.Backsolved"/>.
        /// </summary>
        public bool IsEndogenous => VariableType == ModelVariableType.Condensed || VariableType == ModelVariableType.Backsolved;

        /// <summary>
        /// Constructs a <see cref="SolutionArray"/> containing the base properties of a variable in a <see cref="SolutionFile"/>.
        /// </summary>
        /// <param name="variableIndex">
        /// The index number of this variable among all variables. [VCNM, NUMVC].
        /// </param>
        /// <param name="numberOfSets">
        /// The number of sets defined on this array. [VCNI, VCNIND(NUMVC)].
        /// </param>
        /// <param name="name">
        /// The name of the variable. [VCNM, VCNAM(NUMVC)].
        /// </param>
        /// <param name="description">
        /// The variable description. [VCL0, VCLB(NUMVC)].
        /// </param>
        /// <param name="unitType">
        /// The unit type of the variable (e.g. ln, lv = levels var, ol = ORIG_LEV). [VCLE].
        /// </param>
        /// <param name="changeType">
        /// The <see cref="ModelChangeType"/> for this object. [VCT0, VCTP(NUMVC)].
        /// </param>
        /// <param name="variableType">
        /// The <see cref="ModelVariableType"/> for this object. [VCS0, VCSTAT(NUMVC)].
        /// </param>
        /// <param name="sets"></param>
        public SolutionArray(int variableIndex, int numberOfSets, string name, string description, string unitType, ModelChangeType changeType, ModelVariableType variableType, IImmutableList<SetInformation> sets)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (description is null)
            {
                throw new ArgumentNullException(nameof(description));
            }
            if (unitType is null)
            {
                throw new ArgumentNullException(nameof(unitType));
            }
            if (sets is null)
            {
                throw new ArgumentNullException(nameof(sets));
            }

            Name = name;
            Description = description;
            VariableIndex = variableIndex;
            ChangeType = changeType;
            VariableType = variableType;
            UnitType = unitType;
            NumberOfSets = numberOfSets;
            Sets = sets;
        }

        /// <summary>
        /// Constructs a <see cref="SolutionArray"/> containing the base properties of a variable in a <see cref="SolutionFile"/> from an existing <see cref="SolutionArray"/>.
        /// </summary>
        public SolutionArray(SolutionArray solutionDataObject)
            : this(solutionDataObject.VariableIndex,
                   solutionDataObject.NumberOfSets,
                   solutionDataObject.Name,
                   solutionDataObject.Description,
                   solutionDataObject.UnitType,
                   solutionDataObject.ChangeType,
                   solutionDataObject.VariableType,
                   solutionDataObject.Sets) { }

        /// <summary>
        /// Returns a JSON representation of the current object.
        /// </summary>
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
}