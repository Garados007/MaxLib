using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MaxLib.Data.Config
{
    /// <summary>
    /// An Editor that allows the user to edit <see cref="ConfigBase"/>.
    /// </summary>
    public partial class ConfigEditor : Form
    {
        /// <summary>
        /// Create an editor that allows the user to edit <see cref="ConfigBase"/>.
        /// </summary>
        /// <param name="config">The <see cref="ConfigBase"/> that should be edited</param>
        /// <param name="settings">The <see cref="ConfigEditorSettings"/> that contains the configuration for this editor</param>
        public ConfigEditor(ConfigBase config, ConfigEditorSettings settings)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Settings = settings ?? throw new ArgumentNullException(nameof(config));
            InitializeComponent();
            using (var g = CreateGraphics())
                BuildLayout(g);
        }
        /// <summary>
        /// The <see cref="ConfigBase"/> that will be edited
        /// </summary>
        public ConfigBase Config { get; private set; }

        /// <summary>
        /// The <see cref="ConfigEditorSettings"/> that contains the configuration for this editor.
        /// </summary>
        public ConfigEditorSettings Settings { get; private set; }

        private void BuildLayout(Graphics graphics)
        {
            var settings = Config.GetConfigs()
                .GroupBy((c) => c.Category)
                .OrderBy((g) => g.Key)
                .ToArray();
            var groupEditors = new Dictionary<string, GroupBox>();
            //setup colors
            categoryPanel.BackColor = Settings.CategoryMainBackground;
            valuePanel.BackColor = Settings.ValueEditorBackground;
            //setup category quick links
            var category = settings.Select((g) => g.Key).ToArray();
            categoryQuick.RowCount = category.Length + 1;
            categoryQuick.RowStyles.Clear();
            for (int i = 0; i < category.Length; ++i)
                categoryQuick.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            categoryQuick.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            for (int i = 0; i < category.Length; ++i)
            {
                var label = new Label
                {
                    Text = category[i],
                    Cursor = Cursors.Hand,
                    AutoSize = false,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(5),
                    Tag = i,
                    BackColor = i % 2 == 0 ? Settings.CategoryMainBackground : Settings.CategorySecondBackground,
                    Width = categoryQuick.Width
                };
                label.MouseEnter += (s, e) => label.BackColor = Settings.CategoryHoverBackground;
                label.MouseLeave += (s, e) => label.BackColor = (int)label.Tag % 2 == 0 ? Settings.CategoryMainBackground : Settings.CategorySecondBackground;
                label.Click += (s, e) =>
                {
                    var group = groupEditors[category[(int)label.Tag]];
                    valuePanel.ScrollControlIntoView(group);
                };
                categoryQuick.Controls.Add(label, 0, i);
            }
            //setup value setter groups
            for (int i = 0; i<settings.Length; ++i)
            {
                var setting = settings[i].ToArray();
                var group = new GroupBox
                {
                    Text = settings[i].Key,
                    Dock = DockStyle.Top,
                    AutoSize = true
                };
                var table = new TableLayoutPanel
                {
                    Dock = DockStyle.Top,
                    ColumnCount = 3
                };
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Settings.ErrorColumnWidth));
                table.RowCount = setting.Length;
                var totalHeight = 0;
                for (int j = 0; j<setting.Length; ++j)
                {
                    var height = 0;
                    var label = new Label
                    {
                        Text = setting[j].Name,
                        Dock = DockStyle.Left,
                        TextAlign = ContentAlignment.MiddleLeft,
                    };
                    toolTip1.SetToolTip(label, setting[j].Description);
                    table.Controls.Add(label, 0, j);
                    height = Math.Max((int)graphics.MeasureString(label.Text, Font).Height + Settings.ValueLabelVertPadding, height);
                    var control = Settings.GetEditor(errorProvider1, setting[j])
                        ?? new Label
                        {
                            Text = setting[j].Value?.ToString(),
                            Height = (int)graphics.MeasureString(setting[j].Value?.ToString(), Font).Height,
                            TextAlign = ContentAlignment.TopLeft,
                        };
                    control.Dock = DockStyle.Top;
                    table.Controls.Add(control, 1, j);
                    height = Math.Max(control.Height, height);
                    table.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
                    totalHeight += height;
                }
                table.Height = totalHeight;
                //group.ClientSize = new Size(group.ClientSize.Width, totalHeight);
                group.Controls.Add(table);
                groupEditors.Add(settings[i].Key, group);
                valuePanel.Controls.Add(group);
                valuePanel.Controls.SetChildIndex(group, 0);
            }
        }
    }
}
