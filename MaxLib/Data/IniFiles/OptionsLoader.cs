using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GenC = System.Collections.Generic;

namespace MaxLib.Data.IniFiles
{
    public class OptionsLoader
    {
        public OptionsGroupCollection Groups { get; private set; }

        public OptionsGroup this[int index]
        {
            get { return Groups[index]; }
        }

        public OptionsGroup this[string name]
        {
            get { return Get(name); }
        }

        #region Minimizer

        public OptionsLoader Minimize()
        {
            return Minimize((o, n) => true, (g, a, k) => true);
        }
        public OptionsLoader Minimize(MinimizeTakeOptionGroup takeOptionGroup)
        {
            return Minimize(takeOptionGroup, (g, a, k) => true);
        }
        public OptionsLoader Minimize(MinimizeTakeOptionKey takeOptionKey)
        {
            return Minimize((o, n) => true, takeOptionKey);
        }
        public OptionsLoader Minimize(MinimizeTakeOptionGroup takeOptionGroup, MinimizeTakeOptionKey takeOptionKey)
        {
            if (takeOptionGroup == null) throw new ArgumentNullException("takeOptionGroup");
            if (takeOptionKey == null) throw new ArgumentNullException("takeOptionKey");
            var ol = new OptionsLoader();
            for (int i = 0; i < Groups.Count; ++i)
            {
                var o = Groups[i];
                if (i != 0)
                {
                    if (!takeOptionGroup(o.Name, o)) continue;
                    ol.Groups.Add(new OptionsGroup(o.Name));
                }
                var n = ol.Groups[i];
                for (var si = 0; si < o.Attributes.Count; ++si)
                {
                    if (!takeOptionKey(o, true, (OptionsKey)o.Attributes[si])) continue;
                    n.Attributes.Add(o.Attributes[si]);
                }
                for (var si = 0; si < o.Options.Count; ++si)
                    if (o.Options[si] is OptionsKey)
                    {
                        if (!takeOptionKey(o, false, (OptionsKey)o.Options[si])) continue;
                        n.Options.Add(o.Options[si]);
                    }
            }
            return ol;
        }

        public delegate bool MinimizeTakeOptionGroup(string name, OptionsGroup group);
        public delegate bool MinimizeTakeOptionKey(OptionsGroup group, bool isAttribute, OptionsKey key);

        #endregion

        #region constructors

        public OptionsLoader()
        {
            Groups = new OptionsGroupCollection();
        }

        public OptionsLoader(string data)
            : this()
        {
            Import(data);
        }

        public OptionsLoader(System.IO.Stream stream)
            : this()
        {
            Import(stream);
        }

        public OptionsLoader(string file, bool throwError)
            : this()
        {
            Import(file, throwError);
        }

        #endregion

        #region Export

