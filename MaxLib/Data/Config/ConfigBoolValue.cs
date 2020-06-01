using MaxLib.Data.IniFiles;

namespace MaxLib.Data.Config
{
    /// <summary>
    /// A configurable value of type bool.
    /// </summary>
    public class ConfigBoolValue : ConfigValueBase<bool>
    {
        /// <summary>
        /// Create a new configurable value container for a value of type bool.
        /// </summary>
        /// <param name="category">The target category</param>
        /// <param name="name">the name of the value</param>
        /// <param name="description">the description of the value</param>
        /// <param name="value">the inital value</param>
        public ConfigBoolValue(string category, string name, string description, bool value) 
            : base(category, name, description, value)
        {
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
            Value = option.GetBool();
        }
    }
}
