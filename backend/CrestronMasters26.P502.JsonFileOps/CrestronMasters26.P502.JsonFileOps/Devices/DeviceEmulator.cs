namespace CrestronMasters26.P502.JsonFileOps
{
    // Fake switcher and DSP. Swap for a real NVX or Biamp driver later.
    internal sealed class DeviceEmulator
    {
        // Analog range is 0-65535.
        public const ushort MinLevel = 0;
        public const ushort MaxLevel = 65535;

        /// <summary>Selected source. Starts at 1. 0 = none.</summary>
        public int SelectedSource { get; private set; }

        public ushort SourceLevel { get; private set; } = 26214;   // about 40%
        public ushort MicLevel { get; private set; } = 39321;      // about 60%

        public bool SourceMuted { get; private set; }
        public bool MicMuted { get; private set; }

        /// <summary>Raised when the selected source or a display route changes.</summary>
        public event EventHandler? RoutingChanged;

        /// <summary>Raised when a level or mute changes.</summary>
        public event EventHandler? AudioChanged;

        // ---------------------------------------------------------------- routing

        public void SelectSource(int source)
        {
            int src = source < 0 ? 0 : source;

            if (src == SelectedSource)
                return;      // nothing changed, so nothing to announce

            SelectedSource = src;

            //RoutingChanged?.Invoke(this, EventArgs.Empty);
        }

        // Display id -> source id on that display. Missing = cleared.
        private readonly Dictionary<int, int> _routes = new Dictionary<int, int>();

        /// <summary>Source id shown on a display. 0 = cleared.</summary>
        public int RoutedSourceId(int displayId)
        {
            return _routes.TryGetValue(displayId, out int sourceId) ? sourceId : 0;
        }

        /// <summary>Put a source on a display. Source id 0 clears it.</summary>
        public void Route(int displayId, int sourceId)
        {
            if (RoutedSourceId(displayId) == sourceId)
                return;

            _routes[displayId] = sourceId;

            RoutingChanged?.Invoke(this, EventArgs.Empty);
        }

        // ---------------------------------------------------------------- audio

        public void SetSourceLevel(ushort level)
        {
            if (level == SourceLevel)
                return;

            SourceLevel = level;

            // Moving the fader clears the mute
            SourceMuted = false;

            AudioChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SetMicLevel(ushort level)
        {
            if (level == MicLevel)
                return;

            MicLevel = level;
            MicMuted = false;

            AudioChanged?.Invoke(this, EventArgs.Empty);
        }

        public void ToggleSourceMute()
        {
            SetSourceMute(!SourceMuted);
        }

        public void ToggleMicMute()
        {
            SetMicMute(!MicMuted);
        }

        public void SetSourceMute(bool muted)
        {
            if (muted == SourceMuted)
                return;

            SourceMuted = muted;

            AudioChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SetMicMute(bool muted)
        {
            if (muted == MicMuted)
                return;

            MicMuted = muted;

            AudioChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
