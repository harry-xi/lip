using Flurl;
using Lip;
using Lip.GUI.Lite.Services;
using Semver;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Wpf.Ui;

namespace Lip.GUI.Lite.ViewModels.Pages
{
    public partial class DashboardViewModel : ObservableObject
    {
        public record Package
        {
            public required Core.PackageManifest Manifest { get; init; }
            public required Core.PackageManifest.Variant Variant { get; init; }
            public required Core.PackageSpecifier Specifier { get; init; }

        }
        [ObservableProperty]
        ObservableCollection<Package> _packs = [];

        private async Task<List<Package>> GetPackages()
        {
            var lip = await workspaceManageService.BuildCurrentLip(new Helpers.UserInteraction(contentDialogService),
                new Helpers.LoggerWrapper()
                );

            if (lip == null)
            {
                return [];
            }

            var listResult = await lip.List(new());

            var tasks = listResult.Select(async item =>
            {
                var packageJson = await lip.View(item.Specifier.ToString(), null, new());
                var package = await Core.PackageManifest.FromStream(new MemoryStream(Encoding.UTF8.GetBytes(packageJson)));
                return new Package
                {
                    Manifest = package,
                    Variant = item.Variant,
                    Specifier = item.Specifier
                };
            }).ToList();

            var packages = (await Task.WhenAll(tasks)).ToList();

            return packages;
        }

        public async Task RefreshPackagesAsync()
        {
            var packages = await GetPackages();
            _packs.Clear();
            foreach (var pkg in packages)
            {
                _packs.Add(pkg);
            }
        }

        private readonly WorkspaceManageService workspaceManageService;
        private readonly IContentDialogService contentDialogService;
        private Package? _selectedPack;

        public DashboardViewModel(WorkspaceManageService workspaceManageService,IContentDialogService contentDialogService)
        {
            this.workspaceManageService = workspaceManageService;
            this.contentDialogService = contentDialogService;
            this.workspaceManageService.WorkspaceChanged += async () =>
            {
                await RefreshPackagesAsync();
            };
            _ = RefreshPackagesAsync();
        }

        public Package? SelectedPack
        {
            get => _selectedPack;
            set
            {
                if (_selectedPack != value)
                {
                    _selectedPack = value;
                    OnPropertyChanged();
                }
            }
        }
    }
}