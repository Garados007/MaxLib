using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MaxLib.WinForms
{
    public partial class DigitList : Control
    {
        public DigitList()
        {
            InitializeComponent();
            digitLister = new DigitLister();
            digitLister.controls = Controls;
            digitLister.Owner = this;
            Height = 40;
            Width = 200;
            Resize += DigitList_Resize;
            timer1.Tick += timer1_Tick;
        }

        int offset, length;

        void timer1_Tick(object sender, EventArgs e)
        {
            if (length == 0) offset = 0;
            else offset = (offset + 1) % length;
            UpdateSort();
        }

        [DefaultValue(true)]
        public bool TimerEnabled
        {
            get { return timer1.Enabled; }
            set { timer1.Enabled = value; UpdateSort(); }
        }

        [DefaultValue(1000)]
        public int TimerIntervall
        {
            get { return timer1.Interval; }
            set { timer1.Interval = value; }
        }

        void DigitList_Resize(object sender, EventArgs e)
        {
            UpdateSort();
        }

        bool CanChangeCount = false;

        protected override void OnPaint(PaintEventArgs pe)
        {
            base.OnPaint(pe);
            if (!CanChangeCount)
            {
                CanChangeCount = true;
                UpdateSort();
            }
        }

        private int digitWidth = 20;
        [DefaultValue(20)]
        public int DigitWidth
        {
            get { return digitWidth; }
            set { if (digitWidth <= 0) throw new ArgumentOutOfRangeException(); digitWidth = value; }
        }

        private DigitConverter converter = new StandartConverter();
        [DefaultValue(typeof(StandartConverter))]
        public DigitConverter Converter
        {
            get { return converter; }
            set { converter = value ?? throw new ArgumentNullException(); }
        }

        private Color inactiveColor = Color.Gray;

        public Color InactiveColor
        {
            get { return inactiveColor; }
            set { inactiveColor = value; UpdateSort(); }
        }

        void UpdateSort()
        {
            if (autoUpdateCount)
            {
                var mc = Width / digitWidth;
                if (Count != mc) Count = mc;
            }
            var s = converter.BuildConvertable(Text);
            length = s.Length;
            for (int i = 0; i < digitLister.Count; ++i)
            {
                var dv = digitLister[i];
                dv.Width = digitWidth;
                dv.Left = digitWidth * i;
                dv.Height = Height;
                dv.ForeColor = ForeColor;
                dv.BackColor = BackColor;
                dv.InactiveColor = InactiveColor;
                if (TimerEnabled && s.Length > 0) dv.Text = s[(offset + i) % length].ToString();
                else if (s.Length>i) dv.Text = s[i].ToString();
                else dv.Text = "";

                dv.Invalidate();
            }
        }

        private bool autoUpdateCount = true;
        [DefaultValue(true)]
        public bool AutoUpdateCount
        {
            get { return autoUpdateCount; }
            set { autoUpdateCount = value; UpdateSort(); }
        }

        [DefaultValue(10)]
        public int Count
        {
            get { return digitLister.Count; }
            set
            {
                if (!CanChangeCount) return;
                if (digitLister.Count == value) return;
                while (digitLister.Count > value) digitLister.RemoveAt(0);
                while (digitLister.Count < value) digitLister.Add(new DigitViewer());
                UpdateSort();
            }
        }
        [DefaultValue("")]
        public override string Text
        {
            get
            {
                return base.Text;
            }
            set
            {
                base.Text = value;
                UpdateSort();
            }
        }

        private DigitLister digitLister;

        public DigitLister Digits
        {
            get { return digitLister; }
        }

        public class DigitLister : IList<DigitViewer>, ICollection<DigitViewer>, IEnumerable<DigitViewer>
        {
            internal ControlCollection controls;

            internal DigitList Owner;

            public int IndexOf(DigitViewer item)
            {
                return controls.IndexOf(item);
            }

            public void Insert(int index, DigitViewer item)
            {
                controls.Add(item);
                controls.SetChildIndex(item, index);
                Owner.UpdateSort();
            }

            public void RemoveAt(int index)
            {
                controls.RemoveAt(index);
                Owner.UpdateSort();
            }

            public DigitViewer this[int index]
            {
                get
                {
                    return controls[index] as DigitViewer;
                }
                set
                {
                    controls.RemoveAt(index);
                    controls.Add(value);
                    controls.SetChildIndex(value, index);
                    Owner.UpdateSort();
                }
            }

            public void Add(DigitViewer item)
            {
                controls.Add(item);
                Owner.UpdateSort();
            }

            public void Clear()
            {
                controls.Clear(); 
                Owner.UpdateSort();
            }

            public bool Contains(DigitViewer item)
            {
                return controls.Contains(item);
            }

            public void CopyTo(DigitViewer[] array, int arrayIndex)
            {
                controls.CopyTo(array, arrayIndex);
            }

            public int Count
            {
                get { return controls.Count; }
            }

            public bool IsReadOnly
            {
                get { return controls.IsReadOnly; }
            }

            public bool Remove(DigitViewer item)
            {
                var ex = controls.Contains(item);
                controls.Remove(item);
                Owner.UpdateSort();
                return ex;
            }

            public IEnumerator<DigitViewer> GetEnumerator()
            {
                return controls.GetEnumerator() as IEnumerator<DigitViewer>;
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return controls.GetEnumerator();
            }
        }
    }
}
