namespace CrestronMasters26.P502.JsonFileOps
{
    // Swap this for a real NVX or Biamp driver later
    internal sealed class DeviceEmulator
    {
        // Analog joins are 0-65535 end to end.
        public const ushort MinLevel = 0;
        public const ushort MaxLevel = 65535;

        /// <summary>1-based, matching the contract. 0 means nothing is selected.</summary>
        public int SelectedSource { get; private set; }

        public ushort SourceLevel { get; private set; } = 26214;   // about 40%
        public ushort MicLevel { get; private set; } = 39321;      // about 60%

        public bool SourceMuted { get; private set; }
        public bool MicMuted { get; private set; }

        /// <summary>Raised once the switch has happened.</summary>
        public event EventHandler? RoutingChanged;

        /// <summary>Raised on any level or mute change, from any cause.</summary>
        public event EventHandler? AudioChanged;

        // ---------------------------------------------------------------- routing

        public void SelectSource(int source)
        {
            int src = source < 0 ? 0 : source;

            if (src == SelectedSource)
                return;      // nothing changed, so nothing to announce

            SelectedSource = src;

            RoutingChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// A reloaded config can be shorter than the one the selection was made against.
        /// The device is what holds the selection, so the clamp belongs here.
        /// </summary>
        public void ClampSelectedSource(int sourceCount)
        {
            if (SelectedSource > sourceCount)
                SelectSource(0);
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
