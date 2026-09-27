namespace CrestronMasters26.P502.JsonFileOps
{
    internal sealed partial class UiHandler
    {
        private void InitializeSourcesContract()
        {
            // An analog join carries its value, so there's no release to filter out.
            _contract.Sources.Select += OnAnalog("Sources.Select", SelectSource);
        }

        // ---------------------------------------------------------------- what it does

        private void SelectSource(ushort source)
        {
            // Select is 1-based; 0 means "nothing selected", which is a legal press.
            if (_roomConfig == null || source > _roomConfig.Sources.Count)
                return;

            // Tell the hardware and stop. The emulator raises RoutingChanged once it has
            // switched, and that is what moves the panel - the press itself pushes nothing.
            _emulator.SelectSource(source);
        }

        /// <summary>Runs when the device reports a new route, never when a button is pressed.</summary>
        private void UpdateRoutingFeedback()
        {
            RestoreRoutedSourceFromSelection();

            UpdateSelectedSourceFeedback();
            UpdateDisplaysFeedback();
        }

        // The contract counts from 1, the List counts from 0. This minus one is the only
        // place that conversion happens, which is where you want it. Returns the source's
        // id from the file, not its position - 0 when nothing is selected.
        private int SourceIdAt(int source)
        {
            if (_roomConfig == null || source <= 0 || source > _roomConfig.Sources.Count)
                return 0;

            return _roomConfig.Sources[source - 1].Id;
        }

        // Empty when the id is 0 or the source has since been removed from the file.
        private string SourceNameById(int id)
        {
            return _roomConfig?.Sources.Find(s => s.Id == id)?.Name ?? string.Empty;
        }

        // ---------------------------------------------------------------- feedback

        private void UpdateSourceFeedback()
        {
            if (_roomConfig == null)
                return;

            for (int i = 0; i < _contract.Sources.Source.Length; i++)
            {
                // The contract has a fixed ten slots; the config usually fills fewer. Blank
                // the rest, or the panel keeps showing names from the config before this one.
                string name = i < _roomConfig.Sources.Count ? _roomConfig.Sources[i].Name : string.Empty;
                _contract.Sources.Source[i].Name((sig, s) => sig.StringValue = name);
            }
        }

        private void UpdateSelectedSourceFeedback()
        {
            ushort selected = (ushort)_emulator.SelectedSource;
            _contract.Sources.Selected((sig, d) => sig.UShortValue = selected);
        }
    }
}
