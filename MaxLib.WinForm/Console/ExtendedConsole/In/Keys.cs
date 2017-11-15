using System;

namespace MaxLib.Console.ExtendedConsole.In
{
    [Flags]
    public enum Keys
    {
        // Zusammenfassung:
        //     Die Bitmaske zum Extrahieren von Modifizierern aus einem Tastenwert.
        Modifiers = -65536,
        //
        // Zusammenfassung:
        //     Keine Taste gedrückt.
        None = 0,
        //
        // Zusammenfassung:
        //     Die linke Maustaste.
        LButton = 1,
        //
        // Zusammenfassung:
        //     Die rechte Maustaste.
        RButton = 2,
        //
        // Zusammenfassung:
        //     Die CANCEL-TASTE.
        Cancel = 3,
        //
        // Zusammenfassung:
        //     Die mittlere Maustaste (Drei-Tasten-Maus).
        MButton = 4,
        //
        // Zusammenfassung:
        //     Die erste X-Maus-Taste (Fünf-Tasten-Maus).
        XButton1 = 5,
        //
        // Zusammenfassung:
        //     Die zweite X-Maus-Taste (Fünf-Tasten-Maus).
        XButton2 = 6,
        //
        // Zusammenfassung:
        //     Die RÜCKTASTE.
        Back = 8,
        //
        // Zusammenfassung:
        //     The TAB key.
        Tab = 9,
        //
        // Zusammenfassung:
        //     Die ZEILENVORSCHUBTASTE.
        LineFeed = 10,
        //
        // Zusammenfassung:
        //     Die CLEAR-TASTE.
        Clear = 12,
        //
        // Zusammenfassung:
        //     Die EINGABETASTE.
        Enter = 13,
        //
        // Zusammenfassung:
        //     Die RETURN-TASTE.
        Return = 13,
        //
        // Zusammenfassung:
        //     Die UMSCHALTTASTE.
        ShiftKey = 16,
        //
        // Zusammenfassung:
        //     The CTRL key.
        ControlKey = 17,
        //
        // Zusammenfassung:
        //     Die ALT-TASTE.
        Menu = 18,
        //
        // Zusammenfassung:
        //     Die PAUSE-TASTE.
        Pause = 19,
        //
        // Zusammenfassung:
        //     The CAPS LOCK key.
        CapsLock = 20,
        //
        // Zusammenfassung:
        //     The CAPS LOCK key.
        Capital = 20,
        //
        // Zusammenfassung:
        //     Die Taste für den IME-Kana-Modus.
        KanaMode = 21,
        //
        // Zusammenfassung:
        //     Die Taste für den IME-Hanguel-Modus.(aus Kompatibilitätsgründen beibehalten;
        //     verwenden Sie HangulMode)
        HanguelMode = 21,
        //
        // Zusammenfassung:
        //     Die Taste für den IME-Hangul-Modus.
        HangulMode = 21,
        //
        // Zusammenfassung:
        //     Die Taste für den IME-Junja-Modus.
        JunjaMode = 23,
        //
        // Zusammenfassung:
        //     Die Taste für den IME-Abschlussmodus.
        FinalMode = 24,
        //
        // Zusammenfassung:
        //     Die Taste für den IME-Kanji-Modus.
        KanjiMode = 25,
        //
        // Zusammenfassung:
        //     Die Taste für den IME-Hanja-Modus.
        HanjaMode = 25,
        //
        // Zusammenfassung:
        //     The ESC key.
        Escape = 27,
        //
        // Zusammenfassung:
        //     Die Taste für die IME-Konvertierung.
        IMEConvert = 28,
        //
        // Zusammenfassung:
        //     Die Taste für die IME-Nicht-Konvertierung.
        IMENonconvert = 29,
        //
        // Zusammenfassung:
        //     Die Taste für das Annehmen im IME.Veraltet, verwenden Sie stattdessen System.Windows.Forms.Keys.IMEAccept.
        IMEAceept = 30,
        //
        // Zusammenfassung:
        //     Die Taste für das Annehmen im IME (ersetzt System.Windows.Forms.Keys.IMEAceept).
        IMEAccept = 30,
        //
        // Zusammenfassung:
        //     Die Taste für den IME-Moduswechsel.
        IMEModeChange = 31,
        //
        // Zusammenfassung:
        //     Die LEERTASTE.
        Space = 32,
        //
        // Zusammenfassung:
        //     Die BILD-AUF-TASTE.
        Prior = 33,
        //
        // Zusammenfassung:
        //     Die BILD-AUF-TASTE.
        PageUp = 33,
        //
        // Zusammenfassung:
        //     The PAGE DOWN key.
        Next = 34,
        //
        // Zusammenfassung:
        //     The PAGE DOWN key.
        PageDown = 34,
        //
        // Zusammenfassung:
        //     The END key.
        End = 35,
        //
        // Zusammenfassung:
        //     The HOME key.
        Home = 36,
        //
        // Zusammenfassung:
        //     Die NACH-LINKS-TASTE.
        Left = 37,
        //
        // Zusammenfassung:
        //     Die NACH-OBEN-TASTE.
        Up = 38,
        //
        // Zusammenfassung:
        //     Die NACH-RECHTS-TASTE.
        Right = 39,
        //
        // Zusammenfassung:
        //     Die NACH-UNTEN-TASTE.
        Down = 40,
        //
        // Zusammenfassung:
        //     Die SELECT-TASTE.
        Select = 41,
        //
        // Zusammenfassung:
        //     Die DRUCKTASTE.
        Print = 42,
        //
        // Zusammenfassung:
        //     Die EXECUTE-TASTE.
        Execute = 43,
        //
        // Zusammenfassung:
        //     Die DRUCK-TASTE.
        PrintScreen = 44,
        //
        // Zusammenfassung:
        //     Die DRUCK-TASTE.
        Snapshot = 44,
        //
        // Zusammenfassung:
        //     The INS key.
        Insert = 45,
        //
        // Zusammenfassung:
        //     The DEL key.
        Delete = 46,
        //
        // Zusammenfassung:
        //     The HELP key.
        Help = 47,
        //
        // Zusammenfassung:
        //     The 0 key.
        D0 = 48,
        //
        // Zusammenfassung:
        //     The 1 key.
        D1 = 49,
        //
        // Zusammenfassung:
        //     The 2 key.
        D2 = 50,
        //
        // Zusammenfassung:
        //     The 3 key.
        D3 = 51,
        //
        // Zusammenfassung:
        //     The 4 key.
        D4 = 52,
        //
        // Zusammenfassung:
        //     The 5 key.
        D5 = 53,
        //
        // Zusammenfassung:
        //     The 6 key.
        D6 = 54,
        //
        // Zusammenfassung:
        //     The 7 key.
        D7 = 55,
        //
        // Zusammenfassung:
        //     The 8 key.
        D8 = 56,
        //
        // Zusammenfassung:
        //     The 9 key.
        D9 = 57,
        //
        // Zusammenfassung:
        //     Die A-TASTE.
        A = 65,
        //
        // Zusammenfassung:
        //     Die B-TASTE.
        B = 66,
        //
        // Zusammenfassung:
        //     Die C-TASTE.
        C = 67,
        //
        // Zusammenfassung:
        //     Die D-TASTE.
        D = 68,
        //
        // Zusammenfassung:
        //     Die E-TASTE.
        E = 69,
        //
        // Zusammenfassung:
        //     Die F-TASTE.
        F = 70,
        //
        // Zusammenfassung:
        //     Die G-TASTE.
        G = 71,
        //
        // Zusammenfassung:
        //     Die H-TASTE.
        H = 72,
        //
        // Zusammenfassung:
        //     Die I-TASTE.
        I = 73,
        //
        // Zusammenfassung:
        //     Die J-TASTE.
        J = 74,
        //
        // Zusammenfassung:
        //     Die K-TASTE.
        K = 75,
        //
        // Zusammenfassung:
        //     Die L-TASTE.
        L = 76,
        //
        // Zusammenfassung:
        //     Die M-TASTE.
        M = 77,
        //
        // Zusammenfassung:
        //     Die N-TASTE.
        N = 78,
        //
        // Zusammenfassung:
        //     Die O-TASTE.
        O = 79,
        //
        // Zusammenfassung:
        //     Die P-TASTE.
        P = 80,
        //
        // Zusammenfassung:
        //     Die Q-TASTE.
        Q = 81,
        //
        // Zusammenfassung:
        //     Die R-TASTE.
        R = 82,
        //
        // Zusammenfassung:
        //     Die S-TASTE.
        S = 83,
        //
        // Zusammenfassung:
        //     Die T-TASTE.
        T = 84,
        //
        // Zusammenfassung:
        //     Die U-TASTE.
        U = 85,
        //
        // Zusammenfassung:
        //     Die V-TASTE.
        V = 86,
        //
        // Zusammenfassung:
        //     Die W-TASTE.
        W = 87,
        //
        // Zusammenfassung:
        //     Die X-TASTE.
        X = 88,
        //
        // Zusammenfassung:
        //     Die Y-TASTE.
        Y = 89,
        //
        // Zusammenfassung:
        //     Die Z-TASTE.
        Z = 90,
        //
        // Zusammenfassung:
        //     Die linke WINDOWS-TASTE (Microsoft Natural Keyboard).
        LWin = 91,
        //
        // Zusammenfassung:
        //     Die rechte WINDOWS-TASTE (Microsoft Natural Keyboard).
        RWin = 92,
        //
        // Zusammenfassung:
        //     Die ANWENDUNGSTASTE (Microsoft Natural Keyboard).
        Apps = 93,
        //
        // Zusammenfassung:
        //     Die Standbytaste des Computers.
        Sleep = 95,
        //
        // Zusammenfassung:
        //     The 0 key on the numeric keypad.
        NumPad0 = 96,
        //
        // Zusammenfassung:
        //     The 1 key on the numeric keypad.
        NumPad1 = 97,
        //
        // Zusammenfassung:
        //     Die 2-TASTE auf der Zehnertastatur.
        NumPad2 = 98,
        //
        // Zusammenfassung:
        //     Die 3-TASTE auf der Zehnertastatur.
        NumPad3 = 99,
        //
        // Zusammenfassung:
        //     Die 4-TASTE auf der Zehnertastatur.
        NumPad4 = 100,
        //
        // Zusammenfassung:
        //     Die 5-TASTE auf der Zehnertastatur.
        NumPad5 = 101,
        //
        // Zusammenfassung:
        //     Die 6-TASTE auf der Zehnertastatur.
        NumPad6 = 102,
        //
        // Zusammenfassung:
        //     Die 7-TASTE auf der Zehnertastatur.
        NumPad7 = 103,
        //
        // Zusammenfassung:
        //     The 8 key on the numeric keypad.
        NumPad8 = 104,
        //
        // Zusammenfassung:
        //     The 9 key on the numeric keypad.
        NumPad9 = 105,
        //
        // Zusammenfassung:
        //     Die MULTIPLIKATIONSTASTE.
        Multiply = 106,
        //
        // Zusammenfassung:
        //     Die ADDITIONSTASTE.
        Add = 107,
        //
        // Zusammenfassung:
        //     Die TRENNZEICHENTASTE.
        Separator = 108,
        //
        // Zusammenfassung:
        //     Die SUBTRAKTIONSTASTE.
        Subtract = 109,
        //
        // Zusammenfassung:
        //     Die KOMMATASTE.
        Decimal = 110,
        //
        // Zusammenfassung:
        //     Die DIVISIONSTASTE.
        Divide = 111,
        //
        // Zusammenfassung:
        //     The F1 key.
        F1 = 112,
        //
        // Zusammenfassung:
        //     The F2 key.
        F2 = 113,
        //
        // Zusammenfassung:
        //     The F3 key.
        F3 = 114,
        //
        // Zusammenfassung:
        //     The F4 key.
        F4 = 115,
        //
        // Zusammenfassung:
        //     The F5 key.
        F5 = 116,
        //
        // Zusammenfassung:
        //     The F6 key.
        F6 = 117,
        //
        // Zusammenfassung:
        //     The F7 key.
        F7 = 118,
        //
        // Zusammenfassung:
        //     The F8 key.
        F8 = 119,
        //
        // Zusammenfassung:
        //     The F9 key.
        F9 = 120,
        //
        // Zusammenfassung:
        //     The F10 key.
        F10 = 121,
        //
        // Zusammenfassung:
        //     The F11 key.
        F11 = 122,
        //
        // Zusammenfassung:
        //     The F12 key.
        F12 = 123,
        //
        // Zusammenfassung:
        //     The F13 key.
        F13 = 124,
        //
        // Zusammenfassung:
        //     The F14 key.
        F14 = 125,
        //
        // Zusammenfassung:
        //     The F15 key.
        F15 = 126,
        //
        // Zusammenfassung:
        //     The F16 key.
        F16 = 127,
        //
        // Zusammenfassung:
        //     The F17 key.
        F17 = 128,
        //
        // Zusammenfassung:
        //     The F18 key.
        F18 = 129,
        //
        // Zusammenfassung:
        //     The F19 key.
        F19 = 130,
        //
        // Zusammenfassung:
        //     The F20 key.
        F20 = 131,
        //
        // Zusammenfassung:
        //     The F21 key.
        F21 = 132,
        //
        // Zusammenfassung:
        //     The F22 key.
        F22 = 133,
        //
        // Zusammenfassung:
        //     The F23 key.
        F23 = 134,
        //
        // Zusammenfassung:
        //     The F24 key.
        F24 = 135,
        //
        // Zusammenfassung:
        //     The NUM LOCK key.
        NumLock = 144,
        //
        // Zusammenfassung:
        //     Die ROLLEN-TASTE.
        Scroll = 145,
        //
        // Zusammenfassung:
        //     Die linke UMSCHALTTASTE.
        LShiftKey = 160,
        //
        // Zusammenfassung:
        //     Die rechte UMSCHALTTASTE.
        RShiftKey = 161,
        //
        // Zusammenfassung:
        //     The left CTRL key.
        LControlKey = 162,
        //
        // Zusammenfassung:
        //     The right CTRL key.
        RControlKey = 163,
        //
        // Zusammenfassung:
        //     Die linke ALT-TASTE.
        LMenu = 164,
        //
        // Zusammenfassung:
        //     Die rechte ALT-TASTE.
        RMenu = 165,
        //
        // Zusammenfassung:
        //     Die BROWSER-ZURÜCK-TASTE (Windows 2000 oder höher).
        BrowserBack = 166,
        //
        // Zusammenfassung:
        //     Die BROWSER-VORWÄRTS-TASTE (Windows 2000 oder höher).
        BrowserForward = 167,
        //
        // Zusammenfassung:
        //     Die BROWSER-AKTUALISIEREN-TASTE (Windows 2000 oder höher).
        BrowserRefresh = 168,
        //
        // Zusammenfassung:
        //     Die BROWSER-ABBRECHEN-TASTE (Windows 2000 oder höher).
        BrowserStop = 169,
        //
        // Zusammenfassung:
        //     Die BROWSER-SUCHEN-TASTE (Windows 2000 oder höher).
        BrowserSearch = 170,
        //
        // Zusammenfassung:
        //     Die BROWSER-FAVORITEN-TASTE (Windows 2000 oder höher).
        BrowserFavorites = 171,
        //
        // Zusammenfassung:
        //     Die BROWSER-STARTSEITE-TASTE (Windows 2000 oder höher).
        BrowserHome = 172,
        //
        // Zusammenfassung:
        //     Die Taste zum Stummschalten (Windows 2000 oder höher).
        VolumeMute = 173,
        //
        // Zusammenfassung:
        //     Die Taste zum Verringern der Lautstärke (Windows 2000 oder höher).
        VolumeDown = 174,
        //
        // Zusammenfassung:
        //     Die Taste zum Erhöhen der Lautstärke (Windows 2000 oder höher).
        VolumeUp = 175,
        //
        // Zusammenfassung:
        //     Die Playertaste für den nächsten Titel (Windows 2000 oder höher).
        MediaNextTrack = 176,
        //
        // Zusammenfassung:
        //     Die Playertaste für den vorherigen Titel (Windows 2000 oder höher).
        MediaPreviousTrack = 177,
        //
        // Zusammenfassung:
        //     Die Playertaste für das Beenden der Wiedergabe (Windows 2000 oder höher).
        MediaStop = 178,
        //
        // Zusammenfassung:
        //     Die Playertaste für Wiedergabe und Pause (Windows 2000 oder höher).
        MediaPlayPause = 179,
        //
        // Zusammenfassung:
        //     Die MAILTASTE (Windows 2000 oder höher).
        LaunchMail = 180,
        //
        // Zusammenfassung:
        //     Die Taste für die Medienauswahl (Windows 2000 oder höher).
        SelectMedia = 181,
        //
        // Zusammenfassung:
        //     Die ANWENDUNGSSTARTTASTE 1 (Windows 2000 oder höher).
        LaunchApplication1 = 182,
        //
        // Zusammenfassung:
        //     Die ANWENDUNGSSTARTTASTE 2 (Windows 2000 oder höher).
        LaunchApplication2 = 183,
        //
        // Zusammenfassung:
        //     The OEM 1 key.
        Oem1 = 186,
        //
        // Zusammenfassung:
        //     Die OEM-abhängige SEMIKOLONTASTE auf einer US-Standardtastatur (Windows 2000
        //     oder höher).
        OemSemicolon = 186,
        //
        // Zusammenfassung:
        //     Die OEM-abhängige PLUSTASTE auf Tastaturen beliebiger Länder/Regionen (Windows 2000
        //     oder höher).
        Oemplus = 187,
        //
        // Zusammenfassung:
        //     Die OEM-abhängige KOMMATASTE auf Tastaturen beliebiger Länder/Regionen (Windows 2000
        //     oder höher).
        Oemcomma = 188,
        //
        // Zusammenfassung:
        //     Die OEM-abhängige MINUSTASTE auf Tastaturen beliebiger Länder/Regionen (Windows 2000
        //     oder höher).
        OemMinus = 189,
        //
        // Zusammenfassung:
        //     Die OEM-abhängige PUNKTTASTE auf Tastaturen beliebiger Länder/Regionen (Windows 2000
        //     oder höher).
        OemPeriod = 190,
        //
        // Zusammenfassung:
        //     Die OEM-abhängige FRAGEZEICHENTASTE auf einer US-Standardtastatur (Windows 2000
        //     oder höher).
        OemQuestion = 191,
        //
        // Zusammenfassung:
        //     The OEM 2 key.
        Oem2 = 191,
        //
        // Zusammenfassung:
        //     Die OEM-abhängige TILDETASTE auf einer US-Standardtastatur (Windows 2000
        //     oder höher).
        Oemtilde = 192,
        //
        // Zusammenfassung:
        //     The OEM 3 key.
        Oem3 = 192,
        //
        // Zusammenfassung:
        //     The OEM 4 key.
        Oem4 = 219,
        //
        // Zusammenfassung:
        //     Die OEM-abhängige Taste mit der öffnenden Klammer auf einer US-Standardtastatur
        //     (Windows 2000 oder höher).
        OemOpenBrackets = 219,
        //
        // Zusammenfassung:
        //     Die OEM-abhängige Taste mit dem senkrechten Balken auf einer US-Standardtastatur
        //     (Windows 2000 oder höher).
        OemPipe = 220,
        //
        // Zusammenfassung:
        //     The OEM 5 key.
        Oem5 = 220,
        //
        // Zusammenfassung:
        //     The OEM 6 key.
        Oem6 = 221,
        //
        // Zusammenfassung:
        //     Die OEM-abhängige Taste mit der schließenden Klammer auf einer US-Standardtastatur
        //     (Windows 2000 oder höher).
        OemCloseBrackets = 221,
        //
        // Zusammenfassung:
        //     The OEM 7 key.
        Oem7 = 222,
        //
        // Zusammenfassung:
        //     Die OEM-abhängige ANFÜHRUNGSZEICHENTASTE auf einer US-Standardtastatur (Windows 2000
        //     oder höher).
        OemQuotes = 222,
        //
        // Zusammenfassung:
        //     The OEM 8 key.
        Oem8 = 223,
        //
        // Zusammenfassung:
        //     The OEM 102 key.
        Oem102 = 226,
        //
        // Zusammenfassung:
        //     Die OEM-abhängige Taste mit der spitzen Klammer oder Taste mit dem umgekehrten
        //     Schrägstrich auf der RT-102-Tastatur (Windows 2000 oder höher).
        OemBackslash = 226,
        //
        // Zusammenfassung:
        //     Die PROCESS KEY-TASTE.
        ProcessKey = 229,
        //
        // Zusammenfassung:
        //     Wird verwendet, um Unicode-Zeichen wie Tastaturanschläge zu übergeben.Der
        //     Packet-Tastenwert ist das niedrige WORD eines virtuellen 32-Bit-Tastenwerts,
        //     der für Tastatur-unabhängige Eingabemethoden verwendet wird.
        Packet = 231,
        //
        // Zusammenfassung:
        //     The ATTN key.
        Attn = 246,
        //
        // Zusammenfassung:
        //     Die CRSEL-TASTE.
        Crsel = 247,
        //
        // Zusammenfassung:
        //     Die EXSEL-TASTE.
        Exsel = 248,
        //
        // Zusammenfassung:
        //     Die ERASE EOF-TASTE.
        EraseEof = 249,
        //
        // Zusammenfassung:
        //     The PLAY key.
        Play = 250,
        //
        // Zusammenfassung:
        //     The ZOOM key.
        Zoom = 251,
        //
        // Zusammenfassung:
        //     Eine für die zukünftige Verwendung reservierte Konstante.
        NoName = 252,
        //
        // Zusammenfassung:
        //     Die PA1-TASTE.
        Pa1 = 253,
        //
        // Zusammenfassung:
        //     Die CLEAR-TASTE.
        OemClear = 254,
        //
        // Zusammenfassung:
        //     Die Bitmaske zum Extrahieren eines Tastencodes aus einem Tastenwert.
        KeyCode = 65535,
        //
        // Zusammenfassung:
        //     Die Modifizierertaste UMSCHALT.
        Shift = 65536,
        //
        // Zusammenfassung:
        //     The CTRL modifier key.
        Control = 131072,
        //
        // Zusammenfassung:
        //     Die Modifizierertaste ALT.
        Alt = 262144,
    }
}
