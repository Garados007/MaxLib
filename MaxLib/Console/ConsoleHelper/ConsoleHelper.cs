using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MaxLib.Console.ConsoleHelper
{
    using Console = System.Console;

    public sealed class ConsoleHelper
    {
        public ConsoleHelper(int width, int height, bool ShowCursor)
        {
            this.width = width; this.height = height;
            active = new ConsoleCellData[width, height];
            buffer = new ConsoleCellData[width, height];
            for (int x = 0; x < width; ++x) for (int y = 0; y < height; ++y)
                {
                    active[x, y] = new ConsoleCellData();
                    buffer[x, y] = new ConsoleCellData();
                }
            try
            {

                Console.SetWindowSize(width, height);
                Console.SetBufferSize(width, height);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
            }
            Console.CursorVisible = ShowCursor;
            Console.CancelKeyPress += Console_CancelKeyPress;
            Updater = new Thread(Updating);
            Updater.Name = "ConsoleHelper Updater Thread";
            Updater.Start();
        }

        public void Close()
        {
            Active = false;
            Updater.Abort();
        }

        void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            Active = false;
            Updater.Abort();
        }

        internal int width, height;
        internal ConsoleCellData[,] active, buffer;
        Thread Updater;
        bool Active = true;

        internal void WriteBuffer(ConsoleCellData[,] data)
        {
            while (writing) Thread.Sleep(1);
            writing = true;
            for (int x = 0; x < width; ++x) for (int y = 0; y < height; ++y)
                    if (!data[x, y].Empty) buffer[x, y].CopyFrom(data[x, y]);
            writing = false;
        }

        bool writing = false;
        int updateInterval = 200;
        public int UpdateInterval
        {
            get { return updateInterval; }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException("UpdateInterval", value, "Time cant be less 0");
                updateInterval = Math.Abs(value);
            }
        }

        public string Title
        {
            get { return Console.Title; }
            set { Console.Title = value; }
        }

        void Updating()
        {
            while (Active)
            {
                //Swap Buffer
                bool resetCursor = true;
                for (int y = 0; y < height; ++y) for (int x = 0; x < width; ++x)
                    {
                        if (active[x, y] == buffer[x, y] && !buffer[x, y].RePaint) resetCursor = true;
                        else
                        {
                            if (resetCursor) Console.SetCursorPosition(x, y);
                            resetCursor = false;
                            Console.BackgroundColor = buffer[x, y].BackGroundColor;
                            Console.ForegroundColor = buffer[x, y].TextColor;
                            if (y != height - 1 || x != width - 1)
                                Console.Write(buffer[x, y].Data);
                            buffer[x, y].RePaint = false;
                            active[x, y].CopyFrom(buffer[x, y]);
                            active[x, y].RePaint = false;
                        }
                    }
                Console.SetCursorPosition(0, 0);
                //Sleep
                Thread.Sleep(updateInterval);
            }
        }
    }

    public sealed class ConsoleWriterAsync
    {
        public ConsoleHelper ConsoleHelper { get; private set; }

        public ConsoleWriterAsync(ConsoleHelper helper)
        {
            if (helper == null) throw new ArgumentNullException("helper");
            this.ConsoleHelper = helper;
        }

        ConsoleCellData[,] buffer = null;

        public void BeginWrite()
        {
            if (buffer != null) throw new InvalidOperationException();
            buffer = new ConsoleCellData[ConsoleHelper.width, ConsoleHelper.height];
            for (int x = 0; x < ConsoleHelper.width; ++x) for (int y = 0; y < ConsoleHelper.height; ++y)
                    buffer[x, y] = new ConsoleCellData();
        }

        public int WriterLeft = 0, WriterTop = 0;
        Nullable<ConsoleColor> textc = null, bgc = null;

        public void Clear(int left, int top, int width, int height)
        {
            var target = buffer == null ? ConsoleHelper.buffer : buffer;
            for (int x = left; x<left+width; ++x) for (int y = top; y<top+height; ++y)
                {
                    if (textc != null) target[x, y].TextColor = textc.Value;
                    if (bgc != null) target[x, y].BackGroundColor = bgc.Value;
                    target[x, y].Data = ' ';
                    target[x, y].RePaint = true;
                }
        }

        public void SetCursorPos(int left, int top)
        {
            this.WriterLeft = left;
            this.WriterTop = top;
        }

        public void Write<T>(T data)
        {
            var s = data.ToString();
            var target = buffer == null ? ConsoleHelper.buffer : buffer;
            for (int i = 0; i < s.Length; ++i)
            {
                if (textc != null) target[WriterLeft, WriterTop].TextColor = textc.Value;
                if (bgc != null) target[WriterLeft, WriterTop].BackGroundColor = bgc.Value;
                if (s[i] != '\t' && s[i] != '\n')
                {
                    target[WriterLeft, WriterTop].Data = s[i];
                    WriterLeft++;
                }
                else
                {
                    if (s[i] == '\n') WriterLeft = ConsoleHelper.width;
                    else WriterLeft += 4 - WriterLeft % 4;
                }
                if (WriterLeft >= ConsoleHelper.width)
                {
                    var t = WriterLeft / ConsoleHelper.width;
                    WriterLeft = WriterLeft % ConsoleHelper.width;
                    WriterTop += t;
                    if (WriterTop == ConsoleHelper.height) WriterTop = 0;
                }
            }
        }

        public void Write<T>(T data, ConsoleColor textColor, ConsoleColor bgColor)
        {
            var s = data.ToString();
            var target = buffer == null ? ConsoleHelper.buffer : buffer;
            for (int i = 0; i < s.Length; ++i)
            {

                target[WriterLeft, WriterTop].Data = s[i];
                target[WriterLeft, WriterTop].TextColor = textColor;
                target[WriterLeft, WriterTop].BackGroundColor = bgColor;
                WriterLeft++;
                if (WriterLeft == ConsoleHelper.width)
                {
                    WriterLeft = 0;
                    WriterTop++;
                    if (WriterTop == ConsoleHelper.height) WriterTop = 0;
                }
            }
        }

        public void EndWrite()
        {
            if (buffer == null) throw new InvalidOperationException();
            ConsoleHelper.WriteBuffer(buffer);
            buffer = null;
        }

        public void SetTextColor(ConsoleColor color)
        {
            textc = color;
        }
        public void SetBgColor(ConsoleColor color)
        {
            bgc = color;
        }
        public void ResetColor()
        {
            textc = bgc = null;
        }
    }

    public sealed class ConsoleCellData
    {
        public char Data { get; set; }
        public ConsoleColor TextColor { get; set; }
        public ConsoleColor BackGroundColor { get; set; }
        public bool RePaint { get; set; }

        public bool Empty
        {
            get { return Data == (char)0; }
        }

        internal void CopyFrom(ConsoleCellData ccd)
        {
            this.Data = ccd.Data;
            this.BackGroundColor = ccd.BackGroundColor;
            this.TextColor = ccd.TextColor;
            this.RePaint = ccd.RePaint;
        }

        internal ConsoleCellData()
        {
            Data = (char)0;
            TextColor = ConsoleColor.White;
            BackGroundColor = ConsoleColor.Black;
            RePaint = false;
        }

        public override string ToString()
        {
            return Data.ToString();
        }

        public static bool operator ==(ConsoleCellData ccd1, ConsoleCellData ccd2)
        {
            if ((object)ccd1 == null && (object)ccd2 == null) return true;
            if ((object)ccd1 == null || (object)ccd2 == null) return false;
            return ccd1.Data == ccd2.Data && ccd1.TextColor == ccd2.TextColor &&
                ccd1.BackGroundColor == ccd2.BackGroundColor;
        }
        public static bool operator !=(ConsoleCellData ccd1, ConsoleCellData ccd2)
        {
            return !(ccd1 == ccd2);
        }
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }
    }
}
