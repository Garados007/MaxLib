using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace MaxLib.Data.Config
{
    /// <summary>
    /// Marks the class as an config container. This attribute should only applied to descendens of
    /// <see cref="ConfigBase"/>.
    /// 
    /// Classes marked with this attribute are used by <see cref="ConfigFinder"/>. The instanced should be created
    /// with a parameterless constructor (new()).
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class ConfigAttribute : Attribute
    {
        /// <summary>
        /// Marks the class as an config container. This attribute should only applied to descendens of
        /// <see cref="ConfigBase"/>.
        /// </summary>
        /// <param name="names">the categories and the name of the config container</param>
        public ConfigAttribute(params string[] names)
        {
            if (names == null) throw new ArgumentNullException(nameof(names));
            if (names.Length == 0) throw new ArgumentException("no name given", nameof(names));
            Category = names.Take(names.Length - 1).ToArray();
        }

        /// <summary>
        /// The category path of this config container
        /// </summary>
        public string[] Category { get; private set; }

        /// <summary>
        /// The name of this config container
        /// </summary>
        public string Name { get; private set; }
    }
}
