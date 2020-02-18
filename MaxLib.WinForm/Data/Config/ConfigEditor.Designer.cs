namespace MaxLib.Data.Config
{
    partial class ConfigEditor
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.categoryPanel = new System.Windows.Forms.Panel();
            this.categoryQuick = new System.Windows.Forms.TableLayoutPanel();
            this.valuePanel = new System.Windows.Forms.Panel();
            this.errorProvider1 = new System.Windows.Forms.ErrorProvider(this.components);
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.categoryPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider1)).BeginInit();
            this.SuspendLayout();
            // 
            // categoryPanel
            // 
            this.categoryPanel.Controls.Add(this.categoryQuick);
            this.categoryPanel.Dock = System.Windows.Forms.DockStyle.Left;
            this.categoryPanel.Location = new System.Drawing.Point(0, 0);
            this.categoryPanel.Margin = new System.Windows.Forms.Padding(0);
            this.categoryPanel.Name = "categoryPanel";
            this.categoryPanel.Padding = new System.Windows.Forms.Padding(5);
            this.categoryPanel.Size = new System.Drawing.Size(177, 522);
            this.categoryPanel.TabIndex = 0;
            // 
            // categoryQuick
            // 
            this.categoryQuick.ColumnCount = 1;
            this.categoryQuick.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.categoryQuick.Dock = System.Windows.Forms.DockStyle.Fill;
            this.categoryQuick.Location = new System.Drawing.Point(5, 5);
            this.categoryQuick.Margin = new System.Windows.Forms.Padding(0);
            this.categoryQuick.Name = "categoryQuick";
            this.categoryQuick.RowCount = 2;
            this.categoryQuick.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.categoryQuick.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.categoryQuick.Size = new System.Drawing.Size(167, 512);
            this.categoryQuick.TabIndex = 0;
            // 
            // valuePanel
            // 
            this.valuePanel.AutoScroll = true;
            this.valuePanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.valuePanel.Location = new System.Drawing.Point(177, 0);
            this.valuePanel.Name = "valuePanel";
            this.valuePanel.Size = new System.Drawing.Size(641, 522);
            this.valuePanel.TabIndex = 1;
            // 
            // errorProvider1
            // 
            this.errorProvider1.ContainerControl = this;
            // 
            // ConfigEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(818, 522);
            this.Controls.Add(this.valuePanel);
            this.Controls.Add(this.categoryPanel);
            this.Name = "ConfigEditor";
            this.Text = "ConfigEditor";
            this.categoryPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel categoryPanel;
        private System.Windows.Forms.TableLayoutPanel categoryQuick;
        private System.Windows.Forms.Panel valuePanel;
        private System.Windows.Forms.ErrorProvider errorProvider1;
        private System.Windows.Forms.ToolTip toolTip1;
    }
}