using System.IO.Abstractions;

namespace Lip.Core.Infrastructure;

public static class FileSystemExtensions
{
    public static FileSystemStream CreateFileWithDirectory(this IFileSystem fs, string path)
    {
        string? dirPath = fs.Path.GetDirectoryName(path);

        if (dirPath is not null)
        {
            fs.Directory.CreateDirectory(dirPath);
        }

        return fs.File.Create(path);
    }
}