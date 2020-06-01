using MaxLib.Data.IniFiles;
using System;

namespace MaxLib.Data.Config
{
    /// <summary>
    /// A configurable value of type int.
    /// </summary>
    public class ConfigIntValue : ConfigValueBase<int>
    {
        /// <summary>
        /// The minimum value of <see cref="ConfigValueBase{T}.Value"/>
        /// </summary>
        public int Minimum { get; private set; }

        /// <summary>
        /// The maximum value of <see cref="ConfigValueBase{T}.Value"/>
        /// </summary>
        public int Maximum { get; private set; }

        /// <summary>
        /// Create a new configurable value container for a value of type int.
        /// </summary>
        /// <param name="category">The target category</param>
        /// <param name="name">the name of the value</param>
        /// <param name="description">the description of the value</param>
        /// <param name="value">an initial value</param>
        /// <param name="minimum">The minimum value</param>
        /// <param name="maximum">The maximum value</param>
        public ConfigIntValue(string category, string name, string description, int value, int minimum, int maximum) 
            : base(category, name, description, value)
        {
            if (minimum > maximum)
                throw new ArgumentOutOfRangeException(nameof(maximum), "minimum is larger then maximum");
            Minimum = minimum;
            Maximum = maximum;
        }

        /// <summary>
        /// Save the value property to an ini source (see <see cref="OptionsLoader"/>).
        /// </summary>
        /// <param name="option">the target key that should contains the value</param>
        public override void SaveValue(OptionsKey option)
        {
            base.SaveValue(option);
            option.SetValue(Value);
        }

        /// <summary>
        /// Load the value property from an ini source (see <see cref="OptionsLoader"/>).
        /// </summary>
        /// <param name="option">the source key that contains the value</param>
        public override void LoadValue(OptionsKey option)
        {
            base.LoadValue(option);
            Value = option.GetInt32();
        }

        /// <summary>
        /// Validate the given value against the <see cref="Minimum"/> and <see cref="Maximum"/>.
        /// </summary>
        /// <param name="value">the value to check</param>
        /// <returns>true if the buffered value is valid</returns>
        public override bool Validate(int value)
        {
            return value >= Minimum && value <= Maximum;
        }
    }
}
