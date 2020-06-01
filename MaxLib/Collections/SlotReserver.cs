using System;
using System.Collections.Generic;

namespace MaxLib.Collections
{
    public class SlotReserver<T>
    {
        readonly Dictionary<T, SlotReserverEntry<T>> list = new Dictionary<T, SlotReserverEntry<T>>();
        readonly SortedList<int, HashSet<SlotReserverEntry<T>>> levelEntrys = new SortedList<int, HashSet<SlotReserverEntry<T>>>();
        private readonly Dictionary<T, HashSet<T>> parentList = new Dictionary<T, HashSet<T>>(); // child -> parents
        private readonly Dictionary<T, HashSet<T>> childList = new Dictionary<T, HashSet<T>>(); // parent -> children

        public int ParentCount => parentList.Count;
        public int ChildCount => childList.Count;
        public int ElementCount => list.Count;

        public void AddBondage(T parent, T child)
        {
            if (!parentList.ContainsKey(child)) parentList.Add(child, new HashSet<T>());
            if (!childList.ContainsKey(parent)) childList.Add(parent, new HashSet<T>());
            parentList[child].Add(parent);
            childList[parent].Add(child);
        }

        public void RemoveBondage(T parent, T child)
        {
            parentList[child].Remove(parent);
            childList[parent].Remove(child);
            if (parentList[child].Count == 0) parentList.Remove(child);
            if (childList[parent].Count == 0) childList.Remove(parent);
        }

        public float SingleReservedTime { get; private set; }

        public SlotReserver(float singleReservedTime)
        {
            SingleReservedTime = singleReservedTime;
        }

        public void Add(T element, float time)
        {
            if (list.ContainsKey(element)) return;
            var entry = new SlotReserverEntry<T>(element, time);
            Justify(entry);
            list.Add(element, entry);
            Compress(null);
        }

        public int GetLevel(T element)
        {
            return list[element].Level;
        }

        public bool Remove(T element)
        {
            if (list.ContainsKey(element))
            {
                Compress(list[element]);
                list.Remove(element);
                return true;
            }
            else return false;
        }

        void Justify(SlotReserverEntry<T> entry)
        {
            var collides = new List<SlotReserverEntry<T>>();
            int maxLevel = -1, minLevel = 0;
            foreach (var e in list)
                if (e.Value.Time + SingleReservedTime > entry.Time && e.Value.Time < entry.Time + SingleReservedTime)
                {
                    collides.Add(e.Value);
                    if (e.Value.Level > maxLevel) maxLevel = e.Value.Level;
                    if (e.Value.Level < minLevel) minLevel = e.Value.Level;
                }
            int perfectLevel = maxLevel + 1;
            float perfectRating = float.MaxValue;
            for (int level = minLevel - 1; level <= maxLevel + 1; ++level)
            {
                float rating = 0;
                foreach (var c in collides)
                {
                    if (parentList.ContainsKey(c.Element))
                        foreach (var p in parentList[c.Element])
                            rating += Math.Abs(AdjustLevel(list[p], level) - AdjustLevel(c, level));
                    if (childList.ContainsKey(c.Element))
                        foreach (var ch in childList[c.Element])
                            rating += Math.Abs(AdjustLevel(list[ch], level) - AdjustLevel(c, level));
                }
                if (parentList.ContainsKey(entry.Element))
                    foreach (var p in parentList[entry.Element])
                        rating += Math.Abs(AdjustLevel(list[p], level) - level);
                if (childList.ContainsKey(entry.Element))
                    foreach (var ch in childList[entry.Element])
                        rating += Math.Abs(AdjustLevel(list[ch], level) - level);
                if (rating < perfectRating)
                {
                    perfectLevel = level;
                    perfectRating = rating;
                }
            }
            var key = new List<int>();
            foreach (var l in levelEntrys)
                if (l.Key >= perfectLevel)
                    key.Add(l.Key);
            for (int i = key.Count-1; i>=0; --i)
            {
                var l = levelEntrys[key[i]];
                levelEntrys.Remove(key[i]);
                levelEntrys.Add(key[i] + 1, l);
                foreach (var e in l)
                    e.Level++;
            }
            entry.Level = perfectLevel;
            levelEntrys.Add(perfectLevel, new HashSet<SlotReserverEntry<T>>());
            levelEntrys[perfectLevel].Add(entry);
        }

        int AdjustLevel(SlotReserverEntry<T> entry, int insertLevel)
        {
            if (entry.Level >= insertLevel) return entry.Level + 1;
            else return entry.Level;
        }

        void Compress(SlotReserverEntry<T> entry)
        {
            if(entry != null) levelEntrys[entry.Level].Remove(entry);
                var m = new List<SlotReserverEntry<T>>();
            foreach (var l in levelEntrys)
            {
                m.Clear();
                foreach (var e in l.Value)
                {
                    bool collide = false;
                    if (levelEntrys.ContainsKey(l.Key - 1))
                    {
                        foreach (var c in levelEntrys[l.Key - 1])
                            if (c.Time + SingleReservedTime > e.Time && c.Time < e.Time + SingleReservedTime)
                            {
                                collide = true;
                                break;
                            }
                        if (!collide) m.Add(e);
                    }
                }
                foreach (var e in m)
                {
                    l.Value.Remove(e);
                    levelEntrys[l.Key - 1].Add(e);
                }
            }
        }
    }

    public class SlotReserverEntry<T>
    {
        public T Element { get; private set; }

        public float Time { get; private set; }

        public int Level { get; internal set; }

        public SlotReserverEntry(T element, float time)
        {
            Element = element;
            Time = time;
        }
    }
}
