using System;
using System.Collections.Generic;
using System.Text;
using MaxLib.Data.IniFiles;

namespace MaxLib.Data.Config
{
    /// <summary>
    /// The base interface of a configurable value of the <see cref="ConfigBase"/>.
    /// </summary>
    public interface IConfigValueBase
    {
        /// <summary>
        /// The Name of the configurable value
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The Description of the configurable value
        /// </summary>
        string Description { get; }

        /// <summary>
        /// The category of the configurable value
        /// </summary>
        string Category { get; }

        /// <summary>
        /// The value of this configurable value
        /// </summary>
        object Value { get; set; }
    }

    /// <summary>
    /// A configurable value of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">the type of the configurable value</typeparam>
    public class ConfigValueBase<T> : IConfigValueBase
    {
        /// <summary>
        /// The Name of the configurable value
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The Description of the configurable value
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// The category of the configurable value
        /// </summary>
        public string Category { get; private set; }

        object IConfigValueBase.Value
        {
            get => Value;
            set => Value = (T)value;
        }

        private T value;
        /// <summary>
        /// The value of this configurable value
        /// </summary>
        public T Value
        {
            get => value;
            set
            {
                var changed = !Equals(value, this.value);
                this.value = value;
                if (changed)
                    DoValueChanged();
            }
        }

        /// <summary>
        /// Save the value property to an ini source (see <see cref="OptionsLoader"/>).
        /// </summary>
        /// <param name="option">the target key that should contains the value</param>
        public virtual void SaveValue(OptionsKey option)
        {
            if (option == null)
                throw new ArgumentNullException(nameof(option));
        }

        /// <summary>
        /// Load the value property from an ini source (see <see cref="OptionsLoader"/>).
        /// </summary>
        /// <param name="option">the source key that contains the value</param>
        public virtual void LoadValue(OptionsKey option)
        {
            if (option == null)
                throw new ArgumentNullException(nameof(option));
        }

        /// <summary>
        /// Create a new configurable value container for a value of type <typeparamref name="T"/>.
        /// </summary>
        /// <param name="category">The target category</param>
        /// <param name="name">the name of the value</param>
        /// <param name="description">the description of the value</param>
        /// <param name="value">an initial value</param>
        public ConfigValueBase(string category, string name, string description, T value)
        {
            Category = category ?? throw new ArgumentNullException(nameof(category));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            Value = value;
        }

        /// <summary>
        /// This event fires when the buffered value was changed.
        /// </summary>
        public event EventHandler OnValueChanged;

        /// <summary>
        /// Call the event <see cref="OnValueChanged"/>
        /// </summary>
        protected void DoValueChanged()
        {
            OnValueChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Validates the buffered value against the current validation rules.
        /// </summary>
        /// <returns>true if the buffered value is valid</returns>
        public bool Validate()
        {
            return Validate(Value);
        }

        /// <summary>
        /// Validate the given value against the current validation rules.
        /// </summary>
        /// <param name="value">the value to check</param>
        /// <returns>true if the buffered value is valid</returns>
        public virtual bool Validate(T value)
        {
            return true;
        }

        /// <summary>
        /// Get a string representation of this value for better debugging.
        /// </summary>
        /// <returns>the string representation of this value</returns>
        public override string ToString()
            => $"[{Category}] {Name}={Value}";

    }
}
