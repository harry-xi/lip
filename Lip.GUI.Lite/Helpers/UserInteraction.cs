using Lip.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Wpf.Ui;
using Wpf.Ui.Controls;
using System.Windows.Controls;
using TextBox = Wpf.Ui.Controls.TextBox;
using TextBlock = Wpf.Ui.Controls.TextBlock;

namespace Lip.GUI.Lite.Helpers
{
    internal class UserInteraction(IContentDialogService dialogService) : IUserInteraction
    {
        public async Task<bool> Confirm(string format, params object[] args)
        {
            var message = string.Format(format, args);
            var dialog = new ContentDialog(dialogService.GetDialogHost())
            {
                Title = "Confirm",
                Content = new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
                PrimaryButtonText = "OK",
                CloseButtonText = "Cancel"
            };
            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary;
        }

        public async Task<string> PromptForInput(string defaultValue, string format, params object[] args)
        {
            var message = string.Format(format, args);
            var inputBox = new TextBox { Text = defaultValue, MinWidth = 200 };
            var panel = new StackPanel();
            panel.Children.Add(new TextBlock { Text = message, Margin = new System.Windows.Thickness(0, 0, 0, 8) });
            panel.Children.Add(inputBox);

            var dialog = new ContentDialog(dialogService.GetDialogHost())
            {
                Title = "Input",
                Content = panel,
                PrimaryButtonText = "OK",
                CloseButtonText = "Cancel"
            };
            var result = await dialog.ShowAsync();

            return result == ContentDialogResult.Primary ? inputBox.Text ?? defaultValue : defaultValue;
        }

        public async Task<string> PromptForSelection(IEnumerable<string> options, string format, params object[] args)
        {
            var message = string.Format(format, args);
            var comboBox = new ComboBox { ItemsSource = options.ToList(), SelectedIndex = 0, MinWidth = 200 };
            var panel = new StackPanel();
            panel.Children.Add(new TextBlock { Text = message, Margin = new System.Windows.Thickness(0, 0, 0, 8) });
            panel.Children.Add(comboBox);

            var dialog = new ContentDialog(dialogService.GetDialogHost())
            {
                Title = "Select",
                Content = panel,
                PrimaryButtonText = "OK",
                CloseButtonText = "Cancel"
            };
            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary && comboBox.SelectedItem != null
                ? comboBox.SelectedItem.ToString()!
                : options.FirstOrDefault() ?? string.Empty;
        }

        public Task UpdateProgress(string id, float progress, string format, params object[] args)
        {
            var str = string.Format(format, args);
            // Progress update can be implemented via Snackbar, StatusBar, or custom controls. Placeholder here.
            return Task.CompletedTask;
        }
    }
}