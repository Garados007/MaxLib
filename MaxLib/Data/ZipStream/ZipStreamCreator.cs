using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace MaxLib.Data.ZipStream
{
    //source: https://pkware.cachefly.net/webdocs/casestudies/APPNOTE.TXT
    public class ZipStreamCreator
    {
        public ZipStreamCreator(Stream output)
        {
            if (output == null) throw new ArgumentNullException("output");
            if (!output.CanWrite) throw new ArgumentException("cannot write to output", "output");
            this.output = output;
            offset = 0;
        }

        internal ZipStreamCreator()
        {
            output = new MemoryStream();
            offset = 0;
        }

        internal Action<Stream> newStream;
        readonly Stream output;
        ulong offset;
        private readonly object lockAllHeader = new object();
        private readonly object lockWrite = new object();
        readonly List<FileHeader> allHeader = new List<FileHeader>();

        ushort MadeBy()
        {
            return (0 << 8) | (4 * 10 + 5);
        }

        public void AddFile(string name, Stream data, DateTime modified, string comment = "")
        {
            if (data == null) throw new ArgumentNullException("data");
            if (!data.CanSeek) throw new ArgumentException("stream is not seekable", "data");
            if (!data.CanRead) throw new ArgumentException("stream is not readable", "data");
            AddFile(name, data, modified, comment, new SpecialPurpose[0]);
        }

        private void AddFile(string name, Stream data, DateTime modified, string comment,
            SpecialPurpose[] meta)
        {
            var header = CreateHeader(modified, data, name, comment, offset, meta);
            data.Position = 0;
            AttachLocal(header, data);
            lock (lockAllHeader)
                allHeader.Add(header);
        }

        public void AddFile(string zipName, string localName, string comment = "")
        {
            if (localName == null) throw new ArgumentNullException("localName");
            var fi = new FileInfo(localName);
            if (!fi.Exists) throw new FileNotFoundException("file not found", localName);
            var fs = new FileStream(localName, FileMode.Open, FileAccess.Read, FileShare.Read);
            AddFile(zipName, fs, fi.LastWriteTime, comment, new[]
            {
                new SpecialPurpose
                {
                    Id = SpecialPurposeId.ExtendedTimestamp,
                    Data = new ExtendedTimestamp
                    {
                        Access = fi.LastAccessTime,
                        Creation = fi.CreationTime,
                        Modified = fi.LastWriteTime
                    }
                }
            });
            if (newStream == null) fs.Dispose();
        }

        public int Remove(string name)
        {
            lock (lockAllHeader)
                return allHeader.RemoveAll((h) => h._RawName == name);
        }

        public void Finish(string comment = "")
        {
            var fh = offset;
            int ahc;
            lock (lockAllHeader)
            {
                ahc = allHeader.Count;
                foreach (var h in allHeader)
                    if (newStream == null)
                        offset += h.Write(output);
                    else lock (lockWrite)
                        {
                            var output = new MemoryStream();
                            offset += h.Write(output);
                            newStream(output);
                        }
            }
            var zip64eod = new Zip64EndOfCentralDirectory
            {
                VersionMadeBy = MadeBy(),
                VersionNeetToExtract = MadeBy(),
                NumberOfThisDisk = 0,
                DiskCentralDirStarts = 0,
                NumberOfCentralDirRecOnThisDisk = (ulong)ahc,
                TotalNumberOfCentralDirRec = (ulong)ahc,
                SizeOfCentralDir = offset - fh,
                OffsetStartCentralDirToStartDisk = fh,
                Zip64ExtensibleData = new byte[0],
            };
            var enc = new UTF8Encoding(false);
            var commentb = enc.GetBytes(comment ?? "");
            EndOfCentralDirectory eod = new EndOfCentralDirectory
            {
                DiskCentralDirStarts = 0xFFFF,
                NumberOfCentralDirRecOnThisDisk = 0xFFFF,
                NumberOfThisDisk = 0xFFFF,
                OffsetStartCentralDirToStartDisk = 0xFFFFFFFF,
                SizeOfCentralDir = 0xFFFFFFFF,
                TotalNumberOfCentralDirRec = 0xFFFF,
                ZipFileCommentLength = (ushort)commentb.Length,
                ZipFileComment = commentb
            };
            var zip64eodl = new Zip64EndOfCentralDirectoryLocator
            {
                NumberDiskWithZip64EOCD = 0,
                RelativeOffsetOfZip64EOCDRecord = offset,
                TotalNumberOfDisks = 1
            };

            if (newStream == null)
            {
                offset += zip64eod.Write(output);
                offset += zip64eodl.Write(output);
                offset += eod.Write(output);
            }
            else lock (lockWrite)
                {
                    var output = new MemoryStream();
                    offset += zip64eod.Write(output);
                    offset += zip64eodl.Write(output);
                    offset += eod.Write(output);
                    newStream(output);
                }
        }

        void AttachLocal(FileHeader header, Stream data)
        {
            var local = header.LocalFileHeader;
            if (newStream == null)
            {
                offset += local.Write(output);
                int readed;
                var b = new byte[0x1000];
                var crcd = new CrcStream(data);
                while ((readed = crcd.Read(b, 0, b.Length)) > 0)
                {
                    output.Write(b, 0, readed);
                    offset += (ulong)readed;
                }
                var crc32b = crcd.Crc;
                header.Crc32 = ((uint)crc32b[0] << 24) | ((uint)crc32b[1] << 16) | ((uint)crc32b[2] << 8) | crc32b[3];
                //header.GeneralPurposeFlag = (ushort)(GeneralPurpose.EncodingIsUtf8);
                

                var descr = new DataDescriptor
                {
                    Crc = header.Crc32,
                    CompressedSize = crcd.CompressedSize,
                    UncompressedSize = (ulong)data.Length,
                };
                descr.Write(output);
                output.Flush();

                FixHeader(header, crcd.CompressedSize, (ulong)data.Length);
            }
            else lock (lockWrite)
                {
                    var output = new MemoryStream();
                    offset += local.Write(output);
                    newStream(output);

                    var descr = new DataDescriptor();
                    var crcd = new CrcStream(data);
                    crcd.Finished += (s, e) =>
                    {
                        if (descr == null) return;
                        var crc32b = crcd.Crc;
                        header.Crc32 = ((uint)crc32b[0] << 24) | ((uint)crc32b[1] << 16) | ((uint)crc32b[2] << 8) | crc32b[3];
                        descr = new DataDescriptor
                        {
                            Crc = header.Crc32,
                            CompressedSize = crcd.CompressedSize,
                            UncompressedSize = (ulong)data.Length,
                        };
                        FixHeader(header, crcd.CompressedSize, (ulong)data.Length);
                        var m = new MemoryStream();
                        descr.Write(m);
                        crcd.BaseStream?.Dispose();
                        crcd.BaseStream = m;
                        descr = null;
                    };


                    offset += (ulong)data.Length;
                    newStream(crcd);
                }
        }

        void FixHeader(FileHeader header, ulong compressed, ulong uncompressed)
        {
            header.CompressedSize = 0xffffffff;
                header.UncompressedSize = 0xffffffff;
            var z64flag = new Zip64ExtendedInformation
            {
                CompressedSize = compressed,
                UncompressedSize = uncompressed,
                NumberOfDiskWhichThisFileStarts = 1,
                OffsetLocalHeaderRecord = header._LocalFileHeaderStart,
            };
            using (var m = new MemoryStream())
            {
                new SpecialPurpose
                {
                    Data = z64flag,
                    Id = SpecialPurposeId.Zip64ExtendedInformation
                }.Write(m);
                foreach (var f in header._Meta)
                    f.Write(m);
                header.ExtraField = m.ToArray();
                header.ExtraFieldLength = (ushort)m.Length;
            }
        }

        FileHeader CreateHeader(DateTime mod, Stream data, string name, string comment,
            ulong offset, SpecialPurpose[] meta)
        {
            data.Position = 0;
            //byte[] crc32b = CRC.CRC32(data);
            var enc = new UTF8Encoding(false);
            var nameb = enc.GetBytes(name ?? "");
            var commentb = enc.GetBytes(comment ?? "");
            GetDosDateTime(mod, out ushort date, out ushort time);
            byte[] field;
            using (var m = new MemoryStream())
            {
                new SpecialPurpose
                {
                    Data = new NoPurpose(),
                    Id = SpecialPurposeId.Zip64ExtendedInformation
                }.Write(m);

                foreach (var f in meta)
                    f.Write(m);

                field = m.ToArray();
            }

            var header = new FileHeader
            {
                _RawName = name,
                _Meta = meta,
                _LocalFileHeaderStart = offset,
                VersionMadeBy = MadeBy(),
                VersionToExtract = MadeBy(),
                GeneralPurposeFlag = (ushort)(GeneralPurpose.EncodingIsUtf8 | GeneralPurpose.ExposeLocalFileHeaderInfo),
                CompressionMethod = (ushort)(CompressionMethod.Stored),
                LastModFileTime = time,
                LastModFileDate = date,
                Crc32 = 0,
                //CompressedSize = (uint)data.Length,
                CompressedSize = 0,
                //UncompressedSize = (uint)data.Length,
                UncompressedSize = 0,
                FileNameLength = (ushort)nameb.Length,
                ExtraFieldLength = (ushort)field.Length,
                FileCommentLength = (ushort)comment.Length,
                DiskNumberStart = 0,
                InternalFileAttributes = (ushort)(InternalAttributes.BinaryFile),
                ExternalFileAttributes = 0,
                //RelativeOffsetOfLocalHeader = (uint)offset,
                RelativeOffsetOfLocalHeader = 0xFFFFFFFF,
                FileName = nameb,
                ExtraField = field,
                FileComment = commentb
            };

            return header;
        }

        void GetDosDateTime(DateTime dateTime, out ushort date, out ushort time)
        {
            date = (ushort)(
                (dateTime.Day & 0x1F) |
                ((dateTime.Month & 0x0F) << 5) |
                (((dateTime.Year - 1980) & 0x7F) << 9)
                );
            time = (ushort)(
                ((dateTime.Second >> 1) & 0x1F) |
                ((dateTime.Minute & 0x3F) << 5) |
                ((dateTime.Hour & 0x1F) << 11)
                );
        }
    }

    #region ZIP Helper

    abstract class ZipBlock
    {
        public abstract uint Signature { get; }

        public abstract uint Write(Stream output);
    }

    class LocalFileHeader : ZipBlock
    {
        public override uint Signature => 0x04034b50U;

        public ushort VersionToExtract, GeneralPurposeFlag,
            CompressionMethod, LastModTime, LastModDate,
            FileNameLength, ExtraFieldLength;

        public uint Crc32, CompressedSize, UncompressedSize;

        public byte[] FileName, ExtraField;

        public override uint Write(Stream output)
        {
            using (var w = new BinaryWriter(output, Encoding.Default, true))
            {
                w.Write(Signature);
                w.Write(VersionToExtract);
                w.Write(GeneralPurposeFlag);
                w.Write(CompressionMethod);
                w.Write(LastModTime);
                w.Write(LastModDate);
                w.Write(Crc32);
                w.Write(CompressedSize);
                w.Write(UncompressedSize);
                w.Write(FileNameLength);
                w.Write(ExtraFieldLength);
                w.Write(FileName);
                w.Write(ExtraField);
                w.Flush();
                return (uint)(30 + FileName.Length + ExtraField.Length);
            }
        }
    }

    class DataDescriptor : ZipBlock
    {
        public override uint Signature => 0x08074b50;

        public uint Crc;

        public ulong CompressedSize, UncompressedSize;

        public override uint Write(Stream output)
        {
            using (var w = new BinaryWriter(output, Encoding.Default, true))
            {
                return 0;
                //w.Write(Signature);
                //w.Write(Crc);
                //w.Write(CompressedSize);
                //w.Write(UncompressedSize);
                //w.Flush();
                //return 20;
            }
        }
    }

    class FileHeader : ZipBlock
    {
        public string _RawName;
        public SpecialPurpose[] _Meta;
        public ulong _LocalFileHeaderStart;

        public override uint Signature => 0x02014b50U;

        public ushort VersionMadeBy, VersionToExtract,
            GeneralPurposeFlag, CompressionMethod,
            LastModFileTime, LastModFileDate,
            FileNameLength, ExtraFieldLength, FileCommentLength,
            DiskNumberStart, InternalFileAttributes;

        public uint Crc32, CompressedSize, UncompressedSize,
            ExternalFileAttributes, RelativeOffsetOfLocalHeader;

        public byte[] FileName, ExtraField, FileComment;

        public LocalFileHeader LocalFileHeader
        {
            get
            {
                return new LocalFileHeader
                {
                    VersionToExtract = VersionToExtract,
                    GeneralPurposeFlag = GeneralPurposeFlag,
                    CompressionMethod = CompressionMethod,
                    LastModTime = LastModFileTime,
                    LastModDate = LastModFileDate,
                    Crc32 = Crc32,
                    CompressedSize = CompressedSize,
                    UncompressedSize = UncompressedSize,
                    FileNameLength = FileNameLength,
                    ExtraFieldLength = ExtraFieldLength,
                    FileName = FileName,
                    ExtraField = ExtraField
                };
            }
        }

        public override uint Write(Stream output)
        {
            using (var w = new BinaryWriter(output, Encoding.Default, true))
            {
                w.Write(Signature);
                w.Write(VersionMadeBy);
                w.Write(VersionToExtract);
                w.Write(GeneralPurposeFlag);
                w.Write(CompressionMethod);
                w.Write(LastModFileTime);
                w.Write(LastModFileDate);
                w.Write(Crc32);
                w.Write(CompressedSize);
                w.Write(UncompressedSize);
                w.Write(FileNameLength);
                w.Write(ExtraFieldLength);
                w.Write(FileCommentLength);
                w.Write(DiskNumberStart);
                w.Write(InternalFileAttributes);
                w.Write(ExternalFileAttributes);
                w.Write(RelativeOffsetOfLocalHeader);
                w.Write(FileName);
                w.Write(ExtraField);
                w.Write(FileComment);
                w.Flush();
                return (uint)(46 + FileName.Length + ExtraField.Length + FileComment.Length);
            }
        }
    }

    class EndOfCentralDirectory : ZipBlock
    {
        public override uint Signature => 0x06054b50U;

        public ushort NumberOfThisDisk, DiskCentralDirStarts,
            NumberOfCentralDirRecOnThisDisk, TotalNumberOfCentralDirRec,
            ZipFileCommentLength;

        public uint SizeOfCentralDir, OffsetStartCentralDirToStartDisk;

        public byte[] ZipFileComment;

        public override uint Write(Stream output)
        {
            using (var w = new BinaryWriter(output, Encoding.Default, true))
            {
                w.Write(Signature);
                w.Write(NumberOfThisDisk);
                w.Write(DiskCentralDirStarts);
                w.Write(NumberOfCentralDirRecOnThisDisk);
                w.Write(TotalNumberOfCentralDirRec);
                w.Write(SizeOfCentralDir);
                w.Write(OffsetStartCentralDirToStartDisk);
                w.Write(ZipFileCommentLength);
                w.Write(ZipFileComment);
                w.Flush();
                return (uint)(22 + ZipFileComment.Length);
            }
        }
    }

    class Zip64EndOfCentralDirectory : ZipBlock
    {
        public override uint Signature => 0x06064b50U;

        public ushort VersionMadeBy, VersionNeetToExtract;

        public uint NumberOfThisDisk, DiskCentralDirStarts;

        public ulong NumberOfCentralDirRecOnThisDisk, TotalNumberOfCentralDirRec,
            SizeOfCentralDir, OffsetStartCentralDirToStartDisk;

        public byte[] Zip64ExtensibleData;

        public EndOfCentralDirectory EndOfCentralDirectory
        {
            get
            {
                return new EndOfCentralDirectory
                {
                    DiskCentralDirStarts = 0xFFFF,
                    NumberOfCentralDirRecOnThisDisk = 0xFFFF,
                    NumberOfThisDisk = 0xFFFF,
                    OffsetStartCentralDirToStartDisk = 0xFFFFFFFF,
                    SizeOfCentralDir = 0xFFFFFFFF,
                    TotalNumberOfCentralDirRec = 0xFFFF
                };
            }
        }

        public override uint Write(Stream output)
        {
            using (var w = new BinaryWriter(output, Encoding.Default, true))
            {
                w.Write(Signature);
                w.Write((ulong)(44 + Zip64ExtensibleData.Length));
                w.Write(VersionMadeBy);
                w.Write(VersionNeetToExtract);
                w.Write(NumberOfThisDisk);
                w.Write(DiskCentralDirStarts);
                w.Write(NumberOfCentralDirRecOnThisDisk);
                w.Write(TotalNumberOfCentralDirRec);
                w.Write(SizeOfCentralDir);
                w.Write(OffsetStartCentralDirToStartDisk);
                w.Write(Zip64ExtensibleData);
                w.Flush();
                return (uint)(56 + Zip64ExtensibleData.Length);
            }
        }
    }

    class Zip64EndOfCentralDirectoryLocator : ZipBlock
    {
        public override uint Signature => 0x07064b50U;

        public uint NumberDiskWithZip64EOCD, TotalNumberOfDisks;

        public ulong RelativeOffsetOfZip64EOCDRecord;

        public override uint Write(Stream output)
        {
            using (var w = new BinaryWriter(output, Encoding.Default, true))
            {
                w.Write(Signature);
                w.Write(NumberDiskWithZip64EOCD);
                w.Write(RelativeOffsetOfZip64EOCDRecord);
                w.Write(TotalNumberOfDisks);
                w.Flush();
                return 20;
            }
        }
    }

    [Flags]
    enum GeneralPurpose : ushort
    {
        None = 0x0,
        Encypted = 0x1,

        Imploding_8K_Slider = 0x2,
        Imploding_3ShannonFanoTrees = 0x4,

        Deflating_NormalCompression = 0x0,
        Deflating_MaximumCompression = 0x2,
        Deflating_FastCompression = 0x4,
        Deflating_SuperFastCompression = 0x6,

        LZMA_EndOfStreamMarkerUsed = 0x2,

        ExposeLocalFileHeaderInfo = 0x8,
        CompressedPathData = 0x10,
        StrongEncryption = 0x20,
        EncodingIsUtf8 = 0x800,
        CentryDirEncrypted = 0x2000,
    }

    enum CompressionMethod : ushort
    {
        Stored = 0,
        Shrunk = 1,
        ReducedFactor1 = 2,
        ReducedFactor2 = 3,
        ReducedFactor3 = 4,
        ReducedFactor4 = 5,
        Imploded = 6,
        Tokenizing = 7,
        Deflate = 8,
        Deflate64 = 9,
        PKWARE_Imploded = 10,
        BZIP2 = 12,
        LZMA = 14,
        IMB_TERSE = 18,
        IMB_LZ77 = 19,
        WavPack = 97,
        PPMd_V1_1 = 98
    }

    [Flags]
    enum InternalAttributes : ushort
    {
        BinaryFile = 0,
        AsciiFile = 1,
        ExtraVariableRecord = 2,
    }

    class SpecialPurpose
    {
        public SpecialPurposeId Id;

        public SpecialPurposeContent Data;

        public uint Write(Stream output)
        {
            using (var m = new MemoryStream())
            using (var w = new BinaryWriter(output, Encoding.Default, true))
            {
                Data.Write(m);
                var data = m.ToArray();

                w.Write((ushort)Id);
                w.Write((ushort)data.Length);
                w.Write(data);
                w.Flush();
                return (uint)(4 + data.Length);
            }
        }
    }

    enum SpecialPurposeId : ushort
    {
        Zip64ExtendedInformation = 0x0001,
        ExtendedTimestamp = 0x5455,
    }

    abstract class SpecialPurposeContent
    {
        public abstract uint Write(Stream output);
    }

    class NoPurpose : SpecialPurposeContent
    {
        public override uint Write(Stream output)
        {
            return 0;
        }
    }

    class Zip64ExtendedInformation : SpecialPurposeContent
    {
        public uint NumberOfDiskWhichThisFileStarts;

        public ulong UncompressedSize, CompressedSize,
            OffsetLocalHeaderRecord;

        public override uint Write(Stream output)
        {
            using (var w = new BinaryWriter(output, Encoding.Default, true))
            {
                //w.Write((ushort)0x0001);
                //w.Write((ushort)32);
                w.Write(UncompressedSize);
                w.Write(CompressedSize);
                w.Write(OffsetLocalHeaderRecord);
                w.Write(NumberOfDiskWhichThisFileStarts);
                w.Flush();
                return 28;
            }
        }
    }

    class ExtendedTimestamp : SpecialPurposeContent
    {
        public DateTime Modified, Access, Creation;

        public override uint Write(Stream output)
        {
            using (var w = new BinaryWriter(output))
            {
                w.Write((byte)0x7);
                w.Write(Date(Modified));
                w.Write(Date(Access));
                w.Write(Date(Creation));
                w.Flush();
                return 13;
            }
        }

        uint Date(DateTime time)
        {
            var span = time.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0);
            return (uint)span.TotalSeconds;
        }
    }

    /// <summary>
    /// CRC Checksummen-Berechnung
    /// </summary>
    static class CRC
    {
        #region Modbus Funktionen
        /// <summary>
        /// CRC16S Checksummen-Berechnung
        /// wird häufig gebraucht bei Modbusanwendungen
        /// Das S bedeutet "einfach", weil keine Lookup-Tabelle verwendet wird.
        /// Die CRC16 Checksummme wird berechnet und gibt ein ByteArray zurück.
        /// Benutzt wird die Funktion x^16+x^15+x^2+1 und das Generator Polynom (0xA001)
        /// </summary>
        /// <param name="Data">ByteArray des Befehls</param>
        /// <returns>2 byte CRC Checksumme</returns>
        public static byte[] CRC16S(byte[] Data)
        {
            ushort Polynom = 0xA001;
            ushort Register = 0xFFFF;

            // loop through the entire array of bytes
            for (int i = 0; i < Data.Length; i++)
            {
                ushort temp = Data[i];

                // shift all 8 data bits once
                for (int y = 0; y < 8; y++)
                {
                    if (((Register ^ temp) & 0x01) == 0x01)
                    {
                        Register >>= 1;
                        Register ^= Polynom;
                    }
                    else
                    {
                        Register >>= 1;
                    }
                    temp >>= 1; // shift data 1 bit right, dividing it by 2

                } // end of inner for loop (bit shift loop)
            } // end of outer for loop (data loop)

            // now we have got our overall 2-byte CRC "Checksum" number
            return new byte[2] { (byte)(Register % 256), (byte)(Register / 256) };
        } // end of CRC16S Method

        #region crc32 Tabelle
        /// <summary>
        /// CRC32 Tabelle (Lookup-Tabelle)
        /// wird für die Berechnung der CRC32 Checksumme benötigt
        /// </summary>
        internal static readonly UInt32[] crctab =
        {
            0x00000000, 0x77073096, 0xee0e612c, 0x990951ba, 0x076dc419,
            0x706af48f, 0xe963a535, 0x9e6495a3, 0x0edb8832, 0x79dcb8a4,
            0xe0d5e91e, 0x97d2d988, 0x09b64c2b, 0x7eb17cbd, 0xe7b82d07,
            0x90bf1d91, 0x1db71064, 0x6ab020f2, 0xf3b97148, 0x84be41de,
            0x1adad47d, 0x6ddde4eb, 0xf4d4b551, 0x83d385c7, 0x136c9856,
            0x646ba8c0, 0xfd62f97a, 0x8a65c9ec, 0x14015c4f, 0x63066cd9,
            0xfa0f3d63, 0x8d080df5, 0x3b6e20c8, 0x4c69105e, 0xd56041e4,
            0xa2677172, 0x3c03e4d1, 0x4b04d447, 0xd20d85fd, 0xa50ab56b,
            0x35b5a8fa, 0x42b2986c, 0xdbbbc9d6, 0xacbcf940, 0x32d86ce3,
            0x45df5c75, 0xdcd60dcf, 0xabd13d59, 0x26d930ac, 0x51de003a,
            0xc8d75180, 0xbfd06116, 0x21b4f4b5, 0x56b3c423, 0xcfba9599,
            0xb8bda50f, 0x2802b89e, 0x5f058808, 0xc60cd9b2, 0xb10be924,
            0x2f6f7c87, 0x58684c11, 0xc1611dab, 0xb6662d3d, 0x76dc4190,
            0x01db7106, 0x98d220bc, 0xefd5102a, 0x71b18589, 0x06b6b51f,
            0x9fbfe4a5, 0xe8b8d433, 0x7807c9a2, 0x0f00f934, 0x9609a88e,
            0xe10e9818, 0x7f6a0dbb, 0x086d3d2d, 0x91646c97, 0xe6635c01,
            0x6b6b51f4, 0x1c6c6162, 0x856530d8, 0xf262004e, 0x6c0695ed,
            0x1b01a57b, 0x8208f4c1, 0xf50fc457, 0x65b0d9c6, 0x12b7e950,
            0x8bbeb8ea, 0xfcb9887c, 0x62dd1ddf, 0x15da2d49, 0x8cd37cf3,
            0xfbd44c65, 0x4db26158, 0x3ab551ce, 0xa3bc0074, 0xd4bb30e2,
            0x4adfa541, 0x3dd895d7, 0xa4d1c46d, 0xd3d6f4fb, 0x4369e96a,
            0x346ed9fc, 0xad678846, 0xda60b8d0, 0x44042d73, 0x33031de5,
            0xaa0a4c5f, 0xdd0d7cc9, 0x5005713c, 0x270241aa, 0xbe0b1010,
            0xc90c2086, 0x5768b525, 0x206f85b3, 0xb966d409, 0xce61e49f,
            0x5edef90e, 0x29d9c998, 0xb0d09822, 0xc7d7a8b4, 0x59b33d17,
            0x2eb40d81, 0xb7bd5c3b, 0xc0ba6cad, 0xedb88320, 0x9abfb3b6,
            0x03b6e20c, 0x74b1d29a, 0xead54739, 0x9dd277af, 0x04db2615,
            0x73dc1683, 0xe3630b12, 0x94643b84, 0x0d6d6a3e, 0x7a6a5aa8,
            0xe40ecf0b, 0x9309ff9d, 0x0a00ae27, 0x7d079eb1, 0xf00f9344,
            0x8708a3d2, 0x1e01f268, 0x6906c2fe, 0xf762575d, 0x806567cb,
            0x196c3671, 0x6e6b06e7, 0xfed41b76, 0x89d32be0, 0x10da7a5a,
            0x67dd4acc, 0xf9b9df6f, 0x8ebeeff9, 0x17b7be43, 0x60b08ed5,
            0xd6d6a3e8, 0xa1d1937e, 0x38d8c2c4, 0x4fdff252, 0xd1bb67f1,
            0xa6bc5767, 0x3fb506dd, 0x48b2364b, 0xd80d2bda, 0xaf0a1b4c,
            0x36034af6, 0x41047a60, 0xdf60efc3, 0xa867df55, 0x316e8eef,
            0x4669be79, 0xcb61b38c, 0xbc66831a, 0x256fd2a0, 0x5268e236,
            0xcc0c7795, 0xbb0b4703, 0x220216b9, 0x5505262f, 0xc5ba3bbe,
            0xb2bd0b28, 0x2bb45a92, 0x5cb36a04, 0xc2d7ffa7, 0xb5d0cf31,
            0x2cd99e8b, 0x5bdeae1d, 0x9b64c2b0, 0xec63f226, 0x756aa39c,
            0x026d930a, 0x9c0906a9, 0xeb0e363f, 0x72076785, 0x05005713,
            0x95bf4a82, 0xe2b87a14, 0x7bb12bae, 0x0cb61b38, 0x92d28e9b,
            0xe5d5be0d, 0x7cdcefb7, 0x0bdbdf21, 0x86d3d2d4, 0xf1d4e242,
            0x68ddb3f8, 0x1fda836e, 0x81be16cd, 0xf6b9265b, 0x6fb077e1,
            0x18b74777, 0x88085ae6, 0xff0f6a70, 0x66063bca, 0x11010b5c,
            0x8f659eff, 0xf862ae69, 0x616bffd3, 0x166ccf45, 0xa00ae278,
            0xd70dd2ee, 0x4e048354, 0x3903b3c2, 0xa7672661, 0xd06016f7,
            0x4969474d, 0x3e6e77db, 0xaed16a4a, 0xd9d65adc, 0x40df0b66,
            0x37d83bf0, 0xa9bcae53, 0xdebb9ec5, 0x47b2cf7f, 0x30b5ffe9,
            0xbdbdf21c, 0xcabac28a, 0x53b39330, 0x24b4a3a6, 0xbad03605,
            0xcdd70693, 0x54de5729, 0x23d967bf, 0xb3667a2e, 0xc4614ab8,
            0x5d681b02, 0x2a6f2b94, 0xb40bbe37, 0xc30c8ea1, 0x5a05df1b,
            0x2d02ef8d
        };
        #endregion

        /// <summary>
        /// CRC32 Checksummen Berechnung
        /// </summary>
        /// <param name="Data">ByteArray des Befehls</param>
        /// <returns>4 byte CRC Checksumme</returns>
        public static byte[] CRC32(byte[] Data)
        {
            UInt32 crc = 0xffffffff;
            for (int i = 0; i < Data.Length; i++)
                crc = (crc >> 8) ^ crctab[(crc & 0xff) ^ Data[i]];
            crc ^= 0xffffffff;
            byte[] output = new byte[4];

            output[0] = (byte)(crc >> 24);
            output[1] = (byte)(crc >> 16);
            output[2] = (byte)(crc >> 8);
            output[3] = (byte)(crc);

            return output;
        }

        public static byte[] CRC32(Stream DataStream)
        {
            UInt32 crc = 0xffffffff;
            int readed;
            byte[] Buffer = new byte[0x1000];
            while ((readed = DataStream.Read(Buffer, 0, Buffer.Length)) > 0)
                for (int i = 0; i < readed; i++)
                    crc = (crc >> 8) ^ crctab[(crc & 0xff) ^ Buffer[i]];
            crc ^= 0xffffffff;
            byte[] output = new byte[4];

            output[0] = (byte)(crc >> 24);
            output[1] = (byte)(crc >> 16);
            output[2] = (byte)(crc >> 8);
            output[3] = (byte)(crc);

            return output;
        }
        #endregion
    }

    class CrcStream : Stream
    {
        Stream baseStream;
        uint crc = 0xffffffff, changeset = 0;

        public Stream BaseStream
        {
            get => baseStream;
            set
            {
                changeset++;
                baseStream = value;
            }
        }

        public byte[] Crc
        {
            get
            {
                var crc = this.crc;
                crc ^= 0xffffffff;
                byte[] output = new byte[4];

                output[0] = (byte)(crc >> 24);
                output[1] = (byte)(crc >> 16);
                output[2] = (byte)(crc >> 8);
                output[3] = (byte)(crc);

                return output;
            }
        }

        public ulong CompressedSize { get; private set; }

        public event EventHandler Finished;

        public CrcStream(Stream baseStream)
        {
            this.baseStream = baseStream;
            CompressedSize = 0;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var changeset = this.changeset;
            int readed = baseStream.Read(buffer, offset, count);
            for (int i = 0; i<readed; ++i)
                crc = (crc >> 8) ^ CRC.crctab[(crc & 0xff) ^ buffer[i + offset]];
            CompressedSize += (ulong)readed;
            if (readed == 0)
            {
                Finished?.Invoke(this, EventArgs.Empty);
                if (this.changeset != changeset)
                    return Read(buffer, offset, count);
            }
            return readed;
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
            throw new NotSupportedException();
        }
    }

    #endregion

    public delegate void LazyZipTaskHandler(LazyZipTask task);

    enum TaskType
    {
        Execute,
        Halt,
    }

    class TaskInfo
    {
        public TaskType Type;
        public LazyZipTaskHandler Handler;
    }

    public class LazyZipStreamCreator : IDisposable
    {
        internal Queue<TaskInfo> queue = new Queue<TaskInfo>();
        internal ZipStreamCreator creator;
        internal Queue<Stream> outputBuffer = new Queue<Stream>();
        internal Semaphore semaphore = new Semaphore(4, 4);
        readonly int runningTaskCounter = 0;

        internal object lockQueue = new object(), lockOutputBuffer = new object(),
            lockRunningTaskCounter = new object();

        public LazyZipStreamCreator(LazyZipTaskHandler initHandler)
        {
            if (initHandler == null) throw new ArgumentNullException("initHandler");
            queue.Enqueue(new TaskInfo
            {
                Type = TaskType.Execute,
                Handler = initHandler
            });

            creator = new ZipStreamCreator
            {
                newStream = NewStream
            };
        }

        private void NewStream(Stream stream)
        {
            lock (lockOutputBuffer)
                outputBuffer.Enqueue(stream);
        }

        public IEnumerable<Stream> Execute()
        {
            bool enabled = true;
            while (enabled)
            {
                enabled = false;
                var canCreate = false;
                lock (lockQueue)
                {
                    canCreate = enabled = queue.Count > 0;
                }
                if (canCreate)
                {
                    TaskInfo task;
                    lock (lockQueue)
                        task = queue.Dequeue();
                    Execute(task);
                }
                int count;
                lock (lockOutputBuffer)
                    count = outputBuffer.Count;
                if (count > 0)
                {
                    enabled = true;
                    //count = Math.Min(count, 5);
                    for (int i = 0; i < count; ++i)
                    {
                        Stream stream;
                        lock (lockOutputBuffer)
                            stream = outputBuffer.Dequeue();
                        if (stream.CanSeek)
                            stream.Position = 0;
                        yield return stream;
                        stream.Dispose();
                    }
                }
                if (!enabled)
                {
                    bool hasRunningTasks;
                    lock (lockRunningTaskCounter)
                        hasRunningTasks = enabled = runningTaskCounter > 0;
                    if (hasRunningTasks)
                        Thread.Sleep(10);
                }
            }
        }

        private void Execute(TaskInfo task)
        {
            //lock (lockRunningTaskCounter)
            //{
            //    runningTaskCounter++;
            //}
            //Task.Run(() =>
            //{
            task.Handler(new LazyZipTask(this));
            //lock (lockRunningTaskCounter)
            //    {
            //        runningTaskCounter--;
            //    }
            //});
        }

        public void Dispose()
        {
            lock (lockQueue) queue.Clear();
            lock (lockOutputBuffer)
            {
                foreach (var b in outputBuffer)
                    b.Dispose();
                outputBuffer.Clear();
            }
            semaphore.Dispose();
        }
    }

    public class LazyZipTask
    {
        internal LazyZipTask(LazyZipStreamCreator creator)
        {
            this.creator = creator;
        }

        internal LazyZipStreamCreator creator;

        public void AddTask(LazyZipTaskHandler handler)
        {
            if (handler == null) throw new ArgumentNullException("handler");
            lock (creator.lockQueue)
                creator.queue.Enqueue(new TaskInfo
                {
                    Type = TaskType.Execute,
                    Handler = handler
                });
        }

        public void AddFile(string name, Stream data, DateTime modified, string comment = "")
        {
            creator.semaphore.WaitOne();
            creator.creator.AddFile(name, data, modified, comment);
            creator.semaphore.Release(1);
        }

        public void AddFile(string zipName, string localName, string comment = "")
        {
            creator.semaphore.WaitOne();
            creator.creator.AddFile(zipName, localName, comment);
            creator.semaphore.Release(1);
        }

        public int Remove(string name)
        {
            return creator.creator.Remove(name);
        }

        public void AddFinish(string comment = "")
        {
            lock (creator.lockQueue)
                creator.queue.Enqueue(new TaskInfo
                {
                    Type = TaskType.Halt,
                    Handler = (t) => creator.creator.Finish(comment)
                });
        }
    }
}
