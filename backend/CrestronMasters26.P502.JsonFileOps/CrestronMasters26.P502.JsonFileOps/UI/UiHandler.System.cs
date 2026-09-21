using AppContract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrestronMasters26.P502.JsonFileOps.UI
{
    internal partial class UiHandler
    {
        private void InitializeSystemContract()
        {
            _contract.System.ReloadConfig += System_ReloadConfig;
            _contract.System.AutoUpdate += System_AutoUpdate;
        }

        private void System_AutoUpdate(object? sender, UIEventArgs e)
        {
            if (e.SigArgs.Sig.BoolValue)
            {
                _autoUpdateEnabled = !_autoUpdateEnabled;
                _configManager?.EnableAutoUpdate(_autoUpdateEnabled);
            }

            _contract.System.AutoUpdate_Fb((sig, d) => sig.BoolValue = _autoUpdateEnabled);
        }

        private void System_ReloadConfig(object? sender, UIEventArgs e)
        {
            if (e.SigArgs.Sig.BoolValue)
                _configManager?.Load();
        }
    }
}
