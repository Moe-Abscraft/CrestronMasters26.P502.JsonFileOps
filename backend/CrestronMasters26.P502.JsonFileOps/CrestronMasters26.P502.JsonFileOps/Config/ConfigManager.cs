//using System.Text.Json;
using Newtonsoft.Json;
using Crestron.SimplSharp;
using CrestronIO = Crestron.SimplSharp.CrestronIO;

namespace CrestronMasters26.P502.JsonFileOps
{
    #region Read me first: Steps that every JSON config file follows
    // 1. Read the text from the file            File.ReadAllText
    // 2. Deserialize: JSON text -> object       JsonConvert.DeserializeObject   (reading in)
    // 3. Use the object, maybe change it
    // 4. Serialize: object -> JSON text         JsonConvert.SerializeObject     (writing out)
    // 5. Write the text to the file             File.WriteAllText
    //
    // Load() below does 1 and 2 when the file exists.
    // On the first run there is no file, so it builds the defaults and does 4 and 5.
    //
    // The functions you need:
    //   File.Exists          is the file there?
    //   File.ReadAllText     read the whole file as one string
    //   File.WriteAllText    write one string as the whole file
    //   Path.Combine         build a path. Never glue folders together with slashes yourself.
    //   Directory.GetApplicationRootDirectory   where the program lives on the processor
    #endregion

    #region Read me: Newtonsoft.Json or System.Text.Json?
    // Newtonsoft.Json   a NuGet package. Works on .NET 4.8 and on .NET 8. Used in this file,
    //                   because a lot of us are still on .NET 4.8.
    // System.Text.Json  built into .NET 8, nothing to install. The matching lines are left in
    //                   this file, commented out. Swap them and it works the same way.
    //
    // One warning. Newtonsoft is forgiving: it reads single quotes, a trailing comma and
    // comments anyway. Don't rely on that. System.Text.Json, online validators and almost
    // everything else reject them. Write strict JSON.
    #endregion

    internal class ConfigManager
    {
        #region Fields and properties
        private readonly CCriticalSection _lock = new CCriticalSection();
        private readonly string _folder;
        private string _filePath;
        public string FilePath { get => _filePath; }
        public RoomConfig RoomConfig { get; private set; } = new RoomConfig();
        public event EventHandler<RoomConfig>? ConfigChanged;
        private CTimer? _configReadTimer;
        private DateTime _loadedWriteTime;
        public String StatusMessage { get; private set; } = string.Empty;
        public bool MessageIsEnabled => StatusMessage.Length > 0;
        #endregion

        #region JsonSettings: created once, reused for every read and write
        // JsonSerializerOptions is for System.Text.Json, which is built into .NET 8.0.
        //private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        //{
        //    WriteIndented = true,
        //    AllowTrailingCommas = true
        //};

        // JsonSerializerSettings is for Newtonsoft.Json, which is a separate NuGet package.
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            MissingMemberHandling = MissingMemberHandling.Ignore,

            // "sources": null in the file keeps the empty list instead of setting it to null.
            NullValueHandling = NullValueHandling.Ignore
        };
        #endregion

        public ConfigManager(string fileName, string subFolder)
        {
            #region Read me: where does the file go? It depends on the platform
            // Best practice: keep config files in the user folder, NOT in NVRAM.
            // VC-4:      every room already has its own user folder, so we just use that.
            // 4-Series:  the user folder is shared by all program slots, so each slot gets its own
            //            subfolder. That's why ControlSystem passed "program01".
            // Check if the program is running on a VC-4 or a 4-Series processor. This affects where the config file is stored.
            bool isVc4 = CrestronEnvironment.DevicePlatform == eDevicePlatform.Server;
            #endregion

            if (isVc4)
            {
                var appRoot = CrestronIO.Directory.GetApplicationRootDirectory();
                _folder = Path.Combine(appRoot, "user");
            }
            #region 4-Series: /user is shared by all program slots, so we use /user/program01
            else
            {
                if (subFolder != null)
                    _folder = Path.Combine("/user", subFolder);
                else
                    _folder = "/user";
            }

            // same as above, but shorter. The ternary operator is less readable, so I expanded it above.
            //_folder = CrestronEnvironment.DevicePlatform == eDevicePlatform.Server
            //    ? Path.Combine(CrestronIO.Directory.GetApplicationRootDirectory(), "user")
            //    : Path.Combine("/user", subFolder ?? "");
            #endregion

            #region Make sure the folder exists, then build the full file path
            // Path.Combine joins the folder and the file name. Never glue paths together with slashes yourself.
            Directory.CreateDirectory(_folder);
            _filePath = Path.Combine(_folder, fileName);
            #endregion
        }

