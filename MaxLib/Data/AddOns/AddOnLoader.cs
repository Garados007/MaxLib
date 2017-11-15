using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;

namespace MaxLib.Data.AddOns
{
    /// <summary>
    /// Definiert einen allgemeinen AddOn-Lader.
    /// </summary>
    public class AddOnLoader
    {
        string LastDirectory = "";

        /// <summary>
        /// Bestimmt eine Liste an Dateiendungen für die Vorauswahl. Die Endungen müssen so gestaltet sein: ".dll". Am Ende wird nur überprüft, ob die Endung 
        /// vorhanden ist. Danach erfolgt erst die Überprüfung mittels <see cref="AddOnLoader.SupportedFileCheck"/>.
        /// </summary>
        public List<string> SupportedExtensions { get; private set; }
        /// <summary>
        /// Erstellt einen neuen allgemeinen AddOn-Lader.
        /// </summary>
        public AddOnLoader()
        {
            SupportedExtensions = new List<string> { ".dll" };
            RegistredFiles = new List<AddOnFile>();
            AddOnTypes = new List<Type>();
        }
        /// <summary>
        /// Überprüft, ob diese Datei verwendet werden darf. Bevor diese Überprüfung stattfindet, wird mittels <see cref="AddOnLoader.SupportedExtensions"/> 
        /// auf Gültigkeit überprüft. Wenn schon da die Datei als ungültig deklariert wird, findet diese Überprüfung nicht mehr statt und die Datei wird 
        /// ausgeschlossen.
        /// </summary>
        public event SupportedFileCheckHandler SupportedFileCheck;
        /// <summary>
        /// Stellt eine Liste aller Registrierten <see cref="AddOnFile"/> dar.
        /// </summary>
        public List<AddOnFile> RegistredFiles { get; private set; }
        /// <summary>
        /// Diese Liste bestimmt, welche Typen in der Datei gesucht werden sollen und spezifische Informationen für das Nutzen 
        /// des AddOns enthält. Diese Typen stellen die Schnittstelle zwischen den Code des AddOn und dieser Hauptanwendung 
        /// dar. Die Typen sollten im Regelfalle eine Basisklasse bzw. Interface darstellen, welche im AddOn als Klasse abgelitten wird. 
        /// Diese darf nicht statisch sein und muss öffentliche Konstruktoren besitzen!
        /// </summary>
        public List<Type> AddOnTypes { get; private set; }

