using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace MaxLib.Data.Config
{
    /// <summary>
    /// A tool to find and manage <see cref="ConfigBase"/> using reflection. This configs
    /// are automaticly organised using the <see cref="ConfigAttribute"/>.
    /// </summary>
    public sealed class ConfigFinder
    {
        private class Node
        {
            public Dictionary<string, Node> Nodes { get; private set; }

            public Dictionary<string, Type> Configs { get; private set; }

            public Node()
            {
                Nodes = new Dictionary<string, Node>();
                Configs = new Dictionary<string, Type>();
            }
        }

        private Node root = new Node();

        private void AddConfig(string[] categories, string name, Type config)
        {
            var node = root;
            for (int i = 0; i<categories.Length; ++i)
            {
                if (node.Nodes.TryGetValue(categories[i], out Node next))
                    node = next;
                else node.Nodes.Add(categories[i], node = new Node());
            }
            node.Configs[name] = config;
        }

        private bool CreateableConfig(Type config)
        {
            return config.GetConstructor(Type.EmptyTypes) != null && !config.IsAbstract;
        }

        private ConfigAttribute ReadAttribute(Type config)
        {
            while (config != null)
            {
                var a = config.GetCustomAttributes(typeof(ConfigAttribute), false);
                if (a.Length != 0)
                    return (ConfigAttribute)a[0];
                else config = config.BaseType;
            }
            return null;
        }

        private bool AddType(Type configType)
        {
            if (configType == null)
                throw new ArgumentNullException(nameof(configType));
            var a = ReadAttribute(configType);
            if (a != null && CreateableConfig(configType))
            {
                AddConfig(a.Category, a.Name, configType);
                return true;
            }
            else return false;
        }

        /// <summary>
        /// Add a <see cref="ConfigBase"/> to the collection and use the <see cref="ConfigAttribute"/> to
        /// retrieve the path to add
        /// </summary>
        /// <typeparam name="T">the type of the config to add</typeparam>
        /// <returns>true if it could successfully added</returns>
        public bool AddType<T>() where T : ConfigBase, new()
        {
            return AddType(typeof(T));
        }

        /// <summary>
        /// Add a <see cref="Configbase"/> to the collection with the specied path
        /// </summary>
        /// <typeparam name="T">the type of the config to add</typeparam>
        /// <param name="name">the name of the config</param>
        /// <param name="categories">the names of the category path</param>
        /// <returns>true if it could successfully added</returns>
        public bool AddType<T>(string name, params string[] categories) where T : ConfigBase, new()
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (categories == null)
                throw new ArgumentNullException(nameof(categories));
            if (CreateableConfig(typeof(T)))
            {
                AddConfig(categories, name, typeof(T));
                return true;
            }
            else return false;
        }

        /// <summary>
        /// Add all <see cref="ConfigBase"/> from the speciefied assembly
        /// </summary>
        /// <param name="assembly">the type source</param>
        public void AddFromAsembly(Assembly assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));
            foreach (var type in assembly.GetExportedTypes())
            {
                if (type.IsSubclassOf(typeof(ConfigBase)))
                    AddType(type);
            }
        }

        /// <summary>
        /// Get all sub nodes from the specified path
        /// </summary>
        /// <param name="categories">the category path</param>
        /// <returns>the list of the sub nodes</returns>
        public IEnumerable<string> GetSubNodes(params string[] categories)
        {
            if (categories == null)
                throw new ArgumentNullException(nameof(categories));
            var node = root;
            for (int i = 0; node != null && i < categories.Length; ++i)
                if (node.Nodes.TryGetValue(categories[i], out Node result))
                    node = result;
                else node = null;
            return node?.Nodes.Keys;
        }

        /// <summary>
        /// Check if the node exists in the current collection
        /// </summary>
        /// <param name="categories">the category path</param>
        /// <returns>true if path exists in the collection</returns>
        public bool HasNode(params string[] categories)
        {
            if (categories == null)
                throw new ArgumentNullException(nameof(categories));
            var node = root;
            for (int i = 0; i < categories.Length; ++i)
                if (node.Nodes.TryGetValue(categories[i], out Node result))
                    node = result;
                else return false;
            return true;
        }

        /// <summary>
        /// Get the list of config names that exists at this node.
        /// </summary>
        /// <param name="categories">the category path</param>
        /// <returns>the list of all config names</returns>
        public IEnumerable<string> GetConfigs(params string[] categories)
        {
            if (categories == null)
                throw new ArgumentNullException(nameof(categories));
            var node = root;
            for (int i = 0; node != null && i < categories.Length; ++i)
                if (node.Nodes.TryGetValue(categories[i], out Node result))
                    node = result;
                else node = null;
            return node?.Configs.Keys;
        }

        /// <summary>
        /// Check if the specified config exists in the collection.
        /// </summary>
        /// <param name="name">the name of the config</param>
        /// <param name="categories">the category path</param>
        /// <returns>true if the config was found</returns>
        public bool HasConfig(string name, params string[] categories)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (categories == null)
                throw new ArgumentNullException(nameof(categories));
            var node = root;
            for (int i = 0; i < categories.Length; ++i)
                if (node.Nodes.TryGetValue(categories[i], out Node result))
                    node = result;
                else return false;
            return node.Configs.ContainsKey(name);
        }

        /// <summary>
        /// Try to create an instance of the references <see cref="ConfigBase"/>. If the config 
        /// could not be found in the current collection null will be returned.
        /// </summary>
        /// <param name="name">the name of the config</param>
        /// <param name="categories">the category path</param>
        /// <returns>the new instance of the config or null if not found</returns>
        public ConfigBase CreateConfig(string name, params string[] categories)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (categories == null)
                throw new ArgumentNullException(nameof(categories));
            var node = root;
            for (int i = 0; i < categories.Length; ++i)
                if (node.Nodes.TryGetValue(categories[i], out Node result))
                    node = result;
                else return null;
            if (node.Configs.TryGetValue(name, out Type type))
            {
                return (ConfigBase)Activator.CreateInstance(type);
            }
            else return null;
        }

        /// <summary>
        /// Remove a config from the collection
        /// </summary>
        /// <param name="name">the name of the config</param>
        /// <param name="categories">the category path</param>
        /// <returns>true if this member could be removed</returns>
        public bool RemoveConfig(string name, params string[] categories)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (categories == null)
                throw new ArgumentNullException(nameof(categories));
            var node = root;
            for (int i = 0; i < categories.Length; ++i)
                if (node.Nodes.TryGetValue(categories[i], out Node result))
                    node = result;
                else return false;
            return node.Configs.Remove(name);
        }

        /// <summary>
        /// Remove a node and all sub nodes and types from the collection
        /// </summary>
        /// <param name="categories">the category path to remove. If empty the whole collection will be cleared.</param>
        /// <returns>true if this member could be removed.</returns>
        public bool RemoveNode(params string[] categories)
        {
            if (categories == null)
                throw new ArgumentNullException(nameof(categories));
            var node = root;
            for (int i = 0; i < categories.Length - 1; ++i)
                if (node.Nodes.TryGetValue(categories[i], out Node result))
                    node = result;
                else return false;
            if (categories.Length > 0)
            {
                return node.Nodes.Remove(categories[categories.Length - 1]);
            }
            else
            {
                root = new Node();
                return true;
            }
        }
    }
}
