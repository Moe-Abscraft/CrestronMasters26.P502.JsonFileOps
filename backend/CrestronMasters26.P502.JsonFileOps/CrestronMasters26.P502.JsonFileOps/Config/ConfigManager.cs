using System.Text.Json;
using Crestron.SimplSharp;
using CrestronIO = Crestron.SimplSharp.CrestronIO;

namespace CrestronMasters26.P502.JsonFileOps
{
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
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            AllowTrailingCommas = true
        };
        public ConfigManager(string fileName, string subFolder)
        {
            string root = CrestronEnvironment.DevicePlatform == eDevicePlatform.Appliance
                ? "/user"                                                                    // 4-Series
                : Path.Combine(CrestronIO.Directory.GetApplicationRootDirectory(), "user"); // VC-4

            string dir = !string.IsNullOrEmpty(subFolder) && CrestronEnvironment.DevicePlatform == eDevicePlatform.Appliance ? Path.Combine(root, subFolder) : root;
            Directory.CreateDirectory(dir);
            _folder = dir;
            _filePath = Path.Combine(dir, fileName);
        }

        /// <summary>Switch to another file in the same folder and load it. Reload and
        /// auto-update follow the new file from then on.</summary>
        public RoomConfig Load(string fileName)
        {
            _filePath = Path.Combine(_folder, fileName);
            return Load();
        }

        public RoomConfig Load()
        {
            ErrorLog.Notice("[JsonFileOps] loading config from {0}", _filePath);

            // Stays null unless the file is read successfully.
            RoomConfig? config = null;

            // The UI shows this message. Empty means everything is fine.
            StatusMessage = string.Empty;

            // Enter/Leave in a try/finally: Leave runs even if something throws,
            // otherwise the lock stays taken and the next Load() waits forever.
            _lock.Enter();
            try
            {
                // 1. Try to read the file.
                try
                {
                    if (File.Exists(_filePath))
                    {
                        string json = File.ReadAllText(_filePath);
                        config = JsonSerializer.Deserialize<RoomConfig>(json, JsonOptions);

                        // Valid JSON, but the lists we need are missing.
                        if (config == null || config.Sources == null || config.Displays == null)
                        {
                            config = null;
                            StatusMessage = "Config file has no sources or displays — running on defaults";
                        }
                    }
                    else
                    {
                        StatusMessage = $"No config found — default written to {_filePath}";
                    }
                }
                catch (Exception e)
                {
                    // Bad JSON ends up here.
                    StatusMessage = $"Config error: {e.Message} — running on defaults";
                }

                // 2. Nothing usable was read: use the defaults and write them to the file.
                if (config == null)
                {
                    ErrorLog.Error("[JsonFileOps] {0}", StatusMessage);
                    config = CreateDefault();

                    try
                    {
                        string json = JsonSerializer.Serialize(config, JsonOptions);
                        File.WriteAllText(_filePath, json);
                    }
                    catch (Exception e)
                    {
                        ErrorLog.Error("[JsonFileOps] could not write default config: {0}", e.Message);
                    }
                }

                // 3. Remember the file time, or auto-update thinks the file changed.
                _loadedWriteTime = File.GetLastWriteTimeUtc(_filePath);
            }
            finally
            {
                _lock.Leave();
            }

            config.LastReadTime = DateTime.Now;

            RoomConfig = config;

            // Outside the lock: a subscriber that calls back into Load() must not deadlock.
            ConfigChanged?.Invoke(this, config);

            return config;
        }

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
