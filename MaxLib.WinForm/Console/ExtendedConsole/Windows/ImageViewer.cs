using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace MaxLib.Console.ExtendedConsole.Windows
{
    using Elements;

    public class ImageViewer : BasicElement
    {
        Image rawImage = null;
        public Image RawImage
        {
            get { return rawImage; }
            set
            {
                rawImage = value;
                BuildImage();
            }
        }
        public Bitmap ShownImage { get; protected set; }

        void BuildImage(bool dochange = true)
        {
            if (rawImage == null)
            {
                Colors = null;
                return;
            }
            if (Width==0&&Height==0)
            {
                if (ShownImage != null) ShownImage.Dispose();
                ShownImage = null;
                return;
            }
            var b = new Bitmap(Width, Height);
            var g = Graphics.FromImage(b);
            g.DrawImage(rawImage, new Rectangle(Width * showLeft / RawWidth, Height * showTop / RawHeight, 
                Width * showWidth / RawWidth, Height * showHeight / RawHeight));
            g.Flush();
            if (ShownImage != null) ShownImage.Dispose();
            ShownImage = b;
            Colors = null;
            if (dochange) DoChange();
        }

        public int RawWidth { get { return rawImage.Width; } }
        public int RawHeight { get { return rawImage.Height; } }

        int showLeft, showTop, showWidth, showHeight;

        public int ShowLeft
        {
            get { return showLeft; }
            set
            {
                showLeft = value;
                BuildImage();
            }
        }
        public int ShowTop
        {
            get { return showTop; }
            set
            {
                showTop = value;
                BuildImage();
            }
        }
        public int ShowWidth
        {
            get { return showWidth; }
            set
            {
                showWidth = value;
                BuildImage();
            }
        }
        public int ShowHeight
        {
            get { return showHeight; }
            set
            {
                showHeight = value;
                BuildImage();
            }
        }

        public void SetDimensions(int showLeft, int showTop, int showWidth, int showHeight)
        {
            this.showLeft = showLeft;
            this.showTop = showTop;
            this.showWidth = showWidth;
            this.showHeight = showHeight;
            BuildImage();
        }

        Color[,] Colors = null;
        public ExtendedConsoleColor AlternativeColor { get; set; }

        void GetColors()
        {
            var c = new Color[ShownImage.Width, ShownImage.Height];
            for (int x = 0; x < ShownImage.Width; ++x) for (int y = 0; y < ShownImage.Height; ++y) c[x, y] = ShownImage.GetPixel(x, y);
            Colors = c;
        }

        public override void Draw(Out.ClipWriterAsync writer)
        {
            base.Draw(writer);
            if (rawImage == null)
            {
                for (int x = 0; x < Width; ++x) for (int y = 0; y < Height; ++y)
                    {
                        writer.SetWriterRelPos(x, y);
                        writer.Write(" ", Color.White, AlternativeColor);
                    }
            }
            else 
            {
                if (ShownImage == null)
                {
                    BuildImage(false);
                }
                if (Colors == null) GetColors();
                for (int x = 0; x < Width; ++x) for (int y = 0; y < Height; ++y)
                    {
                        writer.SetWriterRelPos(x, y);
                        writer.Write(" ", Color.White, Colors[x, y]);
                    }
            }
        }

        public void Load(string path)
        {
            rawImage = Image.FromFile(path);
        }
    }
}
