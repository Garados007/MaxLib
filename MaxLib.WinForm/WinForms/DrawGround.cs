using System;
using System.Drawing;
using System.Windows.Forms;

namespace MaxLib.WinForms
{
    public partial class DrawGround : Control
    {
        PointF centerPoint;
        public PointF CenterPoint { get => centerPoint; set { centerPoint = value; Invalidate(); } }

        float zoom;
        public float Zoom { get => zoom; set { zoom = value; Invalidate(); } }

        bool drawPostInfo;
        public bool DrawPosInfo { get => drawPostInfo; set { drawPostInfo = value; Invalidate(); } }

        bool drawPosHelper;
        public bool DrawPosHelper { get => drawPosHelper; set { drawPosHelper = value; Invalidate(); } }
        
        public bool InvertScroll { get; set; }

        public bool CenterOnDoubleClick { get; set; }

        public DrawGround()
        {
            InitializeComponent();
            SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = SystemColors.Window;
            CenterPoint = new PointF();
            Zoom = 1;
            DrawPosHelper = DrawPosInfo = true;
        }

        Point mousePoint;
        bool move;

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            var ex = new ExtendedMouseEventArgs(e, ScreenToGround(e.Location));
            OnGroundMouseDown(ex);
            if (!ex.Handled && e.Button == MouseButtons.Left)
            {
                if (e.Clicks == 2 && CenterOnDoubleClick) CenterRectangle(GetFocusRectangle());
                else move = true;
            }
            mousePoint = e.Location;
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            var ex = new ExtendedMouseEventArgs(e, ScreenToGround(e.Location));
            OnGroundMouseUp(ex);
            if (!ex.Handled && e.Button == MouseButtons.Left)
            {
                move = false;
            }
            mousePoint = e.Location;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            var ex = new ExtendedMouseEventArgs(e, ScreenToGround(e.Location));
            OnGroundMouseMove(ex);
            if (!ex.Handled)
            {
                if (move)
                {
                    var z = 1;
                    var dx = (e.X - mousePoint.X) * z;
                    var dy = (e.Y - mousePoint.Y) * z;
                    CenterPoint = new PointF(CenterPoint.X - dx, CenterPoint.Y - dy);
                    Invalidate();
                }
            }
            mousePoint = e.Location;
            if (DrawPosInfo) Invalidate();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            var ex = new ExtendedMouseEventArgs(e, ScreenToGround(e.Location));
            OnGroundMouseWheel(ex);
            if (!ex.Handled)
            {
                var d = InvertScroll ? -e.Delta : e.Delta;
                const float zl = 1.1f, izl = 1 / zl;
                if (d < 0) doZoom(zl);
                else if (d > 0) doZoom(izl);
            }
        }

        private void doZoom(float factor)
        {
            //var cp = GroundToScreen(new PointF(CenterPoint.X / Zoom, CenterPoint.Y / Zoom));
            Zoom *= factor;
            //CenterPoint = ScreenToGround(cp);
            CenterPoint = new PointF(CenterPoint.X * factor, CenterPoint.Y * factor);
            Invalidate();
        }

        protected virtual void OnGroundMouseDown(ExtendedMouseEventArgs e) { }
        protected virtual void OnGroundMouseUp(ExtendedMouseEventArgs e) { }
        protected virtual void OnGroundMouseMove(ExtendedMouseEventArgs e) { }
        protected virtual void OnGroundMouseWheel(ExtendedMouseEventArgs e) { }

