using AppContract;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.DeviceSupport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrestronMasters26.P502.JsonFileOps.UI
{
    internal partial class UiHandler
    {
        List<BasicTriListWithSmartObject> _uiList = new List<BasicTriListWithSmartObject>();
        Contract _contract;
        RoomConfig? _roomConfig;
        ConfigManager? _configManager;
        int _selectedSource;
        bool _autoUpdateEnabled;
        public UiHandler(ConfigManager configManager)
        {
            _contract = new Contract();
            _configManager = configManager;
            _configManager.ConfigChanged += (sender, config) => UpdateUi(config);
        }

        public void AddUi(BasicTriListWithSmartObject ui)
        {
            if (!_uiList.Contains(ui))
            {
                _uiList.Add(ui);
                _contract.AddDevice(ui);

                InitializeSourcesContract();
                InitializeSystemContract();
            }
        }

        public void UpdateUi(RoomConfig? roomConfig)
        {
            ErrorLog.Notice("[JsonFileOps] UpdateUi: {0} source(s), {1} display(s), {2} panel(s)",
                roomConfig?.Sources.Count, roomConfig?.Displays.Count, _uiList.Count);

            if (roomConfig == null)
            {
                return;
            }

            _roomConfig = roomConfig;

            for (int i = 0; i < _contract.Displays.Display.Length; i++)
            {
                DisplayInfo? display = i < roomConfig.Displays.Count ? roomConfig.Displays[i] : null;
                string name = display?.Name ?? string.Empty;
                string model = display?.Model ?? string.Empty;

                _contract.Displays.Display[i].Name((sig, d) => sig.StringValue = name);
                _contract.Displays.Display[i].Model((sig, d) => sig.StringValue = model);
            }

            for (int i = 0; i < _contract.Sources.Source.Length; i++)
            {
                string name = i < roomConfig.Sources.Count ? roomConfig.Sources[i].Name : string.Empty;
                _contract.Sources.Source[i].Name((sig, s) => sig.StringValue = name);
            }

            if (_selectedSource > roomConfig.Sources.Count)
                _selectedSource = 0;          // the reloaded file has fewer sources than before

            PushRouting();

            _contract.System.LastUpdatedTime((sig, d) => sig.StringValue = roomConfig.LastReadTime.ToString("yyyy-MM-dd HH:mm:ss"));
        }

        private void PushRouting()
        {
            string routed = _selectedSource == 0 || _roomConfig == null
                ? string.Empty
                : _roomConfig.Sources[_selectedSource - 1].Name;

            for (int i = 0; i < _contract.Displays.Display.Length; i++)
                _contract.Displays.Display[i].RoutedSource((sig, d) => sig.StringValue = routed);

            ushort selected = (ushort)_selectedSource;
            _contract.Sources.Selected((sig, s) => sig.UShortValue = selected);
        }
    }
}
