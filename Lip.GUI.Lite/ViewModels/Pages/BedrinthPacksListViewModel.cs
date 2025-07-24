using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lip.GUI.Lite.Services;
using Lip.GUI.Lite.Views.Pages;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Wpf.Ui;
using static Lip.GUI.Lite.Views.Pages.BedrinthPack;

namespace Lip.GUI.Lite.ViewModels.Pages
{
    public partial class BedrinthPacksListViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private ObservableCollection<BedrinthPackageItemViewModel> _packages = new();

        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private int _currentPage = 1;

        [ObservableProperty]
        private int _totalPages = 1;

        [ObservableProperty]
        private bool _hasMorePages = false;

        private readonly BedrinthApiService _bedrinthApiService;
        private readonly INavigationService _navigationService;
        private readonly ISnackbarService _snackbarService;

        public BedrinthPacksListViewModel(
            BedrinthApiService bedrinthApiService, 
            INavigationService navigationService,
            ISnackbarService snackbarService)
        {
            _bedrinthApiService = bedrinthApiService;
            _navigationService = navigationService;
            _snackbarService = snackbarService;

            // 初始加载
            _ = LoadPackagesAsync();
        }

        [RelayCommand]
        public async Task SearchAsync()
        {
            CurrentPage = 1;
            await LoadPackagesAsync();
        }

        [RelayCommand]
        public async Task LoadMoreAsync()
        {
            if (HasMorePages && !IsLoading)
            {
                CurrentPage++;
                await LoadPackagesAsync(append: true);
            }
        }

        [RelayCommand]
        public async Task RefreshAsync()
        {
            CurrentPage = 1;
            await LoadPackagesAsync();
        }

        [RelayCommand]
        public void NavigateToPackage(BedrinthPackageItemViewModel package)
        {
            if (package != null)
            {
                _navigationService.Navigate(typeof(BedrinthPack),package.Identifier);
            }
        }

        private async Task LoadPackagesAsync(bool append = false)
        {
            try
            {
                IsLoading = true;

                if (!append)
                {
                    Packages.Clear();
                }
                Debug.WriteLine($"Loading packages for search: {SearchText}, page: {CurrentPage}");
                var response = await _bedrinthApiService.SearchPackagesAsync(
                    q: SearchText,
                    perPage: 20,
                    page: CurrentPage,
                    sort: "hotness",
                    order: "desc"
                );
                Debug.WriteLine($"Loaded {response.Items.Count} packages on page {CurrentPage}");
                Debug.WriteLine(response.ToString());
                TotalPages = response.TotalPages;
                HasMorePages = CurrentPage < TotalPages;

                foreach (var item in response.Items)
                {
                    var packageVm = new BedrinthPackageItemViewModel
                    {
                        Identifier = item.Identifier,
                        Name = item.Name,
                        Description = item.Description,
                        Author = item.Author,
                        Tags = item.Tags,
                        AvatarUrl = item.AvatarUrl,
                        ProjectUrl = item.ProjectUrl,
                        Hotness = item.Hotness,
                        Updated = item.Updated
                    };

                    Packages.Add(packageVm);
                }
            }
            catch (Exception ex)
            {
                _snackbarService.Show("Error loading packages", ex.Message, Wpf.Ui.Controls.ControlAppearance.Danger, null, TimeSpan.FromSeconds(5));
            }
            finally
            {
                IsLoading = false;
            }
        }

        partial void OnSearchTextChanged(string value)
        {
            // 延迟搜索，避免输入时频繁调用API
            _ = Task.Delay(500).ContinueWith(async _ =>
            {
                if (SearchText == value)
                {
                    await SearchAsync();
                }
            });
        }
    }

    public class BedrinthPackageItemViewModel : ObservableObject
    {
        public string Identifier { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
        public string AvatarUrl { get; set; } = string.Empty;
        public string ProjectUrl { get; set; } = string.Empty;
        public double Hotness { get; set; } = 0;
        public string Updated { get; set; } = string.Empty;

        public string TagsDisplay => string.Join(", ", Tags);
        public string FormattedHotness => Hotness.ToString();
        public string FormattedUpdated 
        {
            get
            {
                if (DateTime.TryParse(Updated, out var date))
                {
                    return date.ToString("yyyy-MM-dd");
                }
                return Updated;
            }
        }
    }
}