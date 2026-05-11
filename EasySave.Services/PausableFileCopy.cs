namespace EasySave.Services;

internal static class PausableFileCopy
{
    public static void Copy(
        string sourceFile,
        string destFile,
        Action<CancellationToken> waitForPause,
        CancellationToken ct)
    {
        const int bufferSize = 64 * 1024;

        using var source = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var dest = new FileStream(destFile, FileMode.Create, FileAccess.Write, FileShare.None);

        var buffer = new byte[bufferSize];
        int read;
        while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
        {
            ct.ThrowIfCancellationRequested();
            waitForPause(ct);
            dest.Write(buffer, 0, read);
        }
    }
}
