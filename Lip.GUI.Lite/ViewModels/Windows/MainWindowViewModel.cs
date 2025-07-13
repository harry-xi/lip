using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lip.GUI.Lite.Controls;
using Lip.GUI.Lite.Models;
using Lip.GUI.Lite.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace Lip.GUI.Lite.ViewModels.Windows
{
    public partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _applicationTitle = "LipUI";

        [ObservableProperty]
        private ObservableCollection<object> _menuItems = new()
        {
            new NavigationViewItem()
            {
                Content = "Dashboard",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Home24 },
                TargetPageType = typeof(Views.Pages.DashboardPage)
            },
            new NavigationViewItem()
            {   Content = "Bedrinth",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Box24 },
                TargetPageType = typeof(Views.Pages.BedrinthPacksList)
            },
        };

        [ObservableProperty]
        private ObservableCollection<object> _footerMenuItems = new()
        {
            new NavigationViewItem()
            {
                Content = "Settings",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Settings24 },
                TargetPageType = typeof(Views.Pages.SettingsPage)
            }
        };

        [ObservableProperty]
        private ObservableCollection<Workspace> _workspaces = new();

        [ObservableProperty]
        private Workspace? _currentWorkspace;

        [ObservableProperty]
        private bool _isWorkspaceFlyoutOpen;

        public IEnumerable<Workspace> AvailableWorkspaces =>
            _workspaces.Where(ws => ws != _currentWorkspace);

        public string CurrentWorkspaceDisplayName => CurrentWorkspace?.Name ?? "无工作区";
        public bool HasCurrentWorkspace => CurrentWorkspace != null;
        public bool HasAvailableWorkspaces => AvailableWorkspaces.Any();

        public IRelayCommand<Workspace> SwitchWorkspaceCommand { get; }
        public IRelayCommand<Workspace> RemoveWorkspaceCommand { get; }
        public IRelayCommand NewWorkspaceCommand { get; }
        public IRelayCommand OpenWorkspaceFlyoutCommand { get; }
        public IRelayCommand CloseWorkspaceFlyoutCommand { get; }

        private readonly IContentDialogService _dialogService;
        private readonly WorkspaceManageService _workspacesService;

        public MainWindowViewModel(IContentDialogService dialogService, WorkspaceManageService workspaceManageService)
        {
            _dialogService = dialogService;
            _workspacesService = workspaceManageService;

            // 初始化
            RefreshWorkspaces();

            _workspacesService.WorkspaceChanged += RefreshWorkspaces;

            SwitchWorkspaceCommand = new RelayCommand<Workspace>(ws =>
            {
                if (ws != null)
                {
                    _workspacesService.SwitchWorkspace(ws);
                }
            });

            RemoveWorkspaceCommand = new RelayCommand<Workspace>(ws =>
            {
                if (ws != null)
                {
                    _workspacesService.RemoveWorkspace(ws);
                }
            });

            OpenWorkspaceFlyoutCommand = new RelayCommand(() => IsWorkspaceFlyoutOpen = true);
            CloseWorkspaceFlyoutCommand = new RelayCommand(() => IsWorkspaceFlyoutOpen = false);

            NewWorkspaceCommand = new RelayCommand(async () =>
            {
                var dialog = new NewWorkSpaceDialog(_dialogService.GetDialogHost());
                IsWorkspaceFlyoutOpen = false;
                OnPropertyChanged(nameof(IsWorkspaceFlyoutOpen));
                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    var newWs = new Workspace
                    {
                        Name = dialog.WorkspaceName,
                        Path = dialog.WorkspacePath,
                    };
                    _workspacesService.AddWorkspace(newWs);
                    _workspacesService.SwitchWorkspace(newWs);
                }
            });
        }

        private void RefreshWorkspaces()
        {
            _workspaces.Clear();
            foreach (var ws in _workspacesService.Workspaces)
                _workspaces.Add(ws);
            CurrentWorkspace = _workspacesService.CurrentWorkspace;
            OnPropertyChanged(nameof(AvailableWorkspaces));
            OnPropertyChanged(nameof(CurrentWorkspaceDisplayName));
            OnPropertyChanged(nameof(HasCurrentWorkspace));
            OnPropertyChanged(nameof(HasAvailableWorkspaces));
        }
    }
}