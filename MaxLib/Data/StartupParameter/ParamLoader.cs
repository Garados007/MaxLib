using System.Collections.Generic;

namespace MaxLib.Data.StartupParameter
{
    public class ParamLoader
    {
        public Dictionary<string, string> Options { get; private set; }

        public List<string> Commands { get; private set; }

        public string this[int commandIndex] => Commands[commandIndex];
        public string this[string optionKey] => Options[optionKey];

        public bool MatchCommand(params string[] command)
        {
            if (command.Length != Commands.Count) return false;
            for (int i = 0; i < command.Length; ++i)
                if (command[i].ToLower() != Commands[i].ToLower())
                    return false;
            return true;
        }

        public bool MatchCommandStart(params string[] command)
        {
            if (command.Length > Commands.Count) return false;
            for (int i = 0; i < command.Length; ++i)
                if (command[i].ToLower() != Commands[i].ToLower())
                    return false;
            return true;
        }

        public ParamLoader(string[] args)
        {
            Options = new Dictionary<string, string>();
            Commands = new List<string>();
            for (int i = 0; i<args.Length; ++i)
            {
                if (IsStart(args[i]))
                {
                    var ind = args[i].IndexOf('=');
                    if (ind == -1)
                    {
                        if (i < args.Length - 1 && !IsStart(args[i + 1]))
                            Options[args[i].Substring(1)] = args[++i];
                        else Options[args[i].Substring(1)] = null;
                    }
                    else
                    {
                        var name = args[i].Substring(1, ind - 1);
                        var value = args[i].Substring(ind + 1);
                        Options[name] = value;
                    }
                }
                else Commands.Add(args[i]);
            }
        }

        bool IsStart(string text)
        {
            if (text.Length == 0) return false;
            return text[0] == '-' || text[0] == '/';
        }
    }
}
