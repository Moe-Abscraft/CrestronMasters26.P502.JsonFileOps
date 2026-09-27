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

                // Hold join = save. Press join = recall, on release, unless Hold fired first.
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

        private void SavePreset(int index)
        {
            _presetSaved[index] = true;

            if (_roomConfig == null)
                return;

            var preset = new Preset
            {
                Routes = _roomConfig.Displays.ToDictionary(d => d.Id, d => d.RoutedSourceId),
                SourceLevel = _emulator.SourceLevel,
                SourceMuted = _emulator.SourceMuted,
                MicLevel = _emulator.MicLevel,
                MicMuted = _emulator.MicMuted
            };

            // Read > change > write back.
            Dictionary<int, Preset> presets = _presetStore.Load();
            presets[index + 1] = preset;
            _presetStore.Save(presets);

            ErrorLog.Notice("[JsonFileOps] preset {0} saved", index + 1);
        }

        private void RecallPreset(int index)
        {
            if (_roomConfig == null || !_presetStore.Load().TryGetValue(index + 1, out Preset? preset))
                return;

            foreach (DisplayInfo display in _roomConfig.Displays)
            {
                if (preset.Routes.TryGetValue(display.Id, out int sourceId))
                    display.RoutedSourceId = sourceId;
            }

            UpdateDisplaysFeedback();

            // Level before mute: moving a fader clears the mute.
            _emulator.SetSourceLevel(preset.SourceLevel);
            _emulator.SetSourceMute(preset.SourceMuted);
            _emulator.SetMicLevel(preset.MicLevel);
            _emulator.SetMicMute(preset.MicMuted);

            ErrorLog.Notice("[JsonFileOps] preset {0} recalled", index + 1);
        }
    }
}
