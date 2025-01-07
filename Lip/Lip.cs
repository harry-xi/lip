using System.IO.Abstractions;

namespace Lip;

public partial class Lip(RuntimeConfiguration runtimeConfig, IFileSystem filesystem, Serilog.ILogger logger)
{
    private const string PackageManifestFileName = "tooth.json";

    private readonly IFileSystem _filesystem = filesystem;
    private readonly Serilog.ILogger _logger = logger;
    private readonly RuntimeConfiguration _runtimeConfig = runtimeConfig;
}
