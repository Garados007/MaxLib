using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MaxLib.Data.CompactFileSystem.Extended;

namespace MaxLib.Data.CompactFileSystem
{
    /// <summary>
    /// Stellt ein System bereit, welches mehrere Datenströme wie Dateien in einer Datei zusammenfasst. 
    /// Hierüber ist freier asynchroner Zugriff möglich.
    /// </summary>
    public class CompactSystem : IDisposable, IExtendedSystem
    {
        /// <summary>
        /// Die Flags (Eigenschaften) zu dieser Datei
        /// </summary>
        public CompactSystemFlags Flag { get; private set; }
        /// <summary>
        /// Die Größe eines Pointers. Kleinere Pointer bedeuten weniger Platz, dafür aber weniger mögliche Adressen.
        /// </summary>
        public CompactPointerSize PointerSize
        {
            get { return (CompactPointerSize)((byte)Flag & 7); }
            set { Flag = (CompactSystemFlags)(((byte)Flag & ~7) | (byte)value); }
        }
        /// <summary>
        /// Die Größe eines Blocks in der Datei. Wenn <see cref="Flag"/> den Wert
        /// <see cref="CompactSystemFlags.CompactMode"/> enthält, so ist dieser Wert 0.
        /// </summary>
        public ulong BlockSize { get; private set; }
        /// <summary>
        /// Die Verwaltung für freien Speicher in der Datei.
        /// </summary>
        public CompactFreeSpaceRegistry FreeSpace { get; private set; }
        /// <summary>
        /// Die Verwaltung für alle Datenströme in der Datei
        /// </summary>
        public CompactFileTable FileTable { get; private set; }

        internal uint HeaderSize
        {
            get
            {
                if ((Flag & CompactSystemFlags.CompactMode) == CompactSystemFlags.CompactMode)
                    return 12U + (byte)PointerSize * 2U;
                else return 13U + (byte)PointerSize * 3U;
            }
        }

        internal uint BlockHeaderSize
        {
            get
            {
                if ((Flag & CompactSystemFlags.CompactMode) == CompactSystemFlags.CompactMode)
                    return (byte)PointerSize + 1U;
                else return 2U * (byte)PointerSize + 2U;
            }
        }

        internal ulong ContentBlockSize
        {
            get
            {
                return BlockSize - 2UL * (byte)PointerSize - 2;
            }
        }

        internal ulong StartMFTBlock, FreeSpaceBlock;
        private byte version;

        private Stream FileStream;
        private BinaryReader Reader;
        private BinaryWriter Writer;
        private object lockFileStream = new object();

        private byte[] systemkey = Encoding.UTF8.GetBytes("COMPFS  ");
        /// <summary>
        /// Gibt alle verwendeten Ressourcen, wie den Basisstream, wieder frei.
        /// </summary>
        public void Dispose()
        {
            Reader.Dispose();
            Writer.Dispose();
            FileStream.Dispose();
        }
        /// <summary>
        /// Löscht den Schreibpuffer und schreibt vorher alles in die Datei
        /// </summary>
        public void Flush()
        {
            lock (lockFileStream)
            {
                Writer.Flush();
                FileStream.Flush();
            }
        }

        private ulong ReadPointer()
        {
            var pd = Reader.ReadBytes((int)PointerSize + 1);
            ulong p = 0;
            for (int i = 0; i < pd.Length; ++i)
                p = (p << 8) | pd[i];
            return p;
        }
        private void WritePointer(ulong p)
        {
            var pd = new byte[(int)PointerSize + 1];
            for (int i = pd.Length-1; i>=0; --i)
            {
                pd[i] = (byte)(p & 0xFF);
                p = p >> 8;
            }
            Writer.Write(pd);
        }

        private void ReadManifest()
        {
            lock (lockFileStream)
            {
                FileStream.Position = 0;
                var systemkey = Reader.ReadBytes(this.systemkey.Length);
                for (var i = 0; i < systemkey.Length; ++i)
                    if (systemkey[i] != this.systemkey[i])
                        throw new FormatException("system file has wrong format");
                version = Reader.ReadByte();
                if (version != 1) throw new NotSupportedException("Version " + version.ToString() + " is not supported now");
                Flag = (CompactSystemFlags)Reader.ReadByte();
                BlockSize = ReadPointer();
                StartMFTBlock = ReadPointer();
                if ((Flag & CompactSystemFlags.CompactMode) != CompactSystemFlags.CompactMode)
                    FreeSpaceBlock = ReadPointer();
            }
        }
        private void WriteMainfest()
        {
            lock (lockFileStream)
            {
                FileStream.Position = 0;
                Writer.Write(systemkey);
                Writer.Write((byte)1);
                Writer.Write((byte)Flag);
                WritePointer(BlockSize);
                WritePointer(StartMFTBlock);
                if ((Flag & CompactSystemFlags.CompactMode) != CompactSystemFlags.CompactMode)
                    WritePointer(FreeSpaceBlock);
            }
        }

        internal void ReadHeader(ulong p, out ulong size, out ulong next)
        {
            lock (lockFileStream)
            {
                FileStream.Position = (long)p;
                size = ReadPointer();
                next = ReadPointer();
            }
        }
        internal void WriteHeader(ulong p, ulong size, ulong next)
        {
            lock (lockFileStream)
            {
                FileStream.Position = (long)p;
                WritePointer(size);
                WritePointer(next);
            }
        }
        internal void ReadPart(ulong p, byte[] buffer, int offset, int length)
        {
            lock (lockFileStream)
            {
                FileStream.Position = (long)p;
                FileStream.Read(buffer, offset, length);
            }
        }
        internal void WritePart(ulong p, byte[] buffer, int offset, int length)
        {
            lock (lockFileStream)
            {
                FileStream.Position = (long)p;
                FileStream.Write(buffer, offset, length);
            }
        }
        
        /// <exception cref="OutOfMemoryException" />
        internal void RegisterPage(out ulong p)
        {
            lock (lockFileStream)
            {
                p = (ulong)FileStream.Length;
                var bp = ToBlockPointer(p + BlockSize - 1); //get the pointer of the last byte in the block
                var mp = MaxBlockSize(PointerSize);
                if (bp > mp) throw new OutOfMemoryException("cannot access more memory 'cause the pointer size is too small to access it");
                FileStream.Position = FileStream.Length;
                FileStream.SetLength(FileStream.Length + (long)BlockSize);
                WritePointer(0);
                WritePointer(0);
            }
        }
        internal void SetStreamSize(ulong s)
        {
            lock (lockFileStream)
            {
                if (FileStream.Position > (long)s) FileStream.Position = (long)s;
                FileStream.SetLength((long)s);
            }
        }

        internal ulong ToSystemPointer(ulong p)
        {
            if (p == 0) return 0;
            if ((Flag & CompactSystemFlags.ReferenceByBlock) == CompactSystemFlags.ReferenceByBlock)
                return HeaderSize + (p - 1) * BlockSize;
            else return p;
        }

        internal ulong ToBlockPointer(ulong p)
        {
            if (p == 0) return 0;
            if ((Flag & CompactSystemFlags.ReferenceByBlock) != CompactSystemFlags.ReferenceByBlock)
                return p;
            if (p < HeaderSize) return 0;
            return (p - HeaderSize) / BlockSize + 1;
        }

        #region Extensions

        ulong IExtendedSystem.ToSystemPointer(ulong p)
        {
            return ToSystemPointer(p);
        }

        ulong IExtendedSystem.ToBlockPointer(ulong p)
        {
            return ToBlockPointer(p);
        }

        uint IExtendedSystem.GetHeaderSize()
        {
            return HeaderSize;
        }

        uint IExtendedSystem.GetBlockHeaderSize()
        {
            return BlockHeaderSize;
        }

        ulong IExtendedSystem.GetContentBlockSize()
        {
            return ContentBlockSize;
        }

        ulong IExtendedSystem.GetMFTBlockPointer()
        {
            return StartMFTBlock;
        }

        ulong IExtendedSystem.GetFreeSpaceBlockPointer()
        {
            return FreeSpaceBlock;
        }


        ulong IExtendedSystem.GetBaseStreamLength()
        {
            return (ulong)FileStream.Length;
        }

        void IExtendedSystem.ReadHeader(ulong p, out ulong size, out ulong next)
        {
            ReadHeader(p, out size, out next);
        }

        void IExtendedSystem.ReadPart(ulong p, byte[] buffer, int offset, int length)
        {
            ReadPart(p, buffer, offset, length);
        }

        #endregion

        /// <summary>
        /// Erzeugt ein neues Verwaltungssytem für multiple Streams anhand eines einfachen Streams. Es wird 
        /// vorrausgesetzt, dass der Basistream schon gültige Daten enthält.
        /// </summary>
        /// <param name="stream">Der Basisstream in dem alle Einzelstreams gespeichert werden.</param>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="FormatException"/>
        public CompactSystem(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException("stream");
            if (stream.Length == 0) throw new FormatException("the stream is empty and does not contains any data");
            if (!stream.CanRead) throw new ArgumentException("stream", "stream is not readable");
            if (!stream.CanSeek) throw new ArgumentException("stream", "stream is not seekable");
            this.FileStream = stream;
            Reader = new BinaryReader(stream);
            Writer = new BinaryWriter(stream);
            stream.Position = 0;
            ReadManifest();
            if ((Flag & CompactSystemFlags.CompactMode) != CompactSystemFlags.CompactMode)
                FreeSpace = new CompactFreeSpaceRegistry(this);
            FileTable = new CompactFileTable(this);
        }
        /// <summary>
        /// Erzeugt ein neues Verwaltungssytem für multple Streams anhand eines einfachen Stream. Hierbei wird ein 
        /// neues System in dem Basisstream erzeugt (er wird dadurch gelöscht).
        /// </summary>
        /// <param name="stream">Der Basisstream in dem alle Einzelstreams gespeichert werden.</param>
        /// <param name="pointerSize">Die Größe eines Pointers im Stream</param>
        /// <param name="flags">Eigenschaften des Streams</param>
        /// <param name="blockSize">Die Größe eines Blockes im Stream</param>
        /// <exception cref="ArgumentException" />
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="ArgumentOutOfRangeException" />
        public CompactSystem(Stream stream, CompactPointerSize pointerSize, CompactSystemFlags flags, ulong blockSize)
        {
            if (stream == null) throw new ArgumentNullException("stream");
            flags = (CompactSystemFlags)(((byte)flags & ~7) | (byte)pointerSize);
            if ((flags & CompactSystemFlags.CompactMode) == CompactSystemFlags.CompactMode)
                throw new ArgumentException("flags", "the mode CompactMode is not supported with this constructor");
            if (!stream.CanRead) throw new ArgumentException("stream", "stream is not readable");
            if (!stream.CanSeek) throw new ArgumentException("stream", "stream is not seekable");
            if (!stream.CanWrite) throw new ArgumentException("stream", "stream is not writeable");
            var hs = (byte)pointerSize * 2 + 2;
            if (blockSize <= (ulong)hs) throw new ArgumentOutOfRangeException("blockSize",
                 "this value is not large enough to contains the block header. (required >" + hs.ToString() + ")");
            var mbs = MaxBlockSize(pointerSize);
            if (blockSize > mbs) throw new ArgumentOutOfRangeException("blockSize",
                "this value is to large to store in this file. (required <=" + mbs.ToString() + ")");
            this.FileStream = stream;
            Reader = new BinaryReader(stream);
            Writer = new BinaryWriter(stream);
            Flag = flags;
            BlockSize = blockSize;
            stream.Position = 0;
            WriteMainfest();
            var p = stream.Position;
            stream.SetLength(p + (long)blockSize * 2);
            StartMFTBlock = ToBlockPointer((ulong)p);
            FreeSpaceBlock = ToBlockPointer((ulong)p + blockSize);
            stream.Position = 0;
            WriteMainfest();
            WriteHeader(ToSystemPointer(StartMFTBlock), 0, 0);
            WriteHeader(ToSystemPointer(FreeSpaceBlock), 0, 0);
            FreeSpace = new CompactFreeSpaceRegistry(this);
            FileTable = new CompactFileTable(this);
        }