        /// <summary>
        /// Durchsucht den folgenden Ordner nach allen gültigen Dateien. Diese werden nur <see cref="AddOnLoader.RegistredFiles"/> angefügt, geladen 
        /// werden diese noch nicht. Zuerst werden alle gefundenen Dateien auf eine gültige Endung (unter <seealso cref="AddOnLoader.SupportedExtensions"/>) 
        /// überprüft, danach erfolgt erst eine Überprüfung mittels <see cref="AddOnLoader.RegistredFiles"/>. Doppelte Einträge in
        /// <see cref="AddOnLoader.RegistredFiles"/> werden gelöscht.
        /// </summary>
        /// <param name="directory">Bestimmt den Ordner, in dem gesucht werden soll.</param>
        /// <param name="inSubDirectoriesToo">Gibt an, ob auch alle Unterordner mit durchsucht werden sollen.</param>
        public void SearchAllFiles(string directory, bool inSubDirectoriesToo = false)
        {
            LastDirectory = directory;
            SearchAllFiles(new DirectoryInfo(directory), inSubDirectoriesToo);
        }
        /// <summary>
        /// Durchsucht den folgenden Ordner nach allen gültigen Dateien. Diese werden nur <see cref="AddOnLoader.RegistredFiles"/> angefügt, geladen 
        /// werden diese noch nicht. Zuerst werden alle gefundenen Dateien auf eine gültige Endung (unter <seealso cref="AddOnLoader.SupportedExtensions"/>) 
        /// überprüft, danach erfolgt erst eine Überprüfung mittels <see cref="AddOnLoader.RegistredFiles"/>. Doppelte Einträge in
        /// <see cref="AddOnLoader.RegistredFiles"/> werden gelöscht.
        /// </summary>
        /// <param name="directory">Bestimmt den Ordner, in dem gesucht werden soll.</param>
        /// <param name="inSubDirectoriesToo">Gibt an, ob auch alle Unterordner mit durchsucht werden sollen.</param>
        public void SearchAllFiles(DirectoryInfo directory, bool inSubDirectoriesToo = false)
        {
            LastDirectory = directory.FullName;
            if (inSubDirectoriesToo) foreach (var d in directory.GetDirectories()) SearchAllFiles(d, inSubDirectoriesToo);
            foreach (var f in directory.GetFiles())
            {
                var name = f.Name;
                bool allowed = false;
                foreach (var ext in SupportedExtensions) if (name.EndsWith(ext)) allowed = true;
                if (!allowed) continue;
                if (SupportedFileCheck != null) if (!SupportedFileCheck(f)) continue;
                if (RegistredFiles.Exists((aof) => aof.FileInfo == f)) continue;
                RegistredFiles.Add(new AddOnFile(f, this));
            }
        }
        /// <summary>
        /// Überprüft, ob diese Datei gültig ist und lädt diese gegebenfalls. Diese wird unter <see cref="AddOnLoader.RegistredFiles"/>
        /// gleich mit angebunden.
        /// </summary>
        /// <param name="file">Die Zieldatei</param>
        /// <returns>Gibt ein Zeiger auf die Datei zurück.</returns>
        public AddOnFile SearchSingleFile(string path)
        {
            return SearchSingleFile(new FileInfo(path));
        }
        /// <summary>
        /// Überprüft, ob diese Datei gültig ist und lädt diese gegebenfalls. Diese wird unter <see cref="AddOnLoader.RegistredFiles"/>
        /// gleich mit angebunden.
        /// </summary>
        /// <param name="file">Die Zieldatei</param>
        /// <returns>Gibt ein Zeiger auf die Datei zurück.</returns>
        public AddOnFile SearchSingleFile(FileInfo file)
        {
            var name = file.Name;
            bool allowed = false;
            foreach (var ext in SupportedExtensions) if (name.EndsWith(ext)) allowed = true;
            if (!allowed) return null;
            if (SupportedFileCheck != null) if (!SupportedFileCheck(file)) return null;
            if (RegistredFiles.Exists((aof) => aof.FileInfo == file)) return null;
            var f = new AddOnFile(file, this);
            RegistredFiles.Add(f);
            return f;
        }
        /// <summary>
        /// Durchsucht alle Datein nach Klassen, deren Basistyp genau dem entspricht, welcher mit angegeben wird. Die Verweise werden 
        /// dann zurückgegeben.
        /// </summary>
        /// <typeparam name="T">Der Basistyp</typeparam>
        /// <returns>eine Ansammlung von Verweisen</returns>
        public AddOnType[] GetAllMatchingTypes<T>()
        {
            var l = new List<AddOnType>();
            foreach (var f in RegistredFiles) l.AddRange(f.GetAllMatchingTypes<T>());
            return l.ToArray();
        }

        /// <summary>
        /// Durchsucht die Datei nach allen Typen, welche in <see cref="Loader.AddOnTypes"/> registriert wurden. 
        /// Diese werden in <see cref="AddOnTypes"/> registriert.
        /// </summary>
        /// <exception cref="NotSupportedException"/>
        public void DetectTypes()
        {
            this.RegistredFiles.ForEach((f) => f.DetectTypes());
        }

        public override string ToString()
        {
            return string.Format("Files: {0}; Directory: {1}", RegistredFiles.Count, LastDirectory);
        }
    }

    public delegate bool SupportedFileCheckHandler(FileInfo file);

    /// <summary>
    /// Stellt eine Datei dar, welche Daten für ein AddOn enthält.
    /// </summary>
    public class AddOnFile
    {
        /// <summary>
        /// Stellt einen Verweis auf die Zieldatei dar.
        /// </summary>
        public FileInfo FileInfo { get; private set; }
        /// <summary>
        /// Bestimmt, wenn vorhanden, den <see cref="AddOnLoader"/>. Aus diesem werden auch die Typeninformationen abgeholt. 
        /// </summary>
        public AddOnLoader Loader { get; private set; }
        /// <summary>
        /// Dies stellt alle gefundenen Typen in diesen AddOn dar. Zuerst muss eine Suche mit <see cref="DetectTypes"/> gestartet werden.
        /// </summary>
        public List<AddOnType> AddOnTypes { get; private set; }
        /// <summary>
        /// Erstellt einen neuen Verweis auf eine Datei, welche ein oder mehrere AddOn(s) enthält.
        /// </summary>
        /// <param name="path">Bestimmt den Pfad auf die Zieldatei</param>
        public AddOnFile(string path)
        {
            FileInfo = new FileInfo(path);
            AddOnTypes = new List<AddOnType>();
        }
        /// <summary>
        /// Erstellt einen neuen Verweis auf eine Datei, welche ein oder mehrere AddOn(s) enthält.
        /// </summary>
        /// <param name="fileInfo">Bestimmt den Pfad auf die Zieldatei</param>
        public AddOnFile(FileInfo fileInfo)
        {
            FileInfo = fileInfo;
            AddOnTypes = new List<AddOnType>();
        }
        internal AddOnFile(FileInfo fileInfo, AddOnLoader Loader)
        {
            FileInfo = fileInfo;
            this.Loader = Loader;
            AddOnTypes = new List<AddOnType>();
        }

