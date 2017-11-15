using System.Collections.Generic;

namespace MaxLib.Data.StartupParameter
{
    class ParamLoader
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
                if (args[i].StartsWith("-"))
                {
                    var ind = args[i].IndexOf('=');
                    if (ind == -1)
                    {
                        if (i < args.Length - 1 && !args[i + 1].StartsWith("-"))
                            Options[args[i]] = Options[args[++i]];
                        else Options[args[i]] = null;
                    }
                    else
                    {
                        var name = args[i].Remove(ind);
                        var value = args[i].Substring(ind + 1);
                        Options[name] = value;
                    }
                }
                else Commands.Add(args[i]);
            }
        }
    }
}
