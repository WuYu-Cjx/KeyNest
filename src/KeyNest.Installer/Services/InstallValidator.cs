using System;
using System.IO;
using KeyNest.UI.Services;

namespace KeyNest.Installer.Services;

public readonly record struct ValidationResult(bool IsValid, string Message);

/// <summary>
/// 安装目录校验：非法字符、根目录、受保护目录、磁盘空间与写入权限。
/// 只做本机只读检查与一次探测写入（测试文件随即删除）。
/// </summary>
public static class InstallValidator
{
    private const long RequiredFreeBytes = 200L * 1024 * 1024;

    public static ValidationResult Validate(string? directory, InstallScope scope)
    {
        if (string.IsNullOrWhiteSpace(directory))
        {
            return new ValidationResult(false, LocalizationService.Get("Installer.Validation.Empty"));
        }

        var trimmed = directory.Trim();

        if (trimmed.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
        {
            return new ValidationResult(false, LocalizationService.Get("Installer.Validation.InvalidChars"));
        }

        if (!Path.IsPathRooted(trimmed))
        {
            return new ValidationResult(false, LocalizationService.Get("Installer.Validation.NotRooted"));
        }

        try
        {
            var full = Path.GetFullPath(trimmed);

            if (full.Length > 180)
            {
                return new ValidationResult(false, LocalizationService.Get("Installer.Validation.TooLong"));
            }

            var root = Path.GetPathRoot(full);
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
            {
                return new ValidationResult(false, LocalizationService.Get("Installer.Validation.DriveMissing"));
            }

            if (string.IsNullOrWhiteSpace(Path.GetFileName(full.TrimEnd(Path.DirectorySeparatorChar))))
            {
                return new ValidationResult(false, LocalizationService.Get("Installer.Validation.DriveRoot"));
            }

            if (IsProtectedDirectory(full))
            {
                return new ValidationResult(false, LocalizationService.Get("Installer.Validation.Protected"));
            }

            var drive = new DriveInfo(root);
            if (drive.AvailableFreeSpace < RequiredFreeBytes)
            {
                return new ValidationResult(false, LocalizationService.Get("Installer.Validation.NoSpace"));
            }

            if (!CanWrite(full))
            {
                return new ValidationResult(false, LocalizationService.Get("Installer.Validation.NoPermission"));
            }

            return new ValidationResult(true, LocalizationService.Get("Installer.Validation.Ok"));
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return new ValidationResult(false, LocalizationService.Format("Installer.Validation.InvalidFormat", ex.Message));
        }
    }

    private static bool IsProtectedDirectory(string fullPath)
    {
        var normalized = fullPath.TrimEnd(Path.DirectorySeparatorChar);

        var protectedPaths = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.Windows),
            Environment.GetFolderPath(Environment.SpecialFolder.System),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            Path.GetPathRoot(normalized)
        };

        foreach (var candidate in protectedPaths)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            if (string.Equals(normalized, candidate.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool CanWrite(string directory)
    {
        try
        {
            // 不创建目标目录（用户可能还在输入路径），只在最近已存在的上级目录做一次探测写入。
            var probeDirectory = directory;

            while (!Directory.Exists(probeDirectory))
            {
                var parent = Path.GetDirectoryName(probeDirectory);
                if (string.IsNullOrEmpty(parent))
                {
                    return false;
                }

                probeDirectory = parent;
            }

            var probe = Path.Combine(probeDirectory, ".keynest_probe_" + Guid.NewGuid().ToString("N") + ".tmp");
            File.WriteAllText(probe, "keynest");
            File.Delete(probe);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return false;
        }
    }
}
