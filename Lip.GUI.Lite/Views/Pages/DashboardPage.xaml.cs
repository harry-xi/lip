using Lip.Core;
using Lip.GUI.Lite.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace Lip.GUI.Lite.Views.Pages
{
    public partial class DashboardPage : INavigableView<DashboardViewModel>
    {
        public DashboardViewModel ViewModel { get; }

        public DashboardPage(DashboardViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
        }
    }
}