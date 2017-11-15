using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;
using System.Collections;
using MaxLib.WinForms;

namespace MaxLib.Collections
{
    public class BoxGridViewContainer : IEnumerable<BoxGridElement>
    {
        #region Collection

        LineGridContainer<BoxGridElement> list = new LineGridContainer<BoxGridElement>();

        public IEnumerator<BoxGridElement> GetEnumerator()
        {
            foreach (var e in list)
                yield return e.Element;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            foreach (var e in list)
                yield return e.Element;
        }

        protected BoxGridView Parent { get; private set; }
        
        public BoxGridViewContainer(BoxGridView parent)
        {
            Parent = parent;
        }

        public virtual void Add(BoxGridElement box)
        {
            AddInternal(box);
            CalculateOffsets();
            RaisePaintSettingsUpdated();
        }

        public virtual void AddRange(IEnumerable<BoxGridElement> boxes)
        {
            foreach (var box in boxes)
                AddInternal(box);
            CalculateOffsets();
            RaisePaintSettingsUpdated();
        }

        protected virtual void AddInternal(BoxGridElement box)
        {
            if (list.Contains(box)) throw new ArgumentException("already contains box");
            list.Add(box, box.Location, box.Size);
            box.OnLocationChanged += Box_OnLocationChanged;
            box.OnSizeChanged += Box_OnSizeChanged;
        }

        public virtual bool Remove(BoxGridElement box)
        {
            box.OnLocationChanged -= Box_OnLocationChanged;
            box.OnSizeChanged -= Box_OnSizeChanged;
            if (list.Remove(box))
            {
                CalculateOffsets();
                RaisePaintSettingsUpdated();
                return true;
            }
            else return false;
        }

        private void Box_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            var box = (BoxGridElement)sender;
            list.ChangeSize(box, box.Size);
            CalculateOffsets();
            RaisePaintSettingsUpdated();
        }

        private void Box_OnLocationChanged(object sender, LocationChangedEventArgs e)
        {
            var box = (BoxGridElement)sender;
            list.MoveTo(box, box.Location);
            CalculateOffsets();
            RaisePaintSettingsUpdated();
        }

        #endregion

        float maxDistance = 100;
        public float MaxDistance
        {
            get => maxDistance;
            set
            {
                if (value <= 0) throw new ArgumentOutOfRangeException("MaxDistance");
                maxDistance = value;
                if (minDistance > value) minDistance = value;
                CalculateOffsets();
                RaisePaintSettingsUpdated();
            }
        }

