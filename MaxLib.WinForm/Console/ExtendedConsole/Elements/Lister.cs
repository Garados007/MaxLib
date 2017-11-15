using System;
using System.Collections.Generic;

namespace MaxLib.Console.ExtendedConsole.Elements
{
    using Out;
    using In;

    public class Lister
    {
        public int Top, Left, Width, Height;
        public EasyWriterAsync writer;
        public List<object> Elements = new List<object>();
        public int ListOffset = 0;
        public int SelectedIndex = 0;
        public ConsoleColor Background = ConsoleColor.Black, Text = ConsoleColor.White,
            BackgroundSel = ConsoleColor.White, TextSel = ConsoleColor.Black;
        public ConsoleColor BarBackground = ConsoleColor.Black, BarColor = ConsoleColor.White;

        public Keys KeyUp = Keys.Up, KeyDown = Keys.Down, KeyEnter = Keys.Enter;

        public int UpdateIntervall = 200;

        public Lister(EasyWriterAsync writer, int left, int top, int width, int height, params object[] initialElements)
        {
            Left = left; Top = top; Width = width; Height = height;
            Elements.AddRange(initialElements);
            this.writer = writer;
        }

        public event Action Cancel;
        KeyWatcher ke;

        public void Start()
        {
            ke = new KeyWatcher(writer.Owner.KeyManager, KeyUp, KeyDown, KeyEnter);
            ke.KeyDown += (key) =>
                {
                    if (key == KeyUp) { SelectedIndex = Math.Max(0, SelectedIndex - 1); Render(); return true; }
                    if (key == KeyDown) { SelectedIndex = Math.Min(Math.Max(Elements.Count - 1, 0), SelectedIndex + 1); Render(); return true; }
                    if (key == KeyEnter) { if (Cancel != null) Cancel(); Render(); return true; }
                    Render();
                    return false;
                };
            writer.Owner.Cursor.Move += Cursor_Move;
            writer.Owner.Cursor.Down += Cursor_Down;
            Render();
        }

        void Cursor_Down(Cursor cursor)
        {
            Cursor_Move(cursor);
            if (cursor.X >= Left && cursor.Y >= Top && cursor.X < Left + Width - 2 && cursor.Y <= Top + Height)
            {
                if (((cursor.Y - Top) + ListOffset) < Elements.Count)
                {
                    if (Cancel != null) Cancel();
                    Render();
                }
            }
        }

        void Cursor_Move(Cursor cursor)
        {
            if (cursor.X >= Left && cursor.Y >= Top && cursor.X < Left + Width && cursor.Y <= Top + Height)
            {
                SelectedIndex = Math.Min(Elements.Count - 1, (cursor.Y - Top) + ListOffset);
                Render();
            }
        }
        public void Stop()
        {
            ke.Unbind();
            writer.Owner.Cursor.Move -= Cursor_Move;
            writer.Owner.Cursor.Down -= Cursor_Down;
        }

        void Focus()
        {
            if (SelectedIndex < Height / 2) ListOffset = 0;
            else if (SelectedIndex > Elements.Count - Height / 2) ListOffset = Elements.Count - Height;
            else ListOffset = SelectedIndex - Height / 2;
        }

        void Render()
        {
            writer.BeginWrite();
            writer.Clear();
            //Text
            for (int i = 0; i < Elements.Count; ++i) if (i >= ListOffset && i < ListOffset + Height)
                {
                    var h = i - ListOffset;
                    var s = Elements[i].ToString();
                    if (s.Length > Width - 2) s = s.Remove(Width - 2);
                    s = s.PadRight(Width - 2, ' ');
                    var sel = i == SelectedIndex;
                    writer.SetWriterPos(Left, Top + i - ListOffset);
                    writer.Write(s, sel ? TextSel : Text, sel ? BackgroundSel : Background);
                }
            //Bar
            for (var i = 0; i < Height; ++i)
            {
                writer.SetWriterPos(Left + Width - 1, Top + i);
                writer.Write(" ", ConsoleColor.White, BarBackground);
            }
            if (ListOffset >= 0 && ListOffset < Elements.Count - Height)
            {
                var off = Height * ListOffset / Elements.Count;
                var h = Height * Height / Elements.Count;
                for (int i = off; i < off + h; ++i)
                {
                    writer.SetWriterPos(Left + Width - 1, Top + i);
                    writer.Write(" ", ConsoleColor.White, BarColor);
                }
            }
            writer.EndWrite();
        }
    }
}
