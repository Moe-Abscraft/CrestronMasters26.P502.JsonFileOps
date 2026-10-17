using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.UI;
using Thread = Crestron.SimplSharpPro.CrestronThread.Thread;

namespace CrestronMasters26.P502.JsonFileOps
{
    #region Read me: the four lines in this file that matter for JSON and file IO
    // 1. CrestronInvoke.BeginInvoke(StartupConfig, null)
    //        Start the real work on another thread.
    // 2. new ConfigManager("roomConfig.json", subFolderName)
    //        Which file, and which folder.
    // 3. _configManager.Load()
    //        Read the file and get back a RoomConfig object.
    // 4. new XpanelForHtml5(_roomConfig.XPanelIpId, this)
    //        Use a value from the file where a hard-coded number used to be.
    #endregion
    public class ControlSystem : CrestronControlSystem
    {
        #region Fields
        private ConfigManager? _configManager;
        private RoomConfig? _roomConfig;
        private XpanelForHtml5? _xpanel;
        private UiHandler? _uiHandler;
        private DeviceEmulator? _emulator;
        #endregion

        #region Constructor: runs first, keep it fast (not JSON)
        // The constructor runs first. Keep it fast.
        // We only set the thread limit and hook up the system events here.
        public ControlSystem() : base()
        {
            Thread.MaxNumberOfUserThreads = 20;

            CrestronEnvironment.SystemEventHandler += CrestronEnvironment_SystemEventHandler;
            CrestronEnvironment.ProgramStatusEventHandler += CrestronEnvironment_ProgramStatusEventHandler;
            CrestronEnvironment.EthernetEventHandler += CrestronEnvironment_EthernetEventHandler;
        }
        #endregion

        // Runs next. Start the real work on another thread.
        public override void InitializeSystem()
        {
            #region Log the .NET runtime
            ErrorLog.Notice($"Runtime {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
            #endregion

            // InitializeSystem runs next. The real work goes on another thread,
            // so we don't block the main thread while we read files.
            CrestronInvoke.BeginInvoke(StartupConfig, null);
        }

        // Load the config, create the parts, register the panel.
        private void StartupConfig(object? o)
        {
            #region subFolderName: one folder per program slot (slot 1 = "program01")
            // One subfolder per program slot. For processors with multiple program slots, this allows each slot to have its own config file.
            // Best practice is to store the config files inside the user folder, NOT NVRAM.
            // User folder in Appliance Processors are shared between all program slots, so each slot needs its own subfolder to avoid conflicts.
            // User folder in VC-4 is separated per ROOM, so no need for subfolders.
            string subFolderName = "program" + ProgramNumber.ToString("D2");
            #endregion

            // Which file, and which folder.
            _configManager = new ConfigManager("roomConfig.json", subFolderName);

            // Read the file. We get back a RoomConfig object.
            _roomConfig = _configManager.Load();

            #region Device emulator and UI handler (not JSON)
            // Fake switcher and DSP. Replace with real drivers later.
            _emulator = new DeviceEmulator();

            // UiHandler handles all panel buttons press and feedback.
            _uiHandler = new UiHandler(_configManager, _emulator);
            #endregion

            // The XPanel IPID isn't hard-coded anymore. It comes from the file.
            // That's the whole point of today.
            _xpanel = new XpanelForHtml5(_roomConfig.XPanelIpId, this);

            #region Register the panel and send it the current state (not JSON)
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
            #endregion
        }

        #region System events: log them, and clean up when the program stops (not JSON)
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
