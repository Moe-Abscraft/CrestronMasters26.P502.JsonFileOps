using AppContract;
using Crestron.SimplSharp;

namespace CrestronMasters26.P502.JsonFileOps
{
    internal sealed partial class UiHandler
    {
        // One flag per preset button: did Hold fire during the current press?
        private bool[] _presetSaved = Array.Empty<bool>();

        private void InitializePresetsContract()
        {
            IaPreset[] presets = _contract.System.aPreset;
            _presetSaved = new bool[presets.Length];

            for (int i = 0; i < presets.Length; i++)
            {
                int index = i;

                // Hold = save. Short press = recall (on release).
                presets[index].Hold += OnPress($"System.aPreset[{index}].Hold", () => SavePreset(index));
                presets[index].Press += OnHold($"System.aPreset[{index}].Press", pressed =>
                {
                    if (pressed)
                        _presetSaved[index] = false;
                    else if (!_presetSaved[index])
                        RecallPreset(index);
                });
            }
        }

        // Save routes and audio to preset.json.
        private void SavePreset(int index)
        {
            _presetSaved[index] = true;

            var preset = new Preset
            {
                Routes = _roomConfig.Displays.ToDictionary(d => d.Id, d => _emulator.RoutedSourceId(d.Id)),
                SourceLevel = _emulator.SourceLevel,
                SourceMuted = _emulator.SourceMuted,
                MicLevel = _emulator.MicLevel,
                MicMuted = _emulator.MicMuted
            };

            // Read the file, change one preset, write it back.
            Dictionary<int, Preset> presets = _presetStore.Load();
            presets[index + 1] = preset;
            _presetStore.Save(presets);

            ErrorLog.Notice("[JsonFileOps] preset {0} saved", index + 1);
        }

        // Load a preset and send it to the device.
        // Feedback comes back from the device (RoutingChanged / AudioChanged).
        private void RecallPreset(int index)
        {
            if (!_presetStore.Load().TryGetValue(index + 1, out Preset? preset))
                return;

            foreach (KeyValuePair<int, int> route in preset.Routes)
                _emulator.Route(route.Key, route.Value);

            // Level before mute: moving a fader clears the mute.
            _emulator.SetSourceLevel(preset.SourceLevel);
            _emulator.SetSourceMute(preset.SourceMuted);
            _emulator.SetMicLevel(preset.MicLevel);
            _emulator.SetMicMute(preset.MicMuted);

            ErrorLog.Notice("[JsonFileOps] preset {0} recalled", index + 1);
        }
    }
}
