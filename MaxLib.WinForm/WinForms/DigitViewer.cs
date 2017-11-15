using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MaxLib.WinForms
{
    public partial class DigitViewer : Control
    {
        public DigitViewer()
        {
            InitializeComponent();
            Text = "";
            Size = new Size(20, 40);
            DoubleBuffered = true;
        }

        private Digit digit;
        public Digit Digit
        {
            get { return digit; }
            set { digit = value; Invalidate(); }
        }

        private DigitConverter converter = new StandartConverter();
        [DefaultValue(typeof(StandartConverter))]
        public DigitConverter Converter
        {
            get { return converter; }
            set { if (value == null) throw new ArgumentNullException(); converter = value; }
        }

        private Color inactiveColor = Color.Gray;
        public Color InactiveColor
        {
            get { return inactiveColor; }
            set { inactiveColor = value; }
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            base.OnPaint(pe);
            using (var p = new Pen(ForeColor, 3))
            using (var a = new Pen(InactiveColor, 1))
            {
                var b = new Rectangle(Width / 5, Height / 5, Width * 3 / 5, Height * 3 / 5);
                var u = new Size(b.Width / 2, b.Height / 2);
                var g = pe.Graphics;
                if (!digit.HasFlag(Digit.TopHorzLeft)) g.DrawLine(a, b.Left, b.Top, b.Left + u.Width, b.Top);
                if (!digit.HasFlag(Digit.TopHorzRight)) g.DrawLine(a, b.Left + u.Width, b.Top, b.Right, b.Top);
                if (!digit.HasFlag(Digit.LeftVertTop)) g.DrawLine(a, b.Left, b.Top, b.Left, b.Top + u.Height);
                if (!digit.HasFlag(Digit.SlashTopLeft)) g.DrawLine(a, b.Left, b.Top, b.Left + u.Width, b.Top + u.Height);
                if (!digit.HasFlag(Digit.MiddleVertTop)) g.DrawLine(a, b.Left + u.Width, b.Top, b.Left + u.Width, b.Top + u.Height);
                if (!digit.HasFlag(Digit.SlashTopRight)) g.DrawLine(a, b.Left + u.Width, b.Top + u.Height, b.Right, b.Top);
                if (!digit.HasFlag(Digit.RightVertTop)) g.DrawLine(a, b.Right, b.Top, b.Right, b.Top + u.Height);
                if (!digit.HasFlag(Digit.MiddleHorzLeft)) g.DrawLine(a, b.Left, b.Top + u.Height, b.Left + u.Width, b.Top + u.Height);
                if (!digit.HasFlag(Digit.MiddleHorzRight)) g.DrawLine(a, b.Left + u.Width, b.Top + u.Height, b.Right, b.Top + u.Height);
                if (!digit.HasFlag(Digit.LeftVertBot)) g.DrawLine(a, b.Left, b.Top + u.Height, b.Left, b.Bottom);
                if (!digit.HasFlag(Digit.SlashBotLeft)) g.DrawLine(a, b.Left, b.Bottom, b.Left + u.Width, b.Top + u.Height);
                if (!digit.HasFlag(Digit.MiddleVertBot)) g.DrawLine(a, b.Left + u.Width, b.Top + u.Height, b.Left + u.Width, b.Bottom);
                if (!digit.HasFlag(Digit.SlashBotRight)) g.DrawLine(a, b.Left + u.Width, b.Top + u.Height, b.Right, b.Bottom);
                if (!digit.HasFlag(Digit.RightVertBot)) g.DrawLine(a, b.Right, b.Top + u.Height, b.Right, b.Bottom);
                if (!digit.HasFlag(Digit.BotHorzLeft)) g.DrawLine(a, b.Left, b.Bottom, b.Left + u.Width, b.Bottom);
                if (!digit.HasFlag(Digit.BotHorzRight)) g.DrawLine(a, b.Left + u.Width, b.Bottom, b.Right, b.Bottom);

                if (digit.HasFlag(Digit.TopHorzLeft)) g.DrawLine(p, b.Left, b.Top, b.Left + u.Width, b.Top);
                if (digit.HasFlag(Digit.TopHorzRight)) g.DrawLine(p, b.Left + u.Width, b.Top, b.Right, b.Top);
                if (digit.HasFlag(Digit.LeftVertTop)) g.DrawLine(p, b.Left, b.Top, b.Left, b.Top + u.Height);
                if (digit.HasFlag(Digit.SlashTopLeft)) g.DrawLine(p, b.Left, b.Top, b.Left + u.Width, b.Top + u.Height);
                if (digit.HasFlag(Digit.MiddleVertTop)) g.DrawLine(p, b.Left + u.Width, b.Top, b.Left + u.Width, b.Top + u.Height);
                if (digit.HasFlag(Digit.SlashTopRight)) g.DrawLine(p, b.Left + u.Width, b.Top + u.Height, b.Right, b.Top);
                if (digit.HasFlag(Digit.RightVertTop)) g.DrawLine(p, b.Right, b.Top, b.Right, b.Top + u.Height);
                if (digit.HasFlag(Digit.MiddleHorzLeft)) g.DrawLine(p, b.Left, b.Top + u.Height, b.Left + u.Width, b.Top + u.Height);
                if (digit.HasFlag(Digit.MiddleHorzRight)) g.DrawLine(p, b.Left + u.Width, b.Top + u.Height, b.Right, b.Top + u.Height);
                if (digit.HasFlag(Digit.LeftVertBot)) g.DrawLine(p, b.Left, b.Top + u.Height, b.Left, b.Bottom);
                if (digit.HasFlag(Digit.SlashBotLeft)) g.DrawLine(p, b.Left, b.Bottom, b.Left + u.Width, b.Top + u.Height);
                if (digit.HasFlag(Digit.MiddleVertBot)) g.DrawLine(p, b.Left + u.Width, b.Top + u.Height, b.Left + u.Width, b.Bottom);
                if (digit.HasFlag(Digit.SlashBotRight)) g.DrawLine(p, b.Left + u.Width, b.Top + u.Height, b.Right, b.Bottom);
                if (digit.HasFlag(Digit.RightVertBot)) g.DrawLine(p, b.Right, b.Top + u.Height, b.Right, b.Bottom);
                if (digit.HasFlag(Digit.BotHorzLeft)) g.DrawLine(p, b.Left, b.Bottom, b.Left + u.Width, b.Bottom);
                if (digit.HasFlag(Digit.BotHorzRight)) g.DrawLine(p, b.Left + u.Width, b.Bottom, b.Right, b.Bottom);
            }
        }

        public override string Text
        {
            get
            {
                return base.Text;
            }
            set
            {
                base.Text = value;
                if (Text.Length > 0)
                {
                    var d = converter.Convert(Text[0]);
                    Digit = d;
                }
                else Digit = Digits.Digit.None;
            }
        }
    }
}
