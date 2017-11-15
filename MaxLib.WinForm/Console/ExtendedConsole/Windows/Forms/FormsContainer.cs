using System;
using System.Collections.Generic;

namespace MaxLib.Console.ExtendedConsole.Windows.Forms
{
    public sealed class FormsContainer : ICollection<Form>
    {
        List<Form> forms = new List<Form>();
        public event Action Change;
        void DoChange()
        {
            if (Change != null) Change();
        }

        public Form FocusedForm { get { return forms.Count > 0 ? forms[0] : null; } }

        public void Add(Form item)
        {
            item.Changed += DoChange;
            item.Focus += Focus;
            item.Close += Close;
            forms.Insert(0, item);
        }

        public void Clear()
        {
            forms.Clear();
        }

        public bool Contains(Form item)
        {
            return forms.Contains(item);
        }

        public void CopyTo(Form[] array, int arrayIndex)
        {
            forms.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return forms.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(Form item)
        {
            item.Changed -= DoChange;
            item.Focus -= Focus;
            item.Close -= Close;
            return forms.Remove(item);
        }

        public IEnumerator<Form> GetEnumerator()
        {
            return forms.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return forms.GetEnumerator();
        }

        internal FormsContainer()
        {

        }

        public Form this[int index]
        {
            get { return forms[index]; }
            set { forms[index] = value; }
        }

        void Focus(Form form)
        {
            forms.Remove(form);
            forms.Insert(0, form);
        }
        void Close(Form form)
        {
            Remove(form);
        }

        public void Draw(Out.ClipWriterAsync writer)
        {
            for (int i = Count-1; i>=0; --i)
            {
                var w = writer.CreatePartialWriter(this[i].X, this[i].Y, this[i].Width, this[i].Height);
                w.BeginWrite();
                this[i].Draw(w);
                w.EndWrite();
            }
        }

        public void MouseDown(int x, int y)
        {
            for (int i = 0; i < Count; ++i)
            {
                if (x >= this[i].X && x < this[i].X + this[i].Width && y >= this[i].Y && y < this[i].Y + this[i].Height)
                {
                    Focus(this[i]);
                    this[i].OnMouseDown(x, y);
                    return;
                }
            }
        }
        public void MouseUp(int x, int y)
        {
            for (int i = 0; i < Count; ++i)
            {
                if (x >= this[i].X && x < this[i].X + this[i].Width && y >= this[i].Y && y < this[i].Y + this[i].Height)
                {
                    Focus(this[i]);
                    this[i].OnMouseUp(x, y);
                    return;
                }
            }
        }
        public void MouseMove(int x, int y)
        {
            for (int i = 0; i < Count; ++i)
            {
                if (x >= this[i].X && x < this[i].X + this[i].Width && y >= this[i].Y && y < this[i].Y + this[i].Height)
                {
                    this[i].OnMouseMove(x, y);
                    return;
                }
                else if (i == 0 && this[i].Moving)
                {
                    this[i].OnMouseMove(x, y);
                    return;
                }
            }
        }
    }
}
