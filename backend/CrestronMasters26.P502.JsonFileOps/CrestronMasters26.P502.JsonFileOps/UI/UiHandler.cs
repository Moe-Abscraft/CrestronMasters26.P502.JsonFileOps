using AppContract;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.DeviceSupport;

namespace CrestronMasters26.P502.JsonFileOps
{
    internal sealed partial class UiHandler : IDisposable
    {
        private readonly object _lock = new object();
        private readonly List<BasicTriListWithSmartObject> _uiList = new List<BasicTriListWithSmartObject>();
        private readonly Contract _contract;
        private RoomConfig? _roomConfig;
        private readonly ConfigManager _configManager;
        private readonly DeviceEmulator _emulator;
        private readonly PresetStore _presetStore;
        private bool _autoUpdateEnabled;
        private bool _disposed;

        public UiHandler(ConfigManager configManager, DeviceEmulator emulator)
        {
            _configManager = configManager;
            _emulator = emulator;
            _contract = new Contract();

            // Same folder as roomConfig.json.
            _presetStore = new PresetStore(Path.Combine(Path.GetDirectoryName(configManager.FilePath)!, "preset.json"));

            // Contract events belong to the Contract, not to any one panel, so they are
            // subscribed once here. Left in AddUi they get re-subscribed per panel, and
            // two panels means every press handled twice.
            InitializeSourcesContract();
            InitializeDisplaysContract();
            InitializeAudioContract();
            InitializeSystemContract();
            InitializePresetsContract();

            // The two things that can change underneath the panel: the file on disk, and
            // the hardware. Both report back the same way, and both land in feedback.
            // Named methods, not lambdas: Dispose has to unsubscribe them, and a lambda
            // can't be unsubscribed because there's no way to name it again.
            _configManager.ConfigChanged += ConfigManager_ConfigChanged;
            _emulator.RoutingChanged += Emulator_RoutingChanged;
            _emulator.AudioChanged += Emulator_AudioChanged;
        }

        // The file watcher and the hardware call in on their own threads, so they go
        // through Run and take the same lock as a button press.
        private void ConfigManager_ConfigChanged(object? sender, RoomConfig config)
        {
            UpdateFeedback(config);
        }

        private void Emulator_RoutingChanged(object? sender, EventArgs args)
        {
            Run("Emulator.RoutingChanged", UpdateRoutingFeedback);
        }

        private void Emulator_AudioChanged(object? sender, EventArgs args)
        {
            Run("Emulator.AudioChanged", UpdateAudioFeedback);
        }

        public void AddUi(BasicTriListWithSmartObject ui)
        {
            if (!_uiList.Contains(ui))
            {
                _uiList.Add(ui);
                _contract.AddDevice(ui);
            }
        }

        /// <summary>Push everything: startup, a reload, or a panel coming online.</summary>
        public void UpdateFeedback(RoomConfig? roomConfig)
        {
            ErrorLog.Notice("[JsonFileOps] UpdateFeedback: {0} source(s), {1} display(s), {2} panel(s)",
                roomConfig?.Sources.Count, roomConfig?.Displays.Count, _uiList.Count);

            if (roomConfig == null)
            {
                return;
            }

            // Called from startup, from a reload on the timer thread and from a panel coming
            // online - possibly all at once. Run makes them take turns with the presses.
            Run(nameof(UpdateFeedback), () =>
            {
                _roomConfig = roomConfig;

                // The reloaded file may be shorter than the one the selection was made against.
                _emulator.ClampSelectedSource(roomConfig.Sources.Count);

                RestoreRoutedSourceFromSelection();

                UpdateSourceFeedback();
                UpdateSelectedSourceFeedback();
                UpdateDisplaysFeedback();
                UpdateAudioFeedback();
                UpdateSystemFeedback();
            });
        }

        // ---------------------------------------------------------------- join helpers

        // Each helper turns "what the button does" into the EventHandler the contract wants,
        // so every Initialize...Contract reads as one line per join:
        //     OnPress  - digital, runs once on the press (true) and ignores the release
        //     OnHold   - digital, runs on both edges and hands over the state
        //     OnAnalog - analog, runs with the value the slider landed on
        //     OnSerial - serial, runs with the text
        // They all end in Run, so one lock and one try/catch cover every join.

        private EventHandler<UIEventArgs> OnPress(string buttonName, Action action)
        {
            return (sernder, args) =>
            {
                if (args.SigArgs.Sig.BoolValue)
                    Run(buttonName, action);
            };
        }

        private EventHandler<UIEventArgs> OnHold(string buttonName, Action<bool> action)
        {
            return (sernder, args) =>
            {
                var held = args.SigArgs.Sig.BoolValue;
                Run(buttonName, () => action(held));
            };
        }

        private EventHandler<UIEventArgs> OnAnalog(string buttonName, Action<ushort> action)
        {
            return (sernder, args) =>
            {
                var value = args.SigArgs.Sig.UShortValue;
                Run(buttonName, () => action(value));
            };
        }

        private EventHandler<UIEventArgs> OnSerial(string buttonName, Action<string> action)
        {
            return (sernder, args) =>
            {
                var value = args.SigArgs.Sig.StringValue;
                Run(buttonName, () => action(value));
            };
        }

        private void Run(string buttonName, Action action)
        {
            try
            {
                lock(_lock)
                {
                    // A press can already be queued on a pool thread when the program stops.
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

        /// <summary>
        /// Undo everything the constructor and AddUi did. The config manager and the
        /// emulator outlive this object and their events hold a reference back to it:
        /// without the -= a disposed UiHandler keeps getting events and pushing to panels.
        /// </summary>
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

            // Drops every handler the Initialize...Contract methods attached.
            _contract.Dispose();
        }
    }
}
