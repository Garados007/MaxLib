using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;

namespace MaxLib.Console.ExtendedConsole.Windows
{
    using Elements;

    public class MainMenu:BasicElement
    {
        private string text = "";
        public string Text
        {
            get { return text; }
            set
            {
                text = value ?? "";
                DoChange();
            }
        }
        public event Action Click;
        internal void OnClick()
        {
            Click?.Invoke();
        }
        public List<MainMenu> Submenu { get; private set; }
        public string Key { get; set; }
        public bool Opened { get; set; }
        public ExtendedConsole Console { get; set; }

        public override void Draw(Out.ClipWriterAsync writer)
        {
            if (!Opened) return;
            base.Draw(writer);
            writer.SetWriterRelPos(X, Y);
            writer.Write(new string(' ', Width), Color.White, Color.Gray);
            for (int i = 0; i < Submenu.Count; ++i)
            {
                writer.SetWriterRelPos(X, Y + i + 1);
                writer.Write((" " + Submenu[i].text + (Submenu[i].Submenu.Count > 0 ? " >" : "")).PadRight(Width, ' '), Color.Black, Color.Gray);
            }
            writer.SetWriterRelPos(X, Y + Submenu.Count + 1);
            writer.Write(new string(' ', Width), Color.White, Color.Gray);
            foreach (var m in Submenu) if (m.Opened) m.Draw(writer);
        }

        public void ComputeClientSize()
        {
            Width = Submenu.Max((m) => m.Text.Length + (m.Submenu.Count > 0 ? 2 : 0)) + 2;
            Height = Submenu.Count + 2;
        }

        public override void OnMouseDown(int x, int y)
        {
            base.OnMouseDown(x, y);
            OnWinMouseDown(x, y);
        }

        public bool OnWinMouseDown(int x, int y)
        {
            if (!Opened) return false;
            var rebuild = false;
            if (x > X && x < X + Width - 1 && y > Y && y < Y + Height - 1)
                rebuild = !Submenu[y - Y - 1].Opened;
            foreach (var m in Submenu) if (m.OnWinMouseDown(x, y)) return true;
            if (x > X && x < X + Width - 1 && y > Y && y < Y + Height - 1)
            {
                var si = y - Y - 1;
                foreach (var m in Submenu) m.Opened = false;
                if (rebuild)
                {
                    Submenu[si].Opened = true;
                    if (Submenu[si].Submenu.Count > 0)
                    {
                        Submenu[si].ComputeClientSize();
                        Submenu[si].X = X + Width + Submenu[si].Width < Console.Matrix.Width ?
                            X + Width :
                            X - Submenu[si].Width < 0 ? 0 : x - Submenu[si].Width;
                        Submenu[si].Y = Y + si + Submenu[si].Height < Console.Matrix.Height ?
                            Y + si :
                            Console.Matrix.Height - Submenu[si].Height;
                        return true;
                    }
                    else
                    {
                        Submenu[si].OnClick();
                        return true;
                    }
                }
            }
            else Opened = false;
            return false;
        }

        public MainMenu(ExtendedConsole Console, string Text, Action click)
        {
            Submenu = new List<MainMenu>();
            this.Console = Console;
            this.Text = Text;
            if (click != null) Click += click;
        }
        public MainMenu(ExtendedConsole Console, string Text)
        {
            Submenu = new List<MainMenu>();
            this.Console = Console;
            this.Text = Text;
        }
        public MainMenu(ExtendedConsole Console, string Text, string Key)
        {
            Submenu = new List<MainMenu>();
            this.Console = Console;
            this.Text = Text;
            this.Key = Key;
        }
        public MainMenu(ExtendedConsole Console, string Text, string Key, Action click)
        {
            Submenu = new List<MainMenu>();
            this.Console = Console;
            this.Text = Text;
            this.Key = Key;
            if (click != null) Click += click;
        }
    }
}
