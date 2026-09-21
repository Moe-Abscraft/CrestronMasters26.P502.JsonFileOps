using System.Text.Json;
using System.Text.Json.Serialization;
using Crestron.SimplSharp;
using CrestronIO = Crestron.SimplSharp.CrestronIO;

namespace CrestronMasters26.P502.JsonFileOps
{
    internal class ConfigManager
    {
        private CCriticalSection _lock = new CCriticalSection();
        private readonly string _filePath;
        public string FilePath { get => _filePath; }
        public RoomConfig RoomConfig { get; private set; } = new RoomConfig();
        public event EventHandler<RoomConfig>? ConfigChanged;
        private CTimer _configReadTimer;
        private DateTime _loadedWriteTime;
        public ConfigManager(string fileName, string subFolder)
        {
            string root = CrestronEnvironment.DevicePlatform == eDevicePlatform.Appliance
                ? "/user"                                                                    // 4-Series
                : Path.Combine(CrestronIO.Directory.GetApplicationRootDirectory(), "user"); // VC-4

            string dir = string.IsNullOrEmpty(subFolder) ? root : Path.Combine(root, subFolder);
            Directory.CreateDirectory(dir);
            _filePath = Path.Combine(dir, fileName);
        }

        public RoomConfig Load()
        {
            ErrorLog.Notice("[JsonFileOps] loading config from {0}", _filePath);
            RoomConfig config;

            _lock.Enter();
            try
            {
                if (!File.Exists(_filePath))
                {
                    ErrorLog.Notice("[JsonFileOps] no config at {0} — writing defaults", _filePath);
                    config = CreateDefault();
                    WriteConfig(config);
                }
                else
                {
                    string json = File.ReadAllText(_filePath);
                    _loadedWriteTime = File.GetLastWriteTimeUtc(_filePath);

                    RoomConfig? parsed = JsonSerializer.Deserialize<RoomConfig>(json);

                    if (parsed == null)
                    {
                        ErrorLog.Error("[JsonFileOps] {0} is empty — running on DEFAULTS", _filePath);
                        config = CreateDefault();
                    }
                    else
                    {
                        config = parsed;
                    }
                }
            }
            catch (Exception e)
            {
                ErrorLog.Error("[JsonFileOps] could not read {0}: {1} — running on DEFAULTS", _filePath, e.Message);
                config = CreateDefault();
            }
            finally
            {
                _lock.Leave();
            }

            config.LastReadTime = DateTime.Now;

            RoomConfig = config;

            // Outside the lock: a subscriber that calls back into Load()/Save() must not deadlock.
            ConfigChanged?.Invoke(this, config);

            return config;
        }

        private void Save(RoomConfig config)
        {
            _lock.Enter();

            try
            {
                WriteConfig(config);
            }
            catch (Exception e)
            {
                ErrorLog.Error("ConfigManager.Save failed: {0}", e.Message);
            }
            finally
            {
                _lock.Leave();
            }
        }

        private void WriteConfig(RoomConfig config)
        {
            File.WriteAllText(_filePath, JsonSerializer.Serialize(config, new JsonSerializerOptions() { WriteIndented = true }));
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
                new SourceInfo { Name = "Laptop HDMI",   NvxIpid = 3 },
                new SourceInfo { Name = "Room PC",       NvxIpid = 4 },
                new SourceInfo { Name = "Wireless BYOD", NvxIpid = 5 }
            },

                Displays = new List<DisplayInfo>
            {
                new DisplayInfo { Name = "Left",  Model = "NEC",   NvxIpid = 16 },
                new DisplayInfo { Name = "Right", Model = "Sharp", NvxIpid = 17 }
            },

                Presets = new List<string> { "Presentation", "Video Conference" }
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
