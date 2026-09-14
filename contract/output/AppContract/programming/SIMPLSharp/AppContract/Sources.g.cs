using System;
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro;

namespace AppContract
{
    public interface ISources
    {
        object UserObject { get; set; }

        event EventHandler<UIEventArgs> Select;

        void Selected(SourcesUShortInputSigDelegate callback);

        AppContract.ISource[] Source { get; }
    }

    public delegate void SourcesUShortInputSigDelegate(UShortInputSig uShortInputSig, ISources sources);

    internal class Sources : ISources, IDisposable
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
            internal static class Numerics
            {
                public const uint Select = 1;

                public const uint Selected = 1;
            }
        }

        #endregion

        #region Construction and Initialization

        internal Sources(ComponentMediator componentMediator, uint controlJoinId)
        {
            ComponentMediator = componentMediator;
            Initialize(controlJoinId);
        }

        private static readonly IDictionary<uint, List<uint>> SourceSmartObjectIdMappings = new Dictionary<uint, List<uint>> {
            { 13, new List<uint> { 14, 15, 16, 17, 18, 19, 20, 21, 22, 23 } }};

        internal static void ClearDictionaries()
        {
            SourceSmartObjectIdMappings.Clear();
        }

        private void Initialize(uint controlJoinId)
        {
            ControlJoinId = controlJoinId; 
 
            _devices = new List<BasicTriListWithSmartObject>(); 
 
            ComponentMediator.ConfigureNumericEvent(controlJoinId, Joins.Numerics.Select, onSelect);

            List<uint> sourceList = SourceSmartObjectIdMappings[controlJoinId];
            Source = new AppContract.ISource[sourceList.Count];
            for (int index = 0; index < sourceList.Count; index++)
            {
                Source[index] = new AppContract.Source(ComponentMediator, sourceList[index]); 
            }

        }

        public void AddDevice(BasicTriListWithSmartObject device)
        {
            Devices.Add(device);
            ComponentMediator.HookSmartObjectEvents(device.SmartObjects[ControlJoinId]);
            for (int index = 0; index < Source.Length; index++)
            {
                ((AppContract.Source)Source[index]).AddDevice(device);
            }
        }

        public void RemoveDevice(BasicTriListWithSmartObject device)
        {
            Devices.Remove(device);
            ComponentMediator.UnHookSmartObjectEvents(device.SmartObjects[ControlJoinId]);
            for (int index = 0; index < Source.Length; index++)
            {
                ((AppContract.Source)Source[index]).RemoveDevice(device);
            }
        }

        #endregion

        #region CH5 Contract

        public AppContract.ISource[] Source { get; private set; }

        public event EventHandler<UIEventArgs> Select;
        private void onSelect(SmartObjectEventArgs eventArgs)
        {
            EventHandler<UIEventArgs> handler = Select;
            if (handler != null)
                handler(this, UIEventArgs.CreateEventArgs(eventArgs));
        }


        public void Selected(SourcesUShortInputSigDelegate callback)
        {
            for (int index = 0; index < Devices.Count; index++)
            {
                callback(Devices[index].SmartObjects[ControlJoinId].UShortInput[Joins.Numerics.Selected], this);
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
            return string.Format("Contract: {0} Component: {1} HashCode: {2} {3}", "Sources", GetType().Name, GetHashCode(), UserObject != null ? "UserObject: " + UserObject : null);
        }

        #endregion

        #region IDisposable

        public bool IsDisposed { get; set; }

        public void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;

            for (int index = 0; index < Source.Length; index++)
            {
                ((AppContract.Source)Source[index]).Dispose();
            }

            Select = null;
        }

        #endregion

    }
}
