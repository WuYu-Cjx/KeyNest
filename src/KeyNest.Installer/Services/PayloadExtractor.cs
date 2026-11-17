using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using KeyNest.UI.Services;

namespace KeyNest.Installer.Services;

/// <summary>
/// 解析并解压嵌入在安装器中的负载（payload.zip）。
/// 完全离线：不下载任何内容。
/// </summary>
public static class PayloadExtractor
{
    private const string ResourceName = "KeyNest.Payload.zip";

    public static bool PayloadExists
    {
        get
        {
            using var stream = typeof(PayloadExtractor).Assembly.GetManifestResourceStream(ResourceName);
            return stream is not null;
        }
    }

    public static async Task ExtractAsync(string targetDirectory, Action<int>? onProgress, CancellationToken cancellationToken)
    {
        await using var payload = typeof(PayloadExtractor).Assembly.GetManifestResourceStream(ResourceName)
                                  ?? throw new InvalidOperationException(LocalizationService.Get("Installer.Error.PayloadMissing"));

        using var archive = new ZipArchive(payload, ZipArchiveMode.Read);

        var root = Path.GetFullPath(targetDirectory);
        var entries = archive.Entries;
        var total = Math.Max(1, entries.Count);
        var index = 0;

        foreach (var entry in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrEmpty(entry.Name))
            {
                index++;
                continue;
            }

            var destination = Path.GetFullPath(Path.Combine(root, entry.FullName));

            if (!destination.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(LocalizationService.Format("Installer.Error.InvalidEntryFormat", entry.FullName));
            }

            var directory = Path.GetDirectoryName(destination);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            entry.ExtractToFile(destination, overwrite: true);

            index++;
            onProgress?.Invoke((int)(index * 100.0 / total));
        }
    }
}
