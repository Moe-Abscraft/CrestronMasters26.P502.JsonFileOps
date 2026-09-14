using System;
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro;

namespace AppContract
{
    /// <summary>
    /// Common Interface for Root Contracts.
    /// </summary>
    public interface IContract
    {
        object UserObject { get; set; }
        void AddDevice(BasicTriListWithSmartObject device);
        void RemoveDevice(BasicTriListWithSmartObject device);
    }

    public class Contract : IContract, IDisposable
    {
        #region Components

        private ComponentMediator ComponentMediator { get; set; }

        public AppContract.ISystem System { get { return (AppContract.ISystem)InternalSystem; } }
        private AppContract.System InternalSystem { get; set; }

        public AppContract.IDisplays Displays { get { return (AppContract.IDisplays)InternalDisplays; } }
        private AppContract.Displays InternalDisplays { get; set; }

        public AppContract.ISources Sources { get { return (AppContract.ISources)InternalSources; } }
        private AppContract.Sources InternalSources { get; set; }

        #endregion

        #region Construction and Initialization

        public Contract()
            : this(new List<BasicTriListWithSmartObject>().ToArray())
        {
        }

        public Contract(BasicTriListWithSmartObject device)
            : this(new [] { device })
        {
        }

        public Contract(BasicTriListWithSmartObject[] devices)
        {
            if (devices == null)
                throw new ArgumentNullException("Devices is null");

            ComponentMediator = new ComponentMediator();

            InternalSystem = new AppContract.System(ComponentMediator, 1);
            InternalDisplays = new AppContract.Displays(ComponentMediator, 2);
            InternalSources = new AppContract.Sources(ComponentMediator, 13);

            for (int index = 0; index < devices.Length; index++)
            {
                AddDevice(devices[index]);
            }
        }

        #endregion

        #region Standard Contract Members

        public object UserObject { get; set; }

        public void AddDevice(BasicTriListWithSmartObject device)
        {
            InternalSystem.AddDevice(device);
            InternalDisplays.AddDevice(device);
            InternalSources.AddDevice(device);
        }

        public void RemoveDevice(BasicTriListWithSmartObject device)
        {
            InternalSystem.RemoveDevice(device);
            InternalDisplays.RemoveDevice(device);
            InternalSources.RemoveDevice(device);
        }

        #endregion

        #region IDisposable

        public bool IsDisposed { get; set; }

        public void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;

            InternalSystem.Dispose();
            InternalDisplays.Dispose();
            InternalSources.Dispose();
            ComponentMediator.Dispose(); 
        }

        #endregion

    }
}
