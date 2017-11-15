using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MaxLib.Maths
{
    public abstract class Determinat<T> : MatrixBase<T>, ICloneable
    {
        public Determinat(int size)
            : base(size, size)
        {

        }

        public Determinat(T[,] data)
            : base(data)
        {

        }

        public Determinat<T> GetSubDeterminant(int rowIndex, int collumnIndex)
        {
            if (rowIndex < 0 || rowIndex >= Height) throw new ArgumentOutOfRangeException("rowIndex");
            if (collumnIndex < 0 || collumnIndex >= Width) throw new ArgumentOutOfRangeException("collumnIndex");
            var d = new T[Height - 1, Width - 1];
            for (int x = 0; x < Width; ++x)
                if (x != collumnIndex)
                    for (int y = 0; y < Height; ++y)
                        if (y != rowIndex)
                            d[y < rowIndex ? y : y - 1, x < collumnIndex ? x : x - 1] = Data[y, x];
            return CreateDeterminat(d);
        }

        //public Determinat<T> RemoveRow(int index)
        //{
        //    if (index < 0 || index >= Height) throw new ArgumentOutOfRangeException("index");
        //    var d = new T[Height - 1, Width];
        //    for (int x = 0; x < Width; ++x)
        //        for (int y = 0; y < Height; ++y)
        //            if (y != index)
        //                d[y < index ? y : y - 1, x] = data[y, x];
        //    return CreateDeterminat(d);
        //}

        //public Determinat<T> RemoveCollumn(int index)
        //{
        //    if (index < 0 || index >= Width) throw new ArgumentOutOfRangeException("index");
        //    var d = new T[Height, Width - 1];
        //    for (int x = 0; x < Width; ++x)
        //        if (x != index)
        //            for (int y = 0; y < Height; ++y)
        //                d[y, x < index ? x : x - 1] = data[y, x];
        //    return CreateDeterminat(d);
        //}


        #region iCloneable

        object ICloneable.Clone()
        {
            return Clone();
        }

        public Determinat<T> Clone()
        {
            var d = new T[Height, Width];
            for (int y = 0; y < Height; ++y)
                for (int x = 0; x < Width; ++x)
                    d[y, x] = this[y, x];
            return CreateDeterminat(d);
        }

        #endregion
    }
}
