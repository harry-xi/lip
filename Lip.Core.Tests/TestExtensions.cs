namespace Lip.Core.Tests;

public static class LipTestExtensions
{
    public static byte[] ToJsonBytes(this PackageManifest manifest)
    {
        using var ms = new MemoryStream();
        PackageManifest.WriteToStreamAsync(manifest, ms).Wait();
        return ms.ToArray();
    }
    public static byte[] ToJsonBytes(this PackageLock lockFile)
    {
        using var ms = new MemoryStream();
        lockFile.ToStream(ms).Wait();
        return ms.ToArray();
    }
}