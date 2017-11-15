using System;
using System.Drawing;
using System.Windows.Forms;

namespace MaxLib.Console.ExtendedConsole
{
    public partial class ExtendedConsoleForm : Form
    {
        internal ExtendedConsoleForm()
        {
            InitializeComponent();
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            Load += ExtendedConsoleForm_Load;
            FormClosing += ExtendedConsoleForm_FormClosing;
            MouseMove += ExtendedConsoleForm_MouseMove;
            KeyDown += ExtendedConsoleForm_KeyDown;
            KeyUp += ExtendedConsoleForm_KeyUp;
            MouseDown += ExtendedConsoleForm_MouseDown;
            MouseUp += ExtendedConsoleForm_MouseUp;
            DoubleBuffered = true;
            Cursor.Hide();
        }

        bool marking = false;

        void ExtendedConsoleForm_MouseUp(object sender, MouseEventArgs e)
        {
            marking = false;
            Owner.Cursor.DoUp();
        }

        void ExtendedConsoleForm_MouseDown(object sender, MouseEventArgs e)
        {
            if (Owner.Marker.Enabled && Owner.Marker.EnableToChange)
            {
                marking = true;
                Owner.Marker.StartX = Owner.Marker.StopX = e.X / Owner.Options.BoxWidth;
                Owner.Marker.StartY = Owner.Marker.StopY = e.Y / Owner.Options.BoxHeight;
            }
            Owner.Cursor.DoDown();
        }

        void ExtendedConsoleForm_KeyUp(object sender, KeyEventArgs e)
        {
            Owner.KeyManager.Push((In.Keys)(int)e.KeyCode, true);
        }

        void ExtendedConsoleForm_KeyDown(object sender, KeyEventArgs e)
        {
            Owner.KeyManager.Push((In.Keys)(int)e.KeyCode, false);
        }

        void ExtendedConsoleForm_MouseMove(object sender, MouseEventArgs e)
        {
            if (Owner.Options.ShowMouse) Flush();
            if (Owner.Marker.Enabled && marking && Owner.Marker.EnableToChange)
            {
                Owner.Marker.StopX = e.X / Owner.Options.BoxWidth;
                if (Owner.Marker.StopX > Owner.Marker.StartX) Owner.Marker.StopX++;
                Owner.Marker.StopY = e.Y / Owner.Options.BoxHeight;
                if (Owner.Marker.StopY > Owner.Marker.StartY) Owner.Marker.StopY++;
            }
            Owner.Cursor.X = e.X / Owner.Options.BoxWidth;
            Owner.Cursor.Y = e.Y / Owner.Options.BoxHeight;
            Owner.Cursor.DoMove();
        }

        void ExtendedConsoleForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Buffer.Dispose();
        }

        void ExtendedConsoleForm_Load(object sender, EventArgs e)
        {
            targetGraphics = CreateGraphics();
            //Flush();
            Show();
            Owner.RunLoad();
        }

        BufferedGraphics Buffer;
        Graphics targetGraphics;
        Size LastSize = new Size();
        public new ExtendedConsole Owner { get; internal set; }

        object flushlocker = new object();

        internal void Flush()
        {
            System.Threading.Monitor.Enter(flushlocker);
            //set Buffer
            if (LastSize != Size)
            {
                if (Buffer != null) Buffer.Dispose();
                Buffer = BufferedGraphicsManager.Current.Allocate(targetGraphics, new Rectangle(0, 0, Width, Height));
                LastSize = Size;
            }
            //Flush Elements
            var w = new Out.ClipWriterAsync(Owner);
            w.BeginWrite();
            Owner.MainContainer.DrawElements(w);
            w.EndWrite();
            //Draw Elements
            var g = Buffer.Graphics;
            if (g == null) return;
            g.Clear(Owner.Options.Background);
            var m = Owner.Matrix;
            int bw = Owner.Options.BoxWidth, bh = Owner.Options.BoxHeight;
            for (int x = 0; x < m.Width; ++x) for (int y = 0; y < m.Height; ++y)
                {
                    using (var brush = new SolidBrush(m[x, y].Background))
                        g.FillRectangle(brush, new Rectangle(x * bw, y * bh, bw, bh));
                    using (var brush = new SolidBrush(m[x, y].Foreground))
                        g.DrawString((m[x, y].Value < 32 ? ' ' : m[x, y].Value).ToString(), Owner.Options.Font, brush, x * bw-1, y * bh-2);
                }
            //Draw Mouse
            if (Owner.Options.ShowMouse)
            {
                if (Owner.Marker.Enabled)
                {
                    int sx = Math.Min(Owner.Marker.StartX, Owner.Marker.StopX), sy = Math.Min(Owner.Marker.StartY, Owner.Marker.StopY),
                        ex = Math.Max(Owner.Marker.StartX, Owner.Marker.StopX), ey = Math.Max(Owner.Marker.StartY, Owner.Marker.StopY);
                    using (var brush = new SolidBrush(Color.FromArgb(64, Color.White)))
                        g.FillRectangle(brush, new Rectangle(sx * bw, sy * bh, (ex - sx) * bw, (ey - sy) * bh));
                    g.DrawRectangle(Pens.Black, new Rectangle(sx * bw, sy * bh, (ex - sx) * bw, (ey - sy) * bh));
                }
                var x = (Cursor.Position.X - Left) / bw;
                var y = (Cursor.Position.Y - Top) / bh;
                using (var brush = new SolidBrush(Color.FromArgb(128, Color.White)))
                    g.FillRectangle(brush, new Rectangle(x * bw, y * bh, bw, bh));
                g.DrawRectangle(Pens.Black, new Rectangle(x * bw, y * bh, bw, bh));
            }
            //Render
            Buffer.Render();
            System.Threading.Monitor.Exit(flushlocker);
        }
    }
}
