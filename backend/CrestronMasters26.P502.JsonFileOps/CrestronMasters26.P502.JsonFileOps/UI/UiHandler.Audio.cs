
namespace CrestronMasters26.P502.JsonFileOps
{
    internal sealed partial class UiHandler
    {
        private void InitializeAudioContract()
        {
            // Mutes are buttons: act on the press, not the release.
            _contract.Audio.Source_Mute += OnPress("Audio.Source_Mute", ToggleSourceMute);
            _contract.Audio.Mic_Mute += OnPress("Audio.Mic_Mute", ToggleMicMute);

            // A slider sends the value it landed on, so there's no press/release here.
            _contract.Audio.Source_Level += OnAnalog("Audio.Source_Level", SetSourceLevel);
            _contract.Audio.Mic_Level += OnAnalog("Audio.Mic_Level", SetMicLevel);
        }

        // ---------------------------------------------------------------- what it does

        // Each of these only tells the device. Nothing is pushed to the panel from here on
        // purpose: the emulator raises AudioChanged once it has acted, and that is what
        // moves the feedback. A real DSP works the same way, and it can refuse.

        private void ToggleSourceMute()
        {
            _emulator.ToggleSourceMute();
        }

        private void ToggleMicMute()
        {
            _emulator.ToggleMicMute();
        }

        private void SetSourceLevel(ushort level)
        {
            _emulator.SetSourceLevel(level);
        }

        private void SetMicLevel(ushort level)
        {
            _emulator.SetMicLevel(level);
        }

        // ---------------------------------------------------------------- feedback

        private void UpdateAudioFeedback()
        {
            bool sourceMuted = _emulator.SourceMuted;
            bool micMuted = _emulator.MicMuted;

            // A muted channel shows its fader at zero while the device remembers where it
            // was, so unmuting puts it back. The stored level is never overwritten.
            ushort sourceLevel = sourceMuted ? DeviceEmulator.MinLevel : _emulator.SourceLevel;
            ushort micLevel = micMuted ? DeviceEmulator.MinLevel : _emulator.MicLevel;

            _contract.Audio.Source_Muted((sig, a) => sig.BoolValue = sourceMuted);
            _contract.Audio.Mic_Muted((sig, a) => sig.BoolValue = micMuted);

            _contract.Audio.Source_Level_Fb((sig, a) => sig.UShortValue = sourceLevel);
            _contract.Audio.Mic_Level_Fb((sig, a) => sig.UShortValue = micLevel);
        }
    }
}
