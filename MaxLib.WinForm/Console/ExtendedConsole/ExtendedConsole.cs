using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace MaxLib.Console.ExtendedConsole
{
    public sealed class ExtendedConsole : IDisposable
    {
        internal void OptionsChanged()
        {
            if (!writing) Flush();
        }

        internal void BeginEdit()
        {
            System.Threading.Monitor.Enter(locker);
            writing = true;
        }

        bool writing = false;
        object locker = new object();

        internal void EndEdit()
        {
            writing = false;
            System.Threading.Monitor.Exit(locker);
        }

        public ExtendedConsoleOptions Options { get; private set; }
        public ExtendedConsoleCellMatrix Matrix { get; private set; }
        public ExtendedConsoleForm Form { get; private set; }
        public In.KeyManager KeyManager { get; private set; }
        public ExtendedConsoleMarker Marker { get; private set; }
        public In.Cursor Cursor { get; private set; }
        public Elements.ElementContainer MainContainer { get; private set; }

        public event Action Load;
        internal void RunLoad()
        {
            if (Load != null) Load();
        }

        System.Threading.Thread Viewer;
        int screennum = 0;
        public int Screen
        {
            get { return screennum; }
            set
            {
                if (value < 0 || value >= System.Windows.Forms.Screen.AllScreens.Length) throw new ArgumentOutOfRangeException("Screen");
                else
                {
                    screennum = value;
                    Form.Bounds = System.Windows.Forms.Screen.AllScreens[value].Bounds;
                }
            }
        }
        public int ScreenCount
        {
            get { return System.Windows.Forms.Screen.AllScreens.Length; }
        }

        public ExtendedConsole(ExtendedConsoleOptions Options)
        {
            this.Options = Options;
            Options.Owner = this;

            Matrix = new ExtendedConsoleCellMatrix();
            var s = System.Windows.Forms.Screen.AllScreens[0].Bounds.Size;
            Matrix.SetSize(s.Width / Options.BoxWidth, s.Height / Options.BoxHeight);
            Matrix.Changed = OptionsChanged;

            KeyManager = new In.KeyManager();
            Marker = new ExtendedConsoleMarker(this);
            Cursor = new In.Cursor(this);
            MainContainer = new Elements.ElementContainer();
            MainContainer.Changed += () =>
                {
                    if (Form != null) Flush();
                };

            if (Options.RunFormInExtraThread)
            {
                Viewer = new System.Threading.Thread(StartViewer);
                Viewer.Name = "Viewer Thread";
                Viewer.Start();
                while (Form == null || !Form.Visible) System.Threading.Thread.Sleep(1);
            }
        }

        void StartViewer()
        {
            Form = new ExtendedConsoleForm();
            Form.Owner = this;
            Screen = 0;
            System.Windows.Forms.Application.Run(Form);
        }

        public void Start()
        {
            StartViewer();
        }

        public void Flush()
        {
            Form.Flush();
        }

        public void Dispose()
        {
            if (!Form.Disposing && !Form.IsDisposed) Form.Invoke(new Action(Form.Close));
            Matrix = null;
            Options = null;
        }

        public void CloseForm()
        {
            if (!Form.Disposing && !Form.IsDisposed) Form.Invoke(new Action(Form.Close));
        }
    }

    public sealed class ExtendedConsoleCellMatrix
    {
        internal void CellChanged()
        {
            if (Changed != null) Changed();
        }

        internal Action Changed = null;

        internal List<List<ExtendedConsoleCell>> matrix = new List<List<ExtendedConsoleCell>>();

        public ExtendedConsoleCell this[int x, int y]
        {
            get { return (matrix[x])[y]; }
        }

        public int Width { get; private set; }
        public int Height { get; private set; }

        internal void SetSize(int width, int height)
        {
            Width = width;
            Height = height;
            //x anpassen
            while (matrix.Count < width) matrix.Add(new List<ExtendedConsoleCell>());
            while (matrix.Count > width) matrix.RemoveAt(matrix.Count - 1);
            //y anpassen
            foreach (var y in matrix)
            {
                while (y.Count < height) y.Add(new ExtendedConsoleCell(this));
                while (y.Count > height) y.RemoveAt(y.Count - 1);
            }
            //
            if (Changed != null) Changed();
        }

        public void ClearData(ExtendedConsoleOptions options)
        {
            foreach (var x in matrix) foreach (var c in x)
                {
                    c.Value = (char)0;
                    c.Background = options.Background;
                    c.Foreground = options.Foreground;
                }
            if (Changed != null) Changed();
        }

        ExtendedConsoleDataCatcher row, column;

        public ExtendedConsoleDataCatcher Rows { get { return row; } }
        public ExtendedConsoleDataCatcher Columns { get { return column; } }

        internal ExtendedConsoleCellMatrix()
        {
            row = new ExtendedConsoleDataCatcher(this, false);
            column = new ExtendedConsoleDataCatcher(this, true);
        }

        internal void CopyData(ExtendedConsoleCellMatrix m)
        {
            for (int x = 0; x < Width && x < m.Width; ++x) for (int y = 0; y < Height && y < m.Height; ++y)
                    this[x, y].CopyData(m[x, y]);
        }
    }

    public sealed class ExtendedConsoleDataCatcher : IEnumerable<ExtendedConsoleDataStrip>
    {
        ExtendedConsoleCellMatrix matrix;
        internal bool Vertical;

        public ExtendedConsoleDataStrip this[int index]
        {
            get
            {
                return new ExtendedConsoleDataStrip(matrix, index, Vertical);
            }
        }

        internal ExtendedConsoleDataCatcher(ExtendedConsoleCellMatrix matrix, bool Vertical)
        {
            this.matrix = matrix;
            this.Vertical = Vertical;
        }

        public IEnumerator<ExtendedConsoleDataStrip> GetEnumerator()
        {
            return new Enumerator(Vertical, matrix);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new Enumerator(Vertical, matrix);
        }

        class Enumerator : IEnumerator<ExtendedConsoleDataStrip>
        {
            int index = 0;
            bool vertical = false;
            ExtendedConsoleCellMatrix matrix;

            public Enumerator(bool Vertical, ExtendedConsoleCellMatrix matrix)
            {
                vertical = Vertical;
                this.matrix = matrix;
            }

            public ExtendedConsoleDataStrip Current
            {
                get { return new ExtendedConsoleDataStrip(matrix, index, vertical); }
            }

            public void Dispose()
            {
            }

            object System.Collections.IEnumerator.Current
            {
                get { return new ExtendedConsoleDataStrip(matrix, index, vertical); }
            }

            public bool MoveNext()
            {
                if (index + 1 == (vertical ? matrix.Height : matrix.Width)) return false;
                else
                {
                    index++;
                    return true;
                }
            }

            public void Reset()
            {
                index = 0;
            }
        }
    }

    public sealed class ExtendedConsoleDataStrip : IEnumerable<ExtendedConsoleCell>
    {
        internal ExtendedConsoleCell[] Cells;
        public ExtendedConsoleCell this[int index]
        {
            get { return Cells[index]; }
        }
        public bool Vertical { get; internal set; }

        public override string ToString()
        {
            return new string(Cells.ToList().ConvertAll((c) => c.Value < 32 ? (char)32 : c.Value).ToArray());
        }

        internal ExtendedConsoleDataStrip(ExtendedConsoleCellMatrix m, int index, bool vertical)
        {
            Vertical = vertical;
            Cells = new ExtendedConsoleCell[vertical ? m.Height : m.Width];
            for (int i = 0; i < Cells.Length; ++i) Cells[i] = m[vertical ? index : i, vertical ? i : index];
        }

        public IEnumerator<ExtendedConsoleCell> GetEnumerator()
        {
            return (IEnumerator<ExtendedConsoleCell>)Cells.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return Cells.GetEnumerator();
        }
    }

    public sealed class ExtendedConsoleCell
    {
        public ExtendedConsoleCellMatrix Owner { get; private set; }
        internal ExtendedConsoleCell(ExtendedConsoleCellMatrix Owner)
        {
            this.Owner = Owner;
        }

        char value = (char)0;
        public char Value { get { return value; } set { this.value = value; Owner.CellChanged(); } }

        ExtendedConsoleColor bgCol = Color.Black;
        public ExtendedConsoleColor Background { get { return bgCol; } set { bgCol = value; Owner.CellChanged(); } }
        ExtendedConsoleColor fgCol = Color.White;
        public ExtendedConsoleColor Foreground { get { return fgCol; } set { fgCol = value; Owner.CellChanged(); } }

        public override string ToString()
        {
            return string.Format("Value={0} Background={1} Foreground={2}", Value, Background, Foreground);
        }

        internal void CopyData(ExtendedConsoleCell c)
        {
            if (c.Value >= 32)
            {
                value = c.value;
                bgCol = c.bgCol;
                fgCol = c.fgCol;
            }
        }
    }

    public sealed class ExtendedConsoleOptions
    {
        public ExtendedConsole Owner { get; internal set; }

        private ExtendedConsoleColor bg = Color.Black, fg = Color.White;
        public ExtendedConsoleColor Background { get { return bg; } set { bg = value; } }
        public ExtendedConsoleColor Foreground { get { return fg; } set { fg = value; } }

        bool runFormInExtraThread = true;
        public bool RunFormInExtraThread { get { return runFormInExtraThread; } set { runFormInExtraThread = value; } }

        private int boxWidth = 10;
        public int BoxWidth
        {
            get { return boxWidth; }
            set
            {
                if (value <= 0) throw new ArgumentOutOfRangeException("BoxWidth");
                else
                {
                    boxWidth = value;
                    if (Owner != null) Owner.OptionsChanged();
                }
            }
        }
        private int boxHeight = 10;
        public int BoxHeight
        {
            get { return boxHeight; }
            set
            {
                if (value <= 0) throw new ArgumentOutOfRangeException("BoxHeight");
                else
                {
                    boxHeight = value;
                    if (Owner != null) Owner.OptionsChanged();
                }
            }
        }

        private Font font = new Font("Consolas", 12);
        public Font Font
        {
            get { return font; }
            set
            {
                if (font == null) throw new ArgumentNullException("Font");
                else
                {
                    font = value;
                    if ((Owner != null)) Owner.OptionsChanged();
                }
            }
        }
        public void SetFontSize(float newSize)
        {
            var old = Font;
            Font = new Font(font.FontFamily, newSize, font.Style);
            old.Dispose();
        }

        private bool showMouse = true;
        public bool ShowMouse
        {
            get { return showMouse; }
            set
            {
                showMouse = value;
                if (Owner != null) Owner.OptionsChanged();
            }
        }
    }

    public struct ExtendedConsoleColor
    {
        public int A { get; set; }
        public int R { get; set; }
        public int G { get; set; }
        public int B { get; set; }

        public ExtendedConsoleColor(int r, int g, int b) : this(255, r, g, b) { }

        public ExtendedConsoleColor(int a, int r, int g, int b) : this()
        {
            A = a;
            R = r;
            G = g;
            B = b;
        }

        public ExtendedConsoleColor(System.Drawing.Color c) : this()
        {
            A = c.A;
            R = c.R;
            G = c.G;
            B = c.B;
        }

        public ExtendedConsoleColor(int c) : this(System.Drawing.Color.FromArgb(c)) { }

        public static implicit operator ExtendedConsoleColor(System.Drawing.Color c)
        {
            return new ExtendedConsoleColor(c);
        }
        public static implicit operator System.Drawing.Color(ExtendedConsoleColor c)
        {
            return System.Drawing.Color.FromArgb(c.A, c.R, c.G, c.B);
        }

        public static implicit operator ExtendedConsoleColor(int c)
        {
            return new ExtendedConsoleColor(c);
        }
        public static implicit operator int(ExtendedConsoleColor c)
        {
            return ((System.Drawing.Color)c).ToArgb();
        }

        public static implicit operator ExtendedConsoleColor(ConsoleColor c)
        {
            switch (c)
            {
                case ConsoleColor.Black: return Color.Black;
                case ConsoleColor.Blue: return Color.Blue;
                case ConsoleColor.Cyan: return Color.Cyan;
                case ConsoleColor.DarkBlue: return Color.DarkBlue;
                case ConsoleColor.DarkCyan: return Color.DarkCyan;
                case ConsoleColor.DarkGray: return Color.DarkGray;
                case ConsoleColor.DarkGreen: return Color.DarkGreen;
                case ConsoleColor.DarkMagenta: return Color.DarkMagenta;
                case ConsoleColor.DarkRed: return Color.DarkRed;
                case ConsoleColor.DarkYellow: return Color.FromArgb(Color.Yellow.R / 2, Color.Yellow.G / 2, Color.Yellow.B / 2);
                case ConsoleColor.Gray: return Color.Gray;
                case ConsoleColor.Green: return Color.Green;
                case ConsoleColor.Magenta: return Color.Magenta;
                case ConsoleColor.Red: return Color.Red;
                case ConsoleColor.White: return Color.White;
                case ConsoleColor.Yellow: return Color.Yellow;
                default: throw new InvalidCastException();
            }
        }

        public override string ToString()
        {
            return ((Color)this).ToString();
        }
    }

    public sealed class ExtendedConsoleMarker
    {
        public bool Enabled { get; set; }
        public bool EnableToChange { get; set; }
        public int StartX { get; set; }
        public int StartY { get; set; }
        public int StopX { get; set; }
        public int StopY { get; set; }

        public ExtendedConsole Owner { get; private set; }

        public string MarkedText
        {
            get
            {
                var s = "";
                for (int y = Math.Min(StartY, StopY); y<=Math.Max(StartY, StopY); ++y)
                {
                    for (int x = Math.Min(StartX, StopX); x <= Math.Max(StartX, StopX); ++x)
                        s += (Owner.Matrix[x, y].Value < 32 ? ' ' : Owner.Matrix[x, y].Value);
                    if (y < Math.Max(StartY, StopY)) s += '\n';
                }
                return s;
            }
        }

        internal ExtendedConsoleMarker(ExtendedConsole Owner)
        {
            this.Owner = Owner;
            Enabled = EnableToChange = true;
        }
    }
}
