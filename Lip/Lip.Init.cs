using System.Text;
using Microsoft.Extensions.Logging;

namespace Lip;

public partial class Lip
{
    public record InitArgs
    {
        public bool Force { get; init; }
        public string? InitAuthor { get; init; }
        public string? InitAvatarUrl { get; init; }
        public string? InitDescription { get; init; }
        public string? InitName { get; init; }
        public string? InitTooth { get; init; }
        public string? InitVersion { get; init; }
        public string? Workspace { get; init; }
        public bool Yes { get; init; }
    }

    private const string DefaultTooth = "example.com/org/package";
    private const string DefaultVersion = "0.1.0";

    public async Task Init(InitArgs args)
    {
        string workspace = args.Workspace ?? _filesystem.Directory.GetCurrentDirectory();

        // Check if the workspace is a directory.
        if (!_filesystem.Directory.Exists(workspace))
        {
            throw new DirectoryNotFoundException($"The directory '{workspace}' does not exist.");
        }

        // Convert to absolute path.
        workspace = _filesystem.Path.GetFullPath(workspace);

        PackageManifest? manifest = null;

        if (args.Yes)
        {
            manifest = new()
            {
                FormatVersion = PackageManifest.DefaultFormatVersion,
                FormatUuid = PackageManifest.DefaultFormatUuid,
                Tooth = args.InitTooth ?? DefaultTooth,
                Version = args.InitVersion ?? DefaultVersion,
                Info = new()
                {
                    Name = args.InitName,
                    Description = args.InitDescription,
                    Author = args.InitAuthor,
                    AvatarUrl = args.InitAvatarUrl
                }
            };
        }
        else
        {
            string tooth = args.InitTooth ?? await _userInteraction.PromptForInputAsync("Enter the tooth path (e.g. {DefaultTooth}):", DefaultTooth) ?? DefaultTooth;
            string version = args.InitVersion ?? await _userInteraction.PromptForInputAsync("Enter the package version (e.g. {DefaultVersion}):", DefaultVersion) ?? DefaultVersion;
            string? name = args.InitName ?? await _userInteraction.PromptForInputAsync("Enter the package name:");
            string? description = args.InitDescription ?? await _userInteraction.PromptForInputAsync("Enter the package description:");
            string? author = args.InitAuthor ?? await _userInteraction.PromptForInputAsync("Enter the package author:");
            string? avatarUrl = args.InitAvatarUrl ?? await _userInteraction.PromptForInputAsync("Enter the author's avatar URL:");

            manifest = new()
            {
                FormatVersion = PackageManifest.DefaultFormatVersion,
                FormatUuid = PackageManifest.DefaultFormatUuid,
                Tooth = tooth,
                Version = version,
                Info = new()
                {
                    Name = name,
                    Description = description,
                    Author = author,
                    AvatarUrl = avatarUrl
                }
            };

            string jsonString = Encoding.UTF8.GetString(manifest.ToBytes());
            if (!await _userInteraction.ConfirmAsync("Do you want to create the following package manifest file?\n{jsonString}", jsonString))
            {
                throw new OperationCanceledException("Operation canceled by the user.");
            }
        }

        // Create the manifest file path.
        string manifestPath = _filesystem.Path.Combine(workspace, PackageManifestFileName);

        // Check if the manifest file already exists.
        if (_filesystem.File.Exists(manifestPath))
        {
            if (!args.Force)
            {
                throw new InvalidOperationException($"The file '{manifestPath}' already exists. Use the -f or --force option to overwrite it.");
            }

            _logger.LogWarning("The file '{ManifestPath}' already exists. Overwriting it.", manifestPath);
        }

        await _filesystem.File.WriteAllBytesAsync(manifestPath, manifest!.ToBytes());

        _logger.LogInformation("Successfully initialized the package manifest file '{ManifestPath}'.", manifestPath);
    }
}
