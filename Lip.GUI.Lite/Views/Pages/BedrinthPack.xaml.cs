using Lip.GUI.Lite.Controls;
using Lip.GUI.Lite.Services;
using Lip.GUI.Lite.ViewModels.Pages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace Lip.GUI.Lite.Views.Pages
{
    /// <summary>
    /// BedrinthPack.xaml 的交互逻辑
    /// </summary>
    public partial class BedrinthPack : Page
    {
        public record NewPageInfo(string Identifier);

        private readonly BedrinthApiService _bedrinthApiService;
        private readonly ISnackbarService _snackbarService;
        private string? _identifier;

        public BedrinthPack(
            BedrinthApiService bedrinthApiService,
            ISnackbarService snackbarService,
            string? identifier = null)
        {
            _bedrinthApiService = bedrinthApiService;
            _snackbarService = snackbarService;
            _identifier = identifier;

            InitializeComponent();

            Loaded += BedrinthPack_Loaded;
        }

        private async void BedrinthPack_Loaded(object sender, RoutedEventArgs e)
        {
            _identifier = (string)DataContext;
            if (!string.IsNullOrEmpty(_identifier))
            {
                
                await LoadPackageAsync(_identifier);
            } else
            {
                throw new Exception();
            }
        }

        private async Task LoadPackageAsync(string identifier)
        {
            try
            {
                LoadingIndicator.Visibility = Visibility.Visible;
                PackageHeader.Visibility = Visibility.Collapsed;
                PackageDetails.Visibility = Visibility.Collapsed;

                var currentPackage = await _bedrinthApiService.GetPackageAsync(identifier);

                // 显示包基本信息
                PackageName.Text = currentPackage.Name;
                PackageAuthor.Text = currentPackage.Author;
                PackageDescription.Text = currentPackage.Description;
                PackageHotness.Text = currentPackage.Hotness.ToString("F1");

                if (DateTime.TryParse(currentPackage.Updated, out var date))
                {
                    PackageUpdated.Text = date.ToString("yyyy-MM-dd HH:mm");
                }
                else
                {
                    PackageUpdated.Text = currentPackage.Updated;
                }

                // 显示头像
                if (!string.IsNullOrEmpty(currentPackage.AvatarUrl))
                {
                    AvatarImage.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(currentPackage.AvatarUrl));
                    AvatarImage.Visibility = Visibility.Visible;
                    DefaultIcon.Visibility = Visibility.Collapsed;
                }

                // 显示标签
                if (currentPackage.Tags.Count > 0)
                {
                    PackageTags.Text = string.Join(", ", currentPackage.Tags);
                    TagsPanel.Visibility = Visibility.Visible;
                }

                // 加载版本列表
                var versionViewModels = new ObservableCollection<VersionInfo>();
                foreach (var version in currentPackage.Versions)
                {
                    versionViewModels.Add(new VersionInfo
                    {
                        VersionNumber = version.VersionNumber,
                        ReleasedAt = version.ReleasedAt,
                        PlatformVersionRequirement = version.PlatformVersionRequirement,
                        FormattedReleasedAt = DateTime.TryParse(version.ReleasedAt, out var releaseDate)
                            ? releaseDate.ToString("yyyy-MM-dd")
                            : version.ReleasedAt
                    });
                }

                VersionComboBox.ItemsSource = versionViewModels;
                if (versionViewModels.Count > 0)
                {
                    VersionComboBox.SelectedIndex = 0;
                }

                if (versionViewModels.Count > 0)
                {
                    //VersionsList.ItemsSource = versionViewModels;
                    //VersionsCard.Visibility = Visibility.Visible;
                }

                // 加载贡献者
                if (currentPackage.Contributors.Count > 0)
                {
                    var contributorViewModels = new List<Contributor>();
                    foreach (var contributor in currentPackage.Contributors)
                    {
                        contributorViewModels.Add(new Contributor
                        {
                            Username = contributor.Username,
                            Contributions = contributor.Contributions
                        });
                    }
                    //ContributorsList.ItemsSource = contributorViewModels;
                    //ContributorsCard.Visibility = Visibility.Visible;
                }

                // 尝试加载README
                await LoadReadmeAsync(currentPackage);

                LoadingIndicator.Visibility = Visibility.Collapsed;
                PackageHeader.Visibility = Visibility.Visible;
                PackageDetails.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                LoadingIndicator.Visibility = Visibility.Collapsed;
                _snackbarService.Show("Error loading package", ex.Message, ControlAppearance.Danger, null, TimeSpan.FromSeconds(5));
            }
        }

        private async Task LoadReadmeAsync(GetPackageResponse currentPackage)
        {
            try
            {
                if (currentPackage != null && !string.IsNullOrEmpty(currentPackage.ProjectUrl) && currentPackage.ProjectUrl.Contains("github.com"))
                {
                    var githubPath = currentPackage.ProjectUrl.Replace("https://github.com/", "");
                    var readme = await _bedrinthApiService.FetchGithubReadmeAsync(githubPath);
                    
                    if (!string.IsNullOrEmpty(readme))
                    {
                        //ReadmeText.Text = readme;
                        Markdownview.Markdown = readme;
                        ReadmeCard.Visibility = Visibility.Visible;
                    }
                }
            }
            catch
            {
                // 忽略README加载失败
            }
        }

        private async void InstallButton_Click(object sender, RoutedEventArgs e)
        {
        }

    }

    public class VersionInfo
    {
        public string VersionNumber { get; set; } = string.Empty;
        public string ReleasedAt { get; set; } = string.Empty;
        public string PlatformVersionRequirement { get; set; } = string.Empty;
        public string FormattedReleasedAt { get; set; } = string.Empty;
    }

    public class Contributor
    {
        public string Username { get; set; } = string.Empty;
        public int Contributions { get; set; } = 0;
    }
}