using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MaxLib.Data.VirtualIO
{
    /// <summary>
    /// This is a virtual path that is used by the virtual IO. This doesn't map real paths on disk
    /// or other sources. It can only represent some parts of them.
    /// </summary>
    public class VirtualPath : IEnumerable<string>, IEquatable<VirtualPath>
    {
        readonly string[] path;

        public int Length { get; }

        public string this[int index]
        {
            get
            {
                if (index < 0 || index >= Length)
                    throw new ArgumentOutOfRangeException(nameof(index));
                return path[index];
            }
        }

        public bool IsRootPath => Length > 0 && path[0] == "@";

        public bool HasHostFilter => this.FirstOrDefault(part => part == ":") == ":";

        public VirtualPath Parent => Length > 1 ? new VirtualPath(path, Length - 1) : null;

        protected VirtualPath(string[] path, int length)
        {
            this.path = path ?? throw new ArgumentNullException(nameof(path));
            if (length > path.Length)
                throw new ArgumentOutOfRangeException(nameof(length));
            Length = length;
        }

        protected VirtualPath(string[] path)
            : this(path, path.Length)
        { }

        /// <summary>
        /// Combines two virtual paths to one.
        /// </summary>
        /// <param name="basePath">the basic path</param>
        /// <param name="relativePath">the path to add</param>
        /// <returns>returns the combined path</returns>
        public static VirtualPath Combine(VirtualPath basePath, VirtualPath relativePath)
        {
            _ = basePath ?? throw new ArgumentNullException(nameof(basePath));
            _ = relativePath ?? throw new ArgumentNullException(nameof(relativePath));
            if (relativePath.IsRootPath)
                return relativePath;
            var result = new List<string>(basePath.Length + relativePath.Length);
            result.AddRange(basePath);
            for (int i = 0; i<relativePath.Length; ++i)
            {
                var path = relativePath[i];
                switch (path)
                {
                    case ":":
                        if (!result.Contains(":"))
                            result.Add(path);
                        break;
                    case "..":
                        if (result.Count > 0)
                            switch (result[result.Count - 1])
                            {
                                case "..":
                                    result.Add(path);
                                    break;
                                case "@":
                                    break;
                                case ":":
                                    result.RemoveAt(result.Count-1);
                                    i--; //the part before the : need to be trimmed
                                    //therefore repeat this item
                                    break;
                                default:
                                    result.RemoveAt(result.Count - 1);
                                    break;
                            }
                        else result.Add(path);
                        break;
                    default:
                        result.Add(path);
                        break;
                }
            }
            return new VirtualPath(result.ToArray());
            //int walkBack = 0;
            //for (int i = 0; i < relativePath.Length; ++i)
            //    if (relativePath.path[i] != "..")
            //        break;
            //    else walkBack++;
            //if (walkBack > basePath.Length)
            //    walkBack = basePath.Length;
            //var path = new string[basePath.Length + relativePath.Length - walkBack * 2];
            //Array.Copy(basePath.path, 0, path, 0, basePath.Length - walkBack);
            //Array.Copy(relativePath.path, walkBack, path, basePath.Length - walkBack, relativePath.Length - walkBack);
            //return new VirtualPath(path);
        }

        /// <summary>
        /// Combines multiple virtual paths into one.
        /// </summary>
        /// <param name="paths">the paths to combine</param>
        /// <returns>the combined path</returns>
        public static VirtualPath Combine(params VirtualPath[] paths)
        {
            _ = paths ?? throw new ArgumentNullException(nameof(paths));
            VirtualPath result = null;
            foreach (var path in paths)
                if (result == null)
                    result = path;
                else result = Combine(result, path);
            return result ?? new VirtualPath(new string[0]);
        }

        public VirtualPath GetHostFilter()
        {
            if (!HasHostFilter)
                return null;
            var index = 0;
            for (; index < Length; ++index)
                if (path[index] == ":")
                    break;
            return new VirtualPath(path, index);
        }

        /// <summary>
        /// Create a sub path that starts at position <paramref name="start"/>.
        /// </summary>
        /// <param name="start">the start position</param>
        /// <param name="ignoreNonEntries">
        /// if selected <paramref name="start"/> will ignore path elements that 
        /// are not entry names (like @ or :) and skip them
        /// </param>
        /// <returns>the created subpath</returns>
        public VirtualPath CreateSubPath(int start, bool ignoreNonEntries = false)
        {
            if (start < 0 || start > Length)
                throw new ArgumentOutOfRangeException(nameof(start));
            if (ignoreNonEntries)
            {
                var extra = IsRootPath ? 1 : 0;
                var ind = new List<string>(path).LastIndexOf(":", start + extra);
                extra += ind >= 0 ? 1 : 0;
                if (start + extra > Length)
                    throw new ArgumentOutOfRangeException(nameof(start));
                var result = new string[Length - start - extra];
                Array.Copy(path, start + extra, result, 0, result.Length);
                return new VirtualPath(result);
            }
            else
            {
                var path = new string[Length - start];
                Array.Copy(this.path, start, path, 0, path.Length);
                return new VirtualPath(path, path.Length);
            }
        }

        /// <summary>
        /// Convert this path into one that is a root path
        /// </summary>
        /// <returns>the root path</returns>
        public VirtualPath MakeRootPath()
        {
            if (IsRootPath)
                return this;
            var trim = 0;
            for (int i = 0; i < Length; ++i)
                if (path[i] == "..")
                    trim++;
                else break;
            var result = new string[Length - trim + 1];
            result[0] = "@";
            Array.Copy(path, trim, result, 1, Length - trim);
            return new VirtualPath(result, result.Length);
        }

        /// <summary>
        /// Remove any parts that are used to lock to specific <see cref="IIOHost"/>
        /// </summary>
        /// <returns>the new path</returns>
        public VirtualPath RemoveLocks()
        {
            for (int i = 0; i< Length; ++i)
                if (path[i] == ":")
                {
                    var result = new string[Length - 1];
                    Array.Copy(path, 0, result, 0, i);
                    Array.Copy(path, i + 1, result, i, result.Length - i);
                    return new VirtualPath(result);
                }
            return this;
        }

        /// <summary>
        /// Check if this <see cref="VirtualPath"/> is one of the parents of <paramref name="path"/>.
        /// </summary>
        /// <param name="path">the path that could be a child in any hierarchy of this <see cref="VirtualPath"/>.</param>
        /// <param name="allowEquality">allows that this <see cref="VirtualPath"/> could be equal to <paramref name="path"/></param>
        /// <param name="ignoreHostLock">will ignore locks that will fix to specific <see cref="IIOHost"/>.</param>
        /// <returns>true if the check condition met</returns>
        public bool IsParentOf(VirtualPath path, bool allowEquality = false, bool ignoreHostLock = false)
        {
            _ = path ?? throw new ArgumentNullException(nameof(path));
            var comp = CompareTo(path, true, ignoreHostLock);
            if (comp == null)
                return false;
            return allowEquality ? comp.Value <= 0 : comp.Value < 0;
        }

        /// <summary>
        /// Compare this <see cref="VirtualPath"/> with <paramref name="other"/>. The result determines if
        /// one could be the root of the other ore if they are equal.
        /// </summary>
        /// <param name="other">the other <see cref="VirtualPath"/> to compare to</param>
        /// <param name="ignoreRoot">this will ignore the root pattern and do not force its presence</param>
        /// <param name="ignoreHostLock">the will ignore host lock patterns and do not force their presence</param>
        /// <returns>
        /// null if both are not on the same path, lower than 0 if this <see cref="VirtualPath"/> could be
        /// a parent, larger than 0 if <paramref name="other"/> could be a parent and 0 if both are equal.
        /// </returns>
        public int? CompareTo(VirtualPath other, bool ignoreRoot = false, bool ignoreHostLock = false)
        {
            _ = other ?? throw new ArgumentNullException(nameof(other));
            if (!ignoreRoot && !ignoreHostLock && other.Length != Length)
                return Length - other.Length;
            int i = 0, j = 0;
            if (ignoreRoot)
            {
                if (Length > 0 && path[0] == "@")
                    i++;
                if (other.Length > 0 && other.path[0] == "@")
                    j++;
            }
            while (i < Length && j < other.Length)
            {
                if (ignoreHostLock)
                {
                    var skip = false;
                    if (path[i] == ":")
                    {
                        i++;
                        skip = true;
                    }
                    if (other.path[j] == ":")
                    {
                        j++;
                        skip = true;
                    }
                    //if both paths has a : then both of them need to skip at the same time
                    //otherwise the equality comparision could have some problems
                    if (skip)
                        continue;
                }
                if (path[i] != other.path[j])
                    return null;
                i++;
                j++;
            }
            return (Length - i).CompareTo(other.Length - j);
        }

        /// <summary>
        /// Will try to parse the string path to its <see cref="VirtualPath"/> representation.
        /// </summary>
        /// <param name="path">the string path</param>
        /// <returns>the generated path</returns>
        /// <remarks>
        /// The / represents as a splitter between multiple <see cref="IVirtualEntry"/>
        /// and their hierarchy. Also the following entry names are reserved:
        ///     .   : represents the current layer. This will be trimmed from the output
        ///     ..  : will move one layer up
        ///     @   : represents the root. This will remove all previous parts of the path.
        ///     :   : force the switch of the <see cref="IIOHost"/> only results are returned for
        ///           the <see cref="IIOHost"/> that is located at this position. Only the first
        ///           one is used.
        /// All other symbols apart from / are used as <see cref="IVirtualEntry"/> names.
        /// </remarks>
        /// <exception cref="FormatException" />
        public static VirtualPath Parse(string path)
        {
            if (TryParse(path, out VirtualPath result))
                return result;
            else throw new FormatException("argument cannot parsed to a virtual path");
        }

        /// <summary>
        /// Will try to parse the string path to its <see cref="VirtualPath"/> representation.
        /// </summary>
        /// <param name="path">the string path</param>
        /// <param name="virtualPath">the generated path</param>
        /// <returns>true on success</returns>
        /// <remarks>
        /// The / represents as a splitter between multiple <see cref="IVirtualEntry"/>
        /// and their hierarchy. Also the following entry names are reserved:
        ///     .   : represents the current layer. This will be trimmed from the output
        ///     ..  : will move one layer up
        ///     @   : represents the root. This will remove all previous parts of the path.
        ///     :   : force the switch of the <see cref="IIOHost"/> only results are returned for
        ///           the <see cref="IIOHost"/> that is located at this position. Only the first
        ///           one is used.
        /// All other symbols apart from / are used as <see cref="IVirtualEntry"/> names.
        /// </remarks>
        public static bool TryParse(string path, out VirtualPath virtualPath)
        {
            _ = path ?? throw new ArgumentNullException(nameof(path));
            virtualPath = default;
            var parts = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            var list = new List<string>();
            var hasSwitch = false;
            foreach (var part in parts)
            {
                if (part == ".")
                    continue;
                else if (part == "..")
                {
                    if (list.Count > 0 && list[list.Count - 1] != "..")
                        list.RemoveAt(list.Count - 1);
                    else list.Add(part);
                }
                else if (part == "@")
                {
                    list.Clear();
                    list.Add(part);
                }
                else if (part == ":")
                {
                    if (!hasSwitch)
                        list.Add(part);
                    hasSwitch = true;
                }
                else list.Add(part);
            }
            virtualPath = new VirtualPath(list.ToArray(), list.Count);
            return true;
        }

        public IEnumerator<string> GetEnumerator()
        {
            for (int i = 0; i < Length; ++i)
                yield return path[i];
        }

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        public bool Equals(VirtualPath other, bool ignoreRoot = false, bool ignoreLock = false)
        {
            if (other is null)
                return false;
            return CompareTo(other, ignoreRoot, ignoreLock) == 0;
        }

        public override bool Equals(object obj)
        {
            if (obj is VirtualPath path)
                return Equals(path);
            else return false;
        }

        public override int GetHashCode()
            => ToString().GetHashCode();

        public static bool operator ==(VirtualPath path1, VirtualPath path2)
            => Equals(path1, path2);

        public static bool operator !=(VirtualPath path1, VirtualPath path2)
            => !Equals(path1, path2);

        public override string ToString()
        {
            if (Length == 0)
                return "/";
            var sb = new StringBuilder();
            for (int i = 0; i < Length; ++i)
            {
                if (i > 0)
                    sb.Append('/');
                sb.Append(path[i]);
            }
            return sb.ToString();
        }

        bool IEquatable<VirtualPath>.Equals(VirtualPath other)
            => Equals(other);
    }
}
