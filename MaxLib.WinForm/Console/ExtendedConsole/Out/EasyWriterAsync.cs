using System;

namespace MaxLib.Console.ExtendedConsole.Out
{
    public class EasyWriterAsync
    {
        public ExtendedConsole Owner { get; private set; }

        public EasyWriterAsync(ExtendedConsole Owner)
        {
            this.Owner = Owner;
        }

        ExtendedConsoleCellMatrix matrix = null;

        public void BeginWrite()
        {
            matrix = new ExtendedConsoleCellMatrix();
            matrix.SetSize(Owner.Matrix.Width, Owner.Matrix.Height);
        }

        public void EndWrite()
        {
            if (matrix == null) return;
            Owner.BeginEdit();
            Owner.Matrix.CopyData(matrix);
            Owner.EndEdit();
            Owner.Flush();
        }

        public void Clear()
        {
            var m = matrix == null ? Owner.Matrix : matrix;
            foreach (var s in m.matrix) foreach (var c in s)
                {
                    c.Background = Owner.Options.Background;
                    c.Foreground = Owner.Options.Foreground;
                    c.Value = (char)32;
                }
        }

        public void MakeEmpty()
        {
            var m = matrix == null ? Owner.Matrix : matrix;
            foreach (var s in m.matrix) foreach (var c in s)
                {
                    c.Background = Owner.Options.Background;
                    c.Foreground = Owner.Options.Foreground;
                    c.Value = (char)0;
                }
        }

        public int WriterLeft { get; private set; }
        public int WriterTop { get; private set; }
        public void SetWriterPos(int x, int y)
        {
            if (x<0||x>=Owner.Matrix.Width) throw new ArgumentOutOfRangeException("x");
            if (y < 0 || y >= Owner.Matrix.Height) throw new ArgumentOutOfRangeException("y");
            WriterLeft = x;
            WriterTop = y;
        }

        public void Write<T>(T text)
        {
            var s = text.ToString().ToCharArray();
            var m = matrix == null ? Owner.Matrix : matrix;
            for (int i = 0; i<s.Length; ++i)
            {
                var c = m[WriterLeft, WriterTop];
                c.Background = Owner.Options.Background;
                c.Foreground = Owner.Options.Foreground;
                c.Value = s[i];
                WriterLeft++;
                if (WriterLeft>=m.Width)
                {
                    WriterLeft = 0;
                    WriterTop++;
                    if (WriterTop>=m.Height)
                    {
                        WriterTop = 0;
                    }
                }
            }
        }

        public void Write<T>(T text, ExtendedConsoleColor Foreground, ExtendedConsoleColor Background)
        {
            var s = text.ToString().ToCharArray();
            var m = matrix == null ? Owner.Matrix : matrix;
            for (int i = 0; i < s.Length; ++i)
            {
                var c = m[WriterLeft, WriterTop];
                c.Background = Background;
                c.Foreground = Foreground;
                c.Value = s[i];
                WriterLeft++;
                if (WriterLeft >= m.Width)
                {
                    WriterLeft = 0;
                    WriterTop++;
                    if (WriterTop >= m.Height)
                    {
                        WriterTop = 0;
                    }
                }
            }
        }
    }
}
