namespace EasySave.Services;

internal static class PausableFileCopy
{
    public static async Task Copy(
        string sourceFile,
        string destFile,
        CancellationToken ct)
    {
        const int bufferSize = 64 * 1024;

        using var source = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var dest = new FileStream(destFile, FileMode.Create, FileAccess.Write, FileShare.None);

        var buffer = new byte[bufferSize];
        int read;
        while ((read = await source.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
        {
            ct.ThrowIfCancellationRequested();
            await dest.WriteAsync(buffer.AsMemory(0, read), ct);
        }
    }
}
