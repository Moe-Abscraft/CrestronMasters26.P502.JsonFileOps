using AppContract;

namespace CrestronMasters26.P502.JsonFileOps
{
    internal sealed partial class UiHandler
    {
        private void InitializeDisplaysContract()
        {
            // Copy i into 'index' so each button keeps its own number.
            // Using i directly would give every button the last value.
            for (int i = 0; i < _contract.Displays.Display.Length; i++)
            {
                int index = i;
                IDisplay display = _contract.Displays.Display[index];

                // Send the selected source to this display. Ignore presses on empty slots.
                // Only tell the device. The panel updates when the device reports back (RoutingChanged).
                display.Select += OnPress($"Displays.Display[{index}].Select", () =>
                {
                    if (index < _roomConfig.Displays.Count)
                        _emulator.Route(_roomConfig.Displays[index].Id, SelectedSourceId());
                });

                // Clear this display.
                display.Clear += OnPress($"Displays.Display[{index}].Clear", () =>
                {
                    if (index < _roomConfig.Displays.Count)
                        _emulator.Route(_roomConfig.Displays[index].Id, 0);
                });
            }
        }

        // ---------------------------------------------------------------- config feedback

        // Display names and models. Blank the unused slots.
        private void UpdateDisplaysConfigFeedback()
        {
            for (int i = 0; i < _contract.Displays.Display.Length; i++)
            {
                DisplayInfo? display = i < _roomConfig.Displays.Count ? _roomConfig.Displays[i] : null;
                string name = display?.Name ?? string.Empty;
                string model = display?.Model ?? string.Empty;

                IDisplay d = _contract.Displays.Display[i];
                d.Name((sig, _) => sig.StringValue = name);
                d.Model((sig, _) => sig.StringValue = model);
            }
        }

        // ---------------------------------------------------------------- state feedback

        // Source shown on each display.
        private void UpdateDisplaysStateFeedback()
        {
            for (int i = 0; i < _contract.Displays.Display.Length; i++)
            {
                // Look up the name each time so renames show up. Blank the unused slots.
                string routed = string.Empty;
                if (i < _roomConfig.Displays.Count)
                {
                    int sourceId = _emulator.RoutedSourceId(_roomConfig.Displays[i].Id);
                    routed = _roomConfig.Sources.Find(s => s.Id == sourceId)?.Name ?? string.Empty;
                }

                _contract.Displays.Display[i].RoutedSource((sig, _) => sig.StringValue = routed);
            }
        }
    }
}
