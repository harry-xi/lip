using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Lip.GUI.Lite.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace Lip.GUI.Lite.Views.Pages
{
    /// <summary>
    /// BedrinthPacksList.xaml 的交互逻辑
    /// </summary>
    public partial class BedrinthPacksList : INavigableView<BedrinthPacksListViewModel>
    {
        public BedrinthPacksListViewModel ViewModel { get; }

        public BedrinthPacksList(BedrinthPacksListViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
        }
    }
}