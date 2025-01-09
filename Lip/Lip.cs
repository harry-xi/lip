using System.IO.Abstractions;
using Microsoft.Extensions.Logging;

namespace Lip;

/// <summary>
/// The main class of the Lip library.
/// </summary>
/// <param name="runtimeConfig">The runtime configuration.</param>
/// <param name="filesystem">The file system wrapper.</param>
/// <param name="logger">The logger.</param>
/// <param name="userInteraction">The user interaction wrapper.</param>
public partial class Lip(RuntimeConfiguration runtimeConfig, IFileSystem filesystem, ILogger logger, IUserInteraction userInteraction)
{
    private const string PackageManifestFileName = "tooth.json";

    private readonly IFileSystem _filesystem = filesystem;
    private readonly ILogger _logger = logger;
    private readonly RuntimeConfiguration _runtimeConfig = runtimeConfig;
    private readonly IUserInteraction _userInteraction = userInteraction;
}
