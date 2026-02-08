using Flurl;
using Lip.Core.FileSources;

namespace Lip.Core.Services;

public interface ISourceService
{
    enum ParsingMode
    {
        Archive,
        File,
        Directory,
    }

    Task<IFileSource> Get(Url url, ParsingMode parsingMode);
}