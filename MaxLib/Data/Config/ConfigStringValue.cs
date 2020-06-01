using MaxLib.Data.IniFiles;
using System;
using System.Text.RegularExpressions;

namespace MaxLib.Data.Config
{
    /// <summary>
    /// A configurable value of type string.
    /// </summary>
    public class ConfigStringValue : ConfigValueBase<string>
    {
        /// <summary>
        /// The Regex pattern to validate the value
        /// </summary>
        public string Pattern { get; private set; }

        /// <summary>
        /// Create a new configurable value container for a value of type string.
        /// </summary>
        /// <param name="category">The target category</param>
        /// <param name="name">the name of the value</param>
        /// <param name="description">the description of the value</param>
        /// <param name="value">an initial value</param>
        /// <param name="pattern">the regex pattern to validate</param>
        public ConfigStringValue(string category, string name, string description, string value, string pattern) 
            : base(category, name, description, value)
        {
            Pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
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
            Value = option.GetString();
        }

        /// <summary>
        /// Validate the given value against the <see cref="Pattern"/>.
        /// </summary>
        /// <param name="value">the value to check</param>
        /// <returns>true if the buffered value is valid</returns>
        public override bool Validate(string value)
        {
            var m = Regex.Match(value, Pattern, RegexOptions.Compiled, TimeSpan.FromSeconds(1));
            return m.Success;
        }
    }
}