        #region Load(fileName): same code, different file (Separate / Combine)
        // The Separate and Combine buttons call this with roomConfig_Separate.json or roomConfig_Combine.json.
        /// <summary>Load a different file from the same folder.</summary>
        public RoomConfig Load(string fileName)
        {
            _filePath = Path.Combine(_folder, fileName);
            return Load();
        }
        #endregion

        /// <summary>Load the current file.</summary>
        public RoomConfig Load()
        {
            #region Log it, and start clean: no config yet, no error message
            ErrorLog.Notice("[JsonFileOps] loading config from {0}", _filePath);

            // Stays null unless the file is read successfully.
            RoomConfig? config = null;

            // The UI shows this message. Empty means everything is fine.
            StatusMessage = string.Empty;
            #endregion

            #region Read me: doing it safely (the lock, the deadlock, power loss)
            // Event handlers in SIMPL# Pro run on different threads. A button press can call Load at
            // the exact same moment the auto-update timer calls Load. Two threads in the same file at
            // once means corrupted data or an exception.
            //
            // The fix is a lock. In Crestron that's CCriticalSection (the same idea as the C# lock).
            // If you've done SIMPL+:  Enter() = StartFileOperations,  Leave() = EndFileOperations.
            // Only one thread gets in at a time. Everyone else waits.
            //
            // Leave() goes in finally. Always. If Leave() sits at the end of the try and something
            // throws, we never reach it. The lock is never released, and the next Load waits forever.
            // That's a deadlock: the program just stops responding.
            //
            // Power loss is the other risk. File.WriteAllText empties the file first, then writes.
            // If the power drops in between, you're left with an empty config. The production fix is
            // to write to a temp file, then move it over the real one. This project doesn't do that,
            // but Load() does treat an empty file like bad JSON and runs on defaults.
            #endregion

            // The lock: only one thread at a time. Enter here, Leave in finally.
            _lock.Enter();
            try
            {
                // 1. The file is there: read the text, then deserialize it (JSON text -> object).
                if (File.Exists(_filePath))
                {
                    try
                    {
                        string jsonText = File.ReadAllText(_filePath);

                        config = JsonConvert.DeserializeObject<RoomConfig>(jsonText, JsonSettings) ?? throw new Exception("file is empty");

                        #region System.Text.Json version of the line above
                        // If you want to use System.Text.Json instead of Newtonsoft.Json, uncomment the line below and comment out the line above.
                        //config = JsonSerializer.Deserialize<RoomConfig>(jsonText, JsonOptions);
                        #endregion
                    }
                    #region catch: bad JSON lands here. Show the error on the panel and run on defaults
                    catch (Exception e)
                    {
                        // We don't crash. The error goes in StatusMessage, and the panel shows it.
                        // We do NOT write the defaults over a broken file. Imagine a tech on site fixing a typo:
                        // if we overwrote their file, all their edits would be gone.
                        // Bad JSON ends up here.
                        StatusMessage = $"Config error: {e.Message} — running on defaults";
                        config = CreateDefault();
                    }
                    #endregion

                }
                else
                {
                    // 2. No file (first run): build the defaults, serialize them (object -> JSON text), write the file.
                    #region Status message and log
                    StatusMessage = $"No config found — default written to {_filePath}";
                    ErrorLog.Error(StatusMessage);
                    #endregion
                    try
                    {
                        config = CreateDefault();
                        // Convert the RoomConfig object to JSON text. If the object is invalid, this will throw an exception.
                        string jsonText = JsonConvert.SerializeObject(config, JsonSettings);

                        // If you want to use System.Text.Json instead of Newtonsoft.Json, uncomment the line below and comment out the line above.
                        //string json = JsonSerializer.Serialize(config, JsonOptions);

                        File.WriteAllText(_filePath, jsonText);
                    }
                    #region catch: could not write the default file
                    catch (Exception e)
                    {
                        ErrorLog.Error("[JsonFileOps] could not write default config: {0}", e.Message);
                    }
                    #endregion
                }

                #region 3. Remember the file's write time, so auto-update can tell when it changes
                // 3. Remember the file time, or auto-update thinks the file changed.
                _loadedWriteTime = File.GetLastWriteTimeUtc(_filePath);
                #endregion
            }
            #region catch: anything else that went wrong goes to the error log
            catch (Exception e)
            {
                ErrorLog.Error("[JsonFileOps] could not load config: {0}", e.Message);
            }
            #endregion
            finally
            {
                // finally runs no matter what happens, so the lock is always released.
                _lock.Leave();
            }

            #region Keep the config, tell the UI (outside the lock), and return it
            if(config == null)
            {
                // This should never happen, but if it does, we don't want to crash. We return a default config.
                ErrorLog.Error("[JsonFileOps] Load() returned null, using default config");
                config = CreateDefault();
            }
            config.LastReadTime = DateTime.Now;

            RoomConfig = config;

            // Tell the UI. Done outside the lock to avoid a deadlock.
            ConfigChanged?.Invoke(this, config);

            return config;
            #endregion
        }

