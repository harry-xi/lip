using Flurl;
using Lip.Core.SourceProviders;

namespace Lip.Core.Services;

public interface ISourceService
{
    enum ParsingMode
    {
        Composite,
        Single,
    }

    Task<ISourceProvider> Get(Url url, ParsingMode parsingMode);
}