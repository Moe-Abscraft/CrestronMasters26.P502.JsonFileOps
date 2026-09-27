using AppContract;

namespace CrestronMasters26.P502.JsonFileOps
{
    internal sealed partial class UiHandler
    {
        private void InitializeDisplaysContract()
        {
            // The generated event args don't say WHICH display fired, so the index gets
            // captured in the closure. 'index' has to be a fresh local each pass: a for
            // loop reuses its variable, so handing 'i' straight to the lambda leaves every
            // handler looking at the same variable - which by then holds Length.
            for (int i = 0; i < _contract.Displays.Display.Length; i++)
            {
                int index = i;
                IDisplay display = _contract.Displays.Display[index];

                display.Select += OnPress($"Displays.Display[{index}].Select", () => RouteSelectedSourceToDisplay(index));
                display.Clear += OnPress($"Displays.Display[{index}].Clear", () => ClearDisplay(index));
            }
        }

        // ---------------------------------------------------------------- what it does

        private void RouteSelectedSourceToDisplay(int displayIndex)
        {
            // The contract always has ten display slots; the config usually fills fewer, so
            // a press on an empty slot is possible and has to be ignored, not thrown on.
            if (_roomConfig == null || displayIndex < 0 || displayIndex >= _roomConfig.Displays.Count)
                return;

            _roomConfig.Displays[displayIndex].RoutedSourceId = SourceIdAt(_emulator.SelectedSource);

            UpdateDisplaysFeedback();
        }

        private void ClearDisplay(int displayIndex)
        {
            if (_roomConfig == null || displayIndex < 0 || displayIndex >= _roomConfig.Displays.Count)
                return;

            _roomConfig.Displays[displayIndex].RoutedSourceId = 0;

            UpdateDisplaysFeedback();
        }

        // RoutedSourceId is [JsonIgnore], so a freshly loaded config carries none. A real
        // system would ask the router what it's actually doing; this kit rebuilds it from
        // the last source pressed, so a reload doesn't blank the routing the user can see.
        private void RestoreRoutedSourceFromSelection()
        {
            if (_roomConfig == null)
                return;

            int routedSourceId = SourceIdAt(_emulator.SelectedSource);

            _roomConfig.Displays.ForEach(display => display.RoutedSourceId = routedSourceId);
        }

        // ---------------------------------------------------------------- feedback

        private void UpdateDisplaysFeedback()
        {
            if (_roomConfig == null)
                return;

            for (int i = 0; i < _contract.Displays.Display.Length; i++)
            {
                DisplayInfo? display = i < _roomConfig.Displays.Count ? _roomConfig.Displays[i] : null;

                string name = display?.Name ?? string.Empty;
                string model = display?.Model ?? string.Empty;
                // Looked up fresh each time, so a rename in the file shows up on the next reload.
                string routed = display == null ? string.Empty : SourceNameById(display.RoutedSourceId);

                IDisplay d = _contract.Displays.Display[i];

                d.Name((sig, _) => sig.StringValue = name);
                d.Model((sig, _) => sig.StringValue = model);
                d.RoutedSource((sig, _) => sig.StringValue = routed);
            }
        }
    }
}
