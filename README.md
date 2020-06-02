# MaxLib

This is a huge collection of tools and stuff that I have developed at my own in my free time. For fun of course. 

> Most content isn't documented at all. I will add this time by time.

You can explore this project if you want. You will find some nice stuff.

Most Projects are written in .NET Standard and therefore compatible with .NET Framework and .NET Core. The currently method to import this to your project is to clone this project as a submodule.

If you find some bugs feel free to create an issue for this. I will try my best to solve this.

## Project MaxLib

| Stats | Value |
|-|-|
| Framework | .NET Standard 2.0 |
| Dependencies | none |
| Systems | Windows, Linux |

**Content:**

- [`MaxLib.Collections`](MaxLib/Collections)
    - `class:ByteTree<T>`: A collection in a tree structure. Each none use a single byte as a key to identify its child. Each node can have to max 256 children. Each node con contain data.
    - `class:EnumeratorBackup<T>`: A thread safe enumerator that will concat 2 other enumerators. 
        > *This may be removed in a future release*
    - `class:EnumeratorBuilder<T>`: A thread safe builder that can combines yields with single T, enumerations of T or functions that will generate one of them. If the builder is iterated through it will return every element and execute the yielded functions. This will remove statements like this:
        ```csharp
        yield 1;
        foreach (var item in GetCollection())
            yield item;
        ```
        And make this:
        ```csharp
        var builder = new EnumeratorBuilder<int>();
        builder.Yield(1);
        builder.Yield(GetCollection);
        return builder;
        ```
    - `class:KeyedCache<Key,Value>`: a small cache buffer that can contains key value pairs. If the cache is full it full discard old pairs. If a new key is requested it will create the value with its handlers. The discard and creation of values will block the calling thread.
    - `class:MarshalEnumerator<T>`: a safe wrapper for enumerators to be marshalled between different contexts. 
    - `class:OverlapModel<T>`: A model that try to determine overlaps of data blocks. 
        > *This may be removed in a future release*
    - `class:PriorityList<Priority,Element>`: a list that will automatically sort its item according to its priority.
    - `class:SaveDictionary<TKey,TValue>`: A thread safe dictionary. This will block access until other threads are finished with their access.
    - `class:SlotReserver<T>`: This will try to manage the chronological order of tasks or jobs. 
        > *This may be removed in a future release*
    - `class:SyncedList<T>`: This thread safe list can create clones of itself that could be read only. The clones will share their data pool with the original list.
- `MaxLib.Console`
    - `namespace:ConsoleHelper`: This will create a window in the console and allow multi threaded access to an internal buffer to write text on the console.
- `MaxLib.Data`
    - `namespace:AddOns`: This allows you to add AddOn DLLs on the fly in your current AppDomain. For that you need to provide a common base class that all AddOns implement. These will be the connector between the AddOn and the main application.
    - `namespace:Bits`: Allows you bit level access to streams
        > This implementation has a small speed penalty. This will be tried to fix in a future release.
    - `namespace:CompactFileSystem`: This will manage a whole file system in a single file.
        > The current implementation works but they are some features missing that I want to implement later.
    - `namespace:Config`: This will manage configuration that are derived from `MaxLib.Data.Config.ConfigBase`. It will also manage its editing and updates.
    - `namespace:HtmlDom`: A HTML parser that will parse html text files in a dom. The dom allows access to its content.
    - `namespace:IniFiles`: An extended parser for configuration ini files. It allows access to the configuration and storage of them.
        > The name of the classes might be changed in a future release.
    - `namespace:Json`: A library to parse, edit and save JSON files.
        > This is deprecated. Use the NuGet System.Text.Json. This is way faster and better than my implementation. Some utils in it I might to port to System.Text.Json.

        > **This will be removed in a future release!**
    - `namespace:StartupParameter`: This will manage the parameter that are given to the `Main(string[] args)` method. It will automatically detect options and commands. This allows better management with startup parameters.
    - `namespace:ZipStream`: This will generate zip files **on the fly**. It is pretty useful if you want to send someone a zip file and don't want to wait until the zip process is finished. This will generate the zip file while you send it! This supports large zip files (64bit) too.
        > Currently the zip file is uncompressed. I will add some compression algorithms later.
