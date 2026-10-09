//using System.Text.Json;
using Newtonsoft.Json;
using Crestron.SimplSharp;
using CrestronIO = Crestron.SimplSharp.CrestronIO;

namespace CrestronMasters26.P502.JsonFileOps
{
    // Reads roomConfig.json. Bad or missing file = use defaults.
    // Can also watch the file and reload when it changes.
    internal class ConfigManager
    {
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

        #region JsonSettings
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
            // Check if the program is running on a VC-4 or a 4-Series processor. This affects where the config file is stored.
            bool isVc4 = CrestronEnvironment.DevicePlatform == eDevicePlatform.Server;

            if (isVc4)
            {
                // VC-4: each room already has its own user folder, so subFolder isn't needed.

                var appRoot = CrestronIO.Directory.GetApplicationRootDirectory();
                _folder = Path.Combine(appRoot, "user");
            }
            else
            {
                // 4-Series: /user is shared by all program slots, so each slot gets its own subfolder.

                if (subFolder != null)
                    _folder = Path.Combine("/user", subFolder);
                else
                    _folder = "/user";
            }

            // same as above, but shorter. The ternary operator is less readable, so I expanded it above.
            //_folder = CrestronEnvironment.DevicePlatform == eDevicePlatform.Server
            //    ? Path.Combine(CrestronIO.Directory.GetApplicationRootDirectory(), "user")
            //    : Path.Combine("/user", subFolder ?? "");

            Directory.CreateDirectory(_folder);
            _filePath = Path.Combine(_folder, fileName);
        }

        /// <summary>Load a different file from the same folder.</summary>
        public RoomConfig Load(string fileName)
        {
            _filePath = Path.Combine(_folder, fileName);
            return Load();
        }

        /// <summary>Load the current file.</summary>
        public RoomConfig Load()
        {
            ErrorLog.Notice("[JsonFileOps] loading config from {0}", _filePath);

            // Stays null unless the file is read successfully.
            RoomConfig? config = null;

            // The UI shows this message. Empty means everything is fine.
            StatusMessage = string.Empty;

            // Always release the lock in finally.
            _lock.Enter();
            try
            {
                // 1. Try to read the file.

                if (File.Exists(_filePath))
                {
                    try
                    {
                        string jsonText = File.ReadAllText(_filePath);

                        // Convert the JSON text to a RoomConfig object. If the JSON is invalid, this will throw an exception.
                        // An empty file returns null instead of throwing, so throw here and treat it like bad JSON.
                        config = JsonConvert.DeserializeObject<RoomConfig>(jsonText, JsonSettings)
                                 ?? throw new Exception("file is empty");

                        // If you want to use System.Text.Json instead of Newtonsoft.Json, uncomment the line below and comment out the line above.
                        //config = JsonSerializer.Deserialize<RoomConfig>(jsonText, JsonOptions);
                    }
                    catch (Exception e)
                    {
                        // Bad JSON ends up here.
                        StatusMessage = $"Config error: {e.Message} — running on defaults";
                        config = CreateDefault();
                    }

                }
                else
                {
                    StatusMessage = $"No config found — default written to {_filePath}";
                    ErrorLog.Error(StatusMessage);
                    try
                    {
                        config = CreateDefault();
                        // Convert the RoomConfig object to JSON text. If the object is invalid, this will throw an exception.
                        string jsonText = JsonConvert.SerializeObject(config, JsonSettings);

                        // If you want to use System.Text.Json instead of Newtonsoft.Json, uncomment the line below and comment out the line above.
                        //string json = JsonSerializer.Serialize(config, JsonOptions);

                        File.WriteAllText(_filePath, jsonText);
                    }
                    catch (Exception e)
                    {
                        ErrorLog.Error("[JsonFileOps] could not write default config: {0}", e.Message);
                    }
                }

                // 3. Remember the file time, or auto-update thinks the file changed.
                _loadedWriteTime = File.GetLastWriteTimeUtc(_filePath);
            }
            catch (Exception e)
            {
                ErrorLog.Error("[JsonFileOps] could not load config: {0}", e.Message);
            }
            finally
            {
                _lock.Leave();
            }

            config.LastReadTime = DateTime.Now;

            RoomConfig = config;

            // Tell the UI. Done outside the lock to avoid a deadlock.
            ConfigChanged?.Invoke(this, config);

            return config;
        }

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

        // Turn file watching on or off. Checks every second.
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
    }
}