        #region CreateDefault: the config we use on first run or when the file is bad
        // Default config for first run or a bad file.
        private static RoomConfig CreateDefault()
        {
            return new RoomConfig
            {
                RoomName = "Boardroom A",
                AutoShutdown = true,
                XPanelIpId = 0x30,

                Sources = new List<SourceInfo>
                {
                    new SourceInfo { Id = 1, Name = "Laptop HDMI",   NvxIpid = 3 },
                    new SourceInfo { Id = 2, Name = "Room PC",       NvxIpid = 4 },
                    new SourceInfo { Id = 3, Name = "Wireless BYOD", NvxIpid = 5 }
                },

                Displays = new List<DisplayInfo>
                {
                    new DisplayInfo { Id = 1, Name = "Left",  Model = "NEC",   NvxIpid = 16 },
                    new DisplayInfo { Id = 2, Name = "Right", Model = "Sharp", NvxIpid = 17 }
                }
            };
        }
        #endregion

        #region Auto-update: a timer checks the file's write time every second
        // The timer calls CheckFileTime every second.
        // If the write time changed since we loaded the file, we load it again.
        // That's why Load() remembers the write time in step 3.
        public void EnableAutoUpdate(bool enable)
        {
            if(enable)
            {
                if (_configReadTimer == null)
                {
                    _configReadTimer = new CTimer(_ => CheckFileTime(), null, 1000, 1000);
                }
            }
            else
            {
                if (_configReadTimer != null)
                {
                    _configReadTimer.Dispose();
                    _configReadTimer = null;
                }
            }
        }

        // Reload if the file time changed.
        private void CheckFileTime()
        {
            try
            {
                if (!File.Exists(_filePath)) return;

                DateTime lastWriteTime = File.GetLastWriteTimeUtc(_filePath);

                if (lastWriteTime != _loadedWriteTime)
                {
                    ErrorLog.Notice("[JsonFileOps] {0} changed on disk — reloading", _filePath);
                    Load();
                }
            }
            catch (Exception e)
            {
                ErrorLog.Error("[JsonFileOps] could not check file time for {0}: {1}", _filePath, e.Message);
            }
        }
        #endregion
    }
}
