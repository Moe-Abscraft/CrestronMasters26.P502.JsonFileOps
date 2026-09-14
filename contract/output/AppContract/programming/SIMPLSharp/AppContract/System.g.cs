using System;
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro;

namespace AppContract
{
    public interface ISystem
    {
        object UserObject { get; set; }

        event EventHandler<UIEventArgs> ReloadConfig;
        event EventHandler<UIEventArgs> AutoUpdate;

        void AutoUpdate_Fb(SystemBoolInputSigDelegate callback);
        void LastUpdatedTime(SystemStringInputSigDelegate callback);

    }

    public delegate void SystemBoolInputSigDelegate(BoolInputSig boolInputSig, ISystem system);
    public delegate void SystemStringInputSigDelegate(StringInputSig stringInputSig, ISystem system);

    internal class System : ISystem, IDisposable
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
                public const uint ReloadConfig = 1;
                public const uint AutoUpdate = 2;

                public const uint AutoUpdate_Fb = 2;
            }
            internal static class Strings
            {

                public const uint LastUpdatedTime = 1;
            }
        }

        #endregion

        #region Construction and Initialization

        internal System(ComponentMediator componentMediator, uint controlJoinId)
        {
            ComponentMediator = componentMediator;
            Initialize(controlJoinId);
        }

        private void Initialize(uint controlJoinId)
        {
            ControlJoinId = controlJoinId; 
 
            _devices = new List<BasicTriListWithSmartObject>(); 
 
            ComponentMediator.ConfigureBooleanEvent(controlJoinId, Joins.Booleans.ReloadConfig, onReloadConfig);
            ComponentMediator.ConfigureBooleanEvent(controlJoinId, Joins.Booleans.AutoUpdate, onAutoUpdate);

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

        public event EventHandler<UIEventArgs> ReloadConfig;
        private void onReloadConfig(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = ReloadConfig;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> AutoUpdate;
        private void onAutoUpdate(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = AutoUpdate;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }


        public void AutoUpdate_Fb(SystemBoolInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].BooleanInput[Joins.Booleans.AutoUpdate_Fb], this);
            }
        }


        public void LastUpdatedTime(SystemStringInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].StringInput[Joins.Strings.LastUpdatedTime], this);
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
            return string.Format("Contract: {0} Component: {1} HashCode: {2} {3}", "System", GetType().Name, GetHashCode(), UserObject != null ? "UserObject: " + UserObject : null);
        }

        #endregion

        #region IDisposable

        public bool IsDisposed { get; set; }

        public void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;

            ReloadConfig = null;
            AutoUpdate = null;
        }

        #endregion

    }
}
