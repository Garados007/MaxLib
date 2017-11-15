using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MaxLib.WinForms
{
    [Flags]
    public enum Digit : ushort
    {
        None = 0,
        TopHorzLeft = 1,
        TopHorzRight = 2,
        LeftVertTop = 4,
        SlashTopLeft = 8,
        MiddleVertTop = 16,
        SlashTopRight = 32,
        RightVertTop = 64,
        MiddleHorzLeft = 128,
        MiddleHorzRight = 256,
        LeftVertBot = 512,
        SlashBotLeft = 1024,
        MiddleVertBot = 2048,
        SlashBotRight = 4096,
        RightVertBot = 8192,
        BotHorzLeft = 16384,
        BotHorzRight = 32768,

        TopHorz = TopHorzLeft + TopHorzRight,
        MiddleHorz = MiddleHorzLeft + MiddleHorzRight,
        BotHorz = BotHorzLeft + BotHorzRight
    }
}
