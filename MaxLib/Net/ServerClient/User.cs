using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MaxLib.Net.ServerClient
{
    public class User
    {
        public int Ping { get; set; }

        public int Id { get; internal set; }

        public byte[] GlobalId { get; set; }
        /// <summary>
        /// Bestimmt, ob dieser Nutzer nur über ein Proxy erreichbar ist.
        /// </summary>
        public bool IsProxy { get; set; }

        private int defaultConnector = -1;
        public int DefaultConnector
        {
            get { return defaultConnector; }
            set { defaultConnector = value; }
        }

        private Connection defaultConnection = null;
        public Connection DefaultConnection
        {
            get { return defaultConnection; }
            set { defaultConnection = value; }
        }

        public T As<T>() where T:User
        {
            return this as T;
        }
    }

    public sealed class UserCollection
    {
        public List<User> Users { get; private set; }

        public event CreateNewUserHandle CreateNewUser;
        public event GetUserPrototypeHandle GetUserPrototype;

        public User GetUserFromId(byte[] id)
        {
            return Users.Find((u) =>
            {
                if (u.GlobalId.Length == 0) return false;
                for (int i = 0; i < 16; ++i) if (u.GlobalId[i] != id[i]) return false;
                return true;
            });
        }

        public User GetUserFromId(int id)
        {
            return Users.Find((u) => u.Id == id);
        }

        int findfreeid()
        {
            var id = 0;
            while (Users.Exists((u) => u.Id == id)) ++id;
            return id;
        }

        public User AddNewUser()
        {
            var user = GetUserPrototype == null ? new User() : GetUserPrototype();
            user.Id = findfreeid();
            if (CreateNewUser != null) user = CreateNewUser(user);
            Users.Add(user);
            return user;
        }

        public bool RemoveUser(User user)
        {
            return Users.Remove(user);
        }

        public bool RemoveUser(int id)
        {
            return RemoveUser(Users.Find((u) => u.Id == id));
        }

        public int Count
        {
            get { return Users.Count; }
        }

        public int MaxCount { get; set; }

        public UserCollection()
        {
            Users = new List<User>();
        }
    }

    public delegate User CreateNewUserHandle(User defaultUser);
    public delegate User GetUserPrototypeHandle();

    public class CurrentIdentification : ILoadSaveAble
    {
        public byte[] Id { get; private set; }

        public string StaticIdentification { get; set; }

        public string Version { get; set; }

        public CurrentIdentification()
        {
            var l = new List<byte>();
            var b = new byte[8];
            new Random().NextBytes(b);
            l.AddRange(b);
            l.AddRange(BitConverter.GetBytes(DateTime.Now.ToBinary()));
            Id = l.ToArray();
            var versionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetEntryAssembly().Location);
            StaticIdentification = versionInfo.ProductName;
            Version = versionInfo.ProductVersion;
        }

        public void Load(byte[] data)
        {
            using (var m = new System.IO.MemoryStream(data))
            using (var r = new System.IO.BinaryReader(m))
            {
                Id = r.ReadBytes(16);
                StaticIdentification = r.ReadString();
                Version = r.ReadString();
            }
        }

        public byte[] Save()
        {
            using (var m = new System.IO.MemoryStream())
            using (var w = new System.IO.BinaryWriter(m))
            using (var r = new System.IO.BinaryReader(m))
            {
                w.Write(Id);
                w.Write(StaticIdentification);
                w.Write(Version);
                m.Position = 0;
                return r.ReadBytes((int)m.Length);
            }
        }
    }
}
