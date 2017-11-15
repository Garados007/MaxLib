using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MaxLib
{
    public interface ILoadSaveAble
    {
        void Load(byte[] data);

        byte[] Save();
    }
}
