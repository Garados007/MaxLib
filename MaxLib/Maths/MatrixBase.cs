using System;
using System.Text;

namespace MaxLib.Maths
{
    public abstract class MatrixBase<T> : MathBase<T>
    {
        #region Variablen

        protected T[,] Data { get; private set; }

        #endregion

        #region Konstruktoren

        public MatrixBase(int width, int height)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException("width");
            if (height <= 0) throw new ArgumentOutOfRangeException("height");
            Data = new T[height, width];
        }

        public MatrixBase(T[,] data)
        {
            this.Data = data ?? throw new ArgumentNullException("data");
        }

        #endregion

        #region Eigenschaften

        public int Width
        {
            get { return Data.GetLength(1); }
        }

        public int Height
        {
            get { return Data.GetLength(0); }
        }

        public T this[int y, int x]
        {
            get
            {
                if (y < 0 || y >= Height) throw new ArgumentOutOfRangeException("y");
                if (x < 0 || x >= Width) throw new ArgumentOutOfRangeException("x");
                return Data[y, x];
            }
            set
            {
                if (y < 0 || y >= Height) throw new ArgumentOutOfRangeException("y");
                if (x < 0 || x >= Width) throw new ArgumentOutOfRangeException("x");
                Data[y, x] = value;
            }
        }

        public bool IsNullMatrix
        {
            get
            {
                for (var x = 0; x < Width; ++x)
                    for (var y = 0; y < Height; ++y)
                        if (Data[y, x].Equals(Zero))
                            return false;
                return true;
            }
        }

        public bool IsRowMatrix
        {
            get { return Height == 1; }
        }

        public bool IsCollumnMatrix
        {
            get { return Width == 1; }
        }

        public bool IsSquare
        {
            get { return Width == Height; }
        }

        public bool IsSymmetric
        {
            get
            {
                if (Height != Width) return false;
                for (int i1 = 1; i1 < Height; ++i1)
                    for (int i2 = 0; i2 < i1; ++i2)
                        if (Data[i1, i2].Equals(Data[i2, i1])) return false;
                return true;
            }
        }

        public bool IsDiagonalMatrix
        {
            get
            {
                if (Width != Height) return false;
                for (int x = 0; x < Width; ++x)
                    for (int y = 0; y < Height; ++y)
                        if (x != y && !Data[y, x].Equals(Zero))
                            return false;
                return true;
            }
        }

        #endregion

        #region Methoden

        #region von Object

        public override bool Equals(object obj)
        {
            if (!(obj is MatrixBase<T>)) return false;
            var m = (MatrixBase<T>)obj;
            if (Width != m.Width || Height != m.Height) return false;
            for (var x = 0; x < Width; ++x)
                for (var y = 0; y < Height; ++y)
                    if (!Data[y, x].Equals(m.Data[y, x])) return false;
            return true;
        }

        public override int GetHashCode()
        {
            return Width ^ Height; // später Optimieren!!!
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append('(');
            for (int y = 0; y < Height; ++y)
            {
                sb.Append('(');
                for (int x = 0; x < Width; ++x)
                {
                    if (x != 0) sb.Append(';');
                    sb.Append(Data[y, x]);
                }
                sb.Append(')');
            }
            sb.Append(')');
            return sb.ToString();
        }

        #endregion

        #region Umwandlungen

        public Matrix<T> ToMatrix()
        {
            if (this is Matrix<T>) return (Matrix<T>)this;
            return CreateMatrix(Data);
        }

        public Determinat<T> ToDeterminat()
        {
            if (this is Determinat<T>) return (Determinat<T>)this;
            return CreateDeterminat(Data);
        }

        protected abstract Matrix<T> CreateMatrix(T[,] data);

        protected abstract Determinat<T> CreateDeterminat(T[,] data);

        #endregion
        
        #endregion

        #region Operatoren

        #region Vergleich

        public static bool operator ==(MatrixBase<T> m1, MatrixBase<T> m2)
        {
            return m1.Equals(m2);
        }

        public static bool operator !=(MatrixBase<T> m1, MatrixBase<T> m2)
        {
            return !m2.Equals(m2);
        }

        #endregion

        #endregion
    }
}
