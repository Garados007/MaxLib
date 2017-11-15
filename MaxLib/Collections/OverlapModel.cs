using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaxLib.Collections
{
    public class OverlapModel<T>
    {
        List<OverlapEntry<T>> currentConfig = new List<OverlapEntry<T>>();
        List<OverlapEntry<T>> newConfig = new List<OverlapEntry<T>>();

        public OverlapEntry<T>[] CurrentConfig => currentConfig.ToArray();
        public OverlapEntry<T>[] NewConfig => newConfig.ToArray();
        public int CurrentConfigCount => currentConfig.Count;
        public int NewConfigCount => newConfig.Count;

        public void Add(OverlapEntry<T> entry)
        {
            newConfig.Add(entry);
        }

        public void AddRange(IEnumerable<OverlapEntry<T>> entrys)
        {
            newConfig.AddRange(entrys);
        }

        public float GetSaveDistance(float startDistance)
        {
            float save = 0;
            foreach (var nc in newConfig)
                foreach (var cc in currentConfig)
                {
                    var sd = nc.SaveSpace(cc) - startDistance;
                    if (sd > save) save = sd;
                }
            return save;
        }

        public void Merge(float distance)
        {
            currentConfig.ForEach((cc) => cc.LeftSpace -= distance);
            currentConfig.RemoveAll((cc) => cc.LeftSpace <= 0);
            currentConfig.AddRange(newConfig);
            newConfig.Clear();
        }

        public float LeftSpace => currentConfig.Count == 0 ? 0 : currentConfig.Max((cc) => cc.LeftSpace);
    }

    public class OverlapEntry<T>
    {
        public T Element { get; private set; }

        public float WidthSpace { get; private set; }

        public float WidthPosition { get; private set; }

        public float LeftSpace { get; internal set; }

        public OverlapEntry(T element, float widthPosition, float widthSpace, float startSpace)
        {
            Element = element;
            WidthSpace = widthSpace;
            WidthPosition = widthPosition;
            LeftSpace = startSpace;
        }

        public float SaveSpace(OverlapEntry<T> other)
        {
            var mr = WidthPosition + WidthSpace;
            var or = other.WidthPosition + other.WidthSpace;
            if (WidthPosition >= or || mr <= other.WidthPosition)
                return 0;
            else return other.LeftSpace;
        }
    }
}
