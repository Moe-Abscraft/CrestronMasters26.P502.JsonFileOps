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
        private void InitializeSourcesContract()
        {
            _contract.Sources.Select += Sources_Select;
        }

        private void Sources_Select(object? sender, UIEventArgs e)
        {
            int src = e.SigArgs.Sig.UShortValue;

            if (_roomConfig == null || src < 0 || src > _roomConfig.Sources.Count)
                return;

            _selectedSource = src;

            // Real system: call your NVX routing here. This example fakes the feedback.
            string routed = src == 0 ? string.Empty : _roomConfig.Sources[src - 1].Name;
            _roomConfig.Displays.ForEach(d => d.RoutedSource = routed);

            PushRouting();
        }
    }
}
