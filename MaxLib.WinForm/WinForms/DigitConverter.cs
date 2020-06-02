namespace MaxLib.WinForms
{
    public interface IDigitConverter
    {
        Digit Convert(char text);

        string BuildConvertable(string source);
    }

    public class StandartConverter : IDigitConverter
    {

        public Digit Convert(char text)
        {
            text = text.ToString().ToLowerInvariant()[0];
            switch (text)
            {
                case '1': return Digit.RightVertTop | Digit.RightVertBot;
                case '2': return Digit.TopHorz | Digit.RightVertTop | Digit.MiddleHorz | Digit.LeftVertBot | Digit.BotHorz;
                case '3': return Digit.TopHorz | Digit.RightVertTop | Digit.MiddleHorz | Digit.RightVertBot | Digit.BotHorz;
                case '4': return Digit.LeftVertTop | Digit.RightVertTop | Digit.MiddleHorz | Digit.RightVertBot;
                case '5': return Digit.TopHorz | Digit.LeftVertTop | Digit.MiddleHorz | Digit.RightVertBot | Digit.BotHorz;
                case '6': return Digit.TopHorz | Digit.LeftVertTop | Digit.MiddleHorz | Digit.LeftVertBot | Digit.RightVertBot | Digit.BotHorz;
                case '7': return Digit.TopHorz | Digit.RightVertTop | Digit.RightVertBot;
                case '8': return Digit.TopHorz | Digit.LeftVertTop | Digit.RightVertTop | Digit.MiddleHorz | Digit.LeftVertBot | Digit.RightVertBot | Digit.BotHorz;
                case '9': return Digit.TopHorz | Digit.LeftVertTop | Digit.RightVertTop | Digit.MiddleHorz | Digit.RightVertBot | Digit.BotHorz;
                case '0': return Digit.TopHorz | Digit.LeftVertTop | Digit.RightVertTop | Digit.LeftVertBot | Digit.RightVertBot | Digit.BotHorz | Digit.SlashBotLeft | Digit.SlashTopRight;
                case 'a': return Digit.TopHorz | Digit.LeftVertTop | Digit.RightVertTop | Digit.MiddleHorz | Digit.LeftVertBot | Digit.RightVertBot;
                case 'b': return Digit.TopHorzLeft | Digit.LeftVertTop | Digit.MiddleVertTop | Digit.MiddleHorz | Digit.LeftVertBot | Digit.RightVertBot | Digit.BotHorz;
                case 'c': return Digit.TopHorz | Digit.LeftVertTop | Digit.LeftVertBot | Digit.BotHorz;
                case 'd': return Digit.RightVertTop | Digit.MiddleHorz | Digit.LeftVertBot | Digit.RightVertBot | Digit.BotHorz;
                case 'e': return Digit.TopHorz | Digit.LeftVertTop | Digit.MiddleHorz | Digit.LeftVertBot | Digit.BotHorz;
                case 'f': return Digit.TopHorz | Digit.LeftVertTop | Digit.MiddleHorz | Digit.LeftVertBot;
                case 'g': return Digit.TopHorz | Digit.LeftVertTop | Digit.MiddleHorzRight | Digit.LeftVertBot | Digit.RightVertBot | Digit.BotHorz;
                case 'h': return Digit.LeftVertTop | Digit.RightVertTop | Digit.MiddleHorz | Digit.LeftVertBot | Digit.RightVertBot;
                case 'i': return Digit.MiddleVertBot | Digit.MiddleVertTop | Digit.TopHorz | Digit.BotHorz;
                case 'j': return Digit.RightVertTop | Digit.RightVertBot | Digit.SlashBotRight;
                case 'k': return Digit.LeftVertTop | Digit.SlashTopRight | Digit.MiddleHorzLeft | Digit.LeftVertBot | Digit.SlashBotRight;
                case 'l': return Digit.LeftVertTop | Digit.LeftVertBot | Digit.BotHorz;
                case 'm': return Digit.LeftVertTop | Digit.SlashTopLeft | Digit.SlashTopRight | Digit.RightVertTop | Digit.LeftVertBot | Digit.RightVertBot;
                case 'n': return Digit.LeftVertTop | Digit.SlashTopLeft | Digit.RightVertTop | Digit.LeftVertBot | Digit.SlashBotRight | Digit.RightVertBot;
                case 'o': return Digit.LeftVertBot | Digit.RightVertBot | Digit.BotHorz | Digit.LeftVertTop | Digit.RightVertTop | Digit.TopHorz;
                case 'p': return Digit.LeftVertTop | Digit.TopHorz | Digit.RightVertTop | Digit.MiddleHorz | Digit.LeftVertBot;
                case 'q': return Digit.TopHorz | Digit.LeftVertTop | Digit.RightVertTop | Digit.LeftVertBot | Digit.RightVertBot | Digit.BotHorz | Digit.SlashBotRight;
                case 'r': return Digit.LeftVertTop | Digit.TopHorz | Digit.RightVertTop | Digit.MiddleHorz | Digit.LeftVertBot | Digit.SlashBotRight;
                case 's': return Digit.TopHorzRight | Digit.MiddleVertTop | Digit.MiddleVertBot | Digit.BotHorzLeft;
                case 't': return Digit.TopHorz | Digit.MiddleVertTop | Digit.MiddleVertBot;
                case 'u': return Digit.LeftVertTop | Digit.RightVertTop | Digit.LeftVertBot | Digit.RightVertBot | Digit.BotHorz;
                case 'v': return Digit.SlashTopLeft | Digit.SlashTopRight;
                case 'w': return Digit.LeftVertTop | Digit.RightVertTop | Digit.LeftVertBot | Digit.SlashBotLeft | Digit.SlashBotRight | Digit.RightVertBot;
                case 'x': return Digit.SlashBotRight | Digit.SlashTopLeft | Digit.SlashTopRight | Digit.SlashBotLeft;
                case 'y': return Digit.SlashTopLeft | Digit.SlashTopRight | Digit.MiddleVertBot;
                case 'z': return Digit.TopHorz | Digit.SlashTopRight | Digit.SlashBotLeft | Digit.BotHorz;

                case 'ä': return Digit.TopHorz | Digit.LeftVertTop | Digit.RightVertTop | Digit.MiddleHorz | Digit.LeftVertBot | Digit.RightVertBot | Digit.SlashTopLeft | Digit.SlashTopRight;
                case 'ö': return Digit.LeftVertBot | Digit.MiddleHorz | Digit.RightVertBot | Digit.BotHorz | Digit.SlashTopLeft | Digit.SlashTopRight;
                case 'ü': return Digit.LeftVertBot | Digit.RightVertBot | Digit.BotHorz | Digit.SlashTopLeft | Digit.SlashTopRight;
                case 'ß': return Digit.LeftVertTop | Digit.LeftVertBot | Digit.TopHorz | Digit.SlashTopRight | Digit.SlashBotRight | Digit.BotHorzRight;

                case '-': return Digit.MiddleHorz;
                case '+': return Digit.MiddleVertTop | Digit.MiddleVertBot | Digit.MiddleHorz;
                case '*': return Digit.SlashBotLeft | Digit.SlashBotRight | Digit.SlashTopLeft | Digit.SlashTopRight | Digit.MiddleVertBot | Digit.MiddleVertTop | Digit.MiddleHorz;
                case '/': return Digit.SlashBotLeft | Digit.SlashTopRight;

                case '"': return Digit.LeftVertTop | Digit.RightVertTop;
                case '$': return Digit.TopHorz | Digit.LeftVertTop | Digit.MiddleHorz | Digit.RightVertBot | Digit.BotHorz | Digit.MiddleVertTop | Digit.MiddleVertBot;
                case '%': return Digit.LeftVertTop | Digit.TopHorzLeft | Digit.MiddleHorz | Digit.MiddleVertTop | Digit.MiddleVertBot | Digit.RightVertBot | Digit.BotHorzRight | Digit.SlashBotLeft | Digit.SlashTopRight;
                case '<': return Digit.SlashTopRight | Digit.SlashBotRight;
                case '>': return Digit.SlashTopLeft | Digit.SlashBotLeft;
                case '=': return Digit.MiddleHorz | Digit.BotHorz;
                case '?': return Digit.TopHorz | Digit.RightVertTop | Digit.MiddleHorzRight | Digit.MiddleVertBot;
                case '\'': return Digit.MiddleVertTop;
                case '°': return Digit.TopHorzLeft | Digit.LeftVertTop | Digit.MiddleVertTop | Digit.MiddleHorzLeft;
                case '(': return Digit.TopHorzRight | Digit.MiddleVertTop | Digit.MiddleVertBot | Digit.BotHorzRight;
                case ')': return Digit.TopHorzLeft | Digit.MiddleVertTop | Digit.MiddleVertBot | Digit.BotHorzLeft;
                case '|': return Digit.MiddleVertTop | Digit.MiddleVertBot;
                case '_': return Digit.BotHorz;
                case ',': return Digit.SlashBotLeft;
                case '.': goto case ',';
                case '[': goto case '(';
                case '{': return Digit.TopHorzRight | Digit.MiddleVertTop | Digit.MiddleVertBot | Digit.BotHorzRight | Digit.MiddleHorzLeft;
                case ']': goto case ')';
                case '}': return Digit.TopHorzLeft | Digit.MiddleVertTop | Digit.MiddleVertBot | Digit.BotHorzLeft | Digit.MiddleHorzRight;
                case '\\': return Digit.SlashTopLeft | Digit.SlashBotRight;

                default: return Digit.None;
            }
        }


        public string BuildConvertable(string source)
        {
            return source.Replace("&", " und ").Replace("€", " EURO").Replace("~", "CIRCA ").Replace(";", ",").Replace(":", ".").Replace(
                "²", "^2").Replace("³", "^3").Replace("§", " PARAGRAF ").Replace("#", "Nr. ");
        }
    }
}
