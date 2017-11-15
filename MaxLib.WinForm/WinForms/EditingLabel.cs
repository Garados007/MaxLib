using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;

namespace MaxLib.WinForms
{
    public class EditingLabel : TextBox
    {
        private bool showBoxWhenEdit = true;
        [DefaultValue(true)]
        [Description("Gibt an, ob der Hintergrund angezeigt werden soll, wenn der Text bearbeitet wird.")]
        public bool ShowBoxWhenEdit
        {
            get { return showBoxWhenEdit; }
            set { showBoxWhenEdit = value; }
        }

        [DefaultValue(BorderStyle.None)]
        [Browsable(false)]
        new public BorderStyle BorderStyle
        {
            get { return base.BorderStyle; }
            set
            {
                base.BorderStyle = value;
            }
        }

        private bool selectAllWhenFocus = false;
        [DefaultValue(false)]
        [Description("Gibt an, ob der ganze Text markiert werden soll, wenn diese Box den Fokus erhält.")]
        public bool SelectAllWhenFocus
        {
            get { return selectAllWhenFocus; }
            set { selectAllWhenFocus = value; }
        }

        Color BufColor = Color.White;
        [DefaultValue(typeof(Color),"White")]
        new public Color BackColor
        {
            get { return BufColor; }
            set
            {
                BufColor = value;
                if (BorderStyle != BorderStyle.None)
                    base.BackColor = value;
            }
        }

        private string emptyText = "";
        [DefaultValue("")]
        [Description("Dieser Text wird angezeigt, wenn keiner angegeben wurde und der Fokus nicht da ist.")]
        public string EmptyText
        {
            get { return emptyText; }
            set
            {
                emptyText = value;
                if (!Focused) base.Text = emptyText;
            }
        }

        private string text;
        [DefaultValue("")]
        [Description("Der eingegebene Text")]
        new public string Text
        {
            get { return text; }
            set
            {
                text = value;
                if (Focused) base.Text = value;
            }
        }

        public EditingLabel()
        {
            base.BorderStyle = System.Windows.Forms.BorderStyle.None;
            GotFocus += EditingLabel_GotFocus;
            LostFocus += EditingLabel_LostFocus;
            BorderStyleChanged += EditingLabel_BorderStyleChanged;
            SetStyle(
                ControlStyles.SupportsTransparentBackColor 
                 | ControlStyles.OptimizedDoubleBuffer
                 | ControlStyles.AllPaintingInWmPaint 
                 | ControlStyles.ResizeRedraw
                 //| ControlStyles.UserPaint
                 , true);
            base.BackColor = Color.Transparent;
            base.TextChanged += EditingLabel_TextChanged;
            base.HandleCreated += EditingLabel_HandleCreated;
        }

        void EditingLabel_HandleCreated(object sender, EventArgs e)
        {
            base.Text = Text;
        }

        void EditingLabel_TextChanged(object sender, EventArgs e)
        {
            if (Focused) Text = base.Text;
        }

        void EditingLabel_BorderStyleChanged(object sender, EventArgs e)
        {
            sf = lf = false;
        }

        bool lf = false, sf = false;

        void EditingLabel_LostFocus(object sender, EventArgs e)
        {
            if (sf)
            {
                sf = false;
                return;
            }
            lf = true;
            //Invalidate();
            //if (base.BorderStyle!=System.Windows.Forms.BorderStyle.None)
            base.BorderStyle = System.Windows.Forms.BorderStyle.None;
            base.BackColor = Color.Transparent;
            if (showBoxWhenEdit && EditEnded != null) EditEnded(this, EventArgs.Empty);
            Text = base.Text;
            base.Text = Text == "" ? emptyText : Text;
        }

        void EditingLabel_GotFocus(object sender, EventArgs e)
        {
            if (lf)
            { lf = false; return; }
            //Invalidate(); Refresh();
            if (showBoxWhenEdit)
            {
                sf = true;
                base.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
                base.BackColor = BufColor;
                if (EditStart != null) EditStart(this, EventArgs.Empty);
            }
            base.Text = Text;
            if (selectAllWhenFocus) SelectAll();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
        }

        /// <summary>
        /// Wird ausgelöst, wenn die Bearbeitung gestartet und der Hintergrund geändert wurde.
        /// </summary>
        public event EventHandler EditStart;
        /// <summary>
        /// Wird ausgelöst, wenn die Bearbeitung beendet und der Hintergrund geändert wurde.
        /// </summary>
        public event EventHandler EditEnded;
    }
}