        /// <summary>
        /// Ermittelt die maximale Blockgröße die für eine Pointergröße möglich ist.
        /// </summary>
        /// <param name="pointerSize">Die Pointergröße</param>
        /// <returns>Die maximale Blockgröße</returns>
        public static ulong MaxBlockSize(CompactPointerSize pointerSize)
        {
            ulong ms = 0;
            for (var i = 0; i <= (int)pointerSize; ++i)
                ms = (ms << 8) | 0xFF;
            return ms;
        }
    }
    
    #region Enums

    /// <summary>
    /// Bestimmt wie viel Speicher ein Pointer einnimmt.
    /// </summary>
    public enum CompactPointerSize : byte
    {
        /// <summary>
        /// Ein Pointer nimmt 1 Byte ein und hat somit einen Wertebereich von 0 bis 255 (255 Byte).
        /// </summary>
        Byte1 = 0,
        /// <summary>
        /// Ein Pointer nimmt 2 Byte ein und hat somit einen Wertebereich von 0 bis 65.535 (~64 KB).
        /// </summary>
        Byte2 = 1,
        /// <summary>
        /// Ein Pointer nimmt 3 Byte ein und hat somit einen Wertebereich von 0 bis 16.777.215 (~16 MB).
        /// </summary>
        Byte3 = 2,
        /// <summary>
        /// Ein Pointer nimmt 4 Byte ein und hat somit einen Wertebereich von 0 bis 4.294.967.295 (~4 GB).
        /// </summary>
        Byte4 = 3,
        /// <summary>
        /// Ein Pointer nimmt 5 Byte ein und hat somit einen Wertebereich von 0 bis 1.099.511.627.775 (~1 TB).
        /// </summary>
        Byte5 = 4,
        /// <summary>
        /// Ein Pointer nimmt 6 Byte ein und hat somit einen Wertebereich von 0 bis 281.474.976.710.655 (~256 TB).
        /// </summary>
        Byte6 = 5,
        /// <summary>
        /// Ein Pointer nimmt 7 Byte ein und hat somit einen Wertebereich von 0 bis 72.057.594.037.927.935 (~64 PB).
        /// </summary>
        Byte7 = 6,
        /// <summary>
        /// Ein Pointer nimmt 8 Byte ein und hat somit einen Wertebereich von 0 bis 18.446.744.073.709.551.615 (~16 EB).
        /// </summary>
        Byte8 = 7
    }
    
    /// <summary>
    /// Bestimmt den Modus der Dateibibliothek.
    /// </summary>
    public enum CompactSystemFlags : byte
    {
        /// <summary>
        /// Gibt die Standartdefinition an.
        /// </summary>
        None = 0,
        /// <summary>
        /// Der 1. Bitflag für die Pointergröße <see cref="CompactPointerSize"/>
        /// </summary>
        Bit1PointerFlag = 1,
        /// <summary>
        /// Der 2. Bitflag für die Pointergröße <see cref="CompactPointerSize"/>
        /// </summary>
        Bit2PointerFlag = 2,
        /// <summary>
        /// Der 3. Bitflag für die Pointergröße <see cref="CompactPointerSize"/>
        /// </summary>
        Bit3PointerFlag = 4,
        /// <summary>
        /// Der 4. Bitflag für die Pointergröße <see cref="CompactPointerSize"/>
        /// </summary>
        Bit4PointerFlag = 8,
        /// <summary>
        /// Bestimmt, dass alle Pointer nur auf ganze Blöche bzw. Einträge innerhalb eines Blocks 
        /// beziehen. Der Pointer muss mindestens so groß sein wie die Blockgröße und die Anzahl aller
        /// verbrauchten Blöcke (Dateigröße/Blockgröße). 
        /// Wenn dieser Flag nicht gesetzt wird, so gilt der Pointer für die gesammte Datei und muss 
        /// mindestens so groß wie die Datei an sich sein.
        /// </summary>
        ReferenceByBlock = 16,
        /// <summary>
        /// Die Datei befindet sich im kompakten Modus. Es wird nur ein Block pro Stream 
        /// verwendet und die Blockgrößen sind variabel. Es existiert ein Schreibschutz und die Freispeichertabelle 
        /// existiert nicht. 
        /// Wenn dieser Flag nicht gesetzt wird, so wird ein Stream anhand der Blockgröße in mehrere 
        /// Teilblöcke aufgeteilt, die Daten sind jederzeit änderbar und es existiert eine Freispeichertabelle 
        /// um schnell freie Blöcke für neue Streams finden zu können.
        /// </summary>
        CompactMode = 32,
        /// <summary>
        /// Es existieren keine Ordner, Dateihierarchien und Offstreams. Dafür kann der Name einer Datei leer 
        /// sein und diese wird nur über eine ID identifiziert. 
        /// Wenn dieser Flag nicht gesetzt wird, so kann man verschiedene Ordner anlegen um den Dateien eine 
        /// Hierarchie zu geben. Der Name und Pfad einer Datei muss immer eindeutig sein und es können 
        /// Offstreams angelegt werden.
        /// </summary>
        SingleFileMode = 64,
        /// <summary>
        /// Die Attributdefinition zu Dateien und Ordnern wird erweitert und speichert Daten wie Erzeugungszeitpunkt 
        /// und letzte Änderung. Wird dieser Flag nicht gesetzt, so sind diese Daten nicht vorhanden.
        /// </summary>
        ExtendedAttribute = 128,
    }

    /// <summary>
    /// Bestimmt den Modus eines Eintrags
    /// </summary>
    public enum CompactEntryFlags : byte
    {
        /// <summary>
        /// Gibt die Standartdefinition an.
        /// </summary>
        None = 0,
        /// <summary>
        /// Dieser Eintrag ist ein Ordner und enthält somit keinen direkten Inhalt. 
        /// Dafür können andere Dateien und Ordner darauf eine Hierarchiebeziehung 
        /// aufbauen.
        /// </summary>
        DirectoryMode = 1,
        /// <summary>
        /// Dieser Eintrag ist versteckt und soll in der normalen Dateiansicht nicht angezeigt werden.
        /// </summary>
        Hidden = 2,
        /// <summary>
        /// Dieser Eintrag ist schreibgeschützt und darf weder verändert noch gelöscht werden. 
        /// Andere Einträge die hierauf Bezug nehmen dürfen schon geändert werden.
        /// </summary>
        Protected = 4,
        /// <summary>
        /// Dieser Eintrag (egal ob Ordner oder Datei) enthält zusätzliche Streams mit zusätzlichen Daten. 
        /// Diese können getrennt zum Eintrag abgerufen werden sind aber direkt abhängig, d.h. wenn der 
        /// Eintrag gelöscht wird, so werden auch alle Offstreams gelöscht.
        /// </summary>
        ContainsOffstreams = 8,
        /// <summary>
        /// Der Name des Eintrags ist zu lang für die MFT und wird deshalb in einen Block ausgelagert. Es wird davon 
        /// abgeraten all zu lange Namen zu verwenden, da sie bei der Suche und beim Abrufen deutlich 
        /// mehr Zeit in Anspruch nehmen.
        /// </summary>
        ExportName = 16,
    }

    #endregion

    #region Streams

    /// <summary>
    /// Stellt einen einzelnen Stream in <see cref="CompactSystem"/> dar. Er kümmert sich 
    /// automatisch um die ganze Verwaltung und man kann ihn ganz normal wie ein 
    /// <see cref="FileStream"/> nutzen.
    /// </summary>
    public class CompactBlockStream : Stream, IExtendedBlockStream
    {
        /// <summary>
        /// Das <see cref="CompactSystem"/> welcher die Quelle für die Daten ist.
        /// </summary>
        public CompactSystem System { get; private set; }
        /// <summary>
        /// Die Adresse unter der der Datenstrom in <see cref="System"/> 
        /// zu finden ist.
        /// </summary>
        public ulong Pointer { get; private set; }
        /// <summary>
        /// Die Systemadresse von <see cref="Pointer"/>.
        /// </summary>
        public ulong SystemPointer { get; private set; }
        /// <summary>
        /// Die Puffergröße für die Zwischenspeicherung von Daten. Darüber ist ein 
        /// <see cref="Read(byte[], int, int)"/> deutlich schneller. Sie ist von 
        /// <see cref="CompactSystem.BlockSize"/> abhängig und beträgt maximal 64 KB.
        /// </summary>
        public int BufferSize { get; private set; }
        /// <summary>
        /// Erzeugt einen neuen Datenstrom für ein <see cref="CompactSystem"/>.
        /// </summary>
        /// <param name="system">Das <see cref="CompactSystem"/> unter dem die ganzen Daten zu finden sind.</param>
        /// <param name="pointer">Die Adresse unter der der Stream zu finden ist.</param>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="ArgumentOutOfRangeException"/>
        public CompactBlockStream(CompactSystem system, ulong pointer)
        {
            if (system == null) throw new ArgumentNullException("system");
            if (pointer == 0) throw new ArgumentOutOfRangeException("pointer", "0 is not a valid pointer");
            this.System = system;
            this.Pointer = pointer;
            this.SystemPointer = System.ToSystemPointer(pointer);
            BufferSize = (int)Math.Min(65535, System.BlockSize);
            GetMeasures();
            buffer = new byte[BufferSize];
            blockIndex = 0;
            bufferStart = 0;
            bufferOffset = 0;
            UpdateBuffer(true);
        }
        
        byte[] buffer;
        int blockIndex;
        List<BlockMeasures> blockMeasures = new List<BlockMeasures>();
        ulong bufferOffset, bufferStart;

        internal void DestroyAllBlocks()
        {
            for (var i = 0; i < blockMeasures.Count; ++i)
                System.FreeSpace.AddFreeBlock(System.ToBlockPointer(blockMeasures[i].Position));
            blockMeasures.Clear();
            buffer = new byte[0];
            blockIndex = 0;
            bufferOffset = bufferStart = 0;
            Pointer = SystemPointer = 0;
            BufferSize = 0;
        }

        private void GetMeasures()
        {
            var pointer = SystemPointer;
            ulong completeSize = 0;
            while (pointer != 0)
            {
                ulong size, next;
                System.ReadHeader(pointer, out size, out next);
                completeSize += size;
                var m = new BlockMeasures();
                m.CompleteSize = completeSize;
                m.NextBlock = next;
                m.Position = pointer;
                m.BlockPosition = System.ToBlockPointer(pointer);
                m.Size = size;
                m.SystemNextBlock = pointer = System.ToSystemPointer(next);
                blockMeasures.Add(m);
            }
        }