        /// <summary>
        /// Stellt die AssemblyInformationen auf die Datei dar.
        /// </summary>
        public AssemblyName AssemblyName { get; private set; }
        /// <summary>
        /// Stellt die AssemblyInformationen auf die Datei dar.
        /// </summary>
        public Assembly Assembly { get; private set; }

        /// <summary>
        /// Durchsucht die Datei nach allen Typen, welche in <see cref="Loader.AddOnTypes"/> registriert wurden. 
        /// Diese werden in <see cref="AddOnTypes"/> registriert.
        /// </summary>
        /// <exception cref="NotSupportedException"/>
        public void DetectTypes()
        {
            if (Loader == null) throw new NotSupportedException();
            DetectTypes(Loader.AddOnTypes.ToArray());
        }
        /// <summary>
        /// Durchsucht die Datei nach allen Typen, welche mit angegeben wurden. Diese werden in <see cref="AddOnTypes"/> registriert.
        /// </summary>
        /// <param name="AddOnTypes">registrierte Typen</param>
        public void DetectTypes(params Type[] AddOnTypes)
        {
            try
            {
                this.AssemblyName = AssemblyName.GetAssemblyName(FileInfo.FullName);
                this.Assembly = Assembly.Load(this.AssemblyName);
                if (this.Assembly == null) return;
                var types = Assembly.GetTypes();
                foreach (var type in types)
                {
                    if (type.IsInterface || type.IsAbstract) continue;
                    foreach (var aot in AddOnTypes) if (type.GetInterface(aot.FullName) != null)
                        {
                            this.AddOnTypes.Add(new AddOnType(aot, type, this));
                            break;
                        }
                }
            }
            catch { }
        }
        /// <summary>
        /// Durchsucht das Verzeichnis dieser Datei nach allen Klassen, welche dem Basistyp genau entsprechen. Diese müssen aber schon zuvor 
        /// mittels <see cref="DetectTypes"/> ermittelt wurden sein!
        /// </summary>
        /// <typeparam name="T">Der Basistyp</typeparam>
        /// <returns>alle passenden Verweise</returns>
        public AddOnType[] GetAllMatchingTypes<T>()
        {
            var l = new List<AddOnType>();
            foreach (var t in AddOnTypes) if (typeof(T) == t.BasicType) l.Add(t);
            return l.ToArray();
        }

        public override string ToString()
        {
            return string.Format("Name: {0}; Types: {1}", FileInfo.Name, AddOnTypes.Count);
        }
    }

    /// <summary>
    /// Definiert spezifische Informationen zum Abrufen der Klasse aus einer AddOn-Datei. Erst hier wird das direkte Abrufen der Schnittstelle 
    /// gewährleistet.
    /// </summary>
    public class AddOnType
    {
        /// <summary>
        /// Gibt den Basis-Typ an. Dies stellt eine Schnittstelle dar und muss sowohl dem AddOn, als auch dieser Anwendung bekannt, 
        /// d.h. implementiert, sein.
        /// </summary>
        public Type BasicType { get; private set; }
        /// <summary>
        /// Dies liefert die Typeninformationen zu der Klasse aus der AddOn-Datei.
        /// </summary>
        public Type RemoteType { get; private set; }
        /// <summary>
        /// Stellt einen Verweis auf die Datei dar, welche dieses AddOn enthält.
        /// </summary>
        public AddOnFile File { get; private set; }

        internal AddOnType(Type BasicType, Type RemoteType, AddOnFile File)
        {
            this.BasicType = BasicType;
            this.RemoteType = RemoteType;
            this.File = File;
        }
        /// <summary>
        /// Erstellt eine neue Instanz der Klasse aus dem AddOn. Dabei wird direkt die Klasse aus dem AddOn erstellt, ist aber hier nur über den 
        /// Basistyp ansprechbar.
        /// </summary>
        /// <typeparam name="T">Der Basistyp in dem das sofort gecastet werden soll</typeparam>
        /// <param name="args">Hier kommen alle Variablen rein, die dem Konstruktor übermittelt werden müssen</param>
        /// <returns>Eine Klasse aus dem AddOn.</returns>
        public T CreateInstance<T>(params object[] args)
        {
            return (T)Activator.CreateInstance(RemoteType, args);
        }

        public override string ToString()
        {
            return string.Format("Local: {0}; Remote: {1}; File: {2}", BasicType.Name, RemoteType.Name, File.FileInfo.Name);
        }
    }
}
