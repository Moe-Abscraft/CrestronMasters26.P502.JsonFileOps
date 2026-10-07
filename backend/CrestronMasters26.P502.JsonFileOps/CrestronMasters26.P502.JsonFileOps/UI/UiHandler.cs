using AppContract;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.DeviceSupport;

namespace CrestronMasters26.P502.JsonFileOps
{
    // All panel code. One file per contract component: Sources, Displays, Audio, Presets, System.
    // Each file has:
    //   Initialize...Contract   - what each button does
    //   Update...ConfigFeedback - panel updates from the config file (names, status)
    //   Update...StateFeedback  - panel updates from buttons and the emulator (routes, audio)
    // The config is read-only here. Runtime state (routes, levels) lives in the emulator.
    internal sealed partial class UiHandler : IDisposable
    {
        private readonly object _lock = new object();
        private readonly List<BasicTriListWithSmartObject> _uiList = new List<BasicTriListWithSmartObject>();
        private readonly Contract _contract;
        private readonly ConfigManager _configManager;
        private readonly DeviceEmulator _emulator;
        private readonly PresetStore _presetStore;
        private RoomConfig _roomConfig;
        private bool _autoUpdateEnabled;
        private bool _disposed;

        public UiHandler(ConfigManager configManager, DeviceEmulator emulator)
        {
            _configManager = configManager;
            _emulator = emulator;
            _contract = new Contract();

            // Never null: ConfigManager always has a config (defaults if the file is bad).
            _roomConfig = configManager.RoomConfig;

            // preset.json sits next to roomConfig.json.
            _presetStore = new PresetStore(Path.Combine(Path.GetDirectoryName(configManager.FilePath)!, "preset.json"));

            // Hook up the buttons once, here (not in AddUi).
            InitializeSourcesContract();
            InitializeDisplaysContract();
            InitializeAudioContract();
            InitializeSystemContract();
            InitializePresetsContract();

            // Update the panel when the file or the device changes.
            // Named methods so Dispose can unsubscribe them.
            _configManager.ConfigChanged += ConfigManager_ConfigChanged;
            _emulator.RoutingChanged += Emulator_RoutingChanged;
            _emulator.AudioChanged += Emulator_AudioChanged;
        }

        // ---------------------------------------------------------------- events

        // These run on other threads, so they go through Run (uses the lock).

        // The config file was (re)loaded.
        private void ConfigManager_ConfigChanged(object? sender, RoomConfig config)
        {
            Run("ConfigManager.ConfigChanged", () =>
            {
                _roomConfig = config;

                // New config = start clean: no source selected, all displays cleared.
                _emulator.SelectSource(0);
                foreach (DisplayInfo d in config.Displays)
                    _emulator.Route(d.Id, 0);

                UpdateSourcesConfigFeedback();
                UpdateDisplaysConfigFeedback();
                UpdateSystemConfigFeedback();

                // Sent here too: nothing is raised if routes were already cleared.
                UpdateSourcesStateFeedback();
                UpdateDisplaysStateFeedback();
            });
        }

        // Device events only update the panel. They never change the config.
        private void Emulator_RoutingChanged(object? sender, EventArgs args)
        {
            Run("Emulator.RoutingChanged", () =>
            {
                UpdateSourcesStateFeedback();
                UpdateDisplaysStateFeedback();
            });
        }

        private void Emulator_AudioChanged(object? sender, EventArgs args)
        {
            Run("Emulator.AudioChanged", UpdateAudioStateFeedback);
        }

        // ---------------------------------------------------------------- panels

        // Add a panel so it gets feedback.
        public void AddUi(BasicTriListWithSmartObject ui)
        {
            if (!_uiList.Contains(ui))
            {
                _uiList.Add(ui);
                _contract.AddDevice(ui);
            }
        }

        /// <summary>Send everything to the panel. Use at startup and when a panel comes online.</summary>
        public void UpdateAllFeedback()
        {
            Run(nameof(UpdateAllFeedback), () =>
            {
                UpdateSourcesConfigFeedback();
                UpdateDisplaysConfigFeedback();
                UpdateSystemConfigFeedback();

                UpdateSourcesStateFeedback();
                UpdateDisplaysStateFeedback();
                UpdateAudioStateFeedback();
                UpdateSystemStateFeedback();
            });
        }

        // ---------------------------------------------------------------- join helpers

        // Helpers to hook up a join in one line:
        //   OnPress  - digital, runs on press only
        //   OnHold   - digital, runs on press and release
        //   OnAnalog - analog, gives the value

        private EventHandler<UIEventArgs> OnPress(string buttonName, Action action)
        {
            return (sender, args) =>
            {
                if (args.SigArgs.Sig.BoolValue)
                    Run(buttonName, action);
            };
        }

        private EventHandler<UIEventArgs> OnHold(string buttonName, Action<bool> action)
        {
            return (sender, args) =>
            {
                var held = args.SigArgs.Sig.BoolValue;
                Run(buttonName, () => action(held));
            };
        }

        private EventHandler<UIEventArgs> OnAnalog(string buttonName, Action<ushort> action)
        {
            return (sender, args) =>
            {
                var value = args.SigArgs.Sig.UShortValue;
                Run(buttonName, () => action(value));
            };
        }

        // Run an action with the lock and a try/catch.
        private void Run(string buttonName, Action action)
        {
            try
            {
                lock (_lock)
                {
                    // Program is stopping: do nothing.
                    if (_disposed)
                        return;

                    action();
                }
            }
            catch (Exception ex)
            {
                ErrorLog.Error("UI Join {0} failed: {1}", buttonName, ex.Message);
            }
        }

        // ---------------------------------------------------------------- dispose

        /// <summary>Clean up: unsubscribe events and remove panels.</summary>
        public void Dispose()
        {
            lock (_lock)
            {
                if (_disposed)
                    return;

                _disposed = true;
            }

            _configManager.ConfigChanged -= ConfigManager_ConfigChanged;
            _emulator.RoutingChanged -= Emulator_RoutingChanged;
            _emulator.AudioChanged -= Emulator_AudioChanged;

            foreach (BasicTriListWithSmartObject ui in _uiList)
                _contract.RemoveDevice(ui);

            _uiList.Clear();

            // Removes all button handlers.
            _contract.Dispose();
        }
    }
}
