using System;
using System.Collections.Generic;
using System.Text;

namespace MaxLib.Data.Config
{
    /// <summary>
    /// The base class that can contain configurable values
    /// </summary>
    [Config("Base Config")]
    public abstract class ConfigBase
    {
        /// <summary>
        /// returns all configurable values of this class
        /// </summary>
        /// <returns></returns>
        public abstract IEnumerable<IConfigValueBase> GetConfigs();

        /// <summary>
        /// Create a new base class that can contains configurable values. Every sub class of this has to 
        /// implement at least this parameterless constructor.
        /// </summary>
        public ConfigBase()
        {

        }

        /// <summary>
        /// Bind the <paramref name="updater"/> function to <see cref="ConfigValueBase{T}.OnValueChanged"/>.
        /// The <paramref name="updater"/> function will only be called if a valid value was provided.
        /// </summary>
        /// <typeparam name="T">The type of the value</typeparam>
        /// <param name="configValue">the configurable value</param>
        /// <param name="updater">the updater function to update this class</param>
        /// <returns>the same instance as provided to <paramref name="configValue"/></returns>
        protected ConfigValueBase<T> BindValueChanged<T>(ConfigValueBase<T> configValue, Action<T> updater)
        {
            if (configValue == null) throw new ArgumentNullException(nameof(configValue));
            if (updater == null) throw new ArgumentNullException(nameof(updater));
            configValue.OnValueChanged += (s, e) =>
            {
                if (configValue.Validate())
                    updater(configValue.Value);
            };
            return configValue;
        }
    }
}
