namespace MaxLib.Console.ExtendedConsole.Windows
{
    public class MainTargetWindow : Elements.BasicCountainerElement
    {
        public ImageViewer Background { get; private set; }

        public Forms.FormsContainer Forms { get; private set; }

        public override void Draw(Out.ClipWriterAsync writer)
        {
            Background.Width = Width = writer.Owner.Matrix.Width;
            Background.Height = Height = writer.Owner.Matrix.Height - 2;

            writer = writer.ownerWriter.CreatePartialWriter(0, 0, Width, Height);
            writer.BeginWrite();
            Background.Draw(writer);
            base.Draw(writer);
            Forms.Draw(writer);
            writer.EndWrite();
        }
        public override void OnMouseDown(int x, int y)
        {
            base.OnMouseDown(x, y);
            Forms.MouseDown(x, y);
        }
        public override void OnMouseMove(int x, int y)
        {
            base.OnMouseMove(x, y);
            Forms.MouseMove(x, y);
        }
        public override void OnMouseUp(int x, int y)
        {
            base.OnMouseUp(x, y);
            Forms.MouseUp(x, y);
        }

        public MainTargetWindow(ImageViewer Background)
        {
            this.Background = Background;
            Background.Changed += DoChange;
            Forms = new Forms.FormsContainer();
        }
    }
}
