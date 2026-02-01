using Lip.Core.Services;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Lip.CLI;

[Description("Create a package from the current directory.")]
class PackCommand : AsyncCommand<PackCommand.Settings>
{
    public class Settings : BaseCommandSettings
    {
        [CommandArgument(0, "<path>")]
        [Description("The output path for the created archive.")]
        public required string OutputPath { get; init; }

        [CommandOption("--dry-run")]
        [Description("Do not actually create an archive.")]
        public required bool DryRun { get; init; }

        [CommandOption("--ignore-scripts")]
        [Description("Do not run any scripts during packing.")]
        public required bool IgnoreScripts { get; init; }

        [CommandOption("--archive-format <FORMAT>")]
        [Description("The format of the archive to create. Valid formats are `zip`, `tar`, `tgz` and `tar.gz`. Defaults to `zip`.")]
        [DefaultValue("zip")]
        public required string ArchiveFormat { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var ctx = await CommandRoot.CreateContext(settings);

        var packService = new PackService(ctx);

        PackService.ArchiveFormatType format = settings.ArchiveFormat switch
        {
            "zip" => PackService.ArchiveFormatType.Zip,
            "tar" => PackService.ArchiveFormatType.Tar,
            "tgz" or "tar.gz" => PackService.ArchiveFormatType.TarGz,
            _ => throw new ArgumentException($"Invalid archive format: {settings.ArchiveFormat}"),
        };

        await packService.Pack(
            settings.OutputPath,
            dryRun: settings.DryRun,
            ignoreScripts: settings.IgnoreScripts,
            archiveFormat: format);

        return 0;
    }
}