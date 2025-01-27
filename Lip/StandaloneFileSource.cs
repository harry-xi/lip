using System.IO.Abstractions;

namespace Lip;

public class StandaloneFileSource(IFileSystem fileSystem, string filePath) : IFileSource
{
    private readonly string _filePath = filePath;
    private readonly IFileSystem _fileSystem = fileSystem;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public async Task<IFileSourceEntry> AddEntry(string key, Stream stream)
    {
        throw new NotImplementedException();
    }

    public async Task<IFileSourceEntry?> GetEntry(string key)
    {
        return (key == string.Empty)
            ? new StandaloneFileSourceEntry(_fileSystem, _filePath)
            : null;
    }

    public async Task RemoveEntry(string key)
    {
        throw new NotImplementedException();
    }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}

public class StandaloneFileSourceEntry(IFileSystem fileSystem, string filePath) : IFileSourceEntry
{
    private readonly string _filePath = filePath;
    private readonly IFileSystem _fileSystem = fileSystem;

    public bool IsDirectory => false;

    public string Key => string.Empty;

    public async Task<Stream> OpenEntryStream()
    {
        return await _fileSystem.File.OpenReadAsync(_filePath);
    }
}
