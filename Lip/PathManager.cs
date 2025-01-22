using System.IO.Abstractions;

namespace Lip;

public class PathManager(IFileSystem fileSystem, string? baseCacheDir = null)
{
    private const string DownloadedFileCacheDirName = "downloaded_files";
    private const string GitRepoCacheDirName = "git_repos";
    private const string PackageManifestCacheDirName = "package_manifests";
    private const string PackageManifestFileName = "tooth.json";
    private const string PackageLockFileName = "tooth_lock.json";

    private readonly IFileSystem _fileSystem = fileSystem;
    private readonly string? _baseCacheDir = baseCacheDir;

    public string BaseCacheDir => _fileSystem.Path.GetFullPath(_baseCacheDir ?? throw new InvalidOperationException("Base cache directory is not provided."));

    public string BaseDownloadedFileCacheDir => _fileSystem.Path.Join(BaseCacheDir, DownloadedFileCacheDirName);

    public string BaseGitRepoCacheDir => _fileSystem.Path.Join(BaseCacheDir, GitRepoCacheDirName);

    public string BasePackageManifestCacheDir => _fileSystem.Path.Join(BaseCacheDir, PackageManifestCacheDirName);

    public string CurrentPackageManifestPath => _fileSystem.Path.Join(WorkingDir, PackageManifestFileName);

    public string CurrentPackageLockPath => _fileSystem.Path.Join(WorkingDir, PackageLockFileName);

    public string RuntimeConfigPath => _fileSystem.Path.Join(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "lip", "liprc.json");

    public string WorkingDir => _fileSystem.Directory.GetCurrentDirectory();

    public string GetDownloadedFileCachePath(Uri url)
    {
        string downloadedFileName = Uri.EscapeDataString(url.AbsoluteUri);
        return _fileSystem.Path.Join(BaseDownloadedFileCacheDir, downloadedFileName);
    }

    public string GetGitRepoCachePath(string repoUrl)
    {
        string repoDirName = Uri.EscapeDataString(repoUrl);
        return _fileSystem.Path.Join(BaseGitRepoCacheDir, repoDirName);
    }

    public string GetGitRepoPackageManifestCachePath(string repoUrl)
    {
        string repoDir = GetGitRepoCachePath(repoUrl);
        return _fileSystem.Path.Join(repoDir, PackageManifestFileName);
    }

    public string GetPackageManifestCachePath(string packageName)
    {
        string packageManifestFileName = Uri.EscapeDataString(packageName) + ".json";
        return _fileSystem.Path.Join(BasePackageManifestCacheDir, packageManifestFileName);
    }
}
