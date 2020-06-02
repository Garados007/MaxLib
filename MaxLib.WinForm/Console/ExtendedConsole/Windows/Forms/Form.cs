using System;

namespace MaxLib.Console.ExtendedConsole.Windows.Forms
{
    using Elements;

    public class Form :BasicElement
    {
        public ElementContainer Container { get; private set; }

        public event Action<Form> Focus;
        public void DoFocus()
        {
            Focus?.Invoke(this);
        }

        public event Action<Form> Close;
        public void DoClose()
        {
            Close?.Invoke(this);
        }

        string text = "";
        public string Text
        {
            get { return text; }
            set
            {
                text = value ?? "";
                DoChange();
            }
        }

        string baseString = "";
        public string BaseString
        {
            get { return baseString; }
            set
            {
                baseString = value ?? "";
                DoChange();
            }
        }

        ExtendedConsoleColor backgroundColor = System.Drawing.Color.LightBlue;
        public ExtendedConsoleColor BackgroundColor
        {
            get { return backgroundColor; }
            set
            {
                backgroundColor = value;
                DoChange();
            }
        }
        ExtendedConsoleColor contentColor = System.Drawing.Color.LightGray;
        public ExtendedConsoleColor ContentColor
        {
            get { return contentColor; }
            set
            {
                contentColor = value;
                DoChange();
            }
        }

        public Form()
        {
            Container = new ElementContainer();
            Container.Changed += DoChange;
        }

        public override void Draw(Out.ClipWriterAsync writer)
        {
            base.Draw(writer);
            writer.SetWriterRelPos(0, 0);
            var s = Text.PadRight(Width - 2, ' ');
            if (s.Length>Width-2) s = s.Remove(Width - 2);
            writer.Write(s, ConsoleColor.Black, BackgroundColor);
            writer.Write("X", ConsoleColor.White, ConsoleColor.DarkRed);
            writer.Write(" ", ConsoleColor.White, BackgroundColor);
            for (int i = 1; i<Height-1; ++i)
            {
                writer.SetWriterRelPos(0, i);
                writer.Write(" ", ConsoleColor.White, BackgroundColor);
                writer.SetWriterRelPos(Width - 1, i);
                writer.Write(" ", ConsoleColor.White, BackgroundColor);
            }
            writer.SetWriterRelPos(0, Height - 1);
            s = baseString.PadRight(Width, ' ');
            if (s.Length>Width) s = s.Remove(Width);
            writer.Write(s, ConsoleColor.Black, BackgroundColor);
            for (int i = 1; i<Height-1; ++i)
            {
                writer.SetWriterRelPos(1, i);
                writer.Write(new string(' ', Width - 2), ConsoleColor.White, ContentColor);
            }
            var w = writer.CreatePartialWriter(1, 1, Width - 2, Height - 2);
            w.BeginWrite();
            Container.DrawElements(w);
            w.EndWrite();
        }

        public bool Moving { get; private set; }
        int moveX, moveY;

        public override void OnMouseDown(int x, int y)
        {
            base.OnMouseDown(x, y);
            if (y==Y&&x==X+Width-2)
            {
                DoClose();
                return;
            }
            if (y == Y)
            {
                Moving = true;
                moveX = x; moveY = y;
                return;
            }
        }
        public override void OnMouseUp(int x, int y)
        {
            base.OnMouseUp(x, y);
            Moving = false;
        }
        public override void OnMouseMove(int x, int y)
        {
            base.OnMouseMove(x, y);
            if (Moving)
            {
                var dx = x - moveX;
                var dy = y - moveY;
                X += dx;
                Y += dy;
                moveX = x; moveY = y;
                DoChange();
            }
        }
    }
}
