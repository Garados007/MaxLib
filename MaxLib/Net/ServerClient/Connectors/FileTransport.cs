using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace MaxLib.Net.ServerClient.Connectors
{
    public class FileTransport : Connector
    {
        public override void StartProgress()
        {
        }

        public override void StopProgress()
        {
        }

        Connection FindEmptyConnection()
        {
            for (int i = 0; i < Connections.Count; ++i) if (!Connections[Connections[i]]) return Connections[i];
            return null;
        }

        public List<FileTransportTask> Tasks { get; private set; }

        FileTransportTask FindTask(long id, byte[] sender)
        {
            return Tasks.Find((t) =>
                {
                    if (t.ID != id) return false;
                    if (sender.Length != Manager.CurrentId.Id.Length) return false;
                    for (int i = 0; i < sender.Length; ++i) if (sender[i] != Manager.CurrentId.Id[i]) return false;
                    return true;
                });
        }

        public event FileTaskChangedHandler RemoteTaskAdded, RemoteTaskFinished;
       
        public ServerClientRole Role { get; set; }

        volatile int userwait = 0;

        public override void SendMessage(PrimaryMessage message)
        {
            if (message.MessageType == PrimaryMessageType.FileTransportClientToServer)
            {
                var ftd = message.ClientData.GetSerializeAble<FileTransportData>();
                switch (ftd.Type)
                {
                    case FileTransportType.WaitToGetPort:
                        {
                            var task = FindTask(ftd.TransportID, ftd.SenderID);
                            task.State = FileTransportState.WaitForRemoteConnector;
                            task.RestUserToWait = ftd.UserWait;
                            task.OnServerStateUpdated();
                        } break;
                    case FileTransportType.CouldSendFile:
                        {
                            var task = FindTask(ftd.TransportID, ftd.SenderID);
                            task.State = FileTransportState.Transport;
                            if (ftd.Connection != null) task.Connection = ftd.Connection;
                            task.OnServerStateUpdated();
                        } break;
                }
            }
            if (message.MessageType == PrimaryMessageType.FileTransportServerToClient)
            {
                var ftd = message.ClientData.GetSerializeAble<FileTransportData>();
                switch (ftd.Type)
                {
                    case FileTransportType.WantSendFile:
                        {
                            var task = new FileTransportTask
                            {
                                ID = ftd.TransportID,
                                State = FileTransportState.WaitForLocalConnector,
                                TargetUser = Manager.Users.GetUserFromId(message.MessageRoot.RemoteId),
                                Size = ftd.Size,
                                DatasetCount = ftd.Datasets,
                                Connection = ftd.Connection
                            };
                            Tasks.Add(task);
                            RemoteTaskAdded?.Invoke(task);
                            Connection con = null;
                            if (Role != ServerClientRole.Client)
                            {
                                con = FindEmptyConnection();
                                userwait++;
                                while (con == null)
                                {
                                    var pm = new PrimaryMessage
                                    {
                                        MessageRoot = message.MessageRoot,
                                        MessageType = PrimaryMessageType.FileTransportClientToServer
                                    };
                                    ftd.UserWait = task.RestUserToWait = userwait;
                                    ftd.Type = FileTransportType.WaitToGetPort;
                                    pm.ClientData.SetSerializeAble(ftd);
                                    Manager.SendMessage(pm);
                                    System.Threading.Thread.Sleep(10);
                                    con = FindEmptyConnection();
                                }
                                SetUsedState(con, true);
                                userwait--;
                                task.Connection = con;
                            }
                            var rm = new PrimaryMessage
                            {
                                MessageRoot = message.MessageRoot,
                                MessageType = PrimaryMessageType.FileTransportClientToServer
                            };
                            ftd.Type = FileTransportType.CouldSendFile;
                            ftd.Connection = con;
                            rm.ClientData.SetSerializeAble(ftd);
                            Manager.SendMessage(rm);

                            task.State = FileTransportState.Transport;
                            task.OnServerStateUpdated();
                            TcpListener tcplist = null;
                            TcpClient tcp;
                            if (Role == ServerClientRole.Client)
                            {
                                tcp = new TcpClient(task.Connection.Target, task.Connection.Port);
                            }
                            else
                            {
                                tcplist = new TcpListener(new IPEndPoint(IPAddress.Any, con.Port));
                                tcplist.Start();
                                tcp = tcplist.AcceptTcpClient();
                            }
                            var stream = tcp.GetStream();

                            if (task.DatasetCount == 0)
                            {
                                var l = new List<byte>();
                                var b = new byte[tcp.ReceiveBufferSize];
                                int len;
                                while (true)
                                {
                                    len = stream.Read(b, 0, b.Length);
                                    l.AddRange(b.ToList().GetRange(0, len).ToArray());
                                    task.TransportedBytes += len;
                                    task.OnServerStateUpdated();
                                    if (len < b.Length)
                                    {
                                        System.Threading.Thread.Sleep(1);
                                        if (stream.DataAvailable || tcp.Available > 0) continue;
                                        else break;
                                    }
                                }

                                tcp.Close();
                                if (tcplist != null) tcplist.Stop();
                                task.CompressedBytes = l.ToArray();
                            }
                            else
                            {
                                for (task.CurrentDataset = 0; task.CurrentDataset < task.DatasetCount; ++task.CurrentDataset)
                                {
                                    var l = new List<byte>();
                                    var b = new byte[tcp.ReceiveBufferSize];
                                    stream.Read(b, 0, 4);
                                    var length = BitConverter.ToInt32(b, 0);
                                    int len;
                                    var timeout = false;
                                    task.Size = length;
                                    task.TransportedBytes = 0;
                                    while (!timeout)
                                    {
                                        len = stream.Read(b, 0, b.Length);
                                        l.AddRange(b.ToList().GetRange(0, len).ToArray());
                                        task.TransportedBytes += len;
                                        task.OnServerStateUpdated();
                                        if (len < b.Length)
                                        {
                                            if (l.Count >= length) break; //Fertig
                                            int i = 0;
                                            if (!tcp.Connected)
                                            {
                                                timeout = true;
                                                break;
                                            }
                                            for (; i < 2000; ++i)
                                            {
                                                System.Threading.Thread.Sleep(1);
                                                if (stream.DataAvailable || tcp.Available > 0) break;
                                            }
                                            if (i == 2000)
                                            {
                                                timeout = true;
                                                break; //Timeout nach 2 Sekunden
                                            }
                                        }
                                    }
                                    if (timeout) break;
                                    task.OnDatasetReceived(task.CurrentDataset, l.ToArray());
                                }
                                tcp.Close();
                                if (tcplist != null) tcplist.Stop();
                            }

                            task.State = FileTransportState.DecompressBytes;
                            task.OnServerStateUpdated();
//#pragma warning disable CS0618 // Typ oder Element ist veraltet
//                            if (CompressBytes && task.DatasetCount == 0)
//#pragma warning restore CS0618 // Typ oder Element ist veraltet
//                            {
//                                using (var m = new MemoryStream(task.CompressedBytes))
//                                using (var defl = new DeflateStream(m, CompressionMode.Decompress))
//                                using (var m2 = new MemoryStream())
//                                using (var r = new BinaryReader(m2))
//                                {
//                                    defl.CopyTo(m2);
//                                    m2.Position = 0;
//#pragma warning disable CS0618 // Typ oder Element ist veraltet
//                                    task.Data = r.ReadBytes((int)m2.Length);
//#pragma warning restore CS0618 // Typ oder Element ist veraltet
//                                }
//                            }
//#pragma warning disable CS0618 // Typ oder Element ist veraltet
//                            else task.Data = task.CompressedBytes;
//#pragma warning restore CS0618 // Typ oder Element ist veraltet

                            task.State = FileTransportState.Finished;
                            task.OnServerStateUpdated();
                            RemoteTaskFinished?.Invoke(task);
                            Tasks.Remove(task);
                            SetUsedState(con, false);
                        }
                        break;
                }
            }
        }

        public void StartTask(FileTransportTask task) //Sender
        {
            if (task == null) throw new ArgumentNullException("task");
            if (task.State != FileTransportState.Waiting) throw new ArgumentException();
            Tasks.Add(task);
            var t = new Task(() =>
            {
                #region Daten verkleinern nur ohne Datasets
//#pragma warning disable CS0618 // Typ oder Element ist veraltet
//                if (CompressBytes && task.DatasetCount == 0)
//#pragma warning restore CS0618 // Typ oder Element ist veraltet
//                {
//                    task.State = FileTransportState.CompressBytes;
//                    using (var m = new MemoryStream())
//                    using (var comp = new DeflateStream(m, CompressionMode.Compress))
//                    using (var r = new BinaryReader(m))
//                    using (var w = new BinaryWriter(comp))
//                    {
//#pragma warning disable CS0618 // Typ oder Element ist veraltet
//                        w.Write(task.Data);
//#pragma warning restore CS0618 // Typ oder Element ist veraltet
//                        m.Position = 0;
//                        task.CompressedBytes = r.ReadBytes((int)m.Length);
//                    }
//                }
//#pragma warning disable CS0618 // Typ oder Element ist veraltet
//                else task.CompressedBytes = task.Data;
//#pragma warning restore CS0618 // Typ oder Element ist veraltet
                task.Size = task.CompressedBytes.Length;
                #endregion
                //Verbinden
                task.State = FileTransportState.WaitForLocalConnector;
                Connection con = null;
                if (Role == ServerClientRole.Server)
                {
                    con = FindEmptyConnection();
                    while (con == null)
                    {
                        System.Threading.Thread.Sleep(10);
                        con = FindEmptyConnection();
                    }
                    SetUsedState(con, true);
                    task.Connection = con;
                }
                task.State = FileTransportState.ConnectToServer;
                Action act = null;
                act = new Action(() =>
                {
                    if (task.State == FileTransportState.Transport && task.TransportedBytes == 0)
                    {
                        TcpClient tcp = null;
                        TcpListener server = null;
                        if (Role == ServerClientRole.Server)
                        {
                            server = new TcpListener(IPAddress.Any, con.Port);
                            server.Start();
                            tcp = server.AcceptTcpClient();
                            server.Stop();
                            server = null;
                        }
                        else
                        {
                            tcp = new TcpClient();
                            tcp.Connect(new IPEndPoint(IPAddress.Parse(task.Connection.Target), task.Connection.Port));
                        }
                        
                        var stream = tcp.GetStream();

                        if (task.DatasetCount == 0)
                        {
                            var offset = 0;
                            while (offset < task.CompressedBytes.Length)
                            {
                                stream.Write(task.CompressedBytes, offset, Math.Min(task.CompressedBytes.Length - offset, tcp.SendBufferSize));
                                System.Threading.Thread.Sleep(1);
                                offset += tcp.SendBufferSize;
                                task.TransportedBytes = Math.Min(task.CompressedBytes.Length, offset);
                                task.OnServerStateUpdated();
                            }
                        }
                        else for (task.CurrentDataset = 0; task.CurrentDataset < task.DatasetCount; ++task.CurrentDataset)
                            {
                                var bytes = task.OnGetDatasetData(task.CurrentDataset);
                                var datalength = BitConverter.GetBytes(bytes.Length);
                                stream.Write(datalength, 0, datalength.Length);
                                var offset = 0;
                                task.Size = bytes.Length;
                                task.TransportedBytes = 0;
                                while (offset < bytes.Length)
                                {
                                    if (!tcp.Connected) break;
                                    stream.Write(bytes, offset, Math.Min(bytes.Length - offset, tcp.SendBufferSize));
                                    System.Threading.Thread.Sleep(1);
                                    offset += tcp.SendBufferSize;
                                    task.TransportedBytes = Math.Min(bytes.Length, offset);
                                    task.OnServerStateUpdated();
                                }
                                if (!tcp.Connected) break;
                            }

                        tcp.Close();
                        task.State = FileTransportState.Finished;
                        Tasks.Remove(task);
                        task.OnServerStateUpdated();
                        task.ServerStateUpdated -= act;
                        if (con!=null) SetUsedState(con, false);
                    }
                });
                task.ServerStateUpdated += act;
                var mes = new PrimaryMessage();
                var ftd = new FileTransportData
                {
                    TransportID = task.ID,
                    SenderID = Manager.CurrentId.Id,
                    Type = FileTransportType.WantSendFile,
                    Size = task.Size,
                    Datasets = task.DatasetCount,
                    Connection = task.Connection
                };
                mes.ClientData.SetSerializeAble(ftd);
                mes.MessageType = PrimaryMessageType.FileTransportServerToClient;
                mes.MessageRoot.Connection = task.TargetUser.DefaultConnection;
                mes.MessageRoot.Connector = task.TargetUser.DefaultConnector;
                mes.MessageRoot.RemoteId = task.TargetUser.Id;
                Manager.SendMessage(mes);
            });
            t.Start();
        }

        public FileTransport()
        {
            Tasks = new List<FileTransportTask>();
            Connections.MainProtocol = ConnectorProtocol.TCP;
            CanChangeProtocol = false;
            Role = ServerClientRole.Undefined;
        }
    }

    public delegate void FileTaskChangedHandler(FileTransportTask task);
    public delegate byte[] GetFileData(FileTransportTask task, int index);
    public delegate void FileDataReceived(FileTransportTask task, int index, byte[] data);

    public sealed class FileTransportTask
    {
        public int DatasetCount { get; set; }
        public int CurrentDataset { get; set; }

        public event GetFileData GetDatasetData;
        public event FileDataReceived DatasetReceived;

        internal byte[] OnGetDatasetData(int index) //muss schnell gehen (max 1,5 Sekunden)
        {
            return GetDatasetData(this, index);
        }
        internal void OnDatasetReceived(int index, byte[] data)
        {
            new Task(() => DatasetReceived(this, index, data)).Start();
        }

        public long ID { get; internal set; }

        public FileTransportState State { get; internal set; }

        public Connection Connection { get; internal set; }

        public byte[] CompressedBytes { get; internal set; }

        public int Size { get; internal set; }

        public int TransportedBytes { get; internal set; }

        public int RestUserToWait { get; internal set; }

        public User TargetUser { get; set; }

        public event Action Updated;
        internal event Action ServerStateUpdated;
        internal void OnServerStateUpdated()
        {
            ServerStateUpdated?.Invoke();
            var t = new Task(() =>
                {
                    Updated?.Invoke();
                });
            t.Start();
        }

        static long lastid = 0;

        public FileTransportTask()
        {
            DatasetCount = 0;
            ID = lastid += 1;
            State = FileTransportState.Waiting;
            Connection = null;
            CompressedBytes = new byte[0];
            Size = 0;
            TransportedBytes = 0;
            RestUserToWait = 0;
        }
    }

    public enum FileTransportState
    {
        Waiting,
        CompressBytes,
        WaitForLocalConnector,
        ConnectToServer,
        WaitForRemoteConnector,
        Transport,
        DecompressBytes,
        Finished
    }

    [Serializable]
    class FileTransportData
    {
        public long TransportID;
        public byte[] SenderID;
        public FileTransportType Type;
        public Connection Connection;

        public int Size, UserWait, Datasets;

        public FileTransportData()
        {
            Type = FileTransportType.WantSendFile;
            Connection = null;
        }
    }

    enum FileTransportType : byte
    {
        WantSendFile,
        WaitToGetPort,
        CouldSendFile
    }

    public enum ServerClientRole
    {
        Undefined,
        Server,
        Client
    }
}
