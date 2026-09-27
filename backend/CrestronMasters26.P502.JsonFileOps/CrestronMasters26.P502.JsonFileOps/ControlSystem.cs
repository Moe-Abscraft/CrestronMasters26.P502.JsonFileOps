using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.UI;
using Thread = Crestron.SimplSharpPro.CrestronThread.Thread;

namespace CrestronMasters26.P502.JsonFileOps
{
    public class ControlSystem : CrestronControlSystem
    {
        private ConfigManager _configManager;
        private RoomConfig _roomConfig;
        private XpanelForHtml5 _xpanel;
        private UiHandler _uiHandler;
        private DeviceEmulator _emulator;
        public ControlSystem() : base()
        {
            Thread.MaxNumberOfUserThreads = 20;

            CrestronEnvironment.SystemEventHandler += CrestronEnvironment_SystemEventHandler;
            CrestronEnvironment.ProgramStatusEventHandler += CrestronEnvironment_ProgramStatusEventHandler;
            CrestronEnvironment.EthernetEventHandler += CrestronEnvironment_EthernetEventHandler;
        }

        public override void InitializeSystem()
        {
            // Initialization code here
            ErrorLog.Notice($"Runtime {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");

            CrestronInvoke.BeginInvoke(StartupConfig, null);
        }

        private void StartupConfig(object? o)
        {
            string slot = "program" + ProgramNumber.ToString("D2");
            _configManager = new ConfigManager("roomConfig.json", slot);

            _roomConfig = _configManager.Load();

            // Stands in for the switcher and the DSP. Replace it with real drivers and
            // the UI layer above it doesn't change.
            _emulator = new DeviceEmulator();

            // UiHandler wires its own presses. Nothing about routing, audio, reloading or
            // the contract belongs up here.
            _uiHandler = new UiHandler(_configManager, _emulator);

            _xpanel = new XpanelForHtml5(_roomConfig.XPanelIpId, this);

            if (_xpanel.Register() != eDeviceRegistrationUnRegistrationResponse.Success)
            {
                ErrorLog.Error($"Failed to register XPanel with IP ID {_roomConfig.XPanelIpId}");
                return;
            }

            _uiHandler.AddUi(_xpanel);
            _uiHandler.UpdateFeedback(_roomConfig);

            // A panel that was asleep at startup has no feedback until it asks for it.
            _xpanel.OnlineStatusChange += (dev, args) =>
            {
                if (args.DeviceOnLine)
                    _uiHandler.UpdateFeedback(_configManager.RoomConfig);
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
                // The file watcher's timer would keep firing into a stopped program.
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
