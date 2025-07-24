using Lip.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using Wpf.Ui;
using Wpf.Ui.Controls;
using TextBlock = Wpf.Ui.Controls.TextBlock;
using TextBox = Wpf.Ui.Controls.TextBox;

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
            var optionList = options.ToList();
            var selected = optionList.FirstOrDefault();

            var panel = new StackPanel();
            panel.Children.Add(new TextBlock { Text = message, Margin = new System.Windows.Thickness(0, 0, 0, 8) });

            var radioButtons = new List<RadioButton>();
            foreach (var option in optionList)
            {
                var radio = new RadioButton
                {
                    Content = option,
                    GroupName = "Selections",
                    Margin = new System.Windows.Thickness(0, 0, 0, 4),
                    IsChecked = option == selected
                };
                radio.Checked += (s, e) => selected = option;
                radioButtons.Add(radio);
                panel.Children.Add(radio);
            }

            var dialog = new ContentDialog(dialogService.GetDialogHost())
            {
                Title = "Select",
                Content = panel,
                PrimaryButtonText = "OK",
                CloseButtonText = "Cancel"
            };
            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary && !string.IsNullOrEmpty(selected)
                ? selected
                : optionList.FirstOrDefault() ?? string.Empty;
        }

        public Task UpdateProgress(string id, float progress, string format, params object[] args)
        {
            return Task.CompletedTask;
        }
    }

    internal class EmptyUserInteraction : IUserInteraction
    {
        public async Task<bool> Confirm(string format, params object[] args)
        {
            await Task.CompletedTask;
            return false;
        }

        public async Task<string> PromptForInput(string defaultValue, string format, params object[] args)
        {
            await Task.CompletedTask;
            return defaultValue;
        }

        public async Task<string> PromptForSelection(IEnumerable<string> options, string format, params object[] args)
        {
            await Task.CompletedTask;
            return options.FirstOrDefault() ?? string.Empty;
        }

        public async Task UpdateProgress(string id, float progress, string format, params object[] args)
        {
            await Task.CompletedTask;
        }
    }
}