namespace Lip.Context;

public interface IGit
{
    Task Clone(string repository, string directory, int? depth = null);
}
