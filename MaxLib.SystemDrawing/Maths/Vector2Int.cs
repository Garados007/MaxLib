using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace MaxLib.Maths
{
    public struct Vector2Int
    {
        public int X { get; set; }
        public int Y { get; set; }

        public Vector2Int(int x, int y) : this()
        {
            X = x; Y = y;
        }

        public float Abs()
        {
            return (float)Math.Sqrt(AbsSqr());
        }
        public float AbsSqr()
        {
            return X * X + Y * Y;
        }
        public Vector2 Normalize()
        {
            return (Vector2)this * (1 / Abs());
        }

        public static int operator *(Vector2Int v1, Vector2Int v2)
        {
            return v1.X * v2.X + v1.Y * v2.Y;
        }
        public static Vector2Int operator *(int f, Vector2Int v)
        {
            return new Vector2Int(v.X * f, v.Y * f);
        }
        public static Vector2Int operator *(Vector2Int v, int f)
        {
            return new Vector2Int(v.X * f, v.Y * f);
        }

        public static Vector2Int operator +(Vector2Int v1, Vector2Int v2)
        {
            return new Vector2Int(v1.X + v2.X, v1.Y + v2.Y);
        }
        public static Vector2Int operator -(Vector2Int v)
        {
            return new Vector2Int(-v.X, -v.Y);
        }
        public static Vector2Int operator -(Vector2Int v1, Vector2Int v2)
        {
            return v1 + (-v2);
        }

        public static bool operator ==(Vector2Int v1, Vector2Int v2)
        {
            return v1.X == v2.X && v1.Y == v2.Y;
        }
        public static bool operator !=(Vector2Int v1, Vector2Int v2)
        {
            return !(v1 == v2);
        }

        public static implicit operator Vector2(Vector2Int v)
        {
            return new Vector2(v.X, v.Y);
        }
        public static explicit operator Vector2Int(Vector2 v)
        {
            return new Vector2Int((int)v.X, (int)v.Y);
        }

        public static explicit operator Vector2Int(PointF p)
        {
            return new Vector2Int((int)p.X, (int)p.Y);
        }
        public static implicit operator Vector2Int(Point p)
        {
            return new Vector2Int(p.X, p.Y);
        }
        public static explicit operator Vector2Int(SizeF p)
        {
            return new Vector2Int((int)p.Width, (int)p.Height);
        }
        public static implicit operator Vector2Int(Size p)
        {
            return new Vector2Int(p.Width, p.Height);
        }

        public static implicit operator PointF(Vector2Int v)
        {
            return new PointF(v.X, v.Y);
        }
        public static implicit operator Point(Vector2Int v)
        {
            return new Point(v.X, v.Y);
        }
        public static implicit operator SizeF(Vector2Int v)
        {
            return new SizeF(v.X, v.Y);
        }
        public static implicit operator Size(Vector2Int v)
        {
            return new Size(v.X, v.Y);
        }

        public override string ToString()
        {
            return string.Format("X={0}; Y={1}", X, Y);
        }

        public static Vector2Int Zero { get { return new Vector2Int(0, 0); } }
        public static Vector2Int UnitX { get { return new Vector2Int(1, 0); } }
        public static Vector2Int UnitY { get { return new Vector2Int(0, 1); } }
    }
}
