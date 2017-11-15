using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Collections;

namespace MaxLib.Collections
{
    public class LineGridContainer<T> : IEnumerable<GridLineEntry<T>>
    {
        internal Dictionary<T, GridLineEntry<T>> elements = new Dictionary<T, GridLineEntry<T>>();
        public KeyValuePair<T, GridLineEntry<T>>[] Elements => elements.ToArray();

        public int Count => elements.Count;

        public IEnumerator<GridLineEntry<T>> GetEnumerator()
        {
            foreach (var e in elements)
                yield return e.Value;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            foreach (var e in elements)
                yield return e.Value;
        }

        internal SortedList<float, GridLine<T>> horzLines = new SortedList<float, GridLine<T>>(); //-
        internal SortedList<float, GridLine<T>> vertLines = new SortedList<float, GridLine<T>>(); //|

        public GridLine<T>[] Rows
        {
            get
            {
                var r = new GridLine<T>[horzLines.Count];
                int i = 0;
                foreach (var h in horzLines)
                    r[i++] = h.Value;
                return r;
            }
        }
        public int RowCount => horzLines.Count;

        public GridLine<T>[] Columns
        {
            get
            {
                var c = new GridLine<T>[vertLines.Count];
                int i = 0;
                foreach (var v in vertLines)
                    c[i++] = v.Value;
                return c;
            }
        }
        public int ColumnCount => vertLines.Count;

        public float GetLineOffset(float key, bool vertical)
        {
            return (vertical ? vertLines : horzLines)[key].Offset;
        }

        public void Add(T element, PointF location, SizeF size)
        {
            var entry = new GridLineEntry<T>(element, location, size);
            elements.Add(element, entry);
            Attach(horzLines, entry, false);
            Attach(vertLines, entry, true);
        }

        public bool Remove(T element)
        {
            if (elements.ContainsKey(element))
            {
                var entry = elements[element];
                Detach(horzLines, entry, false);
                Detach(vertLines, entry, true);
                elements.Remove(element);
                return true;
            }
            else return false;
        }

        public void MoveTo(T element, PointF location)
        {
            if (!elements.ContainsKey(element)) return;
            var entry = elements[element];
            Detach(horzLines, entry, false);
            Detach(vertLines, entry, true);
            entry.Location = location;
            Attach(horzLines, entry, false);
            Attach(vertLines, entry, true);
        }

        public void ChangeSize(T element, SizeF size)
        {
            if (elements.ContainsKey(element))
            {
                var entry = elements[element];
                entry.Size = size;
                horzLines[entry.Location.Y].UpdateSizeConstrains();
                vertLines[entry.Location.X].UpdateSizeConstrains();
            }
        }

        public bool Contains(T element)
        {
            return elements.ContainsKey(element);
        }

        void Attach(SortedList<float, GridLine<T>> lines, GridLineEntry<T> entry, bool vertical)
        {
            var key = vertical ? entry.Location.X : entry.Location.Y;
            if (lines.ContainsKey(key))
            {
                var line = lines[key];
                line.elements.Add(entry);
                line.UpdateSizeConstrains();
                if (vertical) entry.OffsetX = line.Offset;
                else entry.OffsetY = line.Offset;
            }
            else
            {
                var line = new GridLine<T>(vertical);
                line.elements.Add(entry);
                line.Key = key;
                line.UpdateSizeConstrains();
                if (vertical) entry.OffsetX = line.Offset;
                else entry.OffsetY = line.Offset;
                lines.Add(key, line);
            }
        }

        void Detach(SortedList<float, GridLine<T>> lines, GridLineEntry<T> entry, bool vertical)
        {
            var key = vertical ? entry.Location.X : entry.Location.Y;
            if (lines.ContainsKey(key))
            {
                var line = lines[key];
                line.elements.Remove(entry);
                line.UpdateSizeConstrains();
                if (line.Count == 0)
                    lines.Remove(key);
            }
        }
    }

    public class GridLine<T> : IEnumerable<GridLineEntry<T>>
    {
        internal List<GridLineEntry<T>> elements = new List<GridLineEntry<T>>();
        public GridLineEntry<T>[] Elements => elements.ToArray();

        public int Count => elements.Count;

        public float Key { get; internal set; }

        float offset;
        public float Offset
        {
            get => offset;
            internal set
            {
                offset = value;
                foreach (var e in elements)
                    if (Vertical) e.OffsetX = value;
                    else e.OffsetY = value;
            }
        }

        public SizeF MinSize { get; private set; }

        public SizeF MaxSize { get; private set; }

        public bool Vertical { get; private set; }

        internal void UpdateSizeConstrains()
        {
            if (elements.Count == 0)
            {
                MinSize = MaxSize = new SizeF();
            }
            else
            {
                float minw, maxw, minh, maxh;
                minw = maxw = elements[0].Size.Width;
                minh = maxh = elements[0].Size.Height;
                for (int i = 1; i<elements.Count; ++i)
                {
                    var s = elements[i].Size;
                    if (minw > s.Width) minw = s.Width;
                    if (maxw < s.Width) maxw = s.Width;
                    if (minh > s.Height) minh = s.Height;
                    if (maxh < s.Height) maxh = s.Height;
                }
                MinSize = new SizeF(minw, minh);
                MaxSize = new SizeF(maxw, maxh);
            }
        }

        public IEnumerator<GridLineEntry<T>> GetEnumerator()
        {
            return elements.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return elements.GetEnumerator();
        }

        public GridLine(bool vertical)
        {
            Vertical = vertical;
        }
    }

    public class GridLineEntry<T>
    {
        public T Element { get; private set; }

        public PointF Location { get; internal set; }

        public SizeF Size { get; internal set; }

        public float OffsetX { get; internal set; }

        public float OffsetY { get; internal set; }

        public GridLineEntry(T element, PointF location, SizeF size)
        {
            Element = element;
            Location = location;
            Size = size;
        }
    }
}
