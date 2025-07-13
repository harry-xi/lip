using Microsoft.Win32;
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace Lip.GUI.Lite.Controls
{
    /// <summary>
    /// NewWorkSpaceDialog.xaml 的交互逻辑
    /// </summary>
    public partial class NewWorkSpaceDialog : ContentDialog
    {
        public string WorkspaceName => NameBox.Text.Trim();
        public string WorkspacePath => PathBox.Text.Trim();

        public NewWorkSpaceDialog(ContentPresenter? contentPresenter)
        : base(contentPresenter)
        {
            InitializeComponent();
        }

        protected override void OnButtonClick(ContentDialogButton button)
        {
            if (button == ContentDialogButton.Primary)
            {
                ErrorText.Visibility = Visibility.Collapsed;
                if (string.IsNullOrWhiteSpace(WorkspaceName) || string.IsNullOrWhiteSpace(WorkspacePath))
                {
                    ErrorText.Text = "Name and path cannot be empty.";
                    ErrorText.Visibility = Visibility.Visible;
                    return;
                }
            }
            base.OnButtonClick(button);
        }

        protected override void OnClosed(ContentDialogResult result)
        {
            base.OnClosed(result);
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog { Multiselect = false };
            if (dialog.ShowDialog() != true)
            {
                return;
            }

            if (dialog.FolderNames.Length == 0)
            {
                return;
            }
            PathBox.Text = dialog.FolderNames[0];
        }
    }
}