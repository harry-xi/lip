using System.IO.Abstractions;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Lip;

/// <summary>
/// The main class of the Lip library.
/// </summary>
/// <param name="runtimeConfig">The runtime configuration.</param>
/// <param name="fileSystem">The file system wrapper.</param>
/// <param name="logger">The logger.</param>
/// <param name="pathManager">The path manager.</param>
/// <param name="userInteraction">The user interaction wrapper.</param>
/// <param name="runtimeIdentifier">The simulated runtime identifier. If not provided, the actual runtime identifier is used.</param>
public partial class Lip(
    RuntimeConfig runtimeConfig,
    IFileSystem fileSystem,
    ILogger logger,
    IPathManager pathManager,
    IUserInteraction userInteraction,
    string? runtimeIdentifier = null)
{
    private readonly IFileSystem _fileSystem = fileSystem;
    private readonly ILogger _logger = logger;
    private readonly IPathManager _pathManager = pathManager;
    private readonly RuntimeConfig _runtimeConfig = runtimeConfig;
    private readonly string _runtimeIdentifier = runtimeIdentifier ?? RuntimeInformation.RuntimeIdentifier;
    private readonly IUserInteraction _userInteraction = userInteraction;
}
