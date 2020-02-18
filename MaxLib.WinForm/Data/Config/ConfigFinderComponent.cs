using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MaxLib.Data.Config
{
    /// <summary>
    /// A component to utilize the <see cref="ConfigFinder"/> to select configs. This component use
    /// a <see cref="ToolStripMenuItem"/> as a target.
    /// </summary>
    public partial class ConfigFinderComponent : Component
    {
        /// <summary>
        /// Creates component to utilize the <see cref="ConfigFinder"/> to select configs. This component use
        /// a <see cref="ToolStripMenuItem"/> as a target.
        /// </summary>
        public ConfigFinderComponent()
        {
            InitializeComponent();
            Disposed += (s, e) => rootItem?.Dispose();
        }

        /// <summary>
        /// Creates component to utilize the <see cref="ConfigFinder"/> to select configs. This component use
        /// a <see cref="ToolStripMenuItem"/> as a target.
        /// </summary>
        /// <param name="container">The target container to add this component</param>
        public ConfigFinderComponent(IContainer container)
        {
            container.Add(this);

            InitializeComponent();
            Disposed += (s, e) => rootItem?.Dispose();
        }

        /// <summary>
        /// The <see cref="ConfigFinder"/> that manages all the config types.
        /// </summary>
        public ConfigFinder ConfigFinder { get; } = new ConfigFinder();

        /// <summary>
        /// The text format for the <see cref="ToolStripItem.Text"/> to show a category name.
        /// The "{0}" defines the placeholder for the name itself.
        /// </summary>
        public string CategoryTextFormat { get; set; } = "{0}";

        /// <summary>
        /// The text format for the <see cref="ToolStripItem.Text"/> to show a config name.
        /// The "{0}" defines the placeholder for the name itself.
        /// </summary>
        public string AddConfigTextFormat { get; set; } = "add {0}";

        /// <summary>
        /// This event will be raised if the user has a <see cref="ConfigBase"/> selected.
        /// </summary>
        public event EventHandler<ConfigBase> ConfigSelected;

        private ToolStripMenuItem rootItem = null;
        /// <summary>
        /// The root <see cref="ToolStripMenuItem"/> that will host the complete set of 
        /// entrys from the <see cref="ConfigFinder"/>.
        /// </summary>
        public ToolStripMenuItem RootItem
        {
            get => rootItem;
            set
            {
                rootItem.DropDownOpening -= RootItem_DropDownOpening;
                rootItem.DropDownClosed -= RootItem_DropDownClosed;
                rootItem = value;
                rootItem.DropDownOpening += RootItem_DropDownOpening;
                rootItem.DropDownClosed += RootItem_DropDownClosed;
                rootItem.DropDownItems.Clear();
                rootItem.DropDownItems.Add("... load data ...");
            }
        }

        private void RootItem_DropDownClosed(object sender, EventArgs e)
        {
            rootItem.DropDownItems.Clear();
            rootItem.DropDownItems.Add("... load data ...");
        }

        private void RootItem_DropDownOpening(object sender, EventArgs e)
        {
            BuildNodes(rootItem, new string[0]);
        }

        private void BuildNodes(ToolStripMenuItem item, string[] categories)
        {
            item.DropDownItems.Clear();
            var nodes = ConfigFinder.GetSubNodes(categories);
            var configs = ConfigFinder.GetConfigs(categories);
            foreach (var name in nodes)
            {
                var node = new ToolStripMenuItem(string.Format(CategoryTextFormat, name));
                node.DropDownOpening += (s, e) =>
                {
                    var sender = (ToolStripMenuItem)s;
                    if (sender.Tag == null) return;
                    BuildNodes(sender, (string[])sender.Tag);
                };
                node.Tag = Enumerable.Append(categories, name).ToArray();
                node.DropDownItems.Add("... load data ...");
                item.DropDownItems.Add(node);
            }
            if (nodes.Count() > 0 && configs.Count() > 0)
                item.DropDownItems.Add(new ToolStripSeparator());
            foreach (var name in configs)
            {
                var node = new ToolStripMenuItem(string.Format(AddConfigTextFormat, name));
                node.Tag = new Tuple<string[], string>(categories, name);
                node.Click += (s, e) =>
                {
                    var sender = (ToolStripMenuItem)s;
                    var tag = (Tuple<string[], string>)sender.Tag;
                    var config = ConfigFinder.CreateConfig(tag.Item2, tag.Item1);
                    if (config != null)
                        ConfigSelected?.Invoke(this, config);
                };
                item.DropDownItems.Add(node);
            }
        }
    }
}
