using Wpf.Ui.Appearance;

namespace Lip.GUI.Lite.Models
{
    public class AppConfig
    {
        public ApplicationTheme Theme { get; set; } = ApplicationTheme.Dark;

        public List<Workspace> Workspaces { get; set; } = [];

        public string? LastWorkspacePath { get; set; }

        public static readonly AppConfig Default = new();
    }
}