        float minDistance = 0;
        public float MinDistance
        {
            get => minDistance;
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException("MinDistance");
                minDistance = value;
                if (maxDistance < value) maxDistance = value;
                CalculateOffsets();
                RaisePaintSettingsUpdated();
            }
        }
        bool overlapRows = false;
        bool overlapCols = false;
        public bool OverlapRows
        {
            get => overlapRows;
            set
            {
                if (overlapRows = value) overlapCols = false;
                CalculateOffsets();
                RaisePaintSettingsUpdated();
            }
        }
        public bool OverlapCols
        {
            get => overlapCols;
            set
            {
                if (overlapCols = value) overlapRows = false;
                CalculateOffsets();
                RaisePaintSettingsUpdated();
            }
        }
        
        public SizeF GridSize { get; private set; }
        public event EventHandler OnGridSizeChanged;

        protected void CalculateOffsets()
        {
            float w, h;
            if (overlapRows)
            {
                w = CalculateOffsets(list.Columns, false);
                h = CalculateOffsets(list.Rows, true);
            }
            else
            {
                h = CalculateOffsets(list.Rows, false);
                w = CalculateOffsets(list.Columns, overlapCols);
            }
            GridSize = new SizeF(w, h);
            OnGridSizeChanged?.Invoke(this, EventArgs.Empty);
        }

        protected virtual float CalculateOffsets(GridLine<BoxGridElement>[] lines, bool overlap)
        {
            float offset = 0;
            GridLine<BoxGridElement> last = null;
            var om = new OverlapModel<BoxGridElement>();
            float lastSpace = 0;
            foreach (var line in lines)
            {
                var d = last == null ? 0 : line.Key - last.Key;
                if (d < minDistance) d = minDistance;
                if (d > maxDistance) d = maxDistance;
                if (overlap)
                {
                    foreach (var entry in line.Elements)
                    {
                        var eo = list.GetLineOffset(line.Vertical ? entry.Location.Y : entry.Location.X, !line.Vertical);
                        om.Add(line.Vertical ?
                            new OverlapEntry<BoxGridElement>(entry.Element, eo, entry.Size.Height, entry.Size.Width) :
                            new OverlapEntry<BoxGridElement>(entry.Element, eo, entry.Size.Width, entry.Size.Height));
                    }
                    var dist = om.GetSaveDistance(d) + d;
                    if (last != null && dist > d) dist += minDistance;
                    om.Merge(dist);
                    line.Offset = (offset += dist);
                }
                else
                {
                    d = Math.Max(minDistance, d - lastSpace);
                    line.Offset = offset + d;
                    lastSpace = line.Vertical ? line.MaxSize.Width : line.MaxSize.Height;
                    offset += d + lastSpace;
                }
                last = line;
            }
            return offset + om.LeftSpace;
        }


        public virtual void OnPaint(PaintEventArgs e)
        {
            foreach (var box in list)
            {
                box.Element.OnPaint(new GridPaintEventArgs(
                    e.Graphics, e.ClipRectangle, new PointF(box.OffsetX, box.OffsetY), Parent));
            }
        }

        public virtual void OnMouseDown(ExtendedMouseEventArgs e)
        {
            foreach (var box in list)
            {
                var x = e.X - box.OffsetX;
                var y = e.Y - box.OffsetY;
                if (x >= 0 && y >= 0 && x <= box.Size.Width && y <= box.Size.Height)
                {
                    var ex = new ExtendedMouseEventArgs(e.Button, e.Clicks, x, y, e.Delta);
                    box.Element.OnMouseDown(ex);
                    e.Handled = ex.Handled;
                }
            }
        }

        public virtual void OnMouseUp(ExtendedMouseEventArgs e)
        {
            foreach (var box in list)
            {
                var x = e.X - box.OffsetX;
                var y = e.Y - box.OffsetY;
                if (x >= 0 && y >= 0 && x <= box.Size.Width && y <= box.Size.Height)
                {
                    var ex = new ExtendedMouseEventArgs(e.Button, e.Clicks, x, y, e.Delta);
                    box.Element.OnMouseUp(ex);
                    e.Handled = ex.Handled;
                }
            }
        }

        public virtual void OnMouseMove(ExtendedMouseEventArgs e)
        {
            foreach (var box in list)
            {
                var x = e.X - box.OffsetX;
                var y = e.Y - box.OffsetY;
                if (x >= 0 && y >= 0 && x <= box.Size.Width && y <= box.Size.Height)
                {
                    var ex = new ExtendedMouseEventArgs(e.Button, e.Clicks, x, y, e.Delta);
                    box.Element.OnMouseMove(ex);
                    e.Handled = ex.Handled;
                }
            }
        }
        
        public virtual void OnMouseWheel(ExtendedMouseEventArgs e)
        {
            foreach (var box in list)
            {
                var x = e.X - box.OffsetX;
                var y = e.Y - box.OffsetY;
                if (x >= 0 && y >= 0 && x <= box.Size.Width && y <= box.Size.Height)
                {
                    var ex = new ExtendedMouseEventArgs(e.Button, e.Clicks, x, y, e.Delta);
                    box.Element.OnMouseWheel(ex);
                    e.Handled = ex.Handled;
                }
            }
        }

        public event EventHandler PaintSettingsUpdated;
        protected void RaisePaintSettingsUpdated()
        {
            PaintSettingsUpdated?.Invoke(this, EventArgs.Empty);
        }
    }


    public class BoxGridElement
    {
        PointF location;
        public PointF Location
        {
            get => location;
            set
            {
                var old = location;
                if ((location = value) != old)
                {
                    OnLocationChanged?.Invoke(this, new LocationChangedEventArgs(old, value));
                }
            }
        }
        public event EventHandler<LocationChangedEventArgs> OnLocationChanged;
        SizeF size;
        public SizeF Size
        {
            get => size;
            set
            {
                var old = size;
                if ((size = value) != old)
                {
                    OnSizeChanged?.Invoke(this, new SizeChangedEventArgs(old, value));
                }
            }
        }
        public event EventHandler<SizeChangedEventArgs> OnSizeChanged;

        public virtual void OnPaint(GridPaintEventArgs e)
        {
            e.Graphics.FillRectangle(Brushes.LightBlue, new RectangleF(e.Location, Size));
        }

        public virtual void OnMouseDown(ExtendedMouseEventArgs e)
        {
        }

        public virtual void OnMouseUp(ExtendedMouseEventArgs e)
        {
        }

        public virtual void OnMouseMove(ExtendedMouseEventArgs e)
        {
        }

        public virtual void OnMouseWheel(ExtendedMouseEventArgs e)
        {

        }
    }

    public class LocationChangedEventArgs : EventArgs
    {
        public PointF OldLocation { get; private set; }

        public PointF NewLocation { get; private set; }

        public LocationChangedEventArgs(PointF oldLocation, PointF newLocation)
        {
            OldLocation = oldLocation;
            NewLocation = newLocation;
        }
    }
    public class SizeChangedEventArgs : EventArgs
    {
        public SizeF OldSize { get; private set; }

        public SizeF NewSize { get; private set; }

        public SizeChangedEventArgs(SizeF oldSize, SizeF newSize)
        {
            OldSize = oldSize;
            NewSize = newSize;
        }
    }

    public class GridPaintEventArgs : PaintEventArgs
    {
        public PointF Location { get; private set; }

        public float X => Location.X;

        public float Y => Location.Y;

        public BoxGridView GridView { get; private set; }

        public GridPaintEventArgs(Graphics g, Rectangle clipRect, PointF location, BoxGridView gridView)
            : base(g, clipRect)
        {
            Location = location;
            GridView = gridView;
        }
    }
}
