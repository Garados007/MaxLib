namespace MaxLib
{
    public interface ILoadSaveAble
    {
        void Load(byte[] data);

        byte[] Save();
    }
}
