using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using KeyNest.Core.Models;
using KeyNest.Core.Services;
using KeyNest.Installer.Services;
using KeyNest.UI.Services;

namespace KeyNest.Installer.Views.Pages;

public partial class FinishPage : WizardPage
{
    private readonly InstallerContext _context;

    public FinishPage(InstallerContext context)
    {
        _context = context;
        InitializeComponent();
        DataContext = this;
    }

    public override string Title => LocalizationService.Get("Installer.Finish.Title");

    public override string NextText => LocalizationService.Get("Common.Finish");

    public override bool ShowBack => false;

    /// <summary>完成后是否立即启动键巢。</summary>
    public bool RunNow { get; set; } = true;

    public string InstallPath => _context.TargetDirectory;

    public override Task<bool> OnNextAsync()
    {
        if (!RunNow)
        {
            return Task.FromResult(true);
        }

        try
        {
            var executable = Path.Combine(_context.TargetDirectory, InstallerPaths.AppExeName);
            if (File.Exists(executable))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = executable,
                    Arguments = $"--enabled --language {_context.Language.ToCode()}",
                    WorkingDirectory = _context.TargetDirectory,
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"启动键巢失败：{ex.Message}");
        }

        return Task.FromResult(true);
    }
}
