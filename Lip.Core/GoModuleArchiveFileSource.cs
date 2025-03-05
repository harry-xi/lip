using Golang.Org.X.Mod;
using Semver;
using System.IO.Abstractions;

namespace Lip.Core;

public class GoModuleArchiveFileSource(
    IFileSystem fileSystem,
    string archiveFilePath,
    string goModulePath,
    SemVersion version) : ArchiveFileSource(fileSystem, archiveFilePath)
{
    private readonly string _archiveFilePath = archiveFilePath;
    private readonly IFileSystem _fileSystem = fileSystem;
    private readonly string _prefix = $"{goModulePath}@{Module.CanonicalVersion("v" + version.ToString())}/";

    public override async Task<List<IFileSourceEntry>> GetAllEntries()
    {
        List<IFileSourceEntry> entries = [.. (await base.GetAllEntries())
            .Where(entry => entry.Key.StartsWith(_prefix))
            .Select(entry => new GoModuleArchiveFileSourceEntry(
                _fileSystem,
                _archiveFilePath,
                entry.Key[_prefix.Length..],
                entry.Key))];

        return entries;
    }

    public override async Task<IFileSourceEntry?> GetEntry(string key)
    {
        string archiveEntryKey = _prefix + key;

        if (await base.GetEntry(archiveEntryKey) is null)
        {
            return null;
        }

        return new GoModuleArchiveFileSourceEntry(
            _fileSystem,
            _archiveFilePath,
            key,
            archiveEntryKey);
    }
}

public class GoModuleArchiveFileSourceEntry(
    IFileSystem fileSystem,
    string archiveFilePath,
    string key,
    string archiveEntryKey) : ArchiveFileSourceEntry(fileSystem, archiveFilePath, archiveEntryKey)
{
    private readonly string _key = key;

    public override string Key => _key;
}
