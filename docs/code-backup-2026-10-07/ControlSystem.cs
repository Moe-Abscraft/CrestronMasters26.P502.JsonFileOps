using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.UI;
using Thread = Crestron.SimplSharpPro.CrestronThread.Thread;

namespace CrestronMasters26.P502.JsonFileOps
{
    public class ControlSystem : CrestronControlSystem
    {
        private ConfigManager? _configManager;
        private RoomConfig? _roomConfig;
        private XpanelForHtml5? _xpanel;
        private UiHandler? _uiHandler;
        private DeviceEmulator? _emulator;
        // Runs first. Keep it fast:
        public ControlSystem() : base()
        {
            Thread.MaxNumberOfUserThreads = 20;

            CrestronEnvironment.SystemEventHandler += CrestronEnvironment_SystemEventHandler;
            CrestronEnvironment.ProgramStatusEventHandler += CrestronEnvironment_ProgramStatusEventHandler;
            CrestronEnvironment.EthernetEventHandler += CrestronEnvironment_EthernetEventHandler;
        }

        // Runs next. Start the real work on another thread.
        public override void InitializeSystem()
        {
            ErrorLog.Notice($"Runtime {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");

            // Start the real work on another thread so we don't block the main thread.
            CrestronInvoke.BeginInvoke(StartupConfig, null);
        }

        // Load the config, create the parts, register the panel.
        private void StartupConfig(object? o)
        {
            // One subfolder per program slot. For processors with multiple program slots, this allows each slot to have its own config file.
            // Best practice is to store the config files inside the user folder, NOT NVRAM.
            // User folder in Appliance Processors are shared between all program slots, so each slot needs its own subfolder to avoid conflicts.
            // User folder in VC-4 is separated per ROOM, so no need for subfolders.
            string subFolderName = "program" + ProgramNumber.ToString("D2");
            _configManager = new ConfigManager("roomConfig.json", subFolderName);

            _roomConfig = _configManager.Load();

            // Fake switcher and DSP. Replace with real drivers later.
            _emulator = new DeviceEmulator();

            // UiHandler handles all panel buttons press and feedback.
            _uiHandler = new UiHandler(_configManager, _emulator);

            _xpanel = new XpanelForHtml5(_roomConfig.XPanelIpId, this);

            if (_xpanel.Register() != eDeviceRegistrationUnRegistrationResponse.Success)
            {
                ErrorLog.Error($"Failed to register XPanel with IP ID {_roomConfig.XPanelIpId}");
                return;
            }

            _uiHandler.AddUi(_xpanel);
            _uiHandler.UpdateAllFeedback();

            // Panel came online: send it the current state.
            _xpanel.OnlineStatusChange += (dev, args) =>
            {
                if (args.DeviceOnLine)
                    _uiHandler.UpdateAllFeedback();
            };
        }

        #region System Events
        private void CrestronEnvironment_SystemEventHandler(eSystemEventType systemEventType)
        {
            ErrorLog.Notice("[JsonFileOps] System Event: {0}", systemEventType.ToString());
        }

        private void CrestronEnvironment_ProgramStatusEventHandler(eProgramStatusEventType programEventType)
        {
            ErrorLog.Notice("[JsonFileOps] Program Status Event: {0}", programEventType.ToString());

            if (programEventType == eProgramStatusEventType.Stopping)
            {
                // Program stopping: stop the timer and clean up.
                _configManager?.EnableAutoUpdate(false);
                _uiHandler?.Dispose();
            }
        }

        private void CrestronEnvironment_EthernetEventHandler(EthernetEventArgs ethernetEventArgs)
        {
            ErrorLog.Notice("[JsonFileOps] Ethernet Event: {0}", ethernetEventArgs.EthernetEventType.ToString());
        }
        #endregion
    }
}
