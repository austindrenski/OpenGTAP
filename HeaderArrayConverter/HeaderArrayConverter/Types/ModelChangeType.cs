using JetBrains.Annotations;

namespace HeaderArrayConverter.Types
{
    /// <summary>
    /// Represents how to interpret the variable changes in the model.
    /// </summary>
    [PublicAPI]
    public enum ModelChangeType
    {
        /// <summary>
        /// Level changes.
        /// </summary>
        Change = 'c',

        /// <summary>
        /// Percent changes.
        /// </summary>
        PercentChange = 'p'
    }
}