- `MaxLib.Maths`
    > Some old matrix and vector stuff I played with

    > **This will be removed in a future release!**
- `MaxLib.Net`
    - `namespace:ServerClient`: This allows direct communication between to runtime over the network. You can send single messages to the instance or receive ones.
    - `namespace:Webserver`: This is a fully functional and configurable Http Webserver. You only need to add this library and you have all you need. You can add you own handlers in Code. No need to write scripts on the disk. Because it runs in your code you can do what you want. This are some highlights of this server:
        - Http and Https transport. You only need to add your certificates
        - Http and Https on the **same** socket. The server will automatically distinguish which protocol is used.
        - Multipart Ranges. The server will only send that parts that are requested.
        - Local file support. You can add filter (white- and blacklist) to support this. A single url path can be mapped to multiple local paths.
        - Lazy. The response is created while you send the response.
        - Marshal. You can send content from another AppDomain.
        - Sessions. It will automatically manage sessions
        - Extended local file selector.
        - Service. Everything is a service. You can orchestrate the services you like and add you own.
- `MaxLib.Tools`
    - `namespace:SolutionFinder`: You give a problem and some small steps to solve it. This wil try to find a solution with it.
        > This is experimental.
        
        > *This may be removed in a future release*
    - `namespace:Watcher`
        - `class:ProcessWatcher`: This will watch the CPU and RAM usage of any given process and make the data easily accessible.
- `MaxLib`
    - `class:Disposeable`: an abstract base class that expose the `IsDisposed` property and will automatically dispose the object in its finalizer if its not already disposed.
    - `class:FrameTimeCounter`: a small util to calculate the current FPS. Usefull in applications if heavy graphics.
    - `class:ILoadSaveAble`: an interface for classes that can store its data in a byte array or load their data from it. It is used by some internal libraries.
    - `class:SlicedDelay`: This wait an amount of time in small steps.
        > The `System.Threading.Tasks.Task.Delay(delay, cancelationToken)` provide the same functionality and works faster.
        
        > *This may be removed in a future release*

## Project MaxLib.Js

| Stats | Value |
|-|-|
| Framework | .NET Standard 2.0 |
| Dependencies | MaxLib, Jint |
| Systems | Windows, Linux? |

**Content:**

- `MaxLib.Net`
    - `namespace:ServerScripts`: An extension to the `MaxLib.Net.Webserver` that allows to execute special JavaScript files on the web server. Its output will be sent as response. The files could also contains special tags `<?JS code ?>` to wrap the JS code. The access to the internal application is strictly limited.

## Project MaxLib.Sqlite

| Stats | Value |
|-|-|
| Framework | .NET Standard 2.0 |
| Dependencies | MaxLib, System.Data.SQLite |
| Systems | Windows, Linux? |

**Content:**

- `MaxLib.DB`
    - `class:Database`: Wraps the sqlite database and make the access easier. It provides special mutex locks for transactions (the normal sqlite database class will fail if a transaction exists)
    - `class:Factory`: Utils to map results to classes and vice versa. No queries needed.
    - `class:AsyncTransaction`: `System.Threading.Tasks.Task` based transaction with a job queue. Each job is available for its completion. 
- `MaxLib.Net.Webserver`
    - `namespace:Files`: adds an extension to the `MaxLib.Net.Webserver.Files` namespace in `MaxLib`. This will provide some special buffering for the extended file listing.

## Project MaxLib.SystemDrawing

| Stats | Value |
|-|-|
| Framework | .NET Standard 2.0 |
| Dependencies | MaxLib, System.Drawing.Common |
| Systems | Windows, Linux? |

- `MaxLib.Collections`
    - `class:LineGridContainer<T>`: add support for the MaxLib.WinForms library. This will position elements in a grid
- `MaxLib.Maths`
    > Some old vector stuff I played with

    > **This will be removed in a future release!**
