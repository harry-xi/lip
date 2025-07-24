using CommunityToolkit.Mvvm.Input;
using Lip.GUI.Lite.Controls;
using Lip.GUI.Lite.Helpers;
using Lip.GUI.Lite.Services;
using System.Collections.ObjectModel;
using System.Linq;
using Wpf.Ui;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using Wpf.Ui.Extensions;

namespace Lip.GUI.Lite.ViewModels.Pages
{
    public partial class SettingsViewModel(ConfigService configService, 
        IContentDialogService contentDialogService, 
        ISnackbarService snackbarService, 
        WorkspaceManageService workspaceManageService
    ) : ObservableObject, INavigationAware
    {
        private bool _isInitialized = false;

        [ObservableProperty]
        private string _appVersion = string.Empty;
        [ObservableProperty]
        private string _lipVersion = string.Empty;
        [ObservableProperty]
        private ApplicationTheme _currentTheme = ApplicationTheme.Unknown;

        private string _cachePath = string.Empty;
        public string CachePath
        {
            get => _cachePath;
            set
            {
                if (SetProperty(ref _cachePath, value))
                {
                    configService.UpdateRuntimeConfig(cfg => cfg with { Cache = value });
                    _ = configService.SaveRuntimeConfig();
                }
            }
        }

        public ObservableCollection<string> GitHubProxies { get; } = new();
        public ObservableCollection<string> GoModuleProxies { get; } = new();

        [RelayCommand]
        private async Task AddGitHubProxy(string proxy)
        {
            if (!string.IsNullOrWhiteSpace(proxy) && !GitHubProxies.Contains(proxy))
            {
                GitHubProxies.Add(proxy);
                UpdateGitHubProxies();
            }
            await Task.CompletedTask;
        }

        [RelayCommand]
        private async Task RemoveGitHubProxy(string proxy)
        {       
            if (GitHubProxies.Remove(proxy))
            {
                UpdateGitHubProxies();
            }
            await Task.CompletedTask;
        }

        private void UpdateGitHubProxies()
        {
            configService.UpdateRuntimeConfig(cfg => cfg with { GitHubProxies = GitHubProxies.ToList() });
            _ = configService.SaveRuntimeConfig();
        }

        [RelayCommand]
        private async Task AddGoModuleProxy(string proxy)
        {
            if (!string.IsNullOrWhiteSpace(proxy) && !GoModuleProxies.Contains(proxy))
            {
                GoModuleProxies.Add(proxy);
                UpdateGoModuleProxies();
            }
            await Task.CompletedTask;
        }

        [RelayCommand]
        private async Task RemoveGoModuleProxy(string proxy)
        {
            if (GoModuleProxies.Remove(proxy))
            {
                UpdateGoModuleProxies();
            }
            await Task.CompletedTask;
        }

        private void UpdateGoModuleProxies()
        {
            configService.UpdateRuntimeConfig(cfg => cfg with { GoModuleProxies = GoModuleProxies.ToList() });
            _ = configService.SaveRuntimeConfig();
        }

        public Task OnNavigatedToAsync()
        {
            if (!_isInitialized)
                InitializeViewModel();

            return Task.CompletedTask;
        }

        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        private void InitializeViewModel()
        {
            CurrentTheme = ApplicationThemeManager.GetAppTheme();
            AppVersion = $"LipUI - {GetAssemblyVersion()}";
            LipVersion = $"Lip.Core - {GetLipCoreVersion()}";

            // 初始化配置
            CachePath = configService.RuntimeConfig.Cache ?? string.Empty;
            GitHubProxies.Clear();
            foreach (var proxy in configService.RuntimeConfig.GitHubProxies)
                GitHubProxies.Add(proxy);
            GoModuleProxies.Clear();
            foreach (var proxy in configService.RuntimeConfig.GoModuleProxies)
                GoModuleProxies.Add(proxy);

            _isInitialized = true;
        }

        private string GetAssemblyVersion()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString()
                ?? string.Empty;
        }

        private string GetLipCoreVersion()
        {
            var assembly = typeof(Core.Lip).Assembly;
            var version = assembly.GetName().Version;
            return version?.ToString() ?? string.Empty;
        }

        [RelayCommand]
        private void OnChangeTheme(string parameter)
        {
            switch (parameter)
            {
                case "theme_light":
                    configService.Config.Theme = ApplicationTheme.Light;
                    if (CurrentTheme == ApplicationTheme.Light)
                        break;

                    ApplicationThemeManager.Apply(ApplicationTheme.Light);
                    CurrentTheme = ApplicationTheme.Light;

                    break;

                default:
                    configService.Config.Theme = ApplicationTheme.Dark;
                    if (CurrentTheme == ApplicationTheme.Dark)
                        break;

                    ApplicationThemeManager.Apply(ApplicationTheme.Dark);
                    CurrentTheme = ApplicationTheme.Dark;

                    break;
            }
        }

        [RelayCommand]
        private void MoveGitHubProxyUp(string proxy)
        {
            int idx = GitHubProxies.IndexOf(proxy);
            if (idx > 0)
            {
                GitHubProxies.Move(idx, idx - 1);
                UpdateGitHubProxies();
            }
        }

        [RelayCommand]
        private void MoveGitHubProxyDown(string proxy)
        {
            int idx = GitHubProxies.IndexOf(proxy);
            if (idx >= 0 && idx < GitHubProxies.Count - 1)
            {
                GitHubProxies.Move(idx, idx + 1);
                UpdateGitHubProxies();
            }
        }

        [RelayCommand]
        private void MoveGoModuleProxyUp(string proxy)
        {
            int idx = GoModuleProxies.IndexOf(proxy);
            if (idx > 0)
            {
                GoModuleProxies.Move(idx, idx - 1);
                UpdateGoModuleProxies();
            }
        }

        [RelayCommand]
        private void MoveGoModuleProxyDown(string proxy)
        {
            int idx = GoModuleProxies.IndexOf(proxy);
            if (idx >= 0 && idx < GoModuleProxies.Count - 1)
            {
                GoModuleProxies.Move(idx, idx + 1);
                UpdateGoModuleProxies();
            }
        }

        [RelayCommand]
        private async Task CleanCache()
        {
            try
            {
                var cacheManager = (await workspaceManageService.GetCurrentToolClass(new EmptyUserInteraction(), new EmptyLogger()))?.CacheManager;
                if (cacheManager == null) return;
                await cacheManager.Clean();
                snackbarService.Show("Cache clean success", "", ControlAppearance.Success);
            } catch (Exception ex) {
                await new ExceptionDialog(ex, contentDialogService.GetDialogHost()).ShowAsync();
            }
        }
    }
}