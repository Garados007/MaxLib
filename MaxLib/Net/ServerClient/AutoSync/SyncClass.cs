using System;
using System.Collections.Generic;
using System.Linq;

namespace MaxLib.Net.ServerClient.AutoSync
{
    public abstract class SyncClass : IDisposable
    {
        public int Id { get; internal set; }

        public SyncManager Manager { get; private set; }

        public string GlobalId { get; set; }

        public SyncClass(SyncManager manager)
        {
            manager.Add(this);
            Manager = manager;
            GlobalId = "ID=" + Id.ToString();
        }

         ~SyncClass()
        {
            if (Manager!=null)
            {
                Manager.Remove(this);
                Manager = null;
            }
        }

        public virtual void Dispose()
        {
            if (Id != -1)
            {
                Manager.Remove(this);
                Manager = null;
            }
        }

        public void Changed()
        {
            var b = GetData().ToList();
            var max = (int)Math.Ceiling(b.Count / (16 * 1024f));
            for (int i = 0; i<max; ++i)
            {
                var dat = new SyncMessageData();
                dat.SyncManagerId = Manager.Id;
                dat.SyncClassId = GlobalId;
                dat.MaxDataset = max;
                dat.CurrentDataset = i + 1;
                dat.DatasetBytes = b.GetRange(i * 16 * 1024, (i + 1) * 16 * 1024 >= b.Count ? b.Count - i * 16 * 1024 : 16 * 1024).ToArray();
                var sm = new SyncMessage();
                sm.ClientData.SetSerializeAble(dat);
                sm.Type = SyncMessageType.SingleDatasetChanged;
            }
        }

        protected abstract byte[] GetData();
        protected abstract void SetData(byte[] data);

        Dictionary<User, List<byte>> ReceivedBytes = new Dictionary<User, List<byte>>();

        internal void MessageReceived(SyncMessage message)
        {
            var dat = message.ClientData.GetSerializeAble<SyncMessageData>();
            if (dat.MaxDataset==1)
            {
                SetData(dat.DatasetBytes);
                return;
            }
            var user = Manager.Manager.Users.GetUserFromId(message.MessageRoot.RemoteId);
            if (!ReceivedBytes.ContainsKey(user)) ReceivedBytes.Add(user, new List<byte>());
            ReceivedBytes[user].AddRange(dat.DatasetBytes);
            if (dat.CurrentDataset==dat.MaxDataset)
            {
                var b = ReceivedBytes[user].ToArray();
                ReceivedBytes.Remove(user);
                SetData(b);
            }
        }
    }

    /// <summary>
    /// Stellt einen Manager dar, der es ermöglicht verschiedene Einträge und Klassen über das Netzwerk syncron zu halten.
    /// </summary>
    public sealed class SyncManager : IDisposable
    {
        //Liste aller registrierten SyncClass zur Syncronisation
        List<SyncClass> Registred = new List<SyncClass>();
        //Buffert leer gewordene Einträge zwischen
        Queue<int> EmptyEntries = new Queue<int>();

        /// <summary>
        /// Fügt einen neuen Eintrag hinzu.
        /// </summary>
        /// <param name="syncClass">der neue Eintrag</param>
        public void Add(SyncClass syncClass)
        {
            //Schon angeknüpft
            if (syncClass.Id != -1) throw new ArgumentException();
            //Eintrag erstellen
            if (EmptyEntries.Count>0)
            {
                var id = EmptyEntries.Dequeue();
                Registred[id] = syncClass;
                syncClass.Id = id;
            }
            else
            {
                var id = Registred.Count + 1;
                if (id < 0) throw new OverflowException();
                Registred.Add(syncClass);
                syncClass.Id = id;
            }
        }

        /// <summary>
        /// Eintfernt einen Eintrag wieder. Er kann nicht mehr über diesen Manager syncronisiert werden.
        /// </summary>
        /// <param name="syncClass">der zu entfernende Eintrag</param>
        public void Remove(SyncClass syncClass)
        {
            var id = syncClass.Id;
            Registred[id] = null;
            EmptyEntries.Enqueue(id); //Die ID kann später wiederverwendet werden.
            syncClass.Id = -1;
        }

        /// <summary>
        /// Ruft ab, ob dieser Manager als Server fungiert
        /// </summary>
        public bool RunAsServer { get; private set; }
        /// <summary>
        /// Eine konstante Id zu diesen Manager. Diese hat keine Bewandniss zum Datentransport.
        /// </summary>
        public int Id { get; private set; }
        static int LastID = 0;
        /// <summary>
        /// Ruft den Verbindungsmanager ab, mit dem der Syncronisationsmanager arbeitet.
        /// </summary>
        public ConnectionManager Manager { get; private set; }
        /// <summary>
        /// Erstellt einen neuen Syncronisationsmanager ab, mit dem Daten automatisch über das Netzwerk syncronisiert werden kann.
        /// </summary>
        /// <param name="manager">Der Verbindungsmanager</param>
        /// <param name="runAsServer">gibt an, ob diese Instanz als Server fungiert.</param>
        public SyncManager(ConnectionManager manager, bool runAsServer)
        {
            Manager = manager;
            manager.SyncData.Add(this);
            Id = LastID++;
            RunAsServer = runAsServer;
        }
        /// <summary>
        /// Gibt alle verwendeten Ressourcen wieder frei und beendet den Manager.
        /// </summary>
        public void Dispose()
        {
            Manager.SyncData.Remove(this);
            Registred.ForEach((sc) =>
            {
                if (sc != null)
                {
                    sc.Id = -1;
                    sc.Dispose();
                }
            });
            Registred.Clear();
            EmptyEntries.Clear();
        }

        internal void MessageReceived(SyncMessage message)
        {
            var dat = message.ClientData.GetSerializeAble<SyncMessageData>();
            var sc = Registred.Find((psc) =>
            {
                if (psc == null) return false;
                return (psc.GlobalId == dat.SyncClassId);
            });
            if (sc != null) sc.MessageReceived(message);
        }
    }

    public class SyncMessage : Message
    {
        public SyncMessageType Type
        {
            get { return (SyncMessageType)Reason; }
            set { Reason = (int)value; }
        }
    }

    public enum SyncMessageType
    {
        SingleDatasetChanged,
        ComplexDatasetChanged,
        WantUpdate
    }

    [Serializable]
    public class SyncMessageData
    {
        public string SyncClassId;
        public int SyncManagerId;
        public int CurrentDataset, MaxDataset;
        public byte[] DatasetBytes; //Maximum 16 KB
    }
}
