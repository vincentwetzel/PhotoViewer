namespace PhotoViewer.Services;

public readonly record struct ImportCopyProgress(int Index, int Total, string FileName);

public readonly record struct ImportCopySummary(int Imported, int Skipped, int Failed, bool Canceled)
{
    public IReadOnlyList<string> FailedFiles { get; init; } = Array.Empty<string>();
}

public sealed class ImportCopyService
{
    public async Task<ImportCopySummary> CopyAsync(
        IReadOnlyList<string> sourcePaths,
        string destinationDirectory,
        IProgress<ImportCopyProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var imported = 0;
        var skipped = 0;
        var failed = 0;
        var failedFiles = new List<string>();
        var canceled = false;

        for (var index = 0; index < sourcePaths.Count; index++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                canceled = true;
                break;
            }

            var source = sourcePaths[index];
            var fileName = Path.GetFileName(source);
            progress?.Report(new ImportCopyProgress(index + 1, sourcePaths.Count, fileName));
            var target = Path.Combine(destinationDirectory, fileName);
            try
            {
                if (File.Exists(target))
                    skipped++;
                else
                {
                    await Task.Run(() => File.Copy(source, target), cancellationToken);
                    imported++;
                }
            }
            catch (OperationCanceledException)
            {
                canceled = true;
                break;
            }
            catch
            {
                failed++;
                failedFiles.Add(fileName);
            }
        }

        return new ImportCopySummary(imported, skipped, failed, canceled)
        {
            FailedFiles = failedFiles
        };
    }
}
