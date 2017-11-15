using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MaxLib.Collections;

namespace MaxLib.WinForms
{
    public partial class BoxGridView : DrawGround
    {
        BoxGridViewContainer boxes;
        public BoxGridViewContainer Boxes
        {
            get => boxes;
            protected set
            {
                if (value == null) throw new ArgumentNullException("Boxes");
                if (boxes != null)
                    boxes.PaintSettingsUpdated -= Boxes_PaintSettingsUpdated;
                (boxes = value).PaintSettingsUpdated += Boxes_PaintSettingsUpdated;
            }
        }

        [Browsable(true)]
        public float MinDistance
        {
            get => Boxes.MinDistance;
            set => Boxes.MinDistance = value;
        }
        [Browsable(true)]
        public float MaxDistance
        {
            get => Boxes.MaxDistance;
            set => Boxes.MaxDistance = value;
        }
        [Browsable(true), RefreshProperties(RefreshProperties.All)]
        public bool OverlapRows
        {
            get => Boxes.OverlapRows;
            set => Boxes.OverlapRows = value;
        }
        [Browsable(true), RefreshProperties(RefreshProperties.All)]
        public bool OverlapCols
        {
            get => Boxes.OverlapCols;
            set => Boxes.OverlapCols = value;
        }
        [Browsable(true)]
        public SizeF GridSize
        {
            get => Boxes.GridSize;
        }

        public BoxGridView()
        {
            new LineGridContainer<int>();
            InitializeComponent();
            DrawPosHelper = false;
            InvertScroll = true;
            Boxes = new BoxGridViewContainer(this);

            //Boxes.Add(new BoxGridElement()
            //{
            //    Location = new PointF(0, 0),
            //    Size = new SizeF(30, 40)
            //});
            //Boxes.Add(new BoxGridElement()
            //{
            //    Location = new PointF(35, 5),
            //    Size = new SizeF(40, 30)
            //});
            //Boxes.Add(new BoxGridElement()
            //{
            //    Location = new PointF(0, 25),
            //    Size = new SizeF(40, 30)
            //});
            //Boxes.OverlapRows = true;
        }

        private void Boxes_PaintSettingsUpdated(object sender, EventArgs e)
        {
            Invalidate();
        }

        protected override void OnPaintGround(ExtendedPaintEventArgs e)
        {
            base.OnPaintGround(e);
            Boxes.OnPaint(e);
        }

        protected override void OnGroundMouseDown(ExtendedMouseEventArgs e)
        {
            base.OnGroundMouseDown(e);
            Boxes.OnMouseDown(e);
        }

        protected override void OnGroundMouseMove(ExtendedMouseEventArgs e)
        {
            base.OnGroundMouseMove(e);
            Boxes.OnMouseMove(e);
        }

        protected override void OnGroundMouseUp(ExtendedMouseEventArgs e)
        {
            base.OnGroundMouseUp(e);
            Boxes.OnMouseUp(e);
        }

        protected override void OnGroundMouseWheel(ExtendedMouseEventArgs e)
        {
            base.OnGroundMouseWheel(e);
            Boxes.OnMouseWheel(e);
        }

        protected override RectangleF GetFocusRectangle()
        {
            return new RectangleF(new PointF(), GridSize);
        }
    }
}
