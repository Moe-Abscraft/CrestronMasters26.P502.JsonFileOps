
namespace CrestronMasters26.P502.JsonFileOps
{
    internal sealed partial class UiHandler
    {
        private void InitializeAudioContract()
        {
            // Only tell the device. The panel updates when the device reports back (AudioChanged).

            // Mute buttons: act on press.
            _contract.Audio.Source_Mute += OnPress("Audio.Source_Mute", _emulator.ToggleSourceMute);
            _contract.Audio.Mic_Mute += OnPress("Audio.Mic_Mute", _emulator.ToggleMicMute);

            // Sliders: send the value.
            _contract.Audio.Source_Level += OnAnalog("Audio.Source_Level", _emulator.SetSourceLevel);
            _contract.Audio.Mic_Level += OnAnalog("Audio.Mic_Level", _emulator.SetMicLevel);
        }

        // ---------------------------------------------------------------- state feedback

        // Levels and mutes.
        private void UpdateAudioStateFeedback()
        {
            bool sourceMuted = _emulator.SourceMuted;
            bool micMuted = _emulator.MicMuted;

            // Muted = show the fader at 0. The real level is kept for unmute.
            ushort sourceLevel = sourceMuted ? DeviceEmulator.MinLevel : _emulator.SourceLevel;
            ushort micLevel = micMuted ? DeviceEmulator.MinLevel : _emulator.MicLevel;

            _contract.Audio.Source_Muted((sig, _) => sig.BoolValue = sourceMuted);
            _contract.Audio.Mic_Muted((sig, _) => sig.BoolValue = micMuted);
            _contract.Audio.Source_Level_Fb((sig, _) => sig.UShortValue = sourceLevel);
            _contract.Audio.Mic_Level_Fb((sig, _) => sig.UShortValue = micLevel);
        }
    }
}
