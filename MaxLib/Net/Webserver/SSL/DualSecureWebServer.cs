using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;

namespace MaxLib.Net.Webserver.SSL
{
    public class DualSecureWebServer : WebServer
    {
        public DualSecureWebServerSettings DualSettings => (DualSecureWebServerSettings)Settings;

        public DualSecureWebServer(DualSecureWebServerSettings settings) : base(settings)
        {
            WebServerLog.Add(ServerLogType.Information, GetType(), "StartUp", "The use of dual mode is critical");
        }

        protected override void ClientStartListen(HttpSession session)
        {
            if (session.NetworkStream == null && DualSettings.Certificate != null)
            {
                var peaker = new StreamPeaker(session.NetworkClient.GetStream());
                var mark = peaker.FirstByte;
                if (mark != 0 && (mark < 32 || mark >= 127))
                {
                    var ssl = new SslStream(peaker, false);
                    session.NetworkStream = ssl;
                    ssl.AuthenticateAsServer(
                        serverCertificate:          DualSettings.Certificate,
                        clientCertificateRequired:  false,
                        enabledSslProtocols:        SslProtocols.Default,
                        checkCertificateRevocation: true
                        );
                    if (!ssl.IsAuthenticated)
                    {
                        ssl.Dispose();
                        session.NetworkClient.Close();
                        AllSessions.Remove(session);
                        return;
                    }
                }
                else session.NetworkStream = peaker;
            }
            base.ClientStartListen(session);
        }

        class StreamPeaker : Stream
        {
            public StreamPeaker(NetworkStream baseStream)
            {
                BaseStream = baseStream ?? throw new ArgumentNullException("baseStream");
            }

            public NetworkStream BaseStream { get; private set; }

            int firstByte = -1;
            bool FirstByteReaded = false;

            public override bool CanRead => true;

            public override bool CanSeek => false;

            public override bool CanWrite => true;

            public override long Length => throw new NotSupportedException();

            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            public override void Flush()
            {
                BaseStream.Flush();
            }

            public byte FirstByte
            {
                get
                {
                    if (firstByte == -1)
                        GetFirstByte();
                    return (byte)firstByte;
                }
            }

            void GetFirstByte()
            {
                var b = new byte[1];
                BaseStream.Read(b, 0, 1);
                firstByte = b[0];
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (buffer == null) throw new ArgumentNullException("buffer");
                if (offset < 0 || offset + count > buffer.Length)
                    throw new ArgumentOutOfRangeException("offset");
                if (count < 0)
                    throw new ArgumentOutOfRangeException("count");
                if (count == 0) return 0;
                if (firstByte == -1) GetFirstByte();
                if (!FirstByteReaded)
                {
                    buffer[offset] = FirstByte;
                    FirstByteReaded = true;
                    return BaseStream.Read(buffer, offset +1 , count - 1) + 1;
                }
                return BaseStream.Read(buffer, offset, count);
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                BaseStream.Write(buffer, offset, count);
            }
        }
    }
}
