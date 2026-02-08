using Flurl;
using Lip.Core.FileSources;

namespace Lip.Core.Services;

public interface ISourceService
{
    enum ParsingMode
    {
        Composite,
        Single,
    }

    Task<IFileSource> Get(Url url, ParsingMode parsingMode);
}