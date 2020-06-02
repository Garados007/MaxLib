namespace MaxLib.Console.ExtendedConsole.In
{
    public sealed class Cursor
    {
        public int X { get; internal set; }
        public int Y { get; internal set; }

        public ExtendedConsole Owner { get; private set; }

        public bool Visible
        {
            get { return Owner.Options.ShowMouse; }
            set { Owner.Options.ShowMouse = value; }
        }

        public event CurserChangeEvent Move, Down, Up;
        internal void DoMove()
        {
            Move?.Invoke(this);
            Owner.MainContainer.OnMouseMove(X, Y);
        }
        internal void DoDown()
        {
            Down?.Invoke(this);
            Owner.MainContainer.OnMouseDown(X, Y);
        }
        internal void DoUp()
        {
            Up?.Invoke(this);
            Owner.MainContainer.OnMouseUp(X, Y);
        }

        internal Cursor(ExtendedConsole Owner)
        {
            this.Owner = Owner;
        }
    }

    public delegate void CurserChangeEvent(Cursor cursor);
}
