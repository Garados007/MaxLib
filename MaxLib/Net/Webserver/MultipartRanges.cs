using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaxLib.Net.Webserver
{
    [Serializable]
    public class MultipartRanges : HttpDataSource
    {
        static long joinGap = 0x40; //1kB
        /// <summary>
        /// If two ranges has a gap smaller then <see cref="JoinGap"/> than this two ranges
        /// are merged and transfered in one block. A maximum amount of <see cref="JoinGap"/>
        /// bytes need to send extra.
        /// </summary>
        public static long JoinGap
        {
            get => joinGap;
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException("JoinGap");
                joinGap = value;
            }
        }

        [Serializable]
        struct Range
        {
            public long From, To;

            public string ToString(long total)
            {
                var sb = new StringBuilder();
                sb.Append("bytes ");
                sb.Append(From);
                sb.Append("-");
                sb.Append(To);
                sb.Append("/");
                sb.Append(total);
                return sb.ToString();
            }
        }

        List<HttpDataSource> streams = new List<HttpDataSource>();
        Stream baseStream;
        HttpDocument document;
        List<Range> ranges = new List<Range>();
        string mime;

        public MultipartRanges(Stream stream, HttpDocument document, string mime)
        {
            this.document = document ?? throw new ArgumentNullException("document");
            baseStream = stream ?? throw new ArgumentNullException("stream");
            this.mime = MimeType = mime;

            document.ResponseHeader.HeaderParameter["Accept-Ranges"] = "bytes";
            if (document.RequestHeader.HeaderParameter.ContainsKey("Range"))
            {
                ParseRanges(document.RequestHeader.HeaderParameter["Range"]);
                var valid = ranges.Count > 0;
                foreach (var r in ranges)
                    if (r.From < 0 || r.From >= baseStream.Length || r.To < 0 || r.To >= baseStream.Length)
                        valid = false;
                if (!valid)
                {
                    document.ResponseHeader.StatusCode = HttpStateCode.RequestedRangeNotSatisfiable;
                    return;
                }
                FormatRanges();
                if (ranges.Count == 1)
                    SinglePart();
                else MultiPart();
            }
            else
            {
                streams.Add(new HttpStreamDataSource(stream)
                {
                    TransferCompleteData = true
                });
            }
        }

        void ParseRanges(string code)
        {
            code = code.Trim();
            if (!code.StartsWith("bytes")) return;
            code = code.Substring(5).TrimStart();
            if (!code.StartsWith("=")) return;
            code = code.Substring(1).TrimStart();
            foreach (var part in code.Split(','))
            {
                var t = part.Split('-');
                if (t.Length == 2 && 
                    long.TryParse(t[0].Trim(), out long from))
                {
                    if (long.TryParse(t[1].Trim(), out long to))
                        ranges.Add(new Range { From = from, To = to });
                    else ranges.Add(new Range { From = from, To = baseStream.Length - 1 });
                }
                else
                {
                    ranges.Clear();
                    return;
                }
            }
        }

        void FormatRanges()
        {
            if (ranges.Count < 2) return;
            ranges.Sort((r1, r2) => r1.From.CompareTo(r2.To));
            var nr = new List<Range>(ranges.Count);
            Range? last = null;
            for (int i = 0; i < ranges.Count; ++i)
                if (last != null)
                {
                    if (last.Value.To < ranges[i].From - 1 - JoinGap)
                    {
                        nr.Add(last.Value);
                        last = ranges[i];
                    }
                    else last = new Range
                    {
                        From = last.Value.From,
                        To = Math.Max(last.Value.To, ranges[i].To)
                    };
                }
                else last = ranges[i];
            if (last != null) nr.Add(last.Value);
            ranges = nr;
        }

        void SinglePart()
        {
            document.ResponseHeader.StatusCode = HttpStateCode.PartialContent;
            var h = document.ResponseHeader.HeaderParameter;
            h["Content-Range"] = ranges[0].ToString(baseStream.Length);
            streams.Add(new HttpStreamDataSource(baseStream)
            {
                RangeStart = ranges[0].From,
                RangeEnd = ranges[0].To,
                TransferCompleteData = false
            });
        }

        void MultiPart()
        {
            var b = new byte[8];
            new Random().NextBytes(b);
            var boundary = BitConverter.ToString(b).Replace("-", "");
            MimeType = MimeTypes.MultipartByteranges + "; boundary=" + boundary;
            document.ResponseHeader.StatusCode = HttpStateCode.PartialContent;
            var sb = new StringBuilder();
            foreach (var r in ranges)
            {
                sb.Append("--");
                sb.AppendLine(boundary);
                if (mime != null)
                {
                    sb.Append("Content-Type: ");
                    sb.AppendLine(mime);
                }
                sb.Append("Content-Range: ");
                sb.AppendLine(r.ToString(baseStream.Length));
                sb.AppendLine();
                streams.Add(new HttpStringDataSource(sb.ToString())
                {
                    TransferCompleteData = true
                });
                streams.Add(new HttpStreamDataSource(baseStream)
                {
                    RangeStart = r.From,
                    RangeEnd = r.To,
                    TransferCompleteData = false
                });
                sb.Clear();
            }
            sb.Append("--");
            sb.Append(boundary);
            sb.Append("--");
            streams.Add(new HttpStringDataSource(sb.ToString())
            {
                TransferCompleteData = true
            });
        }

        public override long AproximateLength()
        {
            return streams.Sum((s) => s.AproximateLength());
        }

        public override void Dispose()
        {
            baseStream.Dispose();
            foreach (var s in streams) s.Dispose();
        }

        public override byte[] GetSourcePart(long start, long length)
        {
            var buffer = new byte[length];
            long offset = 0;
            for (int i = 0; i<streams.Count && length > 0; ++i)
            {
                var al = streams[i].AproximateLength();
                if (al < start)
                {
                    start -= al;
                    continue;
                }
                var fl = Math.Min(length, al);
                var fb = streams[i].GetSourcePart(start, fl);
                length -= fl;
                start = 0;
                Array.Copy(fb, 0, buffer, offset, fl);
                offset += fl;
            }
            return buffer;
        }

        public override long ReadFromStream(Stream networkStream, long readlength)
        {
            return 0;
        }

        public override long ReserveExtraMemory(long bytes)
        {
            return 0;
        }

        public override int WriteSourcePart(byte[] source, long start, long length)
        {
            return 0;
        }

        public override long WriteToStream(Stream networkStream)
        {
            long length = 0;
            foreach (var s in streams)
                length += s.WriteToStream(networkStream);
            return length;
        }
    }
}
