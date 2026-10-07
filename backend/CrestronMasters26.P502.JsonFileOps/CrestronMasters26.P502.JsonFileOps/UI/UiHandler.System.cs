using Crestron.SimplSharp;

namespace CrestronMasters26.P502.JsonFileOps
{
    internal sealed partial class UiHandler
    {
        private void InitializeSystemContract()
        {
            // Act on press only, so it doesn't run twice.
            // Load raises ConfigChanged, which updates the panel.
            _contract.System.ReloadConfig += OnPress("System.ReloadConfig", () => _configManager.Load());

            // Two room layouts, each with its own file.
            _contract.System.Separate += OnPress("System.Separate", () => _configManager.Load("roomConfig_Separate.json"));
            _contract.System.Combine += OnPress("System.Combine", () => _configManager.Load("roomConfig_Combine.json"));

            // Turn file watching on or off.
            _contract.System.AutoUpdate += OnPress("System.AutoUpdate", () =>
            {
                _autoUpdateEnabled = !_autoUpdateEnabled;
                _configManager.EnableAutoUpdate(_autoUpdateEnabled);
                UpdateSystemStateFeedback();
            });
        }

        // ---------------------------------------------------------------- config feedback

        // Room name, last read time and status message.
        private void UpdateSystemConfigFeedback()
        {
            string roomName = _roomConfig.RoomName ?? string.Empty;
            _contract.System.RoomName((sig, _) => sig.StringValue = roomName);

            string lastRead = _roomConfig.LastReadTime.ToString("yyyy-MM-dd HH:mm:ss");
            _contract.System.LastUpdatedTime((sig, _) => sig.StringValue = lastRead);

            // Always send the status. Empty clears the message on the panel.
            string status = _configManager.StatusMessage;
            _contract.System.Message((sig, _) => sig.StringValue = status);

            if (status.Length > 0)
                ErrorLog.Warn("[JsonFileOps] {0}", status);
        }

        // ---------------------------------------------------------------- state feedback

        private void UpdateSystemStateFeedback()
        {
            _contract.System.AutoUpdate_Fb((sig, _) => sig.BoolValue = _autoUpdateEnabled);
        }
    }
}
