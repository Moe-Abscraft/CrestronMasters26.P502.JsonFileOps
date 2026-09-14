using System;
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro;

namespace AppContract
{
    public interface IDisplay
    {
        object UserObject { get; set; }

        event EventHandler<UIEventArgs> Select;
        event EventHandler<UIEventArgs> Clear;

        void Name(DisplayStringInputSigDelegate callback);
        void Model(DisplayStringInputSigDelegate callback);
        void RoutedSource(DisplayStringInputSigDelegate callback);

    }

    public delegate void DisplayBoolInputSigDelegate(BoolInputSig boolInputSig, IDisplay display);
    public delegate void DisplayStringInputSigDelegate(StringInputSig stringInputSig, IDisplay display);

    internal class Display : IDisplay, IDisposable
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
                public const uint Select = 1;
                public const uint Clear = 2;

            }
            internal static class Strings
            {

                public const uint Name = 1;
                public const uint Model = 2;
                public const uint RoutedSource = 3;
            }
        }

        #endregion

        #region Construction and Initialization

        internal Display(ComponentMediator componentMediator, uint controlJoinId)
        {
            ComponentMediator = componentMediator;
            Initialize(controlJoinId);
        }

        private void Initialize(uint controlJoinId)
        {
            ControlJoinId = controlJoinId; 
 
            _devices = new List<BasicTriListWithSmartObject>(); 
 
            ComponentMediator.ConfigureBooleanEvent(controlJoinId, Joins.Booleans.Select, onSelect);
            ComponentMediator.ConfigureBooleanEvent(controlJoinId, Joins.Booleans.Clear, onClear);

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

        public event EventHandler<UIEventArgs> Select;
        private void onSelect(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = Select;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }

        public event EventHandler<UIEventArgs> Clear;
        private void onClear(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = Clear;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }



        public void Name(DisplayStringInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].StringInput[Joins.Strings.Name], this);
            }
        }

        public void Model(DisplayStringInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].StringInput[Joins.Strings.Model], this);
            }
        }

        public void RoutedSource(DisplayStringInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].StringInput[Joins.Strings.RoutedSource], this);
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
            return string.Format("Contract: {0} Component: {1} HashCode: {2} {3}", "Display", GetType().Name, GetHashCode(), UserObject != null ? "UserObject: " + UserObject : null);
        }

        #endregion

        #region IDisposable

        public bool IsDisposed { get; set; }

        public void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;

            Select = null;
            Clear = null;
        }

        #endregion

    }
}
