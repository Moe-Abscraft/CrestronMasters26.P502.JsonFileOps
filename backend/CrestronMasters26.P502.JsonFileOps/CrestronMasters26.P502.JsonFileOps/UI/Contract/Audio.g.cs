using System;
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro;

namespace AppContract
{
    public interface IAudio
    {
        object UserObject { get; set; }

        event EventHandler<UIEventArgs> Source_Mute;
        event EventHandler<UIEventArgs> Mic_Mute;
        event EventHandler<UIEventArgs> Source_Level;
        event EventHandler<UIEventArgs> Mic_Level;

        void Source_Muted(AudioBoolInputSigDelegate callback);
        void Mic_Muted(AudioBoolInputSigDelegate callback);
        void Source_Level_Fb(AudioUShortInputSigDelegate callback);
        void Mic_Level_Fb(AudioUShortInputSigDelegate callback);

    }

    public delegate void AudioBoolInputSigDelegate(BoolInputSig boolInputSig, IAudio audio);
    public delegate void AudioUShortInputSigDelegate(UShortInputSig uShortInputSig, IAudio audio);

    internal class Audio : IAudio, IDisposable
    {
        #region Standard CH5 Component members

        private ComponentMediator ComponentMediator { get; set; }

        public object UserObject { get; set; }

        public uint ControlJoinId { get; private set; }

        private IList<BasicTriListWithSmartObject> _devices;
        public IList<BasicTriListWithSmartObject> Devices { get { return _devices; } }

        #endregion

        #region Joins

        private static class Joins
        {
            internal static class Booleans
            {
                public const uint Source_Mute = 1;
                public const uint Mic_Mute = 2;

                public const uint Source_Muted = 1;
                public const uint Mic_Muted = 2;
            }
            internal static class Numerics
            {
                public const uint Source_Level = 1;
                public const uint Mic_Level = 2;

                public const uint Source_Level_Fb = 1;
                public const uint Mic_Level_Fb = 2;
            }
        }

        #endregion

        #region Construction and Initialization

        internal Audio(ComponentMediator componentMediator, uint controlJoinId)
        {
            ComponentMediator = componentMediator;
            Initialize(controlJoinId);
        }

        private void Initialize(uint controlJoinId)
        {
            ControlJoinId = controlJoinId; 
 
            _devices = new List<BasicTriListWithSmartObject>(); 
 
            ComponentMediator.ConfigureBooleanEvent(controlJoinId, Joins.Booleans.Source_Mute, onSource_Mute);
            ComponentMediator.ConfigureBooleanEvent(controlJoinId, Joins.Booleans.Mic_Mute, onMic_Mute);
            ComponentMediator.ConfigureNumericEvent(controlJoinId, Joins.Numerics.Source_Level, onSource_Level);
            ComponentMediator.ConfigureNumericEvent(controlJoinId, Joins.Numerics.Mic_Level, onMic_Level);

        }

        public void AddDevice(BasicTriListWithSmartObject device)
        {
            Devices.Add(device);
            ComponentMediator.HookSmartObjectEvents(device.SmartObjects[ControlJoinId]);
        }

        public void RemoveDevice(BasicTriListWithSmartObject device)
        {
            Devices.Remove(device);
            ComponentMediator.UnHookSmartObjectEvents(device.SmartObjects[ControlJoinId]);
        }

        #endregion

        #region CH5 Contract

        public event EventHandler<UIEventArgs> Source_Mute;
        private void onSource_Mute(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = Source_Mute;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> Mic_Mute;
        private void onMic_Mute(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = Mic_Mute;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }


        public void Source_Muted(AudioBoolInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].BooleanInput[Joins.Booleans.Source_Muted], this);
            }
        }

        public void Mic_Muted(AudioBoolInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].BooleanInput[Joins.Booleans.Mic_Muted], this);
            }
        }

        public event EventHandler<UIEventArgs> Source_Level;
        private void onSource_Level(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = Source_Level;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> Mic_Level;
        private void onMic_Level(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = Mic_Level;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }


        public void Source_Level_Fb(AudioUShortInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].UShortInput[Joins.Numerics.Source_Level_Fb], this);
            }
        }

        public void Mic_Level_Fb(AudioUShortInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].UShortInput[Joins.Numerics.Mic_Level_Fb], this);
            }
        }

        #endregion

        #region Overrides

        public override int GetHashCode()
        {
            return (int)ControlJoinId;
        }

        public override string ToString()
        {
            return string.Format("Contract: {0} Component: {1} HashCode: {2} {3}", "Audio", GetType().Name, GetHashCode(), UserObject != null ? "UserObject: " + UserObject : null);
        }

        #endregion

        #region IDisposable

        public bool IsDisposed { get; set; }

        public void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;

            Source_Mute = null;
            Mic_Mute = null;
            Source_Level = null;
            Mic_Level = null;
        }

        #endregion

    }
}
