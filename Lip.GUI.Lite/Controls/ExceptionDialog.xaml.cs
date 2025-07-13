
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace Lip.GUI.Lite.Controls
{
    public partial class ExceptionDialog : ContentDialog
    {
        public string ExceptionMessage { get; }
        public string ExceptionDetail { get; }

        public ExceptionDialog(Exception ex, ContentPresenter? contentPresenter)
            : base(contentPresenter)
        {
            ExceptionMessage = ex.Message;
            ExceptionDetail = ex.StackTrace ?? "";
            DataContext = this;
            InitializeComponent();
        }
    }
}