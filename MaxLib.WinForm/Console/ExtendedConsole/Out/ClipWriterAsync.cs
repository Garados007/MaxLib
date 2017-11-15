using System;

namespace MaxLib.Console.ExtendedConsole.Out
{
    public class ClipWriterAsync
    {
        public ExtendedConsole Owner { get; private set; }

        public int X { get; private set; }
        public int Y { get; private set; }
        public int Height { get; private set; }
        public int Width { get; private set; }
        public int AbsX
        {
            get
            {
                int v = X;
                if (ownerWriter != null) v += ownerWriter.AbsX;
                return v;
            }
        }
        public int AbsY
        {
            get
            {
                int v = Y;
                if (ownerWriter != null) v += ownerWriter.AbsY;
                return v;
            }
        }

        public ClipWriterAsync(ExtendedConsole Owner, int x, int y, int width, int height)
        {
            this.Owner = Owner;
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public ClipWriterAsync(ExtendedConsole Owner) : this(Owner, 0, 0, Owner.Matrix.Width, Owner.Matrix.Height) { }

        ExtendedConsoleCellMatrix matrix = null;

        internal ClipWriterAsync ownerWriter = null;

        internal void FlushBaseWriter(ClipWriterAsync writer)
        {
            for (int x = writer.X; x < writer.X + writer.Width; ++x) for (int y = writer.Y; y < writer.Y + writer.Height; ++y)
                    matrix[x, y].CopyData(writer.matrix[x, y]);
        }

        public ClipWriterAsync CreatePartialWriter(int offsetX, int offsetY, int width, int height)
        {
            if (offsetX < 0 || offsetX >= Width) throw new ArgumentOutOfRangeException("offsetX");
            if (offsetY < 0 || offsetY >= Height) throw new ArgumentOutOfRangeException("offsetY");
            if (width < 0 || offsetX + width > Width) throw new ArgumentOutOfRangeException("width");
            if (height < 0 || offsetY + height > Height) throw new ArgumentOutOfRangeException("height");
            return new ClipWriterAsync(Owner, X + offsetX, Y + offsetY, width, height) { ownerWriter = this };
        }

        public void BeginWrite()
        {
            matrix = new ExtendedConsoleCellMatrix();
            matrix.SetSize(Owner.Matrix.Width, Owner.Matrix.Height);
        }

        public void EndWrite()
        {
            if (matrix == null) return;
            if (ownerWriter != null) ownerWriter.FlushBaseWriter(this);
            else
            {
                Owner.BeginEdit();
                Owner.Matrix.CopyData(matrix);
                Owner.EndEdit();
            }
            matrix = null;
        }

        public void Clear()
        {
            foreach (var s in matrix.matrix) foreach (var c in s)
                {
                    c.Background = Owner.Options.Background;
                    c.Foreground = Owner.Options.Foreground;
                    c.Value = (char)32;
                }
        }

        public void MakeEmpty()
        {
            foreach (var s in matrix.matrix) foreach (var c in s)
                {
                    c.Background = Owner.Options.Background;
                    c.Foreground = Owner.Options.Foreground;
                    c.Value = (char)0;
                }
        }

        public int WriterLeft { get; private set; }
        public int WriterTop { get; private set; }
        public void SetWriterAbsPos(int x, int y)
        {
            if (x < AbsX || x >= AbsX + Width) throw new ArgumentOutOfRangeException("x");
            if (y < AbsY || y >= AbsY + Height) throw new ArgumentOutOfRangeException("y");
            WriterLeft = x;
            WriterTop = y;
        }
        public void SetWriterRelPos(int x, int y)
        {
            SetWriterAbsPos(x + AbsX, y + AbsY);
        }

        public void Write<T>(T text)
        {
            var s = text.ToString().ToCharArray();
            if (matrix == null) throw new InvalidOperationException("You must cannot write now. You must initial with BeginWrite() first!");
            var m = matrix;
            for (int i = 0; i<s.Length; ++i)
            {
                var c = m[WriterLeft, WriterTop];
                c.Background = Owner.Options.Background;
                c.Foreground = Owner.Options.Foreground;
                c.Value = s[i];
                WriterLeft++;
                if (WriterLeft >= X + Width)
                {
                    WriterLeft = X;
                    WriterTop++;
                    if (WriterTop >= Y + Height)
                    {
                        WriterTop = Y;
                    }
                }
            }
        }

        public void Write<T>(T text, ExtendedConsoleColor Foreground, ExtendedConsoleColor Background)
        {
            var s = text.ToString().ToCharArray();
            if (matrix == null) throw new InvalidOperationException("You must cannot write now. You must initial with BeginWrite() first!");
            var m = matrix;
            for (int i = 0; i < s.Length; ++i)
            {
                var c = m[WriterLeft, WriterTop];
                c.Background = Background;
                c.Foreground = Foreground;
                c.Value = s[i];
                WriterLeft++;
                if (WriterLeft >= X + Width)
                {
                    WriterLeft = X;
                    WriterTop++;
                    if (WriterTop >= Y + Height)
                    {
                        WriterTop = Y;
                    }
                }
            }
        }
    }
}