        private void UpdateBuffer(bool force)
        {
            int nbi = 0;
            ulong pos = (ulong)position;
            if (!force && bufferStart <= pos && bufferStart + (ulong)BufferSize > pos) return;
            for (; nbi < blockMeasures.Count; ++nbi)
                if (blockMeasures[nbi].CompleteSize > pos)
                    break;
            if (nbi >= blockMeasures.Count) nbi = blockMeasures.Count - 1;
            if (nbi != blockIndex)
            {
                blockIndex = nbi;
                bufferOffset = pos - (nbi == 0 ? 0 : blockMeasures[nbi - 1].CompleteSize);
            }
            else
            {
                if (pos > bufferStart)
                    bufferOffset += pos - bufferStart; //Die Startposition des Buffers im Block
                else bufferOffset -= bufferStart - pos;
            }
            bufferStart = pos; //die streampos wo der Buffer startet
            int readed = 0;
            ulong off = bufferOffset;
            while (readed < BufferSize && nbi < blockMeasures.Count)
            {
                int toread = (int)Math.Min((ulong)(BufferSize - readed), blockMeasures[nbi].Size - off);
                if (toread > 0)
                    System.ReadPart(blockMeasures[nbi].Position + 2 * (ulong)System.PointerSize + 2 + off, buffer, readed, toread);
                readed += toread;
                nbi++;
                off = 0;
            }
        }

        /// <summary>
        /// Gibt an ob Leseoperationen möglich sind.
        /// </summary>
        public override bool CanRead
        {
            get
            {
                return true;
            }
        }
        /// <summary>
        /// Gibt an ob Suchoperationen möglich sind.
        /// </summary>
        public override bool CanSeek
        {
            get
            {
                return true;
            }
        }
        /// <summary>
        /// Gibt an ob Schreiboperationen möglich sind.
        /// </summary>
        public override bool CanWrite
        {
            get
            {
                return (System.Flag & CompactSystemFlags.CompactMode) != CompactSystemFlags.CompactMode;
            }
        }
        /// <summary>
        /// Gibt die Länge vom Datenstrom aus. Zum Ändern der Länge nutzen Sie bitte 
        /// <see cref="SetLength(long)"/>.
        /// </summary>
        public override long Length
        {
            get
            {
                return (long)blockMeasures[blockMeasures.Count - 1].CompleteSize;
            }
        }

