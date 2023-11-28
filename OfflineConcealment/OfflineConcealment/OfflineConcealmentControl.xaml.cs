using System.Windows;
using System.Windows.Controls;

namespace OfflineConcealment
{
    public partial class OfflineConcealmentControl : UserControl
    {

        private OfflineConcealment Plugin { get; }

        private OfflineConcealmentControl()
        {
            InitializeComponent();
        }

        public OfflineConcealmentControl(OfflineConcealment plugin) : this()
        {
            Plugin = plugin;
            DataContext = plugin.Config;
        }

        private void SaveButton_OnClick(object sender, RoutedEventArgs e)
        {
            Plugin.Save();
        }
    }
}