        public string Export()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < Groups.Count; ++i)
            {
                sb.Append(Groups[i].ToStreamString());
                if (i < Groups.Count - 1) sb.AppendLine();
            }
            return sb.ToString();
        }

        public void Export(System.IO.Stream stream)
        {
            var w = new System.IO.StreamWriter(stream);
            w.Write(Export());
        }

        public void Export(string file)
        {
            System.IO.File.WriteAllText(file, Export());
        }

        #endregion

        #region Import

        public void Import(string data)
        {
            using (var m = new System.IO.MemoryStream())
            using (var w = new System.IO.StreamWriter(m))
            {
                w.Write(data);
                w.Flush();
                m.Position = 0;
                Import(m);
            }

        }

        public void Import(string file, bool throwError)
        {
            if (!System.IO.File.Exists(file))
            {
                if (throwError) throw new System.IO.FileNotFoundException("", file);
            }
            else Import(System.IO.File.ReadAllText(file));
        }

        public void Import(System.IO.Stream stream)
        {
            var r = new System.IO.StreamReader(stream);
            var d = new List<IOptionsStreamPart>();
            var parser = new LineParser();
            while (!r.EndOfStream)
                d.Add(parser.Parse(r.ReadLine()));
            Groups.Clear();
            var current = Groups[0];
            for (int i = 0; i < d.Count; ++i)
            {
                if (d[i] is OptionsGroup) Groups.Add(current = d[i] as OptionsGroup);
                else current.Options.Add(d[i]);
            }
        }

        #endregion

        #region Fast Access

        public OptionsGroup Add(string name)
        {
            return Groups.Add(name);
        }

        public OptionsGroup Get(string name)
        {
            return Groups.Get(name);
        }

        public OptionsGroup Get(Predicate<OptionsGroup> match)
        {
            return Groups.Get(match);
        }

        public OptionsGroup Get(string name, Predicate<OptionsGroup> match)
        {
            return Groups.Get(name, match);
        }

        public OptionsGroup[] GetAll(string name)
        {
            return Groups.GetAll(name);
        }

        public OptionsGroup[] GetAll(Predicate<OptionsGroup> match)
        {
            return Groups.GetAll(match);
        }

        public OptionsGroup[] GetAll(string name, Predicate<OptionsGroup> match)
        {
            return Groups.GetAll(name, match);
        }

        #endregion
    }

    #region Collections

    public class OptionsGroupCollection :
        IList<OptionsGroup>
    {
        protected List<OptionsGroup> Groups;

        internal OptionsGroupCollection()
        {
            Groups = new List<OptionsGroup>
            {
                new OptionsGroup(true)
            };
        }

        public OptionsGroupSearchCollection GetSearch()
        {
            var ogsc = new OptionsGroupSearchCollection
            {
                Groups = Groups.ToList()
            };
            return ogsc;
        }

        public List<T> ConvertAll<T>(Converter<OptionsGroup, T> converter)
        {
            return Groups.ConvertAll(converter);
        }

        #region Fast Access

        public OptionsGroup Add(string name)
        {
            var og = new OptionsGroup(name);
            Add(og);
            return og;
        }

        public OptionsGroup Get(string name)
        {
            return Groups.Find((og) => og.Name == name);
        }

        public OptionsGroup Get(Predicate<OptionsGroup> match)
        {
            return Groups.Find(match);
        }

        public OptionsGroup Get(string name, Predicate<OptionsGroup> match)
        {
            return Groups.Find((og) => og.Name == name && match(og));
        }

        public OptionsGroup[] GetAll(string name)
        {
            return Groups.FindAll((og) => og.Name == name).ToArray();
        }

        public OptionsGroup[] GetAll(Predicate<OptionsGroup> match)
        {
            return Groups.FindAll(match).ToArray();
        }

        public OptionsGroup[] GetAll(string name, Predicate<OptionsGroup> match)
        {
            return Groups.FindAll((og) => og.Name == name && match(og)).ToArray();
        }

        public OptionsGroup this[string name]
        {
            get { return Get(name); }
        }

        #endregion

        #region IList

        public int IndexOf(OptionsGroup item)
        {
            if (item == null) return -1;
            return Groups.IndexOf(item);
        }

        public void Insert(int index, OptionsGroup item)
        {
            if (item == null) throw new ArgumentNullException("item");
            if (index == 0) throw new NotSupportedException("You can't insert a group before the main group.");
            Groups.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            if (index == 0) throw new NotSupportedException("You can't remove the main group.");
            Groups.RemoveAt(index);
        }

        public OptionsGroup this[int index]
        {
            get
            {
                return Groups[index];
            }
            set
            {
                if (index == 0) throw new NotSupportedException("You can't replace the main group.");
                Groups[index] = value ?? throw new ArgumentNullException("OptionsLoader[int]");
            }
        }

        public void Add(OptionsGroup item)
        {
            if (item == null) throw new ArgumentNullException("item");
            Groups.Add(item);
        }

        public void Clear()
        {
            Groups.Clear();
            Groups.Add(new OptionsGroup(true));
        }

        public bool Contains(OptionsGroup item)
        {
            if (item == null) return false;
            return Groups.Contains(item);
        }

        public void CopyTo(OptionsGroup[] array, int arrayIndex)
        {
            Groups.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return Groups.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(OptionsGroup item)
        {
            if (item == null) return false;
            if (item == Groups[0]) throw new NotSupportedException("You can't remove the main group.");
            return Groups.Remove(item);
        }

        public IEnumerator<OptionsGroup> GetEnumerator()
        {
            return Groups.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return Groups.GetEnumerator();
        }

        #endregion
    }

    public class OptionsCollection :
        GenC.ICollection<IOptionsStreamPart>,
        GenC.IEnumerable<IOptionsStreamPart>,
        GenC.IList<IOptionsStreamPart>
    {
        #region internal Default

        protected List<IOptionsStreamPart> StreamParts = new List<IOptionsStreamPart>();

        public bool CommentsAllowed { get; private set; }

        internal OptionsCollection(bool commentsAllowed)
        {
            CommentsAllowed = commentsAllowed;
        }

        #endregion

        #region Interfaces

        #region ICollection, IEnumerable

        public void Add(IOptionsStreamPart item)
        {
            if (item == null) throw new ArgumentNullException("item");
            if (!CommentsAllowed && !(item is OptionsKey)) throw new NotSupportedException();
            StreamParts.Add(item);
        }

        public void Clear()
        {
            StreamParts.Clear();
        }

        public bool Contains(IOptionsStreamPart item)
        {
            if (item == null) return false;
            return StreamParts.Contains(item);
        }

        public void CopyTo(IOptionsStreamPart[] array, int arrayIndex)
        {
            StreamParts.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return StreamParts.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(IOptionsStreamPart item)
        {
            if (item == null) return false;
            return StreamParts.Remove(item);
        }

        public IEnumerator<IOptionsStreamPart> GetEnumerator()
        {
            return StreamParts.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return StreamParts.GetEnumerator();
        }

        #endregion

        #region Rest of IList

        public int IndexOf(IOptionsStreamPart item)
        {
            if (item == null) return -1;
            return StreamParts.IndexOf(item);
        }

        public void Insert(int index, IOptionsStreamPart item)
        {
            if (item == null) throw new ArgumentNullException("item");
            if (!CommentsAllowed && !(item is OptionsKey)) throw new NotSupportedException();
            StreamParts.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            StreamParts.RemoveAt(index);
        }

        public IOptionsStreamPart this[int index]
        {
            get
            {
                return StreamParts[index];
            }
            set
            {
                if (!CommentsAllowed && !(value is OptionsKey)) throw new NotSupportedException();
                StreamParts[index] = value;
            }
        }

        #endregion

        #endregion

        #region public Methods

        public OptionsKey FindName(string name)
        {
            return StreamParts.Find((osp) =>
            {
                if (!(osp is OptionsKey)) return false;
                return (osp as OptionsKey).Name == name;
            }) as OptionsKey;
        }

        public void RemoveAllComments()
        {
            if (!CommentsAllowed) return;
            for (int i = 0; i < Count; ++i)
                if (!(this[i] is OptionsKey))
                {
                    RemoveAt(i);
                    i--;
                }
        }

        public OptionsSearchCollection GetSearch()
        {
            return new OptionsSearchCollection
            {
                StreamParts = StreamParts.ToList()
            };
        }

        #endregion

        #region static Methods

        public static void Sort(OptionsCollection collection, Comparison<IOptionsStreamPart> comparison)
        {
            if (collection == null) throw new ArgumentNullException("collection");
            if (comparison == null) throw new ArgumentNullException("comparison");
            collection.StreamParts.Sort(comparison);
        }

        #endregion

        #region public GetValue

        public string GetString(string name)
        {
            var key = FindName(name);
            if (key == null) throw new GenC.KeyNotFoundException();
            return key.GetString();
        }

        public string GetString(string name, string defaultValue)
        {
            var key = FindName(name);
            if (key == null) return defaultValue;
            return key.GetString();
        }

        public bool GetBool(string name)
        {
            var key = FindName(name);
            if (key == null) throw new GenC.KeyNotFoundException();
            return key.GetBool();
        }

        public bool GetBool(string name, bool defaultValue)
        {
            var key = FindName(name);
            if (key == null) return defaultValue;
            return key.GetBool();
        }

        public byte GetByte(string name)
        {
            var key = FindName(name);
            if (key == null) throw new GenC.KeyNotFoundException();
            return key.GetByte();
        }

        public byte GetByte(string name, byte defaultValue)
        {
            var key = FindName(name);
            if (key == null) return defaultValue;
            return key.GetByte();
        }

        public short GetInt16(string name)
        {
            var key = FindName(name);
            if (key == null) throw new GenC.KeyNotFoundException();
            return key.GetInt16();
        }

        public short GetInt16(string name, short defaultValue)
        {
            var key = FindName(name);
            if (key == null) return defaultValue;
            return key.GetInt16();
        }

        public int GetInt32(string name)
        {
            var key = FindName(name);
            if (key == null) throw new GenC.KeyNotFoundException();
            return key.GetInt32();
        }

        public int GetInt32(string name, int defaultValue)
        {
            var key = FindName(name);
            if (key == null) return defaultValue;
            return key.GetInt32();
        }

        public long GetInt64(string name)
        {
            var key = FindName(name);
            if (key == null) throw new GenC.KeyNotFoundException();
            return key.GetInt64();
        }

        public long GetInt64(string name, long defaultValue)
        {
            var key = FindName(name);
            if (key == null) return defaultValue;
            return key.GetInt64();
        }

        public sbyte GetSByte(string name)
        {
            var key = FindName(name);
            if (key == null) throw new GenC.KeyNotFoundException();
            return key.GetSByte();
        }

        public sbyte GetSByte(string name, sbyte defaultValue)
        {
            var key = FindName(name);
            if (key == null) return defaultValue;
            return key.GetSByte();
        }

        public ushort GetUInt16(string name)
        {
            var key = FindName(name);
            if (key == null) throw new GenC.KeyNotFoundException();
            return key.GetUInt16();
        }

        public ushort GetUInt16(string name, ushort defaultValue)
        {
            var key = FindName(name);
            if (key == null) return defaultValue;
            return key.GetUInt16();
        }

        public uint GetUInt32(string name)
        {
            var key = FindName(name);
            if (key == null) throw new GenC.KeyNotFoundException();
            return key.GetUInt32();
        }

        public uint GetUInt32(string name, uint defaultValue)
        {
            var key = FindName(name);
            if (key == null) return defaultValue;
            return key.GetUInt32();
        }

        public ulong GetUInt64(string name)
        {
            var key = FindName(name);
            if (key == null) throw new GenC.KeyNotFoundException();
            return key.GetUInt64();
        }

        public ulong GetUInt64(string name, ulong defaultValue)
        {
            var key = FindName(name);
            if (key == null) return defaultValue;
            return key.GetUInt64();
        }

        public float GetFloat(string name)
        {
            var key = FindName(name);
            if (key == null) throw new GenC.KeyNotFoundException();
            return key.GetFloat();
        }

        public float GetFloat(string name, float defaultValue)
        {
            var key = FindName(name);
            if (key == null) return defaultValue;
            return key.GetFloat();
        }

        public double GetDouble(string name)
        {
            var key = FindName(name);
            if (key == null) throw new GenC.KeyNotFoundException();
            return key.GetDouble();
        }

        public double GetDouble(string name, double defaultValue)
        {
            var key = FindName(name);
            if (key == null) return defaultValue;
            return key.GetDouble();
        }

        public decimal GetDecimal(string name)
        {
            var key = FindName(name);
            if (key == null) throw new GenC.KeyNotFoundException();
            return key.GetDecimal();
        }

        public decimal GetDecimal(string name, decimal defaultValue)
        {
            var key = FindName(name);
            if (key == null) return defaultValue;
            return key.GetDecimal();
        }

        public char GetChar(string name)
        {
            var key = FindName(name);
            if (key == null) throw new GenC.KeyNotFoundException();
            return key.GetChar();
        }

        public char GetChar(string name, char defaultValue)
        {
            var key = FindName(name);
            if (key == null) return defaultValue;
            return key.GetChar();
        }

        public object GetEnum(string name, Type enumType)
        {
            var key = FindName(name);
            if (key == null) throw new GenC.KeyNotFoundException();
            return key.GetEnum(enumType);
        }

        public object GetEnum(string name, Type enumType, object defaultValue)
        {
            var key = FindName(name);
            if (key == null) return defaultValue;
            return key.GetEnum(enumType);
        }

        public T GetEnum<T>(string name)
        {
            var key = FindName(name);
            if (key == null) throw new GenC.KeyNotFoundException();
            return key.GetEnum<T>();
        }

        public T GetEnum<T>(string name, T defaultValue)
        {
            var key = FindName(name);
            if (key == null) return defaultValue;
            return key.GetEnum<T>();
        }

        public byte[] GetByteArray(string name, BinaryParseOption option)
        {
            var key = FindName(name);
            if (key == null) throw new GenC.KeyNotFoundException();
            return key.GetByteArray(option);
        }

        public byte[] GetByteArray(string name, BinaryParseOption option, byte[] defaultValue)
        {
            var key = FindName(name);
            if (key == null) return defaultValue;
            return key.GetByteArray(option);
        }

        #endregion

        #region public SetValue

        public void SetValue(string name, string value)
        {
            var key = FindName(name);
            if (key == null) Add(key = new OptionsKey(name, value: value));
            key.SetValue(value);
        }

        public void SetValue(string name, bool value)
        {
            var key = FindName(name);
            if (key == null) Add(key = new OptionsKey(name, value: value));
            key.SetValue(value);
        }

        public void SetValue(string name, byte value)
        {
            var key = FindName(name);
            if (key == null) Add(key = new OptionsKey(name, value: value));
            key.SetValue(value);
        }

        public void SetValue(string name, short value)
        {
            var key = FindName(name);
            if (key == null) Add(key = new OptionsKey(name, value: value));
            key.SetValue(value);
        }

        public void SetValue(string name, int value)
        {
            var key = FindName(name);
            if (key == null) Add(key = new OptionsKey(name, value: value));
            key.SetValue(value);
        }

        public void SetValue(string name, long value)
        {
            var key = FindName(name);
            if (key == null) Add(key = new OptionsKey(name, value: value));
            key.SetValue(value);
        }

        public void SetValue(string name, sbyte value)
        {
            var key = FindName(name);
            if (key == null) Add(key = new OptionsKey(name, value: value));
            key.SetValue(value);
        }

        public void SetValue(string name, ushort value)
        {
            var key = FindName(name);
            if (key == null) Add(key = new OptionsKey(name, value: value));
            key.SetValue(value);
        }

        public void SetValue(string name, uint value)
        {
            var key = FindName(name);
            if (key == null) Add(key = new OptionsKey(name, value: value));
            key.SetValue(value);
        }

        public void SetValue(string name, ulong value)
        {
            var key = FindName(name);
            if (key == null) Add(key = new OptionsKey(name, value: value));
            key.SetValue(value);
        }

        public void SetValue(string name, float value)
        {
            var key = FindName(name);
            if (key == null) Add(key = new OptionsKey(name, value: value));
            key.SetValue(value);
        }

        public void SetValue(string name, double value)
        {
            var key = FindName(name);
            if (key == null) Add(key = new OptionsKey(name, value: value));
            key.SetValue(value);
        }

        public void SetValue(string name, decimal value)
        {
            var key = FindName(name);
            if (key == null) Add(key = new OptionsKey(name, value: value));
            key.SetValue(value);
        }

        public void SetValue(string name, char value)
        {
            var key = FindName(name);
            if (key == null) Add(key = new OptionsKey(name, value: value));
            key.SetValue(value);
        }

        public void SetValue<T>(string name, T enumValue)
        {
            var key = FindName(name);
            if (key == null) Add(key = new OptionsKey(name, enumValue: enumValue));
            key.SetValue<T>(enumValue);
        }

        public void SetValue(string name, byte[] value, BinaryParseOption option)
        {
            var key = FindName(name);
            if (key == null) Add(key = new OptionsKey(name, value, option));
            key.SetValue(value);
        }

        #endregion

        #region Fast Access

        public OptionsMeta FastAdd(string metaText)
        {
            var meta = new OptionsMeta(metaText);
            Add(meta);
            return meta;
        }

        public OptionsEmpty FastAdd()
        {
            var empty = new OptionsEmpty();
            Add(empty);
            return empty;
        }

        public OptionsKey FastAdd(string keyName, bool value)
        {
            var key = new OptionsKey(keyName, value);
            Add(key);
            return key;
        }

        public OptionsKey FastAdd(string keyName, byte value)
        {
            var key = new OptionsKey(keyName, value);
            Add(key);
            return key;
        }

        public OptionsKey FastAdd(string keyName, short value)
        {
            var key = new OptionsKey(keyName, value);
            Add(key);
            return key;
        }

        public OptionsKey FastAdd(string keyName, int value)
        {
            var key = new OptionsKey(keyName, value);
            Add(key);
            return key;
        }

        public OptionsKey FastAdd(string keyName, long value)
        {
            var key = new OptionsKey(keyName, value);
            Add(key);
            return key;
        }

        public OptionsKey FastAdd(string keyName, sbyte value)
        {
            var key = new OptionsKey(keyName, value);
            Add(key);
            return key;
        }

        public OptionsKey FastAdd(string keyName, ushort value)
        {
            var key = new OptionsKey(keyName, value);
            Add(key);
            return key;
        }

        public OptionsKey FastAdd(string keyName, uint value)
        {
            var key = new OptionsKey(keyName, value);
            Add(key);
            return key;
        }

        public OptionsKey FastAdd(string keyName, ulong value)
        {
            var key = new OptionsKey(keyName, value);
            Add(key);
            return key;
        }

        public OptionsKey FastAdd(string keyName, float value)
        {
            var key = new OptionsKey(keyName, value);
            Add(key);
            return key;
        }

        public OptionsKey FastAdd(string keyName, double value)
        {
            var key = new OptionsKey(keyName, value);
            Add(key);
            return key;
        }

        public OptionsKey FastAdd(string keyName, decimal value)
        {
            var key = new OptionsKey(keyName, value);
            Add(key);
            return key;
        }

        public OptionsKey FastAdd(string keyName, string value)
        {
            var key = new OptionsKey(keyName, value);
            Add(key);
            return key;
        }

        public OptionsKey FastAdd(string keyName, char value)
        {
            var key = new OptionsKey(keyName, value);
            Add(key);
            return key;
        }

        public OptionsKey FastAdd(string keyName, object enumValue)
        {
            var key = new OptionsKey(keyName, enumValue);
            Add(key);
            return key;
        }

        public OptionsKey FastAdd(string keyName, byte[] value, BinaryParseOption option)
        {
            var key = new OptionsKey(keyName, value, option);
            Add(key);
            return key;
        }

        #endregion
    }
    /// <summary>
    /// Eine <see cref="OptionsGroupCollection"/>, die zum Heraussuchen gedacht ist.
    /// </summary>
    public class OptionsGroupSearchCollection : OptionsGroupCollection
    {
        internal OptionsGroupSearchCollection() : base() { }
        /// <summary>
        /// Filtert die Gruppen, ob diese Elemente besitzen.
        /// </summary>
        /// <param name="include">true um nur Gruppen zu haben, die keine Elemente besitzen. false um leere Gruppen zu löschen</param>
        /// <returns>Diese Auflistung</returns>
        public OptionsGroupSearchCollection FilterEmpty(bool include)
        {
            Groups.RemoveAll((og) => include ^ (og.Options.Count == 0));
            return this;
        }
        /// <summary>
        /// Filtert die Gruppen anhand ihres Namens.
        /// </summary>
        /// <param name="name">der zu filternde Name</param>
        /// <param name="include">true um die Gruppen mit den Namen zu behalten. false um diese Gruppen zu löschen.</param>
        /// <returns>Diese Auflistung</returns>
        public OptionsGroupSearchCollection FilterName(string name, bool include)
        {
            Groups.RemoveAll((og) => include ^ (og.Name == name));
            return this;
        }
        /// <summary>
        /// Filtert die Gruppen anhand ihrer Attribute. Hier wird nur ermittelt, ob ein Attribut mit diesen Namen vorhanden ist.
        /// </summary>
        /// <param name="name">Der Name des Attributs anhand dessen gefiltert werden soll.</param>
        /// <param name="include">true um die Gruppen mit diesen Attribut zu behalten. false um diese Gruppen zu löschen.</param>
        /// <returns>Diese Auflistung</returns>
        public OptionsGroupSearchCollection FilterAttribute(string name, bool include)
        {
            Groups.RemoveAll((og) => include ^ (og.Attributes.FindName(name) != null));
            return this;
        }
        /// <summary>
        /// Filtert die Gruppen anhand ihrer Attribute. Hier wird zusätzlich verglichen, ob die Werte identisch sind.
        /// </summary>
        /// <param name="name">Der Name des Attributs anhand dessen gefiltert werden soll.</param>
        /// <param name="value">Der Wert des Attributs anhand dessen gefiltert werden soll.</param>
        /// <param name="include">true um die Gruppen zu behalten, bei denen diese Werte stimmten. false um diese Gruppen zu löschen.</param>
        /// <returns>Diese Auflistung</returns>
        public OptionsGroupSearchCollection FilterAttribute(string name, byte value, bool include)
        {
            var valueText = new OptionsKey(name, value).ValueText;
            Groups.RemoveAll((og) => include ^ (og.Attributes.FindName(name).ValueText == valueText));
            return this;
        }
        /// <summary>
        /// Filtert die Gruppen anhand ihrer Attribute. Hier wird zusätzlich verglichen, ob die Werte identisch sind.
        /// </summary>
        /// <param name="name">Der Name des Attributs anhand dessen gefiltert werden soll.</param>
        /// <param name="value">Der Wert des Attributs anhand dessen gefiltert werden soll.</param>
        /// <param name="include">true um die Gruppen zu behalten, bei denen diese Werte stimmten. false um diese Gruppen zu löschen.</param>
        /// <returns>Diese Auflistung</returns>
        public OptionsGroupSearchCollection FilterAttribute(string name, short value, bool include)
        {
            var valueText = new OptionsKey(name, value).ValueText;
            Groups.RemoveAll((og) => include ^ (og.Attributes.FindName(name).ValueText == valueText));
            return this;
        }
        /// <summary>
        /// Filtert die Gruppen anhand ihrer Attribute. Hier wird zusätzlich verglichen, ob die Werte identisch sind.
        /// </summary>
        /// <param name="name">Der Name des Attributs anhand dessen gefiltert werden soll.</param>
        /// <param name="value">Der Wert des Attributs anhand dessen gefiltert werden soll.</param>
        /// <param name="include">true um die Gruppen zu behalten, bei denen diese Werte stimmten. false um diese Gruppen zu löschen.</param>
        /// <returns>Diese Auflistung</returns>
        public OptionsGroupSearchCollection FilterAttribute(string name, int value, bool include)
        {
            var valueText = new OptionsKey(name, value).ValueText;
            Groups.RemoveAll((og) => include ^ (og.Attributes.FindName(name).ValueText == valueText));
            return this;
        }
        /// <summary>
        /// Filtert die Gruppen anhand ihrer Attribute. Hier wird zusätzlich verglichen, ob die Werte identisch sind.
        /// </summary>
        /// <param name="name">Der Name des Attributs anhand dessen gefiltert werden soll.</param>
        /// <param name="value">Der Wert des Attributs anhand dessen gefiltert werden soll.</param>
        /// <param name="include">true um die Gruppen zu behalten, bei denen diese Werte stimmten. false um diese Gruppen zu löschen.</param>
        /// <returns>Diese Auflistung</returns>
        public OptionsGroupSearchCollection FilterAttribute(string name, long value, bool include)
        {
            var valueText = new OptionsKey(name, value).ValueText;
            Groups.RemoveAll((og) => include ^ (og.Attributes.FindName(name).ValueText == valueText));
            return this;
        }
        /// <summary>
        /// Filtert die Gruppen anhand ihrer Attribute. Hier wird zusätzlich verglichen, ob die Werte identisch sind.
        /// </summary>
        /// <param name="name">Der Name des Attributs anhand dessen gefiltert werden soll.</param>
        /// <param name="value">Der Wert des Attributs anhand dessen gefiltert werden soll.</param>
        /// <param name="include">true um die Gruppen zu behalten, bei denen diese Werte stimmten. false um diese Gruppen zu löschen.</param>
        /// <returns>Diese Auflistung</returns>
        public OptionsGroupSearchCollection FilterAttribute(string name, sbyte value, bool include)
        {
            var valueText = new OptionsKey(name, value).ValueText;
            Groups.RemoveAll((og) => include ^ (og.Attributes.FindName(name).ValueText == valueText));
            return this;
        }
        /// <summary>
        /// Filtert die Gruppen anhand ihrer Attribute. Hier wird zusätzlich verglichen, ob die Werte identisch sind.
        /// </summary>
        /// <param name="name">Der Name des Attributs anhand dessen gefiltert werden soll.</param>
        /// <param name="value">Der Wert des Attributs anhand dessen gefiltert werden soll.</param>
        /// <param name="include">true um die Gruppen zu behalten, bei denen diese Werte stimmten. false um diese Gruppen zu löschen.</param>
        /// <returns>Diese Auflistung</returns>
        public OptionsGroupSearchCollection FilterAttribute(string name, ushort value, bool include)
        {
            var valueText = new OptionsKey(name, value).ValueText;
            Groups.RemoveAll((og) => include ^ (og.Attributes.FindName(name).ValueText == valueText));
            return this;
        }
        /// <summary>
        /// Filtert die Gruppen anhand ihrer Attribute. Hier wird zusätzlich verglichen, ob die Werte identisch sind.
        /// </summary>
        /// <param name="name">Der Name des Attributs anhand dessen gefiltert werden soll.</param>
        /// <param name="value">Der Wert des Attributs anhand dessen gefiltert werden soll.</param>
        /// <param name="include">true um die Gruppen zu behalten, bei denen diese Werte stimmten. false um diese Gruppen zu löschen.</param>
        /// <returns>Diese Auflistung</returns>
        public OptionsGroupSearchCollection FilterAttribute(string name, uint value, bool include)
        {
            var valueText = new OptionsKey(name, value).ValueText;
            Groups.RemoveAll((og) => include ^ (og.Attributes.FindName(name).ValueText == valueText));
            return this;
        }
        /// <summary>
        /// Filtert die Gruppen anhand ihrer Attribute. Hier wird zusätzlich verglichen, ob die Werte identisch sind.
        /// </summary>
        /// <param name="name">Der Name des Attributs anhand dessen gefiltert werden soll.</param>
        /// <param name="value">Der Wert des Attributs anhand dessen gefiltert werden soll.</param>
        /// <param name="include">true um die Gruppen zu behalten, bei denen diese Werte stimmten. false um diese Gruppen zu löschen.</param>
        /// <returns>Diese Auflistung</returns>
        public OptionsGroupSearchCollection FilterAttribute(string name, ulong value, bool include)
        {
            var valueText = new OptionsKey(name, value).ValueText;
            Groups.RemoveAll((og) => include ^ (og.Attributes.FindName(name).ValueText == valueText));
            return this;
        }
        /// <summary>
        /// Filtert die Gruppen anhand ihrer Attribute. Hier wird zusätzlich verglichen, ob die Werte identisch sind.
        /// </summary>
        /// <param name="name">Der Name des Attributs anhand dessen gefiltert werden soll.</param>
        /// <param name="value">Der Wert des Attributs anhand dessen gefiltert werden soll.</param>
        /// <param name="include">true um die Gruppen zu behalten, bei denen diese Werte stimmten. false um diese Gruppen zu löschen.</param>
        /// <returns>Diese Auflistung</returns>
        public OptionsGroupSearchCollection FilterAttribute(string name, float value, bool include)
        {
            var valueText = new OptionsKey(name, value).ValueText;
            Groups.RemoveAll((og) => include ^ (og.Attributes.FindName(name).ValueText == valueText));
            return this;
        }
        /// <summary>
        /// Filtert die Gruppen anhand ihrer Attribute. Hier wird zusätzlich verglichen, ob die Werte identisch sind.
        /// </summary>
        /// <param name="name">Der Name des Attributs anhand dessen gefiltert werden soll.</param>
        /// <param name="value">Der Wert des Attributs anhand dessen gefiltert werden soll.</param>
        /// <param name="include">true um die Gruppen zu behalten, bei denen diese Werte stimmten. false um diese Gruppen zu löschen.</param>
        /// <returns>Diese Auflistung</returns>
        public OptionsGroupSearchCollection FilterAttribute(string name, double value, bool include)
        {
            var valueText = new OptionsKey(name, value).ValueText;
            Groups.RemoveAll((og) => include ^ (og.Attributes.FindName(name).ValueText == valueText));
            return this;
        }
        /// <summary>
        /// Filtert die Gruppen anhand ihrer Attribute. Hier wird zusätzlich verglichen, ob die Werte identisch sind.
        /// </summary>
        /// <param name="name">Der Name des Attributs anhand dessen gefiltert werden soll.</param>
        /// <param name="value">Der Wert des Attributs anhand dessen gefiltert werden soll.</param>
        /// <param name="include">true um die Gruppen zu behalten, bei denen diese Werte stimmten. false um diese Gruppen zu löschen.</param>
        /// <returns>Diese Auflistung</returns>
        public OptionsGroupSearchCollection FilterAttribute(string name, decimal value, bool include)
        {
            var valueText = new OptionsKey(name, value).ValueText;
            Groups.RemoveAll((og) => include ^ (og.Attributes.FindName(name).ValueText == valueText));
            return this;
        }
        /// <summary>
        /// Filtert die Gruppen anhand ihrer Attribute. Hier wird zusätzlich verglichen, ob die Werte identisch sind.
        /// </summary>
        /// <param name="name">Der Name des Attributs anhand dessen gefiltert werden soll.</param>
        /// <param name="value">Der Wert des Attributs anhand dessen gefiltert werden soll.</param>
        /// <param name="include">true um die Gruppen zu behalten, bei denen diese Werte stimmten. false um diese Gruppen zu löschen.</param>
        /// <returns>Diese Auflistung</returns>
        public OptionsGroupSearchCollection FilterAttribute(string name, char value, bool include)
        {
            var valueText = new OptionsKey(name, value).ValueText;
            Groups.RemoveAll((og) => include ^ (og.Attributes.FindName(name).ValueText == valueText));
            return this;
        }
        /// <summary>
        /// Filtert die Gruppen anhand ihrer Attribute. Hier wird zusätzlich verglichen, ob die Werte identisch sind.
        /// </summary>
        /// <param name="name">Der Name des Attributs anhand dessen gefiltert werden soll.</param>
        /// <param name="value">Der Wert des Attributs anhand dessen gefiltert werden soll.</param>
        /// <param name="include">true um die Gruppen zu behalten, bei denen diese Werte stimmten. false um diese Gruppen zu löschen.</param>
        /// <returns>Diese Auflistung</returns>
        public OptionsGroupSearchCollection FilterAttribute(string name, string value, bool include)
        {
            var valueText = new OptionsKey(name, value).ValueText;
            Groups.RemoveAll((og) => include ^ (og.Attributes.FindName(name).ValueText == valueText));
            return this;
        }
        /// <summary>
        /// Filtert die Gruppen anhand ihrer Attribute. Hier wird zusätzlich verglichen, ob die Werte identisch sind.
        /// </summary>
        /// <param name="name">Der Name des Attributs anhand dessen gefiltert werden soll.</param>
        /// <param name="value">Der Wert des Attributs anhand dessen gefiltert werden soll.</param>
        /// <param name="include">true um die Gruppen zu behalten, bei denen diese Werte stimmten. false um diese Gruppen zu löschen.</param>
        /// <returns>Diese Auflistung</returns>
        public OptionsGroupSearchCollection FilterAttribute(string name, object enumValue, bool include)
        {
            var valueText = new OptionsKey(name, enumValue).ValueText;
            Groups.RemoveAll((og) => include ^ (og.Attributes.FindName(name).ValueText == valueText));
            return this;
        }
        /// <summary>
        /// Filtert die Gruppen anhand ihrer Attribute. Hier wird zusätzlich verglichen, ob die Werte identisch sind.
        /// </summary>
        /// <param name="name">Der Name des Attributs anhand dessen gefiltert werden soll.</param>
        /// <param name="value">Der Wert des Attributs anhand dessen gefiltert werden soll.</param>
        /// <param name="include">true um die Gruppen zu behalten, bei denen diese Werte stimmten. false um diese Gruppen zu löschen.</param>
        /// <returns>Diese Auflistung</returns>
        public OptionsGroupSearchCollection FilterAttribute(string name, byte[] value, BinaryParseOption option, bool include)
        {
            var valueText = new OptionsKey(name, value, option).ValueText;
            Groups.RemoveAll((og) => include ^ (og.Attributes.FindName(name).ValueText == valueText));
            return this;
        }
        /// <summary>
        /// Filtert die Gruppen anhand eines Benutzerdefinierten Filters.
        /// </summary>
        /// <param name="match">Der Ausdruck, anhand dessen gefiltert werden soll.</param>
        /// <param name="include">true um die gefundenen Elemente zu behalten, false um diese zu entfernen.</param>
        /// <returns>Diese Auflistung</returns>
        public OptionsGroupSearchCollection Filter(Predicate<OptionsGroup> match, bool include)
        {
            Groups.RemoveAll((og) => include ^ match(og));
            return this;
        }
    }
    /// <summary>
    /// Eine <see cref="OptionsCollection"/>, die zum Heraussuchen gedacht ist.
    /// </summary>
    public class OptionsSearchCollection : OptionsCollection
    {
        internal OptionsSearchCollection() : base(true) { }
        /// <summary>
        /// Filtert die Meta Angaben. Ist include wahr, dann werden im Ergebnis nur Meta Angaben enthalten sein. Andernfalls werden 
        /// alle Meta Angaben eliminiert.
        /// </summary>
        /// <param name="include">true um alle Meta Angaben herauszusuchen. false um alle Meta Angaben zu eliminieren.</param>
        /// <returns>Diese Auflistung</returns>
        public OptionsSearchCollection FilterMeta(bool include)
        {
            StreamParts.RemoveAll((osp) => include ^ (osp is OptionsMeta));
            return this;
        }
        /// <summary>
        /// Filtert die Leere Zeilen. Ist include wahr, dann werden im Ergebnis nur die Leeren Zeilen enthalten sein. Andernfalls werden 
        /// alle Leeren Zeilen eliminiert.
        /// </summary>
        /// <param name="include">true um alle Leeren Zeilen herauszusuchen. false um alle Leeren Zeilen zu eliminieren.</param>
        /// <returns>Diese Auflistung</returns>
        public OptionsSearchCollection FilterEmpty(bool include)
        {
            StreamParts.RemoveAll((osp) => include ^ (osp is OptionsEmpty));
            return this;
        }
        /// <summary>
        /// Filtert die Keys. Ist include wahr, dann werden im Ergebnis nur Keys enthalten sein. Andernfalls werden 
        /// alle Keys eliminiert.
        /// </summary>
        /// <param name="include">true um alle Keys herauszusuchen. false um alle Keys zu eliminieren.</param>
        /// <returns>Diese Auflistung</returns>
        public OptionsSearchCollection FilterKeys(bool include)
        {
            StreamParts.RemoveAll((osp) => include ^ (osp is OptionsKey));
            return this;
        }
        /// <summary>
        /// Filtert die Optionen anhand der Namen der Keys. Zusätzlich kann angegeben werden, ob Elemente, die kein Key sind, auch 
        /// entfernt werden sollen.
        /// </summary>
        /// <param name="name">Der Name des Keys, der herausgesucht werden soll.</param>
        /// <param name="include">true um alle gefundenen Keys zu behalten, false um alle gefundenen Keys zu entfernen.</param>
        /// <param name="ignoreOtherTags">true um andere Elemente zu behalten, false um nur Keys im Ergebnis zu haben.</param>
        /// <returns>Diese Auflistung</returns>
        public OptionsSearchCollection FilterKeyNames(string name, bool include, bool ignoreOtherTags = true)
        {
            StreamParts.RemoveAll((osp) =>
            {
                if (osp is OptionsKey) return include ^ ((osp as OptionsKey).Name == name);
                else return !ignoreOtherTags;
            });
            return this;
        }
        /// <summary>
        /// Filtert die Optionen anhand eines benutzerdefinierten Filters.
        /// </summary>
        /// <param name="match">Der Filter anhand dessen gefiltert werden soll.</param>
        /// <param name="include">true um alle gefundenen Elemente zu behalten, false um diese zu entfernen.</param>
        /// <returns>Diese Auflistung</returns>
        public OptionsSearchCollection Filter(Predicate<IOptionsStreamPart> match, bool include)
        {
            StreamParts.RemoveAll((osp) => include ^ match(osp));
            return this;
        }
    }

    #endregion

    #region Data

    public class OptionsGroup : IOptionsStreamPart
    {
        private string name;

        public string Name
        {
            get { return name; }
            set
            {
                name = value ?? throw new ArgumentNullException("Name");
            }
        }

        public OptionsCollection Options { get; private set; }

        public OptionsCollection Attributes { get; private set; }

        public OptionsGroup()
        {
            Name = "Unnamed Group";
            Options = new OptionsCollection(true);
            Attributes = new OptionsCollection(false);
        }

        public OptionsGroup(string name)
        {
            Name = name;
            Options = new OptionsCollection(true);
            Attributes = new OptionsCollection(false);
        }

        internal OptionsGroup(bool main)
        {
            Name = main ? "Main Group" : "Unnamed Group";
            Options = new OptionsCollection(true);
            Attributes = main ? null : new OptionsCollection(false);
        }

        public string ToStreamString()
        {
            var sb = new StringBuilder();
            if (Attributes != null)
            {
                sb.Append("[");
                sb.Append(StringFormatter.ValidateNameString(Name));
                if (Attributes.Count > 0)
                {
                    sb.Append("(");
                    for (int i = 0; i < Attributes.Count; ++i)
                    {
                        sb.Append(Attributes[i].ToStreamString());
                        if (i < Attributes.Count - 1) sb.Append(';');
                    }
                    sb.Append(")");
                }
                sb.Append("]");
            }
            for (int i = 0; i < Options.Count; ++i)
            {
                if (Attributes != null || i > 0) sb.AppendLine();
                sb.Append(Options[i].ToStreamString());
            }
            return sb.ToString();
        }
    }

    public interface IOptionsStreamPart
    {
        string ToStreamString();
    }

    public class OptionsEmpty : IOptionsStreamPart
    {
        public OptionsEmpty() { }

        public string ToStreamString()
        {
            return "";
        }
    }

    public class OptionsMeta : IOptionsStreamPart
    {
        public string MetaText { get; set; }

        public OptionsMeta(string metaText)
        {
            MetaText = metaText;
        }

        public override string ToString()
        {
            return "{" + ToStreamString() + "}";
        }

        public string ToStreamString()
        {
            return (MetaText.Length > 0 && MetaText[0] != ' ' ? "# " : "#") + MetaText;
        }
    }

    public class OptionsKey : IOptionsStreamPart
    {
        public string Name { get; private set; }

        public string ValueText { get; private set; }

        internal OptionsKey(string name, string valueText, bool internalValue = true)
        {
            Name = name;
            ValueText = valueText;
        }

        public string ToStreamString()
        {
            return StringFormatter.ValidateNameString(Name) + "=" + ValueText;
        }

        public override string ToString()
        {
            return "{" + ToStreamString() + "}";
        }

        #region public OptionsKey constructors

        public OptionsKey(string name, string value)
        {
            Name = name;
            ValueText = StringFormatter.ToFileString(value);
        }

        public OptionsKey(string name, bool value)
        {
            Name = name;
            ValueText = value.ToString().ToLower();
        }

        public OptionsKey(string name, byte value)
        {
            Name = name;
            ValueText = value.ToString();
        }

        public OptionsKey(string name, short value)
        {
            Name = name;
            ValueText = value.ToString();
        }

        public OptionsKey(string name, int value)
        {
            Name = name;
            ValueText = value.ToString();
        }

        public OptionsKey(string name, long value)
        {
            Name = name;
            ValueText = value.ToString();
        }

        public OptionsKey(string name, sbyte value)
        {
            Name = name;
            ValueText = value.ToString();
        }

        public OptionsKey(string name, ushort value)
        {
            Name = name;
            ValueText = value.ToString();
        }

        public OptionsKey(string name, uint value)
        {
            Name = name;
            ValueText = value.ToString();
        }

        public OptionsKey(string name, ulong value)
        {
            Name = name;
            ValueText = value.ToString();
        }

        public OptionsKey(string name, float value)
        {
            Name = name;
            ValueText = value.ToString();
        }

        public OptionsKey(string name, double value)
        {
            Name = name;
            ValueText = value.ToString();
        }

        public OptionsKey(string name, decimal value)
        {
            Name = name;
            ValueText = value.ToString();
        }

        public OptionsKey(string name, char value)
        {
            Name = name;
            ValueText = ((int)value).ToString();
        }

        public OptionsKey(string name, object enumValue)
        {
            Name = name;
            if (!(enumValue is Enum)) throw new FormatException("this argument isn't an Enum");
            ValueText = enumValue.ToString();
        }

        public OptionsKey(string name, byte[] value, BinaryParseOption option)
        {
            Name = name;
            SetValue(value, option);
        }

        #endregion

        #region public GetValue

        public string GetString()
        {
            return StringFormatter.ResolveValidationName(ValueText);
        }

        public bool GetBool()
        {
            return bool.Parse(ValueText);
        }

        public byte GetByte()
        {
            return byte.Parse(ValueText);
        }

        public short GetInt16()
        {
            return short.Parse(ValueText);
        }

        public int GetInt32()
        {
            return int.Parse(ValueText);
        }

        public long GetInt64()
        {
            return long.Parse(ValueText);
        }

        public sbyte GetSByte()
        {
            return sbyte.Parse(ValueText);
        }

        public ushort GetUInt16()
        {
            return ushort.Parse(ValueText);
        }

        public uint GetUInt32()
        {
            return uint.Parse(ValueText);
        }

        public ulong GetUInt64()
        {
            return ulong.Parse(ValueText);
        }

        public float GetFloat()
        {
            return float.Parse(ValueText);
        }

        public double GetDouble()
        {
            return double.Parse(ValueText);
        }

        public decimal GetDecimal()
        {
            return decimal.Parse(ValueText);
        }

        public char GetChar()
        {
            return (char)GetInt32();
        }

        public object GetEnum(Type enumType)
        {
            if (!enumType.IsSubclassOf(typeof(Enum))) throw new FormatException("this argument isn't an Enum");
            return Enum.Parse(enumType, ValueText);
        }

        public T GetEnum<T>()
        {
            if (!typeof(T).IsSubclassOf(typeof(Enum))) throw new FormatException("this argument isn't an Enum");
            return (T)Enum.Parse(typeof(T), ValueText);
        }

        public byte[] GetByteArray(BinaryParseOption option)
        {
            switch (option)
            {
                case BinaryParseOption.HexCode:
                    {
                        var hex = "0123456789abcdef";
                        var l = new List<byte>();
                        int now = 0; bool addlist = false;
                        for (int i = 0; i < ValueText.Length; ++i)
                        {
                            if (!hex.Contains(ValueText[i])) continue;
                            now = 16 * now + hex.IndexOf(ValueText[i]);
                            if (addlist) { l.Add((byte)now); now = 0; }
                            addlist = !addlist;
                        }
                        return l.ToArray();
                    }
                case BinaryParseOption.Base64:
                    return System.Convert.FromBase64String(ValueText);
                default: throw new NotImplementedException(option.ToString() + " is not implemented");
            }
        }

        #endregion

        #region public SetValue

        public void SetValue(string value)
        {
            ValueText = StringFormatter.ToFileString(value);
        }

        public void SetValue(bool value)
        {
            ValueText = value.ToString().ToLower();
        }

        public void SetValue(byte value)
        {

            ValueText = value.ToString();
        }

        public void SetValue(short value)
        {

            ValueText = value.ToString();
        }

        public void SetValue(int value)
        {

            ValueText = value.ToString();
        }

        public void SetValue(long value)
        {

            ValueText = value.ToString();
        }

        public void SetValue(sbyte value)
        {

            ValueText = value.ToString();
        }

        public void SetValue(ushort value)
        {

            ValueText = value.ToString();
        }

        public void SetValue(uint value)
        {

            ValueText = value.ToString();
        }

        public void SetValue(ulong value)
        {

            ValueText = value.ToString();
        }

        public void SetValue(float value)
        {

            ValueText = value.ToString();
        }

        public void SetValue(double value)
        {

            ValueText = value.ToString();
        }

        public void SetValue(decimal value)
        {

            ValueText = value.ToString();
        }

        public void SetValue(char value)
        {

            ValueText = ((int)value).ToString();
        }

        public void SetValue<T>(T enumValue)
        {
            if (!(enumValue is Enum)) throw new FormatException("this argument isn't an Enum");
            ValueText = enumValue.ToString();
        }

        public void SetValue(byte[] value, BinaryParseOption option)
        {
            if (value == null) throw new ArgumentNullException("value");
            switch (option)
            {
                case BinaryParseOption.HexCode:
                    {
                        var hex = "0123456789abcdef";
                        var sb = new StringBuilder();
                        for (int i = 0; i < value.Length; ++i)
                        {
                            sb.Append(hex[value[i] / 16]);
                            sb.Append(hex[value[i] % 16]);
                            sb.Append(' ');
                        }
                        ValueText = sb.ToString();
                    }
                    break;
                case BinaryParseOption.Base64:
                    ValueText = System.Convert.ToBase64String(value);
                    break;
                default: throw new NotImplementedException(option.ToString() + " is not implemented");
            }
        }

        public void SetValueText(string valueText)
        {
            ValueText = valueText ?? throw new ArgumentNullException("valueText");
        }

        #endregion
    }

    public enum BinaryParseOption
    {
        HexCode,
        Base64
    }

    static class StringFormatter
    {
        static string[] repl = new[]{
                "\\", "\\\\",
                "\"", "\\\"",
                "\t", "\\t",
                "\r", "\\r",
                "\n", "\\n",
                "(",  "\\(",
                ")",  "\\)",
                ";",  "\\;",
            };

        public static string ToFileString(string valueString)
        {
            var sb = new StringBuilder(valueString);
            for (int i = 0; i < repl.Length; i += 2) sb.Replace(repl[i], repl[i + 1]);
            return "\"" + sb.ToString() + "\"";
        }

        public static string ToValueString(string fileString)
        {
            if (fileString == "\"\"" || fileString == "") return "";
            var sb = new StringBuilder();
            for (int i = 1; i < fileString.Length - 1; ++i)
                if (fileString[i] == '\\' && i < fileString.Length - 2)
                {
                    var part = "" + fileString[i] + fileString[i + 1];
                    if (repl.Contains(part))
                    {
                        var ind = 0;
                        for (; ind < repl.Length; ind += 2) if (repl[ind + 1] == part) break;
                        sb.Append(repl[ind]);
                        i++;
                    }
                }
                else sb.Append(fileString[i]);
            return sb.ToString();
            //var sb = new StringBuilder(fileString);
            //sb.Remove(0, 1);
            //sb.Remove(sb.Length - 1, 1);
            //if (sb.Length == 0) return "";
            //for (int i = repl.Length - 2; i >= 0; i -= 2) sb.Replace(repl[i + 1], repl[i]);
            //return sb.ToString();
        }

        const string markers = "=";

        public static string ValidateNameString(string name)
        {
            for (int i = 0; i < markers.Length; ++i) if (name.Contains(markers[i])) return ToFileString(name);
            for (int i = 0; i < repl.Length; i += 2) if (name.Contains(repl[i])) return ToFileString(name);
            return name;
        }

        public static string ResolveValidationName(string name)
        {
            if (name.StartsWith("\"")) return ToValueString(name);
            return name;
        }
    }

    #endregion

    #region Parser

    interface IOptionParser<Source, Target>
    {
        Target Parse(Source source);
    }

    interface IOptionStringParser<Target> : IOptionParser<string, Target>
    {

    }

    class MetaParser : IOptionStringParser<OptionsMeta>, IOptionStringParser<object>
    {
        public OptionsMeta Parse(string source)
        {
            var sb = new StringBuilder(source);
            sb.Remove(0, 1);
            while (sb[0] == ' ') sb.Remove(0, 1);
            return new OptionsMeta(sb.ToString());
        }

        object IOptionParser<string, object>.Parse(string source)
        {
            return ((IOptionStringParser<OptionsMeta>)this).Parse(source);
        }
    }

    class EmptyParser : IOptionStringParser<OptionsEmpty>, IOptionStringParser<object>
    {
        public OptionsEmpty Parse(string source)
        {
            return new OptionsEmpty();
        }

        object IOptionParser<string, object>.Parse(string source)
        {
            return ((IOptionStringParser<OptionsEmpty>)this).Parse(source);
        }
    }

    class KeyParser : IOptionStringParser<OptionsKey>, IOptionStringParser<object>
    {
        public OptionsKey Parse(string source)
        {
            var key = "";
            var value = "";
            if (source.StartsWith("\""))
            {
                var ind = 1;
                for (; ind < source.Length - 1; ind++)
                {
                    if (source[ind] != '"') continue;
                    if (source[ind - 1] == '\\') continue;
                    break;
                }
                ind++;
                if (source[ind] != '=') throw new System.IO.InvalidDataException();
                key = StringFormatter.ResolveValidationName(source.Remove(ind));
                value = source.Substring(ind + 1);
            }
            else
            {
                var ind = source.IndexOf('=');
                key = source.Remove(ind);
                value = source.Substring(ind + 1);
            }
            return new OptionsKey(key, valueText: value);
        }

        object IOptionParser<string, object>.Parse(string source)
        {
            return ((IOptionStringParser<OptionsKey>)this).Parse(source);
        }
    }

    class GroupParser : IOptionStringParser<OptionsGroup>, IOptionStringParser<object>
    {
        public OptionsGroup Parse(string source)
        {
            var name = "";
            var att = new List<OptionsKey>();
            source = source.Substring(1, source.Length - 2); //[]
            //Name
            if (source.StartsWith("\""))
            {
                var ind = 1;
                for (; ind < source.Length - 1; ind++)
                {
                    if (source[ind] != '"') continue;
                    if (source[ind - 1] == '\\') continue;
                    break;
                }
                ind++;
                name = StringFormatter.ResolveValidationName(source.Remove(ind));
                source = source.Substring(ind);
            }
            else
            {
                var ind = source.IndexOf('(');
                if (ind == -1)
                {
                    name = source;
                    source = "";
                }
                else
                {
                    name = source.Remove(ind);
                    source = source.Substring(ind + 1);
                }
            }
            //Attributes
            if (source != "")
            {
                source = source.Substring(0, source.Length - 1); //()
                var parser = new KeyParser();
                for (int i = 0; i < source.Length; ++i)
                {
                    if (source[i] != ';') continue;
                    if (source[i - 1] == '\\') continue;
                    att.Add(parser.Parse(source.Remove(i)));
                    source = source.Substring(i + 1);
                    i = -1;
                }
                if (source != "") att.Add(parser.Parse(source));
            }
            //Build
            var og = new OptionsGroup(name);
            att.ForEach((ok) => og.Attributes.Add(ok));
            return og;
        }

        object IOptionParser<string, object>.Parse(string source)
        {
            return ((IOptionStringParser<OptionsGroup>)this).Parse(source);
        }
    }

    class LineParser : IOptionStringParser<IOptionsStreamPart>
    {
        public IOptionsStreamPart Parse(string source)
        {
            IOptionStringParser<object> parser;
            if (source.StartsWith("#")) parser = (IOptionStringParser<object>)new MetaParser();
            else if (source.StartsWith("[")) parser = (IOptionStringParser<object>)new GroupParser();
            else if (string.IsNullOrWhiteSpace(source)) parser = (IOptionStringParser<object>)new EmptyParser();
            else parser = (IOptionStringParser<object>)new KeyParser();
            return (IOptionsStreamPart)parser.Parse(source);
        }
    }

    #endregion
}
