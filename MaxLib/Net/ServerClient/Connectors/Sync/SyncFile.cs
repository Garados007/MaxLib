using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MaxLib.Net.ServerClient.Connectors.Sync
{
    public class SyncFileManager
    {
        public ConnectionManager Manager { get; private set; }

        public FileTransport FileTransport { get; private set; }

        public SyncFileManager(ConnectionManager Manager, FileTransport FileTransport)
        {
            this.Manager = Manager;
            this.FileTransport = FileTransport;
            FileTransport.RemoteTaskAdded += FileTransport_RemoteTaskAdded;
            Manager.SyncFile = this;
        }

        void FileTransport_RemoteTaskAdded(FileTransportTask task)
        {
            var file = Files.Find((sf) => sf.Task.ID == task.ID);
            file.Task = task;
            file.SetEvents();
        }

        internal void MessageReceived(SyncFileMessage message)
        {
            if (message.Type==SyncFileMessageType.Connect)
            {
                var d = message.ClientData.GetSerializeAble<SyncFileData>();
                var sf = new SyncFile();
                sf.Task = new FileTransportTask();
                sf.Task.ID = message.TaskID;
                sf.Task.Size = d.Size;
                sf.Manager = this;
                sf.SendData = false;
                sf.AccessID = d.AccessID;
                sf.OwnerID = message.OwnerID;
                sf.Task.TargetUser = Manager.Users.GetUserFromId(message.MessageRoot.RemoteId);
                Files.Add(sf);
                sf.SendInfo(SyncFileMessageType.ConnectionSuccess, null);
                if (SyncFileAdded != null) SyncFileAdded(this, sf);
            }
            else
            {
                var file = Files.Find((sf) =>
                    {
                        if (message.TaskID != sf.Task.ID) return false;
                        if (message.OwnerID.Length != sf.OwnerID.Length) return false;
                        for (int i = 0; i < message.OwnerID.Length; ++i)
                            if (message.OwnerID[i] != sf.OwnerID[i]) return false;
                        return true;
                    });
                if (message.Type == SyncFileMessageType.ConnectionSuccess)
                {
                    this.FileTransport.StartTask(file.Task);
                }
            }
        }

        public SyncFileManager(ConnectionManager Manager) : 
            this(Manager, Manager.DefaultFileTransport as FileTransport)
        { }

        public event Action<object, SyncFile> SyncFileAdded;

        List<SyncFile> Files = new List<SyncFile>();

        [Obsolete("Use Syncronize(User, int, GetFileData, int)")]
        public SyncFile Syncronize(User TargetUser, byte[] Data, int AccessID)
        {
            var sf = new SyncFile();
            var t = sf.Task = new FileTransportTask();
            t.Data = Data;
            t.Size = Data.Length;
            t.TargetUser = TargetUser;
            sf.OwnerID = Manager.CurrentId.Id;
            sf.Manager = this;
            sf.SendData = true;
            sf.AccessID = AccessID;
            Files.Add(sf);
            sf.SetEvents();
            sf.Send();
            return sf;
        }

        public SyncFile Syncronize(User TargetUser, int DatasetCount, GetFileData getDataHandler, int AccessId)
        {
            var sf = new SyncFile();
            var t = sf.Task = new FileTransportTask();
            t.DatasetCount = DatasetCount;
            t.Size = DatasetCount;
            t.GetDatasetData += getDataHandler;
            t.TargetUser = TargetUser;
            sf.OwnerID = Manager.CurrentId.Id;
            sf.Manager = this;
            sf.SendData = true;
            sf.AccessID = AccessId;
            Files.Add(sf);
            sf.SetEvents();
            sf.Send();
            return sf;
        }
    }

    public class SyncFile
    {
        internal SyncFile() { }

        public FileTransportTask Task { get; internal set; }

        public SyncFileManager Manager { get; internal set; }

        public bool SendData { get; internal set; } //Send or Receive

        public byte[] OwnerID { get; internal set; }

        public int AccessID { get; internal set; }

        internal void ReceiveData(SyncFileMessage message)
        {

        }

        internal void Send()
        {
            var d = new SyncFileData();
            d.Size = Task.Size;
            d.AccessID = AccessID;
            OwnerID = Manager.Manager.CurrentId.Id;
            SendInfo(SyncFileMessageType.Connect, d);
        }

        internal void SetEvents()
        {
            Task.ServerStateUpdated += Task_ServerStateUpdated;
            Task.DatasetReceived += Task_DatasetReceived;
        }

        void Task_DatasetReceived(FileTransportTask task, int index, byte[] data)
        {
            if (DatasetReceived != null) DatasetReceived(task, index, data);
        }

        void Task_ServerStateUpdated()
        {
            switch (Task.State)
            {
                case FileTransportState.CompressBytes:
                    if (CompressBytes != null) CompressBytes(this, EventArgs.Empty);
                    break;
                case FileTransportState.ConnectToServer:
                    if (ConnectToServer != null) ConnectToServer(this, EventArgs.Empty);
                    break;
                case FileTransportState.DecompressBytes:
                    if (DecompressBytes != null) DecompressBytes(this, EventArgs.Empty);
                    break;
                case FileTransportState.Finished:
                    if (Finished != null) Finished(this, Task.Data);
                    break;
                case FileTransportState.Transport:
                    if (Transport != null) Transport(this, Task.TransportedBytes, Task.Size);
                    break;
                case FileTransportState.WaitForLocalConnector:
                    if (WaitForLocalConnector != null) WaitForLocalConnector(this, EventArgs.Empty);
                    break;
                case FileTransportState.WaitForRemoteConnector:
                    if (WaitForRemoteConnector != null) WaitForRemoteConnector(this, EventArgs.Empty);
                    break;
                case FileTransportState.Waiting:
                    if (Waiting != null) Waiting(this, Task.RestUserToWait);
                    break;
            }
            if (StateChanged != null) StateChanged(this, EventArgs.Empty);
        }

        public event EventHandler CompressBytes, ConnectToServer, DecompressBytes, WaitForLocalConnector, 
            WaitForRemoteConnector, StateChanged;
        public event SyncFileFinishedHandle Finished;
        public event SyncFileTransportHandle Transport;
        public event SyncFileWaitHandle Waiting;
        public event FileDataReceived DatasetReceived;

        internal void SendInfo(SyncFileMessageType type, object data)
        {
            var sfm = new SyncFileMessage();
            sfm.Type = type;
            sfm.TaskID = Task.ID;
            sfm.OwnerID = OwnerID;
            sfm.MessageRoot.Connection = Task.TargetUser.DefaultConnection;
            sfm.MessageRoot.Connector = Task.TargetUser.DefaultConnector;
            sfm.MessageRoot.RemoteId = Task.TargetUser.Id;
            if (data!=null)
            {
                if (data is Message) sfm.ClientData.SetMessage(data as Message);
                else if (data is ILoadSaveAble) sfm.ClientData.SetLoadSaveAble(data as ILoadSaveAble);
                else if (data is byte[]) sfm.ClientData.SetBinary(data as byte[]);
                else sfm.ClientData.SetSerializeAble(data);
            }
            var pm = new PrimaryMessage();
            pm.MessageType = PrimaryMessageType.SyncFile;
            pm.ClientData.SetMessage(sfm);
            Manager.Manager.SendMessage(pm);
        }
    }

    public delegate void SyncFileFinishedHandle(SyncFile file, byte[] Data);
    public delegate void SyncFileTransportHandle(SyncFile file, long Transported, long Size);
    public delegate void SyncFileWaitHandle(SyncFile file, int RestUserToWait);

    class SyncFileMessage : Message
    {
        public SyncFileMessageType Type
        {
            get { return (SyncFileMessageType)Reason; }
            set { Reason = (int)value; }
        }

        public long TaskID
        {
            get { return BitConverter.ToInt64(HeaderBytes, 0); }
            set
            {
                var l = new List<byte>();
                l.AddRange(BitConverter.GetBytes(value));
                l.AddRange(OwnerID);
                HeaderBytes = l.ToArray();
            }
        }

        public byte[] OwnerID
        {
            get { return HeaderBytes.ToList().GetRange(8, HeaderBytes.Length - 8).ToArray(); }
            set
            {
                var l = new List<byte>();
                l.AddRange(BitConverter.GetBytes(TaskID));
                l.AddRange(value);
                HeaderBytes = l.ToArray();
            }
        }

        public SyncFileMessage()
        {
            HeaderBytes = new byte[8];
        }
    }

    [Serializable]
    class SyncFileData
    {
        public int Size { get; set; }

        public int AccessID { get; set; }
    }

    enum SyncFileMessageType
    {
        Connect,
        ConnectionSuccess,
        ErrorInDataReconnect,
        DataFinished
    }
}
