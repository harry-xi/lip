using System.IO.Abstractions;

namespace Lip;

public class DirectoryFileSource(IFileSystem fileSystem, string rootDirPath) : IFileSource
{
    private readonly string _rootDirPath = rootDirPath;
    private readonly IFileSystem _fileSystem = fileSystem;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public async Task<IFileSourceEntry> AddEntry(string key, Stream stream)
    {
        throw new NotImplementedException();
    }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

    public async Task<IFileSourceEntry?> GetEntry(string key)
    {
        string filePath = _fileSystem.Path.Join(_rootDirPath, key);

        if (await _fileSystem.Path.ExistsAsync(filePath))
        {
            return new DirectoryFileSourceEntry(_fileSystem, filePath, key);
        }

        return null;
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public async Task RemoveEntry(string key)
    {
        throw new NotImplementedException();
    }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}

public class DirectoryFileSourceEntry(IFileSystem fileSystem, string filePath, string key) : IFileSourceEntry
{
    private readonly string _filePath = filePath;
    private readonly IFileSystem _fileSystem = fileSystem;

    public bool IsDirectory => _fileSystem.Directory.Exists(_filePath);

    public string Key { get; } = key;

    public async Task<Stream> OpenEntryStream()
    {
        if (IsDirectory)
        {
            throw new NotSupportedException("Cannot open stream for a directory.");
        }

        return await _fileSystem.File.OpenReadAsync(_filePath);
    }
}