        protected virtual RectangleF GetFocusRectangle()
        {
            return new RectangleF(0, 0, 20, 20);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Control)
            {
                const float movespeed = 10;
                var used = true;
                switch (e.KeyCode)
                {
                    case Keys.D0: Zoom = 1; break;
                    case Keys.Oemplus: doZoom(1.1f); break;
                    case Keys.OemMinus: doZoom(1 / 1.1f); break;
                    case Keys.Left: CenterPoint = new PointF(CenterPoint.X + movespeed, CenterPoint.Y); break;
                    case Keys.Right: CenterPoint = new PointF(CenterPoint.X - movespeed, CenterPoint.Y); break;
                    case Keys.Up: CenterPoint = new PointF(CenterPoint.X, CenterPoint.Y + movespeed); break;
                    case Keys.Down: CenterPoint = new PointF(CenterPoint.X, CenterPoint.Y - movespeed); break;
                    case Keys.T: CenterRectangle(GetFocusRectangle()); break;
                    default: used = false; break;
                }
                if (used)
                {
                    e.Handled = true;
                    Invalidate();
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            var w = Width;
            var h = Height;
            var t = g.Transform;
            g.TranslateTransform(-CenterPoint.X + w * .5f, -CenterPoint.Y + h * .5f);
            g.ScaleTransform(Zoom, Zoom);
            OnPaintGround(new ExtendedPaintEventArgs(e.Graphics, e.ClipRectangle));
            g.Transform = t;
            if (DrawPosHelper)
            {
                g.DrawLine(Pens.Red, w * .5f - 10, h * .5f, w * .5f + 10, h * .5f);
                g.DrawLine(Pens.Red, w * .5f, h * .5f - 10, w * .5f, h * .5f + 10);
            }
            if (DrawPosInfo)
                using (var brush = new SolidBrush(ForeColor))
                    g.DrawString(string.Format("CenterPoint: {0} -> {3}\r\nZoom: {1}\r\nMouse: {2} -> {4}",
                        CenterPoint, Zoom, mousePoint, ScreenToGround(CenterPoint), ScreenToGround(mousePoint)),
                        Font, brush, 5, 5);
        }

        protected virtual void OnPaintGround(ExtendedPaintEventArgs e)
        {
            if (DrawPosHelper)
            {
                float x = CenterPoint.X / Zoom, y = CenterPoint.Y / Zoom;
                e.Graphics.DrawLine(Pens.Blue, x - 10, y, x + 10, y);
                e.Graphics.DrawLine(Pens.Blue, x, y - 10, x, y + 10);
                e.Graphics.DrawRectangle(Pens.Black, new Rectangle(0, 0, 20, 20));
            }
        }

        protected PointF ScreenToGround(PointF pos)
        {
            return new PointF(
                (pos.X - Width * .5f + CenterPoint.X) / Zoom,
                (pos.Y - Height * .5f + CenterPoint.Y) / Zoom
                );
        }

        protected PointF GroundToScreen(PointF pos)
        {
            return new PointF(
                pos.X * Zoom + Width * .5f - CenterPoint.X,
                pos.Y * Zoom + Height * .5f - CenterPoint.Y
                );
        }

        public void CenterRectangle(RectangleF visibleRect)
        {
            var w = Width;
            var h = Height;

            var vx = w / visibleRect.Width;
            var vy = h / visibleRect.Height;
            var v = Zoom = Math.Min(vx, vy);

            //var t = Math.Min(w, h) * 0.5f;
            //CenterPoint = new PointF(t - visibleRect.Left * v, t - visibleRect.Top * v);
            CenterPoint = new PointF((visibleRect.Left + visibleRect.Width * 0.5f) * v, (visibleRect.Top + visibleRect.Height * 0.5f) * v);
        }
    }



    public class ExtendedPaintEventArgs : PaintEventArgs
    {

        public ExtendedPaintEventArgs(Graphics g, Rectangle clipRect) : base(g, clipRect) { }
    }

    public class ExtendedMouseEventArgs : MouseEventArgs
    {
        public ExtendedMouseEventArgs(MouseButtons button, int clicks, float x, float y, int delta)
            : base(button, clicks, (int)x, (int)y, delta)
        {
            X = x;
            Y = y;
        }

        public ExtendedMouseEventArgs(MouseEventArgs e, PointF p)
            : base(e.Button, e.Clicks, (int)p.X, (int)p.Y, e.Delta)
        {
            X = p.X;
            Y = p.Y;
        }

        public bool Handled { get; set; }

        public new float X { get; private set; }

        public new float Y { get; private set; }

        public new PointF Location => new PointF(X, Y);
    }
}
