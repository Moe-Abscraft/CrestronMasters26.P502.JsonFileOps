namespace CrestronMasters26.P502.JsonFileOps
{
    internal sealed partial class UiHandler
    {
        private void InitializeSourcesContract()
        {
            // Analog: gives the source number. Starts at 1. 0 = none.
            // Select it and show it on every display.
            // Only tell the device. The panel updates when the device reports back (RoutingChanged).
            _contract.Sources.Select += OnAnalog("Sources.Select", source =>
            {
                if (source > _roomConfig.Sources.Count)
                    return;

                _emulator.SelectSource(source);

                int sourceId = SelectedSourceId();
                foreach (DisplayInfo d in _roomConfig.Displays)
                    _emulator.Route(d.Id, sourceId);
            });
        }

        // Source number (from 1) of the selected source to its id. 0 = none.
        private int SelectedSourceId()
        {
            int selected = _emulator.SelectedSource;
            return selected > 0 ? _roomConfig.Sources[selected - 1].Id : 0;
        }

        // ---------------------------------------------------------------- config feedback

        // Source names. Panel has 10 slots: blank the unused ones.
        private void UpdateSourcesConfigFeedback()
        {
            for (int i = 0; i < _contract.Sources.Source.Length; i++)
            {
                string name = i < _roomConfig.Sources.Count ? _roomConfig.Sources[i].Name : string.Empty;
                _contract.Sources.Source[i].Name((sig, _) => sig.StringValue = name);
            }
        }

        // ---------------------------------------------------------------- state feedback

        // Selected source.
        private void UpdateSourcesStateFeedback()
        {
            ushort selected = (ushort)_emulator.SelectedSource;
            _contract.Sources.Selected((sig, _) => sig.UShortValue = selected);
        }
    }
}
