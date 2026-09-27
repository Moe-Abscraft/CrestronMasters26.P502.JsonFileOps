using Crestron.SimplSharp;

namespace CrestronMasters26.P502.JsonFileOps
{
    internal sealed partial class UiHandler
    {
        private void InitializeSystemContract()
        {
            // OnPress acts on the press only. Reacting to both edges would run every
            // reload a second time on the way back up.
            _contract.System.ReloadConfig += OnPress("System.ReloadConfig", ReloadConfig);
            _contract.System.AutoUpdate += OnPress("System.AutoUpdate", ToggleAutoUpdate);

            // Same room, two layouts: each button loads its own file. Load fires
            // ConfigChanged, so the panel updates the same way as a reload.
            _contract.System.Separate += OnPress("System.Separate", () => _configManager.Load("roomConfig_Separate.json"));
            _contract.System.Combine += OnPress("System.Combine", () => _configManager.Load("roomConfig_Combine.json"));
        }

        // ---------------------------------------------------------------- what it does

        private void ReloadConfig()
        {
            // Load fires ConfigChanged, which lands back here in UpdateFeedback. Nothing
            // else to do - don't push feedback from here or the panel updates twice.
            _configManager.Load();
        }

        private void ToggleAutoUpdate()
        {
            // The toggle itself is panel state, so it lives here. Watching the file is
            // the config manager's job.
            _autoUpdateEnabled = !_autoUpdateEnabled;
            _configManager.EnableAutoUpdate(_autoUpdateEnabled);

            UpdateSystemFeedback();
        }

        // ---------------------------------------------------------------- feedback

        private void UpdateSystemFeedback()
        {
            string lastRead = _roomConfig?.LastReadTime.ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty;
            _contract.System.LastUpdatedTime((sig, d) => sig.StringValue = lastRead);

            _contract.System.AutoUpdate_Fb((sig, d) => sig.BoolValue = _autoUpdateEnabled);

            // Push the status even when it's empty - that's what clears the banner on the
            // panel after a good reload. A feedback you only send on failure never clears.
            string status = _configManager.StatusMessage;
            _contract.System.Message((sig, d) => sig.StringValue = status);

            if (status.Length > 0)
                ErrorLog.Warn("[JsonFileOps] {0}", status);
        }
    }
}
