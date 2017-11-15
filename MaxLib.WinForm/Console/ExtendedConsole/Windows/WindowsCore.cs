using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MaxLib.Console.ExtendedConsole.Windows
{
    using Forms;

    public class WindowsCore : IDisposable
    {
        public ExtendedConsole Console { get; private set; }

        public TaskBar TaskBar { get; private set; }

        public MainTargetWindow MainPart { get; private set; }

        public ImageViewer BackgroundImage { get { return MainPart.Background; } }

        public MainMenu MainMenu { get { return TaskBar.Menu; } }

        public FormsContainer Forms { get { return MainPart.Forms; } }

        public WindowsCore()
        {
            Console = new ExtendedConsole(new MaxLib.Console.ExtendedConsole.ExtendedConsoleOptions()
                {
                    BoxHeight = 15,
                    BoxWidth = 10,
                    ShowMouse = true,
                    RunFormInExtraThread = false,
                });
            Console.Load += () =>
                {
                    Console.Options.SetFontSize(11);
                    MainPart.Width = Console.Matrix.Width;
                    MainPart.Height = Console.Matrix.Height - 2;
                };
            
            var back = new ImageViewer();
            back.AlternativeColor = ConsoleColor.Green;
            Console.MainContainer.Add(MainPart = new MainTargetWindow(back)
                {
                    X = 0,
                    Y = 0
                });
            Console.MainContainer.Add(TaskBar = new TaskBar());
            InitialStartMenu();
        }

        public void Start()
        {
            Console.Start();
        }

        public void Dispose()
        {
            Console.Dispose();
        }

        void InitialStartMenu()
        {
            var m = TaskBar.Menu = new MainMenu(Console, "Start");
            var sm = new MainMenu(Console, "Programme", WindowsMenuKeys.Programms);
            m.Submenu.Add(sm);
            var sm1 = new MainMenu(Console, "TestForm");
            sm.Submenu.Add(sm1);
            sm1.Click += () => Forms.Add(new Form()
                {
                    Width = 40,
                    Height = 30,
                    X = 0,
                    Y = 0,
                    Text = "TestForm",
                    BaseString = "(c) 2014 - Part of WinCore"
                });
            sm = new MainMenu(Console, "Einstellungen", WindowsMenuKeys.Settings);
            m.Submenu.Add(sm);
            sm1 = new MainMenu(Console, "AddOns verwalten", WindowsMenuKeys.Settings_AddOn);
            sm.Submenu.Add(sm1);
            sm = new MainMenu(Console, "Beenden", WindowsMenuKeys.ShutDown);
            sm.Click += Console.CloseForm;
            m.Submenu.Add(sm);
            
        }

        public MainMenu FindMenuByKey(string Key)
        {
            return FindMenuByKey(MainMenu, Key);
        }

        MainMenu FindMenuByKey(MainMenu search, string Key)
        {
            if (search.Key == Key) return search;
            foreach (var s in search.Submenu)
            {
                var m = FindMenuByKey(s, Key);
                if (m != null) return m;
            }
            return null;
        }
    }

    public static class WindowsMenuKeys
    {
        public const string ShutDown = "ShutDownConsole";
        public const string Programms = "Programms";

        public const string Settings = "Settigs";
        public const string Settings_AddOn = "Settings_AddOn";
    }
}