- `MaxLib.Net.Webserver`
    - `namespace:Files`: adds an extension to the `MaxLib.Net.Webserver.Files` namespace in `MaxLib`. Adds a factory to create icons for the files.

## Project MaxLib.WinForm

| Stats | Value |
|-|-|
| Framework | .NET Framework 4.8 |
| Dependencies | MaxLib, MaxLib.SystemDrawing, System.Windows.Forms |
| Systems | Windows, Mono |

- `MaxLib.Collection`
    - `class:BoxGridViewContainer`: allows you to render elements in a grid style
- `MaxLib.Console`
    - `namespace:ExtendedConsole`: create an own custom console as a form. This adds many handlers to visualize forms, menus and dialogs in a console manner (only text). As render engine the GDI+ is used.
- `MaxLib.Data`
    - `namespace:Config`: add GUI to edit the `MaxLib.Data.Config` in `MaxLib`
- `MaxLib.WinForms`
    - `class:BoxGridView`: GUI element that hosts the `MaxLib.Collection.BoxGridViewContainer`.
    - `class:DigitList`: GUI element that hosts multiple `MaxLib.WinForms.DigitViewer` and display whole strings of text in a segment display style.
    - `class:DigitViewer`: GUI element that display a single letter in a segment display style.
    - `class:DrawGround`: a GUI element that can support the free movement and zoom with the mouse of its whole content. Just click on the ground and drag. Instances that inherit this class can add support for movement of single elements.
    - `class:EditingLabel`: a GUI element that looks like a label. If you click on it you can edit it like a text box.
    - `class:RefreshingListBox`: this expose the `RefreshItem` function of `ListBox` globaly. With this single items can be redrawn instead the whole list.
    - `class:TablessControl`: this GUI element is like a TabControl but you can only see the tab navigation in design mode. At runtime you can only see the current selected tab.

## More projects?

Possibly. I write very often stuff in C# and sometimes like to add this to this library.

## Featured classes/utils/stuff?

This list is the stuff I want to highlight because I am a little proud of. Mostly I invested pretty much time to develop and maintain it. These are quite often used in my private work and don't like to miss it.

1. `MaxLib::MaxLib.Net.Webserver`. This is a giant. This can do pretty everything. First it was for fun and but later I added more and more to make it more capable. I have added a lot of extension to increase its worth. I may re-organize it later and make the code more reader friendly.

    I have included this in many of my projects. Mostly because its:
    - fast to implement, start and use
    - very small (other webservers are pretty large)
    - it can directly interact with my C# code of the project
    - on the fly configuration and while runtime

    I have never done a stress test on it. But it is capable of some users at the same time. Perfect for a local server.

2. `MaxLib::MaxLib.Data.ZipStream`. This thing is crazy. To develop this, I have read the official documentation for days. During development it failed a lot of times. Either I was to stupid to read the documentation or this documentation sucks (I have added it next to the source code).

    I have only developed because I want something that can create ZIP files on the fly and in C#. Nothing found. But now I have my own solution and it works.

    At the moment it only includes the raw files (no compression or encryption) but I am happy with this. Maybe I will add compression later.

3. `MaxLib.Sqlite::MaxLib.DB`. I have only developed this because every time I use an SQLite database I need to develop the same wrapper again and again. So, I have combined all the functions I need and created this. Then I have added some more stuff to make the work with it easier and friendlier.

    In conclusion: It would be pretty nice if System.Data.SQLite has this all on its own, but my stuff make it really easy.

4. `MaxLib::MaxLib.Data.IniFiles`. Small and pretty useful. Use it very often to write or read configurations. This can parse most configuration files and use their data.

5. and more. I have created so much nice stuff to make my life more comfortable and some just for fun. I will never stop to work with this project.

## Future Releases

I will continue to maintain this library. Any major extensions with backward capabilitywill will just change the minor version number (e.g. `1.1` to `1.2`). If I decide to remove stuff and break backward capability I will just create a major release. In the past I have never touched the version number but this will now change.

My next goals are to refactor the code basis more and make it more readable and manageable.
