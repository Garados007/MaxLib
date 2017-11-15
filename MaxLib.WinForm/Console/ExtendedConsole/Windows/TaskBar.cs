using System;

namespace MaxLib.Console.ExtendedConsole.Windows
{
    using Elements;

    public class TaskBar : BasicElement
    {
        public override int Height
        {
            get
            {
                return 2;
            }
        }
        public override int X
        {
            get
            {
                return 0;
            }
        }

        public ExtendedConsoleColor Background { get; set; }
        public ExtendedConsoleColor Foreground { get; set; }
        public ExtendedConsoleColor SplitterColor { get; set; }

        public MainMenu Menu { get; set; }

        public override void Draw(Out.ClipWriterAsync writer)
        {
            base.Draw(writer);
            //ChangeSize
            Y = writer.Owner.Matrix.Height - 2;
            Width = writer.Owner.Matrix.Width;

            writer = writer.ownerWriter.CreatePartialWriter(X, Y, Width, Height);
            writer.BeginWrite();
            //Top-Splitter
            writer.SetWriterRelPos(0, 0);
            var time = " " + DateTime.Now.ToShortTimeString() + " ";
            writer.Write(new string('─', Width - time.Length-1) + "┬" + new string('─', time.Length), SplitterColor, Background);
            writer.SetWriterRelPos(0, 1);
            writer.Write(new string(' ', Width-time.Length-1), Foreground, Background);
            writer.Write('|', SplitterColor, Background);
            writer.Write(time, Foreground, Background);
            //Menu-Label
            if (Menu!=null)
            {
                writer.SetWriterRelPos(0, 1);
                writer.Write(Menu.Text, ConsoleColor.Black, System.Drawing.Color.LightGreen);
            }
            writer.EndWrite();
            //Menu
            if (Menu!=null)
            {
                writer = writer.ownerWriter.CreatePartialWriter(0, 0, Width, writer.Owner.Matrix.Height);
                writer.BeginWrite();
                Menu.Draw(writer);
                writer.EndWrite();
            }
        }

        public override void OnMouseDown(int x, int y)
        {
            base.OnMouseDown(x, y);
            if (Menu != null)
            {
                if (!Menu.OnWinMouseDown(x, y))
                {
                    if (x < Menu.Text.Length && y == Y + 1)
                    {
                        Menu.Opened = !Menu.Opened;
                        if (Menu.Opened)
                        {
                            Menu.ComputeClientSize();
                            Menu.X = 0;
                            Menu.Y = Y - Menu.Height + 1;
                        }
                    }
                }
            }
            DoChange();
        }

        public TaskBar()
        {
            Background = System.Drawing.Color.Blue;
            SplitterColor = System.Drawing.Color.Gray;
            Foreground = System.Drawing.Color.Black;
        }
    }
}
