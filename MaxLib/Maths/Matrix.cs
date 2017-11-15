using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MaxLib.Maths
{
    public abstract class Matrix<T> : MatrixBase<T>, ICloneable
    {
        #region Konstruktoren

        public Matrix(int width, int height)
            : base(width, height)
        {

        }

        public Matrix(T[,] data)
            : base(data)
        {

        }

        #endregion

        #region Eigenschaften

        public bool IsIdentityMatrix
        {
            get
            {
                if (!IsSquare) return false;
                for (int x = 0; x < Width; ++x)
                    for (int y = 0; y < Height; ++y)
                        if (!data[y, x].Equals(x == y ? One : Zero)) return false;
                return true;
            }
        }

        public bool IsUpperTriangularMatrix
        {
            get
            {
                if (!IsSquare) return false;
                for (int y = 1; y < Height; ++y)
                    for (int x = 0; x < y; ++x)
                        if (!data[y, x].Equals(Zero))
                            return false;
                return true;
            }
        }

        public bool IsLowerTriangularMatrix
        {
            get
            {
                if (!IsSquare) return false;
                for (int y = 0; y < Height - 1; ++y)
                    for (int x = y + 1; x < Width; ++x)
                        if (!data[y, x].Equals(Zero))
                            return false;
                return true;
            }
        }

        public bool IsTriangularMatrix
        {
            get
            {
                return IsUpperTriangularMatrix || IsLowerTriangularMatrix;
            }
        }

        #endregion

        #region Methoden

        public Matrix<T> Transpose()
        {
            var d = new T[Width, Height];
            for (int x = 0; x < Width; ++x)
                for (int y = 0; y < Height; ++y)
                    d[x, y] = data[y, x];
            return CreateMatrix(d);
        }

        public Matrix<T> RemoveRow(int index)
        {
            if (index < 0 || index >= Height) throw new ArgumentOutOfRangeException("index");
            var d = new T[Height - 1, Width];
            for (int x = 0; x < Width; ++x)
                for (int y = 0; y < Height; ++y)
                    if (y != index)
                        d[y < index ? y : y - 1, x] = data[y, x];
            return CreateMatrix(d);
        }

        public Matrix<T> RemoveCollumn(int index)
        {
            if (index < 0 || index >= Width) throw new ArgumentOutOfRangeException("index");
            var d = new T[Height, Width - 1];
            for (int x = 0; x < Width; ++x)
                if (x != index)
                    for (int y = 0; y < Height; ++y)
                        d[y, x < index ? x : x - 1] = data[y, x];
            return CreateMatrix(d);
        }

        public Matrix<T> GetRowMatrix(int index)
        {
            if (index < 0 || index >= Height) throw new ArgumentOutOfRangeException("index");
            var d = new T[1, Width];
            for (var i = 0; i < Width; ++i)
                d[0, i] = data[index, i];
            return CreateMatrix(d);
        }

        public Matrix<T> GetCollumnMatrix(int index)
        {
            if (index < 0 || index >= Width) throw new ArgumentOutOfRangeException("index");
            var d = new T[Height, 1];
            for (var i = 0; i < Height; ++i)
                d[i, 0] = data[i, index];
            return CreateMatrix(d);
        }

        #endregion

        #region Operator

        public static Matrix<T> operator +(Matrix<T> m1, Matrix<T> m2)
        {
            if (m1 == null) throw new ArgumentNullException("m1");
            if (m2 == null) throw new ArgumentNullException("m2");
            if (m1.Width!=m2.Width) throw new InvalidOperationException("The widths do not match");
            if (m1.Height != m2.Height) throw new InvalidOperationException("The heights do not match");
            var d = new T[m1.Height, m1.Width];
            for (int x = 0; x < m1.Width; ++x)
                for (int y = 0; y < m1.Height; ++y)
                    d[y, x] = m1.Add(m1[y, x], m2[y, x]);
            return m1.CreateMatrix(d);
        }

        public static Matrix<T> operator -(Matrix<T> m1, Matrix<T> m2)
        {
            if (m1 == null) throw new ArgumentNullException("m1");
            if (m2 == null) throw new ArgumentNullException("m2");
            if (m1.Width != m2.Width) throw new InvalidOperationException("The widths do not match");
            if (m1.Height != m2.Height) throw new InvalidOperationException("The heights do not match");
            var d = new T[m1.Height, m1.Width];
            for (int x = 0; x < m1.Width; ++x)
                for (int y = 0; y < m1.Height; ++y)
                    d[y, x] = m1.Add(m1[y, x], m1.Negate(m2[y, x]));
            return m1.CreateMatrix(d);
        }

        public static Matrix<T> operator *(Matrix<T> m, T val)
        {
            if (m == null) throw new ArgumentNullException("m");
            var d = new T[m.Height, m.Width];
            for (int x = 0; x < m.Width; ++x)
                for (int y = 0; y < m.Height; ++y)
                    d[y, x] = m.Multiplicate(m[y, x], val);
            return m.CreateMatrix(d);
        }

        public static Matrix<T> operator *(T val, Matrix<T> m)
        {
            return m * val;
        }

        public static Matrix<T> operator -(Matrix<T> m)
        {
            if (m == null) throw new ArgumentNullException("m");
            return m * m.Negate(m.One);
        }

        public static Matrix<T> operator *(Matrix<T> m1, Matrix<T> m2)
        {
            if (m1 == null) throw new ArgumentNullException("m1");
            if (m2 == null) throw new ArgumentNullException("m2");
            if (m1.Width != m2.Height) throw new InvalidOperationException("m1.Width do not match with m2.Height");
            var d = new T[m1.Height, m2.Width];
            for (int x = 0; x<m2.Width; ++x)
                for (int y = 0; y < m1.Height; ++y)
                {
                    var z = m1.Zero;
                    for (int i = 0; i < m1.Width; ++i)
                        z = m1.Add(z, m1.Multiplicate(m1[y, i], m2[i, x]));
                    d[y, x] = z;
                }
            return m1.CreateMatrix(d);
        }

        #endregion

        #region iCloneable

        object ICloneable.Clone()
        {
            return Clone();
        }

        public Matrix<T> Clone()
        {
            var d = new T[Height, Width];
            for (int y = 0; y < Height; ++y)
                for (int x = 0; x < Width; ++x)
                    d[y, x] = this[y, x];
            return CreateMatrix(d);
        }

        #endregion
    }
}
