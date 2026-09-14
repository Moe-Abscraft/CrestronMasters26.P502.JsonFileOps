using System;
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro;

namespace AppContract
{
    public interface IDisplays
    {
        object UserObject { get; set; }

        AppContract.IDisplay[] Display { get; }
    }

    internal class Displays : IDisplays, IDisposable
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
        }

        #endregion

        #region Construction and Initialization

        internal Displays(ComponentMediator componentMediator, uint controlJoinId)
        {
            ComponentMediator = componentMediator;
            Initialize(controlJoinId);
        }

        private static readonly IDictionary<uint, List<uint>> DisplaySmartObjectIdMappings = new Dictionary<uint, List<uint>> {
            { 2, new List<uint> { 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 } }};

        internal static void ClearDictionaries()
        {
            DisplaySmartObjectIdMappings.Clear();
        }

        private void Initialize(uint controlJoinId)
        {
            ControlJoinId = controlJoinId; 
 
            _devices = new List<BasicTriListWithSmartObject>(); 
 
            List<uint> displayList = DisplaySmartObjectIdMappings[controlJoinId];
            Display = new AppContract.IDisplay[displayList.Count];
            for (int index = 0; index < displayList.Count; index++)
            {
                Display[index] = new AppContract.Display(ComponentMediator, displayList[index]); 
            }

        }

        public void AddDevice(BasicTriListWithSmartObject device)
        {
            Devices.Add(device);
            ComponentMediator.HookSmartObjectEvents(device.SmartObjects[ControlJoinId]);
            for (int index = 0; index < Display.Length; index++)
            {
                ((AppContract.Display)Display[index]).AddDevice(device);
            }
        }

        public void RemoveDevice(BasicTriListWithSmartObject device)
        {
            Devices.Remove(device);
            ComponentMediator.UnHookSmartObjectEvents(device.SmartObjects[ControlJoinId]);
            for (int index = 0; index < Display.Length; index++)
            {
                ((AppContract.Display)Display[index]).RemoveDevice(device);
            }
        }

        #endregion

        #region CH5 Contract

        public AppContract.IDisplay[] Display { get; private set; }

        #endregion

        #region Overrides

        public override int GetHashCode()
        {
            return (int)ControlJoinId;
        }

        public override string ToString()
        {
            return string.Format("Contract: {0} Component: {1} HashCode: {2} {3}", "Displays", GetType().Name, GetHashCode(), UserObject != null ? "UserObject: " + UserObject : null);
        }

        #endregion

        #region IDisposable

        public bool IsDisposed { get; set; }

        public void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;

            for (int index = 0; index < Display.Length; index++)
            {
                ((AppContract.Display)Display[index]).Dispose();
            }
        }

        #endregion

    }
}
