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
    internal class UiHandler
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
                _contract.System.ReloadConfig += System_ReloadConfig;
                _contract.Sources.Select += Sources_Select;
                _contract.System.AutoUpdate += System_AutoUpdate;
            }
        }

        private void System_AutoUpdate(object? sender, UIEventArgs e)
        {
            if(e.SigArgs.Sig.BoolValue)
            {
                _autoUpdateEnabled = !_autoUpdateEnabled;
                _configManager?.EnableAutoUpdate(_autoUpdateEnabled);
            }

            _contract.System.AutoUpdate_Fb((sig, d) => sig.BoolValue = _autoUpdateEnabled);
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

        private void Sources_Select(object? sender, UIEventArgs e)
        {
            int src = e.SigArgs.Sig.UShortValue;

            if (_roomConfig == null || src < 0 || src > _roomConfig.Sources.Count)
                return;

            _selectedSource = src;

            // Real system: call your NVX routing here. This example fakes the feedback.
            string routed = src == 0 ? string.Empty : _roomConfig.Sources[src - 1].Name;
            _roomConfig.Displays.ForEach(d => d.RoutedSource = routed);

            PushRouting();
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

        private void System_ReloadConfig(object? sender, UIEventArgs e)
        {
            if (e.SigArgs.Sig.BoolValue)
                _configManager?.Load();
        }
    }
}
