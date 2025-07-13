﻿using Lip.GUI.Lite.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace Lip.GUI.Lite.Views.Pages
{
    public partial class SettingsPage : INavigableView<SettingsViewModel>
    {
        public SettingsViewModel ViewModel { get; }

        public SettingsPage(SettingsViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
        }
    }
}