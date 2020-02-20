using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;

namespace MaxLib.Data.Config
{
    /// <summary>
    /// Provides the settings for <see cref="ConfigEditor"/>.
    /// </summary>
    public partial class ConfigEditorSettings : Component
    {
        /// <summary>
        /// Create new provider for settings for <see cref="ConfigEditor"/>.
        /// </summary>
        public ConfigEditorSettings()
        {
            InitializeComponent();
            AddDefaultEditor();
        }

        /// <summary>
        /// Create new provider for settings for <see cref="ConfigEditor"/>.
        /// </summary>
        /// <param name="container">The target container to add this component</param>
        public ConfigEditorSettings(IContainer container)
        {
            container.Add(this);

            InitializeComponent();
            AddDefaultEditor();
        }

        List<(Func<IConfigValueBase, bool>, Func<ErrorProvider, IConfigValueBase, Control>)> editors 
            = new List<(Func<IConfigValueBase, bool>, Func<ErrorProvider, IConfigValueBase, Control>)>();

        /// <summary>
        /// The main color for the background of the category navigation
        /// </summary>
        public Color CategoryMainBackground { get; set; } = Color.White;

        /// <summary>
        /// The second color for the background of the category navigation
        /// </summary>
        public Color CategorySecondBackground { get; set; } = Color.LightBlue;

        /// <summary>
        /// The color for the background of the category navigation when the mouse hover it
        /// </summary>
        public Color CategoryHoverBackground { get; set; } = Color.LightGray;

        /// <summary>
        /// The color of the background of the value editor section
        /// </summary>
        public Color ValueEditorBackground { get; set; } = Color.White;

        /// <summary>
        /// The additional vertical padding for the value editor labels.
        /// </summary>
        public int ValueLabelVertPadding { get; set; } = 10;

        /// <summary>
        /// The column with that is reserved for error notifivation icons.
        /// </summary>
        public int ErrorColumnWidth { get; set; } = 20;

        /// <summary>
        /// Add a new editor control factory for <see cref="IConfigValueBase"/>
        /// </summary>
        /// <param name="checker">this function checks if the <see cref="IConfigValueBase"/> can be applied to the factory</param>
        /// <param name="editor">this function creates the editor control out off the <see cref="IConfigValueBase"/>.</param>
        public void AddEditor(Func<IConfigValueBase, bool> checker, Func<ErrorProvider, IConfigValueBase, Control> editor)
        {
            if (checker == null) throw new ArgumentNullException(nameof(checker));
            if (editor == null) throw new ArgumentNullException(nameof(editor));
            editors.Add((checker, editor));
        }

        /// <summary>
        /// Add a new editor control factory for a specific <see cref="T"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="IConfigValueBase"/> that defines the configurable value</typeparam>
        /// <param name="editor">the factory tha created an editor control for <typeparamref name="T"/></param>
        public void AddEditor<T>(Func<ErrorProvider, T, Control> editor) where T : IConfigValueBase
        {
            if (editor == null) throw new ArgumentNullException(nameof(editor));
            AddEditor((c) => c is T, (e, c) => editor(e, (T)c));
        }

        /// <summary>
        /// Clear all previos defined editors.
        /// </summary>
        public void ClearEditors()
        {
            editors.Clear();
        }

        /// <summary>
        /// Add the default editors for <see cref="ConfigIntValue"/>, <see cref="ConfigStringValue"/> and <see cref="ConfigBoolValue"/>.
        /// </summary>
        public void AddDefaultEditor()
        {
            AddEditor<ConfigIntValue>((error, config) =>
            {
                var control = new NumericUpDown
                {
                    Minimum = config.Minimum,
                    Maximum = config.Maximum,
                };
                control.Value = config.Value;
                control.ValueChanged += (s, e) => config.Value = (int)control.Value;
                control.Validating += (s, e) =>
                {
                    if (!config.Validate())
                    {
                        e.Cancel = true;
                        control.Select(0, control.Text.Length);
                        error.SetError(control, $"Value should be between {config.Minimum:#,#0} and {config.Maximum:#,#0}.");
                    }
                };
                control.Validated += (s, e) =>
                {
                    error.SetError(control, "");
                };
                return control;
            });
            AddEditor<ConfigStringValue>((error, config) =>
            {
                var control = new TextBox
                {
                    Text = config.Value
                };
                control.TextChanged += (s, e) => config.Value = control.Text;
                control.Validating += (s, e) =>
                {
                    if (!config.Validate())
                    {
                        e.Cancel = true;
                        control.Select(0, control.Text.Length);
                        error.SetError(control, $"Value doensn't match regex '{config.Pattern}'.");
                    }
                };
                control.Validated += (s, e) =>
                {
                    error.SetError(control, "");
                };
                return control;
            });
            AddEditor<ConfigBoolValue>((error, config) =>
            {
                var control = new CheckBox
                {
                    Checked = config.Value
                };
                control.CheckedChanged += (s, e) => config.Value = control.Checked;
                control.Validating += (s, e) =>
                {
                    if (!config.Validate())
                    {
                        e.Cancel = true;
                        error.SetError(control, $"Invalid value.");
                    }
                };
                control.Validated += (s, e) =>
                {
                    error.SetError(control, "");
                };
                return control;
            });
        }

        /// <summary>
        /// Try to generate an editor control for the specified <paramref name="config"/>. The generators are previously defined
        /// with <see cref="AddEditor(Func{IConfigValueBase, bool}, Func{ErrorProvider, IConfigValueBase, Control})"/>,
        /// <see cref="AddEditor{T}(Func{ErrorProvider, T, Control})"/> or <see cref="AddDefaultEditor"/>.
        /// </summary>
        /// <param name="errorProvider">the <see cref="ErrorProvider"/> that will support the validation process</param>
        /// <param name="config">the value configuration that should be edited</param>
        /// <returns>the control if a generator was found or null</returns>
        public Control GetEditor(ErrorProvider errorProvider, IConfigValueBase config)
        {
            if (errorProvider == null) throw new ArgumentNullException(nameof(errorProvider));
            if (config == null) throw new ArgumentNullException(nameof(config));
            foreach (var (check, editor) in editors)
            {
                if (check(config))
                    return editor(errorProvider, config);
            }
            return null;
        }
    }
}
