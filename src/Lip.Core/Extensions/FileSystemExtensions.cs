using System.IO.Abstractions;

namespace Lip.Core.Extensions;

public static class FileSystemExtensions
{
    public static Stream CreateFileWithDirectory(this IFileSystem fs, string path)
    {
        string? dirPath = fs.Path.GetDirectoryName(path);

        if (dirPath is not null)
        {
            fs.Directory.CreateDirectory(dirPath);
        }

        return fs.File.Create(path);
    }
}