        private long position = 0;
        /// <summary>
        /// Die aktuelle Position im Datenstrom.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"/>
        /// <exception cref="NotSupportedException"/>
        public override long Position
        {
            get { return position; }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException("Position");
                if (value>=Length)
                {
                    if (!CanWrite) throw new NotSupportedException("Stream is readonly");
                    else SetLength(position);
                }
                position = value;
            }
        }
        /// <summary>
        /// Flusht den Schreibpuffer in <see cref="System"/>.
        /// </summary>
        public override void Flush()
        {
            System.Flush();
        }
        /// <summary>
        /// Ließt Werte aus dem Datenstrom.
        /// </summary>
        /// <param name="buffer">Der Buffer indem die Werte reinsollen.</param>
        /// <param name="offset">Die Stelle ab der in den Buffer geschrieben werden soll.</param>
        /// <param name="count">Die Anzahl der Bytes die gelesen werden sollen.</param>
        /// <returns>Die Anzahl der gelesenen Bytes</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            UpdateBuffer(false);
            int readed = 0;
            while (readed < count)
            {
                int toread = (int)Math.Min((ulong)(count - readed), 
                    (ulong)BufferSize + bufferStart - (ulong)position);
                if (toread == 0) return readed;
                int off2 = (int)((ulong)position - bufferStart);
                for (int i = 0; i < toread; ++i)
                    buffer[offset + i + readed] = this.buffer[i + off2];
                readed += toread;
                position += toread;
                if (readed >= count) return readed;
                UpdateBuffer(true);
            }
            return readed;
        }
        /// <summary>
        /// Sucht eine bestimmte Stelle im Datenstrom und setzt die unter <see cref="Position"/>.
        /// </summary>
        /// <param name="offset">Der Offset für die Suche.</param>
        /// <param name="origin">Die Quelle für die Suche.</param>
        /// <returns>Die Position die gefunden wurde.</returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin: return position = offset;
                case SeekOrigin.Current: return position += offset;
                case SeekOrigin.End:return position = Length - offset;
                default: return 0;
            }
        }
        /// <summary>
        /// Verändert die Länge des Datenstroms.
        /// </summary>
        /// <param name="value">Die neue Länge des Datenstroms.</param>
        /// <exception cref="ArgumentOutOfRangeException"/>
        /// <exception cref="NotSupportedException"/>
        /// <exception cref="OutOfMemoryException"/>
        public override void SetLength(long value)
        {
            if (!CanWrite) throw new NotSupportedException("the stream is readonly");
            if (value < 0) throw new ArgumentOutOfRangeException("value");
            if (value > Length)
            {
                var cbs = System.ContentBlockSize * 3 / 4;
                while (value > Length)
                {
                    var last = blockMeasures[blockMeasures.Count - 1];
                    if (last.Size < cbs)
                    {
                        var inc = Math.Min(cbs-last.Size, (ulong)value - last.CompleteSize);
                        last.Size += inc;
                        last.CompleteSize += inc;
                        System.WriteHeader(last.Position, last.Size, last.NextBlock);
                    }
                    else
                    {
                        var p = System.ToSystemPointer(System.FreeSpace.GetFreePage());
                        last.SystemNextBlock = p;
                        last.NextBlock = System.ToBlockPointer(p);
                        System.WriteHeader(last.Position, last.Size, last.NextBlock);
                        var l= new BlockMeasures();
                        l.CompleteSize = last.CompleteSize;
                        l.NextBlock = 0;
                        l.Position = p;
                        l.BlockPosition = last.NextBlock;
                        l.Size = 0;
                        l.SystemNextBlock = 0;
                        blockMeasures.Add(l);
                    }
                }
            }
            else if (value < Length)
            {
                while (value < Length)
                {
                    var last = blockMeasures[blockMeasures.Count - 1];
                    var dif = (ulong)(Length - value);
                    if (dif > last.Size)
                    {
                        dif = last.Size;
                        System.FreeSpace.AddFreeBlock(System.ToBlockPointer(last.Position));
                        var l = blockMeasures[blockMeasures.Count - 2];
                        l.NextBlock = 0;
                        l.SystemNextBlock = 0;
                        System.WriteHeader(l.Position, l.Size, l.NextBlock);
                        blockMeasures.RemoveAt(blockMeasures.Count - 1);
                    }
                    else
                    {
                        last.Size -= dif;
                        last.CompleteSize -= dif;
                        System.WriteHeader(last.Position, last.Size, last.NextBlock);
                    }
                }
            }
        }
        /// <summary>
        /// Schreibt Werte in den Datenstrom.
        /// </summary>
        /// <param name="buffer">Der Buffer der als Quelle der Daten dient.</param>
        /// <param name="offset">Die Stelle ab der im Buffer gelesen werden soll.</param>
        /// <param name="count">Die Anzahl der Bytes die geschrieben werden sollen.</param>
        /// <exception cref="NotSupportedException"/>
        /// <exception cref="OutOfMemoryException"/>
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (!CanWrite) throw new NotSupportedException("the stream is readonly");
            if (Length < Position + count) SetLength(Position + count);
            int writed = 0;
            int ind = 0;
            for (; ind < blockMeasures.Count; ++ind)
                if (blockMeasures[ind].CompleteSize > (ulong)position)
                    break;
            ulong off = (ulong)position - (ind == 0 ? 0 : blockMeasures[ind - 1].CompleteSize);
            while (writed < count)
            {
                int towrite = (int)Math.Min(blockMeasures[ind].Size - off, (ulong)(count - writed));
                System.WritePart(blockMeasures[ind].Position + 2UL * (byte)System.PointerSize + 2 + off, buffer, offset + writed, towrite);
                writed += towrite;
                ind++;
                off = 0;
            }
            position += count;
            UpdateBuffer(true);
        }
        /// <summary>
        /// Ersetzt einen Bereich im Datenstrom durch neue Werte. Die Größe vom alten und vom 
        /// neuem Bereich müssen nicht übereinstimmen.
        /// </summary>
        /// <param name="orignLength">Die ursprüngliche Länge. Diese Anzahl von Bytes werden ersetzt.</param>
        /// <param name="newContent">Der Buffer mit den neuen Werten.</param>
        /// <param name="offset">Die Stelle ab der vom Buffer die Werte verwendet werden sollen.</param>
        /// <param name="count">Die Anzahl der Bytes die aus dem Buffer verwendet werden und gleichzeitig die neue Länge des Bereichs.</param>
        /// <exception cref="NotSupportedException" />
        /// <exception cref="OutOfMemoryException" />
        public virtual void Replace(int orignLength, byte[] newContent, int offset, int count)
        {
            if (!CanWrite) throw new NotSupportedException("the stream is readonly");
            if (orignLength == count)
            {
                Write(newContent, offset, count);
                return;
            }
            int rawind;
            var targetpos = position + count;
            ulong move = 2UL * (byte)System.PointerSize + 2;
            if (orignLength < count)
            {
                Write(newContent, offset, orignLength);
                int o = offset + orignLength;
                int c = count - orignLength;
                int ind = 0;
                for (; ind < blockMeasures.Count; ++ind)
                    if (blockMeasures[ind].CompleteSize > (ulong)position)
                        break;
                if (ind >= blockMeasures.Count) ind = blockMeasures.Count - 1;
                rawind = ind;
                ulong off = (ulong)position - (ind == 0 ? 0 : blockMeasures[ind - 1].CompleteSize);
                byte[] rest = new byte[blockMeasures[ind].Size - off];
                if (rest.Length > 0) Read(rest, 0, rest.Length);
                int o2 = 0;
                int c2 = rest.Length;
                while (c > 0 || c2>0)
                {
                    int towrite = (int)Math.Min(System.ContentBlockSize - off, (ulong)(c > 0 ? c : c2));
                    if (towrite > 0)
                    {
                        if (c > 0)
                        {
                            System.WritePart(blockMeasures[ind].Position + move + off, newContent, o, c);
                            o += towrite;
                            c -= towrite;
                            blockMeasures[ind].Size = off + (ulong)towrite;
                            off += (ulong)towrite;
                        }
                        else
                        {
                            System.WritePart(blockMeasures[ind].Position + move + off, rest, o2, c2);
                            o2 += towrite;
                            c2 -= towrite;
                            blockMeasures[ind].Size = off + (ulong)towrite;
                        }
                        System.WriteHeader(blockMeasures[ind].Position, blockMeasures[ind].Size, blockMeasures[ind].NextBlock);
                    }
                    else
                    {
                        var page = System.FreeSpace.GetFreePage();
                        var t = new BlockMeasures();
                        t.NextBlock = blockMeasures[ind].NextBlock;
                        t.SystemNextBlock = blockMeasures[ind].SystemNextBlock;
                        t.Position = System.ToSystemPointer(page);
                        t.BlockPosition = page;
                        t.Size = 0;
                        blockMeasures[ind].NextBlock = page;
                        blockMeasures[ind].SystemNextBlock = t.Position;
                        blockMeasures.Insert(ind + 1, t);
                        System.WriteHeader(blockMeasures[ind].Position, blockMeasures[ind].Size, blockMeasures[ind].NextBlock);
                        ind++;
                        System.WriteHeader(blockMeasures[ind].Position, blockMeasures[ind].Size, blockMeasures[ind].NextBlock);
                        off = 0;
                    }
                }
            }
            else
            {
                Write(newContent, offset, count);
                int ind = 0;
                for (; ind < blockMeasures.Count; ++ind)
                    if (blockMeasures[ind].CompleteSize > (ulong)position)
                        break;
                rawind = ind;
                ulong off = (ulong)position - (ind == 0 ? 0 : blockMeasures[ind - 1].CompleteSize);
                ulong dif = (ulong)(orignLength - count);
                while (dif> 0)
                {
                    if (dif > blockMeasures[ind].Size - off)
                    {
                        if (off == 0 && ind != 0)
                        {
                            System.FreeSpace.AddFreeBlock(System.ToBlockPointer(blockMeasures[ind].Position));
                            blockMeasures[ind - 1].NextBlock = blockMeasures[ind].NextBlock;
                            blockMeasures[ind - 1].SystemNextBlock = blockMeasures[ind].SystemNextBlock;
                            System.WriteHeader(blockMeasures[ind - 1].Position, blockMeasures[ind - 1].Size, blockMeasures[ind - 1].NextBlock);
                            dif -= blockMeasures[ind].Size;
                            blockMeasures.RemoveAt(ind);
                        }
                        else
                        {
                            dif -= blockMeasures[ind].Size - off;
                            blockMeasures[ind].Size = off;
                            System.WriteHeader(blockMeasures[ind].Position, blockMeasures[ind].Size, blockMeasures[ind].NextBlock);
                            ind++;
                            off = 0;
                        }
                    }
                    else
                    {
                        var rest = new byte[blockMeasures[ind].Size - off - dif];
                        System.ReadPart(blockMeasures[ind].Position + move + off + dif, rest, 0, rest.Length);
                        System.WritePart(blockMeasures[ind].Position + move + off, rest, 0, rest.Length);
                        blockMeasures[ind].Size -= dif;
                        System.WriteHeader(blockMeasures[ind].Position, blockMeasures[ind].Size, blockMeasures[ind].NextBlock);
                        dif = 0;
                    }
                }
            }
            position = targetpos;
            ulong compsize = rawind==0?0:blockMeasures[rawind-1].CompleteSize;
            for (var i = rawind; i < blockMeasures.Count; ++i)
                blockMeasures[i].CompleteSize = compsize = compsize + blockMeasures[i].Size;
            UpdateBuffer(true);
        }

        #region Extension

        byte[] IExtendedBlockStream.GetReadBuffer()
        {
            var b = new byte[buffer.Length];
            buffer.CopyTo(b, 0);
            return b;
        }

        int IExtendedBlockStream.GetReadBufferBlockIndex()
        {
            return blockIndex;
        }

        ulong IExtendedBlockStream.GetReadBufferStreamOffset()
        {
            return bufferStart;
        }

        ulong IExtendedBlockStream.GetReadBufferBlockOffset()
        {
            return bufferOffset;
        }

        IExtendedMeasures[] IExtendedBlockStream.GetMeasures()
        {
            return blockMeasures.ConvertAll((m) => (IExtendedMeasures)m).ToArray();
        }

        void IExtendedBlockStream.DoDestroyAllBlocks()
        {
            DestroyAllBlocks();
        }

        void IExtendedBlockStream.DoUpdateReadBuffer(bool force)
        {
            UpdateBuffer(force);
        }

        #endregion

        class BlockMeasures : IExtendedMeasures
        {
            public ulong Position;
            public ulong Size;
            public ulong NextBlock;
            public ulong SystemNextBlock;
            public ulong CompleteSize;
            public ulong BlockPosition;

            #region Extension

            ulong IExtendedMeasures.GetPosition()
            {
                return BlockPosition;
            }

            ulong IExtendedMeasures.GetSize()
            {
                return Size;
            }

            ulong IExtendedMeasures.GetNextBlock()
            {
                return NextBlock;
            }

            #endregion
        }
    }

    public class CompactContentStream : Stream, IExtendedHiddenBlockStream
    {
        public CompactEntry Entry { get; private set; }

        private CompactBlockStream Stream;

        internal CompactContentStream(CompactEntry entry)
        {
            this.Entry = entry;
            Stream = new CompactBlockStream(entry.System, entry.ContentPointer);
        }

        public override bool CanRead
        {
            get
            {
                return Stream.CanRead;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return Stream.CanSeek;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return Stream.CanWrite && !Entry.IsProtected;
            }
        }

        public override long Length
        {
            get
            {
                return Stream.Length;
            }
        }

        public override long Position
        {
            get
            {
                return Stream.Position;
            }

            set
            {
                if (value > Stream.Length && Entry.IsProtected)
                    throw new ArgumentOutOfRangeException("Position");
                Stream.Position = value;
            }
        }

        public override void Flush()
        {
            Stream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return Stream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return Stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            if (Entry.IsProtected) throw new InvalidOperationException("the file is protected");
            Stream.SetLength(value);
            if ((Entry.System.Flag & CompactSystemFlags.ExtendedAttribute) == CompactSystemFlags.ExtendedAttribute)
            {
                Entry.ModifiedTime = DateTime.Now;
                Entry.CallChange();
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (Entry.IsProtected) throw new InvalidOperationException("the file is protected");
            Stream.Write(buffer, offset, count);
            if ((Entry.System.Flag & CompactSystemFlags.ExtendedAttribute) == CompactSystemFlags.ExtendedAttribute)
            {
                Entry.ModifiedTime = DateTime.Now;
                Entry.CallChange();
            }
        }

        CompactBlockStream IExtendedHiddenBlockStream.GetBlockStream()
        {
            return Stream;
        }
    }

    public class CompactOffStream : Stream, IExtendedHiddenBlockStream
    {
        public CompactEntry Entry { get; private set; }

        string name;
        public string Name
        {
            get { return name; }
            set
            {
                if (Entry.IsProtected) throw new InvalidOperationException("entry is protected");
                if (!Stream.CanWrite) throw new InvalidOperationException("system is readonly");
                if (value == null) throw new ArgumentNullException("Name");
                if (value == name) return;
                name = value;
                var b = Encoding.UTF8.GetBytes(value);
                Stream.Position = 0;
                Stream.Write(b, 0, Math.Min(namesize, b.Length));
                if (namesize>b.Length)
                {
                    b = new byte[namesize - b.Length];
                    Stream.Write(b, 0, b.Length);
                }
                if ((Entry.System.Flag & CompactSystemFlags.ExtendedAttribute) == CompactSystemFlags.ExtendedAttribute)
                {
                    Entry.ModifiedTime = DateTime.Now;
                    Entry.CallChange();
                }
                Stream.Position = namesize;
            }
        }

        private CompactBlockStream Stream;
        private int namesize;

        internal CompactOffStream(CompactEntry entry, ulong pointer)
        {
            Entry = entry;
            Stream = new CompactBlockStream(entry.System, pointer);
            namesize = (byte)entry.System.PointerSize * 4 + 4;
            var b = new byte[namesize];
            Stream.Position = 0;
            Stream.Read(b, 0, b.Length);
            name = Encoding.UTF8.GetString(b);
            Stream.Position = namesize;
        }

        public override bool CanRead
        {
            get
            {
                return Stream.CanRead;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return Stream.CanSeek;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return Stream.CanWrite && !Entry.IsProtected;
            }
        }

        public override long Length
        {
            get
            {
                return Stream.Length - namesize;
            }
        }

        public override long Position
        {
            get
            {
                return Stream.Position - namesize;
            }

            set
            {
                if (Position < 0) throw new ArgumentOutOfRangeException("Position");
                if (Position > Length && Entry.IsProtected)
                    throw new NotSupportedException("entry is protected");
                Stream.Position = value + namesize;
            }
        }

        public override void Flush()
        {
            Stream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return Stream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    if (offset < 0) throw new ArgumentOutOfRangeException("offset");
                    Stream.Position = offset + namesize;
                    return offset;
                case SeekOrigin.Current:
                    Stream.Position += offset;
                    if (Stream.Position<namesize)
                    {
                        Stream.Position = namesize;
                        throw new ArgumentOutOfRangeException("offset");
                    }
                    return Stream.Position - namesize;
                case SeekOrigin.End:
                    Stream.Position = Stream.Length - offset;
                    if (Stream.Position < namesize)
                    {
                        Stream.Position = namesize;
                        throw new ArgumentOutOfRangeException("offset");
                    }
                    return Stream.Position - namesize;
                default: return 0;
            }
        }

        public override void SetLength(long value)
        {
            if (Entry.IsProtected) throw new InvalidOperationException("this entry is protected");
            if (value < 0) throw new ArgumentOutOfRangeException("value");
            Stream.SetLength(value + namesize);
            if ((Entry.System.Flag & CompactSystemFlags.ExtendedAttribute) == CompactSystemFlags.ExtendedAttribute)
            {
                Entry.ModifiedTime = DateTime.Now;
                Entry.CallChange();
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (Entry.IsProtected) throw new InvalidOperationException("this entry is protected");
            Stream.Write(buffer, offset, count);
            if ((Entry.System.Flag & CompactSystemFlags.ExtendedAttribute) == CompactSystemFlags.ExtendedAttribute)
            {
                Entry.ModifiedTime = DateTime.Now;
                Entry.CallChange();
            }
        }

        CompactBlockStream IExtendedHiddenBlockStream.GetBlockStream()
        {
            return Stream;
        }
    }

    #endregion

    #region Management

    /// <summary>
    /// Ein Eintrag in der Basisdatei. Dieser kann einen Ordner oder eine Datei repräsentieren und stellt alle nötigen 
    /// Informationen dazu bereit.
    /// </summary>
    public class CompactEntry : IExtendedEntry
    {
        /// <summary>
        /// Das System in welchem sich dieser Eintrag befindet.
        /// </summary>
        public CompactSystem System { get; private set; }
        /// <summary>
        /// Die Eigenschaften zu diesem Eintrag
        /// </summary>
        public CompactEntryFlags Flag { get; internal set; }
        /// <summary>
        /// Gibt an ob dieser Eintrag eine Datei ist
        /// </summary>
        public bool IsFile
        {
            get
            {
                return (Flag & CompactEntryFlags.DirectoryMode) != CompactEntryFlags.DirectoryMode;
            }
        }
        /// <summary>
        /// Gibt an ob dieser Eintrag ein Ordner ist
        /// </summary>
        public bool IsDirectory
        {
            get
            {
                return (Flag & CompactEntryFlags.DirectoryMode) == CompactEntryFlags.DirectoryMode;
            }
        }
        /// <summary>
        /// Gibt an ob dieser Eintrag als versteckt betrachtet werden soll. Dies muss in der Anwendung geschehen, 
        /// dem System ist es gleichwertig ob dieser Eintrag versteckt ist oder nicht.
        /// </summary>
        /// <exception cref="InvalidOperationException" />
        public bool IsHidden
        {
            get { return (Flag & CompactEntryFlags.Hidden) == CompactEntryFlags.Hidden; }
            set
            {
                if ((System.Flag & CompactSystemFlags.CompactMode) == CompactSystemFlags.CompactMode)
                    throw new InvalidOperationException("the system is readonly");

                if (value == IsHidden) return;
                Flag = (Flag & ~CompactEntryFlags.Hidden) | (value ? CompactEntryFlags.Hidden : CompactEntryFlags.None);
                CallChange();
            }
        }
        /// <summary>
        /// Gibt an ob dieser Eintrag geschütz werden soll. Wenn der Schutz aktiv ist, so können alle Offstreams, 
        /// der Inhalt und der Name nicht geändert oder gelöscht werden. Außerdem ist dieser Eintrag auch vor
        /// Löschoperationen geschützt. Ein Verschieben ist aber weiterhin möglich. 
        /// Zum Ändern des Modus einfach den Wert setzen.
        /// </summary>
        /// <exception cref="InvalidOperationException" />
        public bool IsProtected
        {
            get { return (Flag & CompactEntryFlags.Protected) == CompactEntryFlags.Protected; }
            set
            {
                if ((System.Flag & CompactSystemFlags.CompactMode) == CompactSystemFlags.CompactMode)
                    throw new InvalidOperationException("the system is readonly");

                if (value == IsProtected) return;
                Flag = (Flag & ~CompactEntryFlags.Protected) | (value ? CompactEntryFlags.Protected : CompactEntryFlags.Protected);
                CallChange();
            }
        }
        /// <summary>
        /// Gibt an ob dieser Eintrag zusätzliche Streams (Offstreams) neben den eigentlichen Inhalt enthält. 
        /// Zu beachten ist, dass auch Ordner zusätzliche Stream enthalten können.
        /// </summary>
        public bool ContainsOffstreams
        {
            get { return (Flag & CompactEntryFlags.ContainsOffstreams) == CompactEntryFlags.ContainsOffstreams; }
        }
        /// <summary>
        /// Gibt an ob der Name ausgelagert wurde, da er zu lang für das MFT war. Es ist nicht zu empfehlen all 
        /// zu lange Namen zu verwenden, da dies das System langsamer macht. Die Grenze zum Auslagern liegt ca. 
        /// bei der Hälfte von <see cref="CompactSystem.BlockSize"/> (Header vorher abgerechnet).
        /// </summary>
        public bool IsNameExported
        {
            get { return (Flag & CompactEntryFlags.ExportName) == CompactEntryFlags.ExportName; }
        }
        /// <summary>
        /// Die eindeutige Id zu diesen Eintrag. Über diese ist sie immer schnell abrufbar.
        /// </summary>
        public ulong Id { get; internal set; }

        internal List<ulong> OffstreamPointer = new List<ulong>();
        /// <summary>
        /// Die Anzahl aller zusätzlichen Streams zu diesen Eintrag.
        /// </summary>
        public int OffstreamCount
        {
            get { return OffstreamPointer.Count; }
        }
        /// <summary>
        /// Das Datum an dem dieser Eintrag erzeugt wurde. Dieser Wert wird nur gesetzt, wenn <see cref="CompactSystem.Flag"/> 
        /// den Flag <see cref="CompactSystemFlags.ExtendedAttribute"/> enthält.
        /// </summary>
        public DateTime CreatedTime { get; internal set; }
        /// <summary>
        /// Das Datum an dem dieser Eintrag zuletzt geändert wurde. Dieser Wert wird nur gesetzt, wenn 
        /// <see cref="CompactSystem.Flag"/> den Flag <see cref="CompactSystemFlags.ExtendedAttribute"/> enthält.
        /// </summary>
        public DateTime ModifiedTime { get; internal set; }
        /// <summary>
        /// Das Elternelement zu diesem Eintrag. Zum Ändern dieser Eigenschaft bitte <see cref="Move(CompactEntry)"/> verwenden.
        /// </summary>
        public CompactEntry Parent { get; internal set; }
        /// <summary>
        /// Verschiebt diesen Eintrag zu einen neuen Ort. Der neue Parent muss ein Ordner sein und der Flag 
        /// <see cref="CompactSystemFlags.SingleFileMode"/> darf nicht in <see cref="CompactSystem.Flag"/> gesetzt sein, 
        /// da sonst die Ordnerverwaltung deaktiviert wurde.
        /// </summary>
        /// <param name="newParent">Der neue Ordner in dem sich dieser Eintrag befinden soll oder null für den Root.</param>
        /// <exception cref="ArgumentException" />
        /// <exception cref="InvalidOperationException" />
        public void Move(CompactEntry newParent)
        {
            if ((System.Flag & CompactSystemFlags.CompactMode) == CompactSystemFlags.CompactMode)
                throw new InvalidOperationException("the system is readonly");
            if ((System.Flag & CompactSystemFlags.SingleFileMode) == CompactSystemFlags.SingleFileMode)
                throw new InvalidOperationException("this system doesn't support parent hierarchy");
            if (newParent == Parent) return;
            if (newParent != null && newParent.IsFile)
                throw new ArgumentException("newParent", "the target parent is a file");
            var entry = newParent != null ? newParent.GetChild(Name) : System.FileTable.GetRootEntry(Name);
            if (entry != null) throw new ArgumentException("newParent", "target parent allready contains entry with same name");

            System.FileTable.OnEntryMoved(this, Parent, newParent);
            if (newParent == null)
            {
                Parent?.Childs.Remove(this);
                Parent = null;
                ParentId = 0;
            }
            else
            {
                Parent?.Childs.Remove(this);
                Parent = newParent;
                ParentId = newParent.Id;
            }
            CallChange();
        }
        /// <summary>
        /// Erzeugt einen neuen <see cref="Stream"/> welcher einem Lese- und Schreibzugriffe auf den Inhalt gewährt. 
        /// Dies ist nur bei einer Datei möglich.
        /// </summary>
        /// <returns>Der neue Stream.</returns>
        /// <exception cref="InvalidOperationException" />
        public CompactContentStream GetContent()
        {
            if (IsDirectory) throw new InvalidOperationException("this entry is not a file");
            return new CompactContentStream(this);
        }
        /// <summary>
        /// Erzeugt einen neuen <see cref="Stream"/> wecher einem Lese- und Schreibzugriffe auf einen Offstream gewährt. 
        /// </summary>
        /// <param name="index">Der Index des Offstreams aus der Liste</param>
        /// <returns>Der neue Stream.</returns>
        /// <exception cref="ArgumentOutOfRangeException" />
        public CompactOffStream GetOffstream(int index)
        {
            if (index < 0 || index >= OffstreamPointer.Count)
                throw new ArgumentOutOfRangeException("index");
            return new CompactOffStream(this, OffstreamPointer[index]);
        }
        /// <summary>
        /// Fügt einen neuen Offstream zur Liste hinzu und gibt einen neuen <see cref="Stream"/> für den Lese- und
        /// Schreibzugriff zurück. 
        /// </summary>
        /// <param name="name">Der Name des neuen Streams (muss nicht eindeutig sein).</param>
        /// <returns>Der neue Stream.</returns>
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="InvalidOperationException " />
        /// <exception cref="OutOfMemoryException" />
        public CompactOffStream AddOffstream(string name)
        {
            if (IsProtected) throw new InvalidOperationException("this entry is protected");
            if ((System.Flag & CompactSystemFlags.CompactMode) == CompactSystemFlags.CompactMode)
                throw new InvalidOperationException("this system is readonly");
            if (name == null) throw new ArgumentNullException(name);

            var p = System.FreeSpace.GetFreePage();
            OffstreamPointer.Add(p);
            Flag |= CompactEntryFlags.ContainsOffstreams;
            CallChange();
            var s = new CompactOffStream(this, p);
            s.Name = name;
            return s;
        }
        /// <summary>
        /// Entfernt einen Offstream aus der Liste und gibt alle verwendeten Blöcke wieder frei.
        /// </summary>
        /// <param name="index">Der Index des Streams in der Streamliste.</param>
        /// <exception cref="ArgumentOutOfRangeException"/>
        /// <exception cref="InvalidOperationException" />
        public void RemoveOffstream(int index)
        {
            if (IsProtected) throw new InvalidOperationException("this entry is protected");
            if ((System.Flag & CompactSystemFlags.CompactMode) == CompactSystemFlags.CompactMode)
                throw new InvalidOperationException("this system is readonly");
            if (index < 0 || index >= OffstreamPointer.Count)
                throw new ArgumentOutOfRangeException("index");
            var p = OffstreamPointer[index];
            OffstreamPointer.RemoveAt(index);
            new CompactBlockStream(System, p).DestroyAllBlocks();
            if (OffstreamPointer.Count == 0)
                Flag = Flag & ~CompactEntryFlags.ContainsOffstreams;
            CallChange();
        }
        /// <summary>
        /// Löscht diesen Eintrag und all seine zugeordneten Streams. Wenn dieser Eintrag ein Ordner ist, so werden 
        /// auch alle seine Kinder gelöscht.
        /// </summary>
        /// <exception cref="InvalidOperationException" />
        public void Delete()
        {
            System.FileTable.DeleteEntry(Id);
        }

        internal List<CompactEntry> Childs = new List<CompactEntry>();
        /// <summary>
        /// Ruft alle Kinder dieses Eintrags ab.
        /// </summary>
        /// <returns>Eine Auflistung aller Kindseinträge</returns>
        public CompactEntry[] GetChilds()
        {
            return Childs.ToArray();
        }
        /// <summary>
        /// Sucht unter den aktuellen Kindern den Eintrag mit diesen Namen heraus.
        /// </summary>
        /// <param name="name">Der Name des Kindes</param>
        /// <returns>Der gefundene Eintrag oder null.</returns>
        public CompactEntry GetChild(string name)
        {
            return Childs.Find((c) => c.Name == name);
        }
        
        internal long TableOffset;
        internal ulong NamePointer;
        internal string BaseName;
        internal ulong ContentPointer;
        internal ulong ParentId;
        internal long EntryLength;
        /// <summary>
        /// Der Name dieses Eintrags. Wenn <see cref="CompactSystemFlags.SingleFileMode"/> in <see cref="CompactSystem.Flag"/> 
        /// gesetzt ist so muss der Name nicht eindeutig sein.
        /// </summary>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="InvalidOperationException" />
        /// <exception cref="OutOfMemoryException" />
        public string Name
        {
            get
            {
                if (BaseName != null) return BaseName;
                using (var s = new CompactBlockStream(System, NamePointer))
                using (var r = new BinaryReader(s))
                    BaseName = r.ReadString();
                return BaseName;
            }
            set
            {
                if (IsProtected) throw new InvalidOperationException("this file is protected");
                if ((System.Flag & CompactSystemFlags.CompactMode) == CompactSystemFlags.CompactMode)
                    throw new InvalidOperationException("this system is in readonly mode");
                if (value == Name) return;
                if ((System.Flag & CompactSystemFlags.SingleFileMode) != CompactSystemFlags.SingleFileMode)
                {
                    if (value == null) throw new ArgumentNullException("Name");
                    var entry = Parent != null ? Parent.GetChild(value) : System.FileTable.GetRootEntry(value);
                    if (entry != null) throw new ArgumentException("Name", "Name allready exists");
                }
                else if (value == null) value = "";

                BaseName = value;
                if ((ulong)value.Length >= System.ContentBlockSize / 2)
                {
                    if (NamePointer == 0) NamePointer = System.FreeSpace.GetFreePage();
                    using (var s = new CompactBlockStream(System, NamePointer))
                    using (var w = new BinaryWriter(s))
                        w.Write(value);
                    Flag |= CompactEntryFlags.ExportName;
                }
                else
                {
                    if (NamePointer != 0)
                    {
                        new CompactBlockStream(System, NamePointer).DestroyAllBlocks();
                        //System.FreeSpace.AddFreeBlock(NamePointer);
                        NamePointer = 0;
                    }
                    Flag &= ~CompactEntryFlags.ExportName;
                }
                ModifiedTime = DateTime.Now;
                CallChange();
            }
        }
        /// <summary>
        /// Der Pfad dieses Eintrags. Er setzt sich zusammen aus allen Namen der Elternelemente mit einem '\' 
        /// (Backslash) getrennt.
        /// </summary>
        public string Path
        {
            get
            {
                if (Parent == null) return Name;
                return Parent.Path + '\\' + Name;
            }
        }

        internal byte[] ToBytes()
        {
            using (var m = new MemoryStream())
            using (var r = new BinaryReader(m))
            using (var w = new BinaryWriter(m))
            {
                w.Write((byte)Flag);
                if (IsNameExported) WritePointer(w, NamePointer);
                else
                {
                    w.Write(Encoding.UTF8.GetBytes(BaseName));
                    w.Write((byte)0);
                }
                WritePointer(w, Id);
                if (IsFile) WritePointer(w, ContentPointer);
                if (ContainsOffstreams)
                {
                    for (int i = 0; i < OffstreamPointer.Count; ++i) WritePointer(w, OffstreamPointer[i]);
                    WritePointer(w, 0);
                }
                if ((System.Flag & CompactSystemFlags.ExtendedAttribute) == CompactSystemFlags.ExtendedAttribute)
                {
                    w.Write(CreatedTime.ToBinary());
                    w.Write(ModifiedTime.ToBinary());
                }
                WritePointer(w, ParentId);

                m.Position = 0;
                var b = r.ReadBytes((int)m.Length);
                EntryLength = b.Length;
                return b;
            }
        }

        internal void FromBytes(BinaryReader r)
        {
            var start = r.BaseStream.Position;
            Flag = (CompactEntryFlags)r.ReadByte();
            if (IsNameExported) NamePointer = ReadPointer(r);
            else
            {
                var l = new List<byte>();
                byte b;
                while ((b = r.ReadByte()) != 0) l.Add(b);
                BaseName = Encoding.UTF8.GetString(l.ToArray());
            }
            Id = ReadPointer(r);
            if (IsFile) ContentPointer = ReadPointer(r);
            if (ContainsOffstreams)
            {
                ulong p;
                while ((p = ReadPointer(r)) != 0) OffstreamPointer.Add(p);
            }
            if ((System.Flag & CompactSystemFlags.ExtendedAttribute) == CompactSystemFlags.ExtendedAttribute)
            {
                CreatedTime = DateTime.FromBinary(r.ReadInt64());
                ModifiedTime = DateTime.FromBinary(r.ReadInt64());
            }
            ParentId = ReadPointer(r);
            EntryLength = r.BaseStream.Position - start;
        }

        private ulong ReadPointer(BinaryReader r)
        {
            var pd = r.ReadBytes((int)System.PointerSize + 1);
            ulong p = 0;
            for (int i = 0; i < pd.Length; ++i)
                p = (p << 8) | pd[i];
            return p;
        }
        private void WritePointer(BinaryWriter w, ulong p)
        {
            var pd = new byte[(int)System.PointerSize + 1];
            for (int i = pd.Length - 1; i >= 0; --i)
            {
                pd[i] = (byte)(p & 0xFF);
                p = p >> 8;
            }
            w.Write(pd);
        }

        private bool constructMode = true;
        internal void CallChange()
        {
            if (!constructMode) System.FileTable.EntryChanged(this);
        }

        #region Extension

        ulong[] IExtendedEntry.GetOffstreamPointer()
        {
            return OffstreamPointer.ToArray();
        }

        long IExtendedEntry.GetMFTOffset()
        {
            return TableOffset;
        }

        long IExtendedEntry.GetMFTLength()
        {
            return EntryLength;
        }

        ulong IExtendedEntry.GetLongNamePointer()
        {
            return NamePointer;
        }

        ulong IExtendedEntry.GetContentPointer()
        {
            return ContentPointer;
        }

        #endregion

        internal CompactEntry(CompactSystem system, BinaryReader r)
        {
            this.System = system;
            TableOffset = r.BaseStream.Position;
            FromBytes(r);
            constructMode = false;
        }

        internal CompactEntry(CompactSystem system, ulong id, string name, CompactEntryFlags flags, CompactEntry parent)
        {
            this.System = system;
            this.Id = id;
            this.BaseName = name;
            this.Flag = flags;
            this.Parent = parent;
            this.ParentId = parent?.Id ?? 0;
            this.constructMode = false;
        }
    }

    /// <summary>
    /// Ein Registrar, welcher  den Überblick über freie Speicherbereiche im
    /// <see cref="CompactSystem"/> behält.
    /// </summary>
    public class CompactFreeSpaceRegistry : IExtendedFreeSpaceRegistry, IExtendedHiddenBlockStream
    {
        private List<ulong> FreeBlocks = new List<ulong>();
        private object lockList = new object();

        private CompactBlockStream Stream;
        private BinaryReader Reader;
        private BinaryWriter Writer;

        private void SaveBlockList()
        {
            Stream.Position = 0;
            for (var i = 0; i < FreeBlocks.Count; ++i)
                Writer.Write(FreeBlocks[i]);
            Stream.SetLength(Stream.Position);
        }

        internal void AddFreeBlock(ulong pointer)
        {
            lock (lockList)
            {
                for (var i = 0; i<FreeBlocks.Count; ++i)
                    if (FreeBlocks[i]>pointer)
                    {
                        if (i>0 && FreeBlocks[i-1] == pointer) return;
                        FreeBlocks.Insert(i, pointer);
                        return;
                    }
                FreeBlocks.Add(pointer);
                SaveBlockList();
            }
        }

        internal ulong GetFreeBlock()
        {
            lock (lockList)
            {
                if (FreeBlocks.Count == 0) return 0;
                var p = FreeBlocks[0];
                FreeBlocks.RemoveAt(0);
                SaveBlockList();
                return p;
            }
        }

        /// <exception cref="OutOfMemoryException"/>
        internal ulong GetFreePage()
        {
            var p = GetFreeBlock();
            if (p != 0) return p;
            System.RegisterPage(out p);
            return System.ToBlockPointer(p);
        }

        public void Optimize()
        {
            if ((System.Flag & CompactSystemFlags.CompactMode) == CompactSystemFlags.CompactMode)
                throw new InvalidOperationException("system is readonly");
            if (FreeBlocks.Count == 0) return;
            var ss = ((IExtendedSystem)System).GetBaseStreamLength();
            var mp = System.ToBlockPointer(ss - 1);
            var bs = (System.Flag & CompactSystemFlags.ReferenceByBlock) == CompactSystemFlags.ReferenceByBlock ? 1 : System.BlockSize;
            var l = new List<ulong>();
            var id = mp;
            lock (lockList)
            {
                for (var i = FreeBlocks.Count - 1; i >= 0; --i)
                    if (FreeBlocks[i] == id)
                        l.Add(id--);
                    else break;
                var ds = ss - (ulong)l.Count * System.BlockSize;
                for (var i = l.Count - 1; i >= 0; --i)
                    FreeBlocks.Remove(l[i]);
                System.SetStreamSize(ds);
            }
        }

        #region Extension

        void IExtendedFreeSpaceRegistry.AddFreeBlock(ulong pointer)
        {
            AddFreeBlock(pointer);
        }

        ulong IExtendedFreeSpaceRegistry.GetFreePage()
        {
            return GetFreePage();
        }

        CompactBlockStream IExtendedHiddenBlockStream.GetBlockStream()
        {
            return Stream;
        }

        #endregion

        /// <summary>
        /// Das <see cref="CompactSystem"/> welcher die Quelle für alle Datenströme ist.
        /// </summary>
        public CompactSystem System { get; private set; }

        internal CompactFreeSpaceRegistry(CompactSystem system)
        {
            this.System = system;
            Stream = new CompactBlockStream(system, system.FreeSpaceBlock);
            Reader = new BinaryReader(Stream);
            Writer = new BinaryWriter(Stream);
            Stream.Position = 0;
            while (Stream.Position < Stream.Length)
                FreeBlocks.Add(Reader.ReadUInt64());
        }
        /// <summary>
        /// Gibt eine Liste an Adressen zu freien Speicherblöcken in <see cref="System"/> 
        /// aus. Diese werden zuerst versucht zu füllen bevor die Basisdatei erweitert wird.
        /// </summary>
        public ulong[] FreeBlockPointers
        {
            get { return FreeBlocks.ToArray(); }
        }
        /// <summary>
        /// Die Anzahl der freien Speicherblöcke in <see cref="System"/>.
        /// </summary>
        public int Count
        {
            get { return FreeBlocks.Count; }
        }
    }

    /// <summary>
    /// Eine Verwaltung für alle Eintrag in <see cref="CompactSystem"/>.
    /// </summary>
    public class CompactFileTable : IExtendedFileTable, IExtendedHiddenBlockStream
    {
        /// <summary>
        /// Das System für den alle Einträge verwaltet werden sollen.
        /// </summary>
        public CompactSystem System { get; private set; }

        private Dictionary<ulong, CompactEntry> Entrys = new Dictionary<ulong, CompactEntry>();
        private Dictionary<string, CompactEntry> EntrysByPath = new Dictionary<string, CompactEntry>();
        private List<CompactEntry> RootEntrys = new List<CompactEntry>();

        private CompactBlockStream Stream;

        internal CompactFileTable(CompactSystem system)
        {
            this.System = system;
            Stream = new CompactBlockStream(system, system.StartMFTBlock);
            Entrys = new Dictionary<ulong, CompactEntry>();
            var r = new BinaryReader(Stream);
            while (Stream.Position<Stream.Length)
            {
                var entry = new CompactEntry(system, r);
                Entrys.Add(entry.Id, entry);
            }
            foreach (var kvp in Entrys)
                if (kvp.Value.ParentId != 0)
                {
                    var parent = Entrys[kvp.Value.ParentId];
                    parent.Childs.Add(kvp.Value);
                    kvp.Value.Parent = parent;
                }
                else RootEntrys.Add(kvp.Value);
        }
        /// <summary>
        /// Ruft einen Eintrag anhand seiner Id ab.
        /// </summary>
        /// <param name="id">Die Id des Eintrags</param>
        /// <returns>Der gefundene Eintrag</returns>
        public CompactEntry GetEntry(ulong id)
        {
            return Entrys[id];
        }
        /// <summary>
        /// Ruft einen Eintrag anhand seines Pfades ab. Es wird <see cref="GetEntry(ulong)"/> empfohlen, da diese 
        /// Variante schneller ist.
        /// </summary>
        /// <param name="path">Der Pfad des Eintrags.</param>
        /// <returns>Der gefundene Eintrag.</returns>
        /// <exception cref="KeyNotFoundException"/>
        public CompactEntry GetEntry(string path)
        {
            if (EntrysByPath.ContainsKey(path))
                return EntrysByPath[path];
            foreach (var k in Entrys)
            {
                var p = k.Value.Path;
                EntrysByPath[p] = k.Value;
                if (p == path) return k.Value;
            }
            throw new KeyNotFoundException("Path \""+path+"\" not found");
        }

        internal void EntryChanged(CompactEntry entry)
        {
            foreach (var kvp in EntrysByPath)
                if (kvp.Value == entry)
                {
                    EntrysByPath.Remove(kvp.Key);
                    break;
                }
            var length = entry.EntryLength;
            var data = entry.ToBytes();
            var dif = data.Length - length;
            Stream.Position = entry.TableOffset;
            Stream.Replace((int)length, data, 0, data.Length);
            if (dif != 0)
                foreach (var kvp in Entrys)
                    if (kvp.Value.TableOffset > entry.TableOffset)
                        kvp.Value.TableOffset += dif;
        }
        /// <summary>
        /// Löscht einen Eintrag anhand seiner Id und gibt alle von ihn verwendeten Blöcke für Inhalte und 
        /// Offstreams frei.
        /// </summary>
        /// <param name="id">die Id des Eintrags</param>
        /// <exception cref="InvalidOperationException"/>
        public void DeleteEntry(ulong id)
        {
            if ((System.Flag & CompactSystemFlags.CompactMode) == CompactSystemFlags.CompactMode)
                throw new InvalidOperationException("the system is in compact mode and therefore readonly");
            var entry = Entrys[id];
            if (entry.IsProtected) throw new InvalidOperationException("the entry is protected");
            foreach (var sub in entry.Childs.ToArray()) DeleteEntry(sub.Id);
            if (entry.Parent != null) entry.Parent.Childs.Remove(entry);
            Entrys.Remove(id);
            foreach (var kvp in EntrysByPath)
                if (kvp.Value == entry)
                {
                    EntrysByPath.Remove(kvp.Key);
                    break;
                }
            Stream.Position = entry.TableOffset;
            Stream.Replace((int)entry.EntryLength, new byte[0], 0, 0);
            foreach (var kvp in Entrys)
                if (kvp.Value.TableOffset > entry.TableOffset)
                    kvp.Value.TableOffset -= entry.EntryLength;
            while (entry.OffstreamCount > 0)
                entry.RemoveOffstream(0);
            if (entry.IsFile)
                new CompactBlockStream(System, entry.ContentPointer).DestroyAllBlocks();
        }
        /// <summary>
        /// Löscht einen Eintrag anhand seines Pfades und gibt alle von ihn verwendeten Blöcke für Inhalte und 
        /// Offstreams frei. Es wird <see cref="DeleteEntry(ulong)"/> empfohlen, da diese Variante schneller ist.
        /// </summary>
        /// <param name="path">Der Pfad des Eintrags.</param>
        /// <exception cref="InvalidOperationException" />
        public void DeleteEntry(string path)
        {
            if ((System.Flag & CompactSystemFlags.CompactMode) == CompactSystemFlags.CompactMode)
                throw new InvalidOperationException("the system is in compact mode and therefore readonly");
            DeleteEntry(GetEntry(path).Id);
        }

        ulong GetFreeId()
        {
            ulong id = 1;
            while (Entrys.ContainsKey(id)) id++;
            return id;
        }
        /// <summary>
        /// Erzeugt einen neuen Eintrag und fügt diesen der MFT hinzu.
        /// </summary>
        /// <param name="name">Der Name des Eintrags (darf nicht null sein).</param>
        /// <param name="flags">
        /// Die Flags für den Eintrag. Erlaubt sind nur <see cref="CompactEntryFlags.DirectoryMode"/>, 
        /// <see cref="CompactEntryFlags.Hidden"/>, <see cref="CompactEntryFlags.None"/> und 
        /// <see cref="CompactEntryFlags.Protected"/>.
        /// </param>
        /// <param name="parent">Der Parent von den neuen Eintrag oder null für Root.</param>
        /// <returns>Der neu erzeugte Eintrag.</returns>
        /// <exception cref="ArgumentException" />
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="InvalidOperationException" />
        /// <exception cref="OutOfMemoryException" />
        public CompactEntry CreateEntry(string name, CompactEntryFlags flags, CompactEntry parent)
        {
            if ((System.Flag & CompactSystemFlags.CompactMode) == CompactSystemFlags.CompactMode)
                throw new InvalidOperationException("the system is in compact mode and therefore readonly");
            if ((flags & CompactEntryFlags.ContainsOffstreams) == CompactEntryFlags.ContainsOffstreams)
                throw new ArgumentException("flags", "you cannot define offstreams here");
            if ((flags & CompactEntryFlags.ExportName) == CompactEntryFlags.ExportName)
                throw new ArgumentException("flags", "you cannot define to long names here");
            if ((System.Flag & CompactSystemFlags.SingleFileMode) == CompactSystemFlags.SingleFileMode)
            {
                if (parent != null) throw new ArgumentException("parent", "parent must be null");
                if ((flags & CompactEntryFlags.DirectoryMode) == CompactEntryFlags.DirectoryMode)
                    throw new ArgumentException("flags", "directory mode is not supported");
            }
            else
            {
                if (name == null) throw new ArgumentNullException("name");
            }
            if (parent != null && parent.IsFile) throw new ArgumentException("parent", "parent is a file");
            var entry = new CompactEntry(System, GetFreeId(), name, flags, parent);
            entry.CreatedTime = entry.ModifiedTime = DateTime.Now;
            entry.TableOffset = Stream.Length;
            Stream.Position = Stream.Length;
            if (entry.IsFile) entry.ContentPointer = System.FreeSpace.GetFreePage();
            var b = entry.ToBytes();
            Stream.SetLength(Stream.Length + b.Length);
            Stream.Write(b, 0, b.Length);
            parent?.Childs.Add(entry);
            Entrys.Add(entry.Id, entry);
            if (parent == null) RootEntrys.Add(entry);
            return entry;
        }

        internal void OnEntryMoved(CompactEntry entry, CompactEntry oldParent, CompactEntry newParent)
        {
            if (oldParent == newParent) return;
            if (oldParent == null) RootEntrys.Remove(entry);
            if (newParent == null) RootEntrys.Add(entry);
        }
        /// <summary>
        /// Ruft alle Einträge ab, die sich Wurzelstamm befinden.
        /// </summary>
        /// <returns>Eine Auflistung aller Einträge im Wurzelstamm</returns>
        public CompactEntry[] GetRootEntrys()
        {
            return RootEntrys.ToArray();
        }
        /// <summary>
        /// Sucht unter allen Wurzelelemente den Eintrag heraus, der diesen Namen trägt.
        /// </summary>
        /// <param name="name">Der Name des Elements</param>
        /// <returns>der gefundene Eintrag</returns>
        public CompactEntry GetRootEntry(string name)
        {
            return RootEntrys.Find((c) => c.Name == name);
        }

        public ulong TotalEntryCount
        {
            get { return (ulong)Entrys.Count; }
        }

        #region Extensions

        CompactEntry[] IExtendedFileTable.GetAllEntries()
        {
            return Entrys.ToList().ConvertAll((e) => e.Value).ToArray();
        }

        CompactBlockStream IExtendedHiddenBlockStream.GetBlockStream()
        {
            return Stream;
        }

        #endregion
    }

    #endregion

    #region Additional Namespaces

    namespace Extended
    {
        /// <summary>
        /// undocumented and unchecked hook to internal functions of 
        /// <see cref="CompactFreeSpaceRegistry"/>. Use it only if you know what you do!
        /// </summary>
        public interface IExtendedFreeSpaceRegistry
        {
            void AddFreeBlock(ulong pointer);
            ulong GetFreePage();
        }

        /// <summary>
        /// undocumented and unchecked hook to internal functions of 
        /// <see cref="CompactFileTable"/>. Use it only if you know what you do!
        /// </summary>
        public interface IExtendedFileTable
        {
            CompactEntry[] GetAllEntries();
        }

        /// <summary>
        /// undocumented and unchecked hook to internal functions of 
        /// <see cref="CompactEntry"/>. Use it only if you know what you do!
        /// </summary>
        public interface IExtendedEntry
        {
            ulong[] GetOffstreamPointer();
            long GetMFTOffset();
            long GetMFTLength();
            ulong GetLongNamePointer();
            ulong GetContentPointer();
        }
        
        /// <summary>
        /// undocumented and unchecked hook to internal functions of 
        /// <see cref="CompactBlockStream.BlockMeasures"/>. Use it only if you know what you do!
        /// </summary>
        public interface IExtendedMeasures
        {
            ulong GetPosition();
            ulong GetSize();
            ulong GetNextBlock();
        }
        
        /// <summary>
        /// undocumented and unchecked hook to internal functions of 
        /// <see cref="CompactBlockStream"/>. Use it only if you know what you do!
        /// </summary>
        public interface IExtendedBlockStream
        {
            byte[] GetReadBuffer();
            int GetReadBufferBlockIndex();
            ulong GetReadBufferStreamOffset();
            ulong GetReadBufferBlockOffset();
            IExtendedMeasures[] GetMeasures();
            void DoDestroyAllBlocks();
            void DoUpdateReadBuffer(bool force);
        }
        
        /// <summary>
        /// undocumented and unchecked hook to internal functions of classes who use
        /// <see cref="CompactBlockStream"/>. Use it only if you know what you do!
        /// </summary>
        public interface IExtendedHiddenBlockStream
        {
            CompactBlockStream GetBlockStream();
        }
        
        /// <summary>
        /// undocumented and unchecked hook to internal functions of 
        /// <see cref="CompactSystem"/>. Use it only if you know what you do!
        /// </summary>
        public interface IExtendedSystem
        {
            ulong ToSystemPointer(ulong p);
            ulong ToBlockPointer(ulong p);
            uint GetHeaderSize();
            uint GetBlockHeaderSize();
            ulong GetContentBlockSize();
            ulong GetMFTBlockPointer();
            ulong GetFreeSpaceBlockPointer();
            ulong GetBaseStreamLength();
            void ReadHeader(ulong p, out ulong size, out ulong next);
            void ReadPart(ulong p, byte[] buffer, int offset, int length);
        }
    }

    namespace Info
    {
        using SyncedCollections;
        /// <summary>
        /// Ermöglicht die Darstellungen der Partitionen und Verteilung der Daten in 
        /// <see cref="CompactSystem"/>.
        /// </summary>
        public class PartitionTable
        {
            /// <summary>
            /// Das System zu dem die Daten herausgesucht wurden bzw. werden.
            /// </summary>
            public CompactSystem System { get; private set; }

            private ulong lastGroupId = 0;
            private SyncedList<PartitionGroup> groups;
            /// <summary>
            /// Die Eintragsgruppen innerhalb von <see cref="System"/>. Sie kann mehrere Partitionen 
            /// enthalten und muss nicht einem <see cref="CompactEntry"/> entsprechen.
            /// </summary>
            public SyncedList<PartitionGroup> Groups { get; private set; }

            private SyncedList<PartitionEntry> entrys;
            /// <summary>
            /// Alle Einträge und Partitionen in <see cref="System"/>. Jeder Eintrag stellt genau 
            /// einen Speicherbereich dar. Mehrere Einträge können zu einer Gruppe zusammengeschlossen 
            /// sein um ein Stream (z.B. für <see cref="CompactEntry"/>) zu repräsentieren.
            /// </summary>
            public SyncedList<PartitionEntry> Entrys { get; private set; }
            /// <summary>
            /// Die maximale Anzahl der Einträge die herausgesucht werden soll. Dies soll zu lange 
            /// Suchen verhindern.
            /// </summary>
            public int MaxEntryCount { get; private set; }
            /// <summary>
            /// Ermöglicht die Darstellung der Partionen und Verteilung dieser in einem 
            /// <see cref="CompactSystem"/>. Die Einträge werden noch nicht sofort abgerufen 
            /// sondern müssen mittels <see cref="StartSearch()"/> gestartet werden. Dies ermöglicht 
            /// die Abfrage über seperate Threads.
            /// </summary>
            /// <param name="system">Das System zu dem Daten abgerufen werden sollen.</param>
            /// <param name="maxEntryCount">
            /// Die maximale Anzahl an Einträgen die abgerufen werden sollen. Dies verhindert all zu 
            /// lange Suchen.
            /// </param>
            /// <exception cref="ArgumentNullException" />
            /// <exception cref="ArgumentOutOfRangeException" />
            public PartitionTable(CompactSystem system, int maxEntryCount)
            {
                if (system == null) throw new ArgumentNullException("system");
                if (maxEntryCount < 1) throw new ArgumentOutOfRangeException("maxEntryCount");
                this.System = system;
                this.MaxEntryCount = maxEntryCount;
                groups = new SyncedList<PartitionGroup>();
                Groups = groups.ToSyncedList(true);
                entrys = new SyncedList<PartitionEntry>();
                Entrys = entrys.ToSyncedList(true);
            }

            private bool CreateEntry(PartitionGroup group, ulong blockPointer, ulong blockIndex, 
                ulong filledSize, ulong blockSize, out PartitionEntry newEntry)
            {
                newEntry = null;
                if (entrys.Count >= MaxEntryCount) return false;
                var entry = newEntry = new PartitionEntry();
                entry.BlockIndex = blockIndex;
                entry.BlockPointer = blockPointer;
                entry.BlockSize = blockSize;
                entry.FilledSize = filledSize;
                entry.Group = group;
                if (group != null) group.BlockCount++;
                entrys.Execute(() =>
                {
                    for (var i = 0; i<entrys.Count; ++i)
                        if (entrys[i].BlockPointer>entry.BlockPointer)
                        {
                            entrys.Insert(i, entry);
                            return;
                        }
                    entrys.Add(entry);
                });
                return true;
            }

            private bool CreateEntry(PartitionGroup group, ulong blockPointer, ulong blockIndex,
                ulong filledSize, ulong blockSize)
            {
                PartitionEntry entry;
                return CreateEntry(group, blockPointer, blockIndex, filledSize, blockSize, out entry);
            }

            private PartitionGroup CreateGroup(PartitionType type, CompactEntry entry = null, 
                uint offstreamIndex = 0)
            {
                var group = new PartitionGroup();
                group.Type = type;
                group.BlockCount = 0;
                group.Id = lastGroupId++;
                group.AssignedEntry = entry;
                group.OffstreamIndex = offstreamIndex;
                groups.Add(group);
                return group;
            }

            private bool FetchEntrysFromStream(PartitionGroup group, CompactBlockStream stream)
            {
                IExtendedBlockStream s = stream;
                var m = s.GetMeasures();
                var bs = ((IExtendedSystem)System).GetContentBlockSize();
                var comp = (System.Flag & CompactSystemFlags.CompactMode) == CompactSystemFlags.CompactMode;
                for (var i = 0; i < m.Length; ++i)
                    if (!CreateEntry(group, m[i].GetPosition(), (ulong)i, m[i].GetSize(), comp ? m[i].GetSize() : bs))
                        return false;
                return true;
            }

            private bool SetManifest()
            {
                var group = CreateGroup(PartitionType.Manifest);
                var size = ((IExtendedSystem)System).GetHeaderSize();
                return CreateEntry(group, 0, 0, size, size);
            }

            private bool SetMft()
            {
                var group = CreateGroup(PartitionType.MFT);
                var stream = ((IExtendedHiddenBlockStream)System.FileTable).GetBlockStream();
                return FetchEntrysFromStream(group, stream);
            }

            private bool SetSpace()
            {
                var group = CreateGroup(PartitionType.SpaceTable);
                var stream = ((IExtendedHiddenBlockStream)System.FreeSpace).GetBlockStream();
                if (!FetchEntrysFromStream(group, stream)) return false;
                var free = System.FreeSpace.FreeBlockPointers;
                var size = ((IExtendedSystem)System).GetContentBlockSize();
                for (var i = 0; i<free.Length; ++i)
                {
                    group = CreateGroup(PartitionType.Free);
                    if (!CreateEntry(group, free[i], 0, 0, size)) return false;
                }
                return true;
            }

            private bool SetEntry(CompactEntry entry)
            {
                IExtendedEntry e = entry;
                var p = e.GetLongNamePointer();
                if (p != 0)
                    using (var stream = new CompactBlockStream(System, p))
                        if (!FetchEntrysFromStream(CreateGroup(PartitionType.ExportedName, entry), stream))
                            return false;
                var off = e.GetOffstreamPointer();
                for (var i = 0; i < off.Length; ++i)
                    using (var stream = new CompactBlockStream(System, off[i]))
                        if (!FetchEntrysFromStream(CreateGroup(PartitionType.Offstream, entry, (uint)i), stream))
                            return false;
                if (entry.IsDirectory) return true;
                p = e.GetContentPointer();
                using (var stream = new CompactBlockStream(System, p))
                    return FetchEntrysFromStream(CreateGroup(PartitionType.Content, entry), stream);
            }

            private bool SetEntrys()
            {
                foreach (var e in ((IExtendedFileTable)System.FileTable).GetAllEntries())
                    if (!SetEntry(e)) return false;
                return true;
            }

            private bool AllocUnknownBlocks()
            {
                if ((System.Flag & CompactSystemFlags.CompactMode) == CompactSystemFlags.CompactMode)
                    return true;
                IExtendedSystem s = System;
                ulong p = 0;
                var d = new Dictionary<ulong, PartitionEntry>();
                var l = new Dictionary<PartitionGroup, List<PartitionEntry>>();
                int index = 0;
                ulong maxlength = s.GetBaseStreamLength();
                uint hs = s.GetBlockHeaderSize();
                ulong cs = s.GetContentBlockSize();
                while (p < maxlength)
                {
                    var pointer = s.ToBlockPointer(p);
                    if (index < entrys.Count)
                    {
                        if (entrys[index].BlockPointer == pointer)
                        {
                            if (d.ContainsKey(pointer))
                            {
                                var oe = d[pointer];
                                if (oe == entrys[index]) goto unkown_stuff; //ja, Goto's sind unschön, aber hier wäre die andere Methode aufwändiger
                                oe.Group.BlockCount--;
                                l[oe.Group].Remove(oe);
                                if (l[oe.Group].Count == 0)
                                {
                                    l.Remove(oe.Group);
                                    groups.Remove(oe.Group);
                                }
                                entrys.Remove(oe);
                                d.Remove(pointer);
                            }
                            p = s.ToSystemPointer(s.ToBlockPointer(p + hs + entrys[index].BlockSize + 1));
                            index++;
                            continue;
                        }
                        if (entrys[index].BlockPointer < pointer)
                        {
                            index++;
                            continue;
                        }
                    }
                    unkown_stuff: //Diese Stelle wird nur extra angesprungen, wenn in der Erkennung oben ein Eintrag schon als Unknown erkannt wurde.
                    PartitionEntry current;
                    if (d.ContainsKey(pointer))
                        current = d[pointer];
                    else
                    {
                        if (!CreateEntry(null, pointer, 0, 0, cs, out current)) return false;
                        d.Add(pointer, current);
                    }
                    ulong size, next;
                    s.ReadHeader(p, out size, out next);
                    current.FilledSize = size;
                    if (next != 0) {
                        PartitionEntry nextp;
                        if (d.ContainsKey(next)) {
                            nextp = d[next];
                            if (current.Group == null)
                            {
                                current.Group = nextp.Group;
                                l[current.Group].Add(current);
                                current.BlockIndex = current.Group.BlockCount;
                                current.Group.BlockCount++;
                            }
                            //else --> zwei Gruppen die zusammengehören
                            else
                            {
                                var group1 = current.Group;
                                var group2 = nextp.Group;
                                if (group2.BlockCount < group2.BlockCount)
                                {
                                    var temp = group2; group2 = group1; group1 = temp;
                                }
                                l[group2].ForEach((e) => e.Group = group1);
                                group1.BlockCount += group2.BlockCount;
                                l.Remove(group2);
                                groups.Remove(group2);
                            }
                        }
                        else
                        {
                            if (current.Group == null)
                            {
                                current.Group = CreateGroup(PartitionType.Unknown);
                                l.Add(current.Group, new List<PartitionEntry>());
                                l[current.Group].Add(current);
                                current.Group.BlockCount++;
                            }
                            if (!CreateEntry(current.Group, next, current.Group.BlockCount, 0, cs, out nextp))
                                return false;
                            d.Add(next, nextp);
                            l[current.Group].Add(nextp);
                        }
                    }
                    else
                    {
                        if (current.Group == null)
                        {
                            current.Group = CreateGroup(PartitionType.Unknown);
                            l.Add(current.Group, new List<PartitionEntry>());
                            l[current.Group].Add(current);
                            current.Group.BlockCount++;
                        }
                    }
                    p = s.ToSystemPointer(s.ToBlockPointer(p + hs + entrys[index].BlockSize + 1));
                }
                return true;
            }

            /// <summary>
            /// Durchsucht das <see cref="System"/> nach den Partitionen und legt diese strukturiert in 
            /// <see cref="Groups"/> und <see cref="Entrys"/> ab.
            /// </summary>
            public void StartSearch()
            {
                if (!SetManifest()) return;
                if (!SetMft()) return;
                if (!SetSpace()) return;
                if (!SetEntrys()) return;
                AllocUnknownBlocks();
            }
        }

        public class PartitionGroup
        {
            public PartitionType Type { get; internal set; }

            public ulong BlockCount { get; internal set; }

            public ulong Id { get; internal set; }

            public CompactEntry AssignedEntry { get; internal set; }

            public uint OffstreamIndex { get; internal set; }

            internal PartitionGroup() { }

            public PartitionGroup(PartitionType type, ulong id, ulong blockCount, CompactEntry assignedEntry = null, uint offstreamIndex = 0)
            {
                Type = type;
                Id = id;
                BlockCount = blockCount;
                AssignedEntry = assignedEntry;
                OffstreamIndex = offstreamIndex;
            }
        }

        public class PartitionEntry
        {
            public ulong BlockPointer { get; internal set; }

            public PartitionGroup Group { get; internal set; }

            public ulong BlockIndex { get; internal set; }

            public ulong FilledSize { get; internal set; }

            public ulong BlockSize { get; internal set; }

            internal PartitionEntry() { }

            public PartitionEntry(PartitionGroup group, ulong blockPointer, ulong blockIndex, ulong filledSize, ulong blockSize)
            {
                Group = group;
                BlockPointer = blockPointer;
                BlockIndex = blockIndex;
                FilledSize = filledSize;
                BlockSize = blockSize;
            }
        }

        public enum PartitionType
        {
            Unknown,
            Manifest,
            MFT,
            SpaceTable,
            Content,
            Offstream,
            ExportedName,
            Free
        }
    }

    #endregion
}
