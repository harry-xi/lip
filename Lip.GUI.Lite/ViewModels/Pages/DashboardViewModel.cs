using Flurl;
using Lip;
using Lip.GUI.Lite.Controls;
using Lip.GUI.Lite.Helpers;
using Lip.GUI.Lite.Services;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using Wpf.Ui;
using Wpf.Ui.Controls;
using Wpf.Ui.Extensions;

namespace Lip.GUI.Lite.ViewModels.Pages
{
    public partial class DashboardViewModel : ObservableObject
    {
        public class PackageListItemViewModel : ObservableObject
        {
            public required Core.PackageSpecifier Specifier { get; init; }
            public required Core.PackageManifest.Variant Variant { get; init; }

            private string? _name;
            public string? Name
            {
                get => _name;
                set => SetProperty(ref _name, value);
            }

            private Core.PackageManifest? _manifest;
            public Core.PackageManifest? Manifest
            {
                get => _manifest;
                set => SetProperty(ref _manifest, value);
            }

            private bool _isManifestLoaded;
            public bool IsManifestLoaded
            {
                get => _isManifestLoaded;
                set => SetProperty(ref _isManifestLoaded, value);
            }
        }

        [ObservableProperty]
        private ObservableCollection<PackageListItemViewModel> _packs = new();

        [ObservableProperty]
        private PackageListItemViewModel? _currentPack;

        [ObservableProperty]
        private string? _currentPackName;

        [ObservableProperty]
        private string? _currentPackVersion;

        [ObservableProperty]
        private string? _currentPackVariantLabel;

        [ObservableProperty]
        private string? _currentPackDescription;

        [ObservableProperty]
        private bool _showCurrentPack;

        [ObservableProperty]
        private bool _inProgress = false;

        [ObservableProperty]
        private bool _isProgressIndeterminate = true;

        [ObservableProperty]
        private float _progress = 0;

        [ObservableProperty]
        private string _progressMessage = string.Empty;

        [ObservableProperty]
        private string _installPackageText = string.Empty;

        private readonly WorkspaceManageService _workspaceManageService;
        private readonly IContentDialogService _contentDialogService;
        private readonly ISnackbarService _snackbarService;


        public DashboardViewModel(WorkspaceManageService workspaceManageService, IContentDialogService contentDialogService, ISnackbarService snackbarService)
        {
            _snackbarService = snackbarService;
            _workspaceManageService = workspaceManageService;
            _contentDialogService = contentDialogService;

            _workspaceManageService.WorkspaceChanged += async () =>
            {
                await RefreshPackages();
            };
            _ = RefreshPackages();
        }

        [RelayCommand]
        public async Task RefreshPackages()
        {
            Packs.Clear();
            var lip = await _workspaceManageService.GetCurrentLip(
                new UserInteraction(_contentDialogService),
                new EmptyLogger());

            if (lip == null)
                return;

            var listResult = await lip.List(new());
            foreach (var item in listResult)
            {
                var vm = new PackageListItemViewModel
                {
                    Specifier = item.Specifier,
                    Variant = item.Variant,
                    Name = item.Specifier.ToothPath, // fallback: 先用包标识符
                    IsManifestLoaded = false,
                };
                Packs.Add(vm);
                _ = LoadPackageNameAsync(lip, vm);
            }
        }

        private static async Task LoadPackageNameAsync(Core.Lip lip, PackageListItemViewModel vm)
        {
            try
            {
                var packageJson = await lip.View(vm.Specifier.ToString(), null, new());
                var manifest = await Core.PackageManifest.FromStream(
                    new MemoryStream(Encoding.UTF8.GetBytes(packageJson)));
                vm.Name = manifest.Info?.Name ?? vm.Specifier.ToothPath;
                vm.Manifest = manifest;
                vm.IsManifestLoaded = true;
            }
            catch
            {
                vm.Name = vm.Specifier.ToothPath;
                vm.IsManifestLoaded = false;
            }
        }

        [RelayCommand]
        public async Task Install()
        {
            if (InstallPackageText == string.Empty) return;
            try
            {
                var installUserInteraction = new ShowProgressUserInteraction(_contentDialogService);
                installUserInteraction.InstallStateUpdate += (progress, message) =>
                {
                    if (progress <= 0)
                    {
                        IsProgressIndeterminate = true;
                    }
                    else
                    {
                        IsProgressIndeterminate = false;
                        Progress = progress * 100;
                    }
                    ProgressMessage = message;
                };
                var lip = await _workspaceManageService.GetCurrentLip(
                    installUserInteraction, installUserInteraction);
                if (lip == null) return;
                InProgress = true;
                CurrentPack = null;
                ShowCurrentPack = true;
                CurrentPackName = InstallPackageText;
                var installText = InstallPackageText;
                await lip.Install([InstallPackageText], new Core.Lip.InstallArgs
                {
                    DryRun = false,
                    Force = false,
                    IgnoreScripts = false,
                    NoDependencies = false,
                    Update = false,
                    OverwriteFiles = false
                });
                
                _snackbarService.Show("Package installed successfully.", $"Package {installText} is installed", ControlAppearance.Success);
            }
            catch (Exception ex)
            {
                var dialog = new ExceptionDialog(ex, _contentDialogService.GetDialogHost());
                await dialog.ShowAsync();
            }
            finally { InProgress = false; }
            await RefreshPackages();
            ShowCurrentPack = false;
            CurrentPack = null;
        }

        [RelayCommand]
        public async Task Uninstall()
        {
            try
            {
                var ui = new ShowProgressUserInteraction(_contentDialogService);
                var lip = await _workspaceManageService.GetCurrentLip(ui, ui);
                if (lip == null || CurrentPack == null) return;
                await lip.Uninstall([CurrentPack.Specifier.Identifier.ToString()], new()
                {
                    DryRun = false,
                    IgnoreScripts = false,
                });
                _snackbarService.Show("Package uninstalled successfully.", $"Package {CurrentPack.Specifier.Identifier} is uninstalled", ControlAppearance.Success);
            }
            catch (Exception ex)
            {
                var dialog = new ExceptionDialog(ex, _contentDialogService.GetDialogHost());
                await dialog.ShowAsync();
            }
            await RefreshPackages();
        }

        partial void OnCurrentPackChanged(PackageListItemViewModel? value)
        {
            if (value?.Manifest != null)
            {
                CurrentPackName = value.Manifest.Info?.Name ?? value.Name ?? string.Empty;
                CurrentPackVersion = value.Manifest.Version?.ToString() ?? string.Empty;
                CurrentPackDescription = value.Manifest.Info?.Description ?? string.Empty;
            }
            else
            {
                CurrentPackName = value?.Name ?? string.Empty;
                CurrentPackVersion = CurrentPack?.Specifier.Version.ToString();
                CurrentPackDescription = string.Empty;
            }
            CurrentPackVariantLabel = value?.Variant.Label ?? string.Empty;
            ShowCurrentPack = value != null;
        }
    }
}