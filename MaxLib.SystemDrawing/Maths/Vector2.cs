using System;
using System.Drawing;

namespace MaxLib.Maths
{
    [Obsolete]
    public struct Vector2 : IEquatable<Vector2Int>
    {
        public float X { get; set; }
        public float Y { get; set; }

        public Vector2(float x, float y) : this()
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
            return this * (1 / Abs());
        }

        public static float operator *(Vector2 v1, Vector2 v2)
        {
            return v1.X * v2.X + v1.Y * v2.Y;
        }
        public static Vector2 operator *(float f, Vector2 v)
        {
            return new Vector2(v.X * f, v.Y * f);
        }
        public static Vector2 operator *(Vector2 v, float f)
        {
            return new Vector2(v.X * f, v.Y * f);
        }
        public static Vector2 operator /(Vector2 v, float f)
        {
            return new Vector2(v.X / f, v.Y / f);
        }
        public static Vector2 operator /(float f, Vector2 v)
        {
            return new Vector2(f / v.X, f / v.Y);
        }
        public static Vector2 operator /(Vector2 v1, Vector2 v2)
        {
            return new Vector2(v1.X / v2.X, v1.Y / v2.Y);
        }

        public static Vector2 operator +(Vector2 v1, Vector2 v2)
        {
            return new Vector2(v1.X + v2.X, v1.Y + v2.Y);
        }
        public static Vector2 operator -(Vector2 v)
        {
            return new Vector2(-v.X, -v.Y);
        }
        public static Vector2 operator -(Vector2 v1, Vector2 v2)
        {
            return v1 + (-v2);
        }

        public static bool operator ==(Vector2 v1, Vector2 v2)
        {
            return v1.X == v2.X && v1.Y == v2.Y;
        }
        public static bool operator !=(Vector2 v1, Vector2 v2)
        {
            return !(v1 == v2);
        }

        public static implicit operator Vector2(PointF p)
        {
            return new Vector2(p.X, p.Y);
        }
        public static implicit operator Vector2(Point p)
        {
            return new Vector2(p.X, p.Y);
        }
        public static implicit operator Vector2(SizeF p)
        {
            return new Vector2(p.Width, p.Height);
        }
        public static implicit operator Vector2(Size p)
        {
            return new Vector2(p.Width, p.Height);
        }

        public static implicit operator PointF(Vector2 v)
        {
            return new PointF(v.X, v.Y);
        }
        public static explicit operator Point(Vector2 v)
        {
            return new Point((int)v.X, (int)v.Y);
        }
        public static implicit operator SizeF(Vector2 v)
        {
            return new SizeF(v.X, v.Y);
        }
        public static explicit operator Size(Vector2 v)
        {
            return new Size((int)v.X, (int)v.Y);
        }

        public static float SpecialAngle(Vector2 v1, Vector2 v2)
        {
            
            if (v1 == Zero || v2 == Zero) return 0;
            //var m1 = MultiplicateAngle(v1, out int t1);
            //var m2 = MultiplicateAngle(v2, out int t2);
            //var a1 = v1.X != 0 ? Math.Atan(m1) : v1.Y >= 0 ? w90 : w90;
            //var a2 = v2.X != 0 ? Math.Atan(m2) : v2.Y >= 0 ? w90 : w90;
            //a1 = TurnAngle(a1, t1);
            //a2 = TurnAngle(a2, t2);
            //var r1 = (float)(a2 - a1);
            //if (r1 < 0) r1 += 2 * (float)Math.PI;
            var r2 = Angle(v1, v2);
            //var d1 = ToDegrees(r1);
            //var d2 = ToDegrees(r2);
            return r2;
        }

        public static float Angle(Vector2 v1, Vector2 v2)
        {
            var d = v1 * v2 / (v1.Abs() * v2.Abs());
            d = Math.Max(-1, Math.Min(1, d));
            var r = Math.Acos(d);
            if (r == 0) r = v1 != v2 ? (float)Math.PI : 0;
            r *= Math.Sign(MultS(v1, v2));
            return (float)r;
        }
        public static float Distance(Vector2 graphP, Vector2 graphDirection, Vector2 point)
        {
            var d = point - graphP;
            var n = graphDirection.Normalize();
            var r = d * n;
            return r;
        }
        static float MultS(Vector2 v1, Vector2 v2)//Same Orign
        {
            var turn = false;
            if (v1.X == v1.Y) { var v3 = v1; v1 = v2; v2 = v3; turn = true; }
            if (v1.X == v1.Y)
            {
                return 0;
            }
            if (v1.X * v1.X - v1.Y * v1.Y == 0) return 0;
            var n1 = v1.Normalize();
            var n2 = v2.Normalize();
            if (n1 == n2) return 0;
            var r = (v2.Y * v1.X - v2.X * v1.Y) / (v1.X * v1.X - v1.Y * v1.Y) * (turn ? -1 : 1);
            return r;
        }

        public override string ToString()
        {
            return string.Format("X={0}; Y={1}", X, Y);
        }

        public static Vector2 Rotate(Vector2 v, float degrees)
        {
            var r = v.Abs();
            var phi = Math.Acos(v.X / r);
            phi += degrees * Math.PI / 180;
            return new Vector2((float)Math.Cos(phi), (float)Math.Sin(phi)) * r;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Vector2))
            {
                return false;
            }

            var vector = (Vector2)obj;
            return X == vector.X &&
                   Y == vector.Y;
        }

        public override int GetHashCode()
        {
            var hashCode = 1861411795;
            hashCode = hashCode * -1521134295 + base.GetHashCode();
            hashCode = hashCode * -1521134295 + X.GetHashCode();
            hashCode = hashCode * -1521134295 + Y.GetHashCode();
            return hashCode;
        }

        public bool Equals(Vector2Int other)
        {
            return X == other.X &&
                      Y == other.Y;
        }

        public static Vector2 Zero { get { return new Vector2(0, 0); } }
        public static Vector2 UnitX { get { return new Vector2(1, 0); } }
        public static Vector2 UnitY { get { return new Vector2(0, 1); } }
    }
}
