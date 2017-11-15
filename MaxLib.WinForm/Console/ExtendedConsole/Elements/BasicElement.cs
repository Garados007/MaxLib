using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MaxLib.Console.ExtendedConsole.Elements
{
    public class BasicElement
    {
        public virtual int X { get; set; }
        public virtual int Y { get; set; }
        public virtual int Width { get; set; }
        public virtual int Height { get; set; }
        public virtual bool Visible { get; set; }

        public BasicElement()
        {
            Visible = true;
        }

        public event Action Changed;
        protected void DoChange()
        {
            if (Changed != null) Changed();
        }

        public virtual void Draw(Out.ClipWriterAsync writer)
        {

        }

        public virtual void OnMouseDown(int x, int y) { }
        public virtual void OnMouseUp(int x, int y) { }
        public virtual void OnMouseMove(int x, int y) { }
    }

    public class ElementContainer : ICollection<BasicElement>
    {
        #region ICollection
        List<BasicElement> elements = new List<BasicElement>();

        public void Add(BasicElement item)
        {
            if (item == null) throw new ArgumentNullException("item");
            elements.Add(item);
            item.Changed += this.DoClientChange;
            DoClientChange();
        }

        public void Clear()
        {
            elements.Clear();
            DoClientChange();
        }

        public bool Contains(BasicElement item)
        {
            return elements.Contains(item);
        }

        public void CopyTo(BasicElement[] array, int arrayIndex)
        {
            elements.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return elements.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(BasicElement item)
        {
            var r = elements.Remove(item);
            item.Changed -= this.DoClientChange;
            DoClientChange();
            return r;
        }

        public IEnumerator<BasicElement> GetEnumerator()
        {
            return elements.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return elements.GetEnumerator();
        }

        public BasicElement this[int index]
        {
            get { return elements[index]; }
            set { elements[index] = value; }
        }
        #endregion

        public event Action Changed;

        protected void DoClientChange()
        {
            if (Changed != null) Changed();
        }

        public void DrawElements(Out.ClipWriterAsync writer)
        {
            for (int i = 0; i < Count; ++i) if (this[i].Visible)
            {
                var w = writer.CreatePartialWriter(this[i].X, this[i].Y, this[i].Width, this[i].Height);
                w.BeginWrite();
                this[i].Draw(w);
                w.EndWrite();
            }
        }

        public ElementContainer()
        {
        }

        public virtual void OnMouseDown(int x, int y)
        {
            foreach (var e in elements) if (e.Visible) e.OnMouseDown(x, y);
        }
        public virtual void OnMouseUp(int x, int y)
        {
            foreach (var e in elements) if (e.Visible) e.OnMouseUp(x, y);
        }
        public virtual void OnMouseMove(int x, int y)
        {
            foreach (var e in elements) if (e.Visible) e.OnMouseMove(x, y);
        }
    }

    public class BasicCountainerElement : BasicElement
    {
        public ElementContainer Elements { get; private set; }

        public override void Draw(Out.ClipWriterAsync writer)
        {
            base.Draw(writer);
            Elements.DrawElements(writer);
        }

        public override void OnMouseDown(int x, int y)
        {
            Elements.OnMouseDown(x - X, y - Y);
        }
        public override void OnMouseMove(int x, int y)
        {
            Elements.OnMouseMove(x - X, y - Y);
        }
        public override void OnMouseUp(int x, int y)
        {
            Elements.OnMouseUp(x - X, y - Y);
        }

        public BasicCountainerElement()
        {
            Elements = new ElementContainer();
        }
    }

    #region Buttons

    public class Button : BasicElement
    {
        protected string text = "";
        public virtual string Text
        {
            get { return text; }
            set
            {
                text = value == null ? "" : value;
                Width = Width;
                DoChange();
            }
        }

        public override int Width
        {
            get
            {
                return base.Width;
            }
            set
            {
                base.Width = Math.Max(2 + text.Length, value);
                DoChange();
            }
        }
        public override int Height
        {
            get
            {
                return 3;
            }
        }

        protected bool doubleBounds = false;
        public virtual bool DoubleBounds
        {
            get { return doubleBounds; }
            set
            {
                doubleBounds = value;
                DoChange();
            }
        }

        public virtual ExtendedConsoleColor TextColor { get; set; }
        public virtual ExtendedConsoleColor TextBackground { get; set; }
        public virtual ExtendedConsoleColor BoundColor { get; set; }
        public virtual ExtendedConsoleColor BoundBackground { get; set; }

        public Button()
        {
            TextColor = BoundColor = System.Drawing.Color.White;
            TextBackground = BoundBackground = System.Drawing.Color.Black;
        }
        public Button(string text) : this()
        {
            this.text = text;
        }

        public override void Draw(Out.ClipWriterAsync writer)
        {
            base.Draw(writer);

            ExtendedConsoleColor tc = TextColor, tb = TextBackground, bc = BoundColor, bb = BoundBackground;
            
            writer.SetWriterRelPos(0, 0);
            writer.Write(doubleBounds ? '╔' : '┌', bc, bb);
            writer.Write(new string(doubleBounds ? '═' : '─', Width - 2), bc, bb);
            writer.Write(doubleBounds ? '╗' : '┐', bc, bb);
            writer.SetWriterRelPos(0, 1);
            writer.Write(doubleBounds ? '║' : '|', bc, bb);
            writer.Write(new string(' ', (Width - 2 - text.Length) / 2), tc, tb);
            writer.Write(text, tc, tb);
            writer.Write(new string(' ', (Width - 2 - text.Length) - (Width - 2 - text.Length) / 2), tc, tb);
            writer.Write(doubleBounds ? '║' : '|', bc, bb);
            writer.SetWriterRelPos(0, 2);
            writer.Write(doubleBounds ? '╚' : '└', bc, bb);
            writer.Write(new string(doubleBounds ? '═' : '─', Width - 2), bc, bb);
            writer.Write(doubleBounds ? '╝' : '┘', bc, bb);
        }

        public event Action<Button> Clicked;

        public override void OnMouseDown(int x, int y)
        {
            if (x >= X && x < X + Width && y >= Y && y < Y + Height && Clicked != null) Clicked(this);
        }
    }

    #endregion
}
