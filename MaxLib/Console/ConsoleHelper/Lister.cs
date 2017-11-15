using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace MaxLib.Console.ConsoleHelper
{
    public class Lister
    {
        public int Top, Left, Width, Height;
        public ConsoleHelper ConsoleHelper;
        public List<object> Elements = new List<object>();
        public int ListOffset = 0;
        public int SelectedIndex=0;
        public ConsoleColor Background = ConsoleColor.Black, Text = ConsoleColor.White, 
            BackgroundSel = ConsoleColor.White, TextSel = ConsoleColor.Black;
        public ConsoleColor BarBackground = ConsoleColor.Black, BarColor = ConsoleColor.White;

        public ConsoleKey KeyUp = ConsoleKey.UpArrow, KeyDown = ConsoleKey.DownArrow, KeyEnter = ConsoleKey.Enter;

        public int UpdateIntervall = 200;

        public Lister(ConsoleHelper helper, int left, int top, int width, int height, params object[] initialElements)
        {
            ConsoleHelper = helper;
            Left = left; Top = top; Width = width; Height = height;
            Elements.AddRange(initialElements);
            writer = new ConsoleWriterAsync(helper);
        }

        ConsoleWriterAsync writer;

        public void Start()
        {
            while (true)
            {
                Render();
                var k = System.Console.ReadKey().Key;
                if (k == KeyUp) SelectedIndex = Math.Max(0, SelectedIndex - 1);
                if (k == KeyDown) SelectedIndex = Math.Min(Math.Max(Elements.Count-1, 0), SelectedIndex + 1);
                if (k == KeyEnter) break;
                Focus();
            }
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
            writer.Clear(Left, Top, Width, Height);
            //Text
            for (int i = 0; i<Elements.Count; ++i) if (i>=ListOffset&&i<ListOffset+Height)
                {
                    var h = i - ListOffset;
                    var s = Elements[i].ToString();
                    if (s.Length > Width - 2) s = s.Remove(Width - 2);
                    s = s.PadRight(Width - 2, ' ');
                    var sel = i == SelectedIndex;
                    writer.SetCursorPos(Left, Top + i - ListOffset);
                    writer.Write(s, sel ? TextSel : Text, sel ? BackgroundSel : Background);
                }
            //Bar
            for (var i = 0; i<Height; ++i)
            {
                writer.SetCursorPos(Left + Width - 1, Top + i);
                writer.Write(" ", ConsoleColor.White, BarBackground);
            }
            if (ListOffset >= 0 && ListOffset < Elements.Count - Height)
            {
                var off = Height * ListOffset / Elements.Count;
                var h = Height * Height / Elements.Count;
                for (int i = off; i<off+h; ++i)
                {
                    writer.SetCursorPos(Left + Width - 1, Top + i);
                    writer.Write(" ", ConsoleColor.White, BarColor);
                }
            }
            writer.EndWrite();
        }
    }
}
