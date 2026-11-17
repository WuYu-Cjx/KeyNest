using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using KeyNest.Core.Services;
using KeyNest.Installer.Services;
using KeyNest.UI.Services;

namespace KeyNest.Installer.Views.Pages;

public partial class ProgressPage : WizardPage
{
    private readonly InstallerContext _context;
    private bool _started;
    private bool _completed;

    public ProgressPage(InstallerContext context)
    {
        _context = context;
        InitializeComponent();
        DataContext = context;
    }

    public override string Title => LocalizationService.Get("Installer.Progress.Title");

    public override bool ShowBack => false;

    public override bool CanGoNext => false;

    public override bool IsCompleted => _completed;

    public bool InstallStarted => _started;

    public override void OnEnter()
    {
        if (_started)
        {
            return;
        }

        _started = true;
        _ = RunInstallAsync();
    }

    private async Task RunInstallAsync()
    {
        RetryPanel.Visibility = Visibility.Collapsed;
        ElevationPanel.Visibility = Visibility.Collapsed;
        _completed = false;
        _context.ProgressValue = 0;
        NotifyStateChanged();

        if (_context.Scope == InstallScope.PerMachine && !ElevationHelper.IsElevated)
        {
            ElevationPanel.Visibility = Visibility.Visible;
            _context.StatusText = LocalizationService.Get("Installer.Progress.NeedElevationStatus");
            NotifyStateChanged();
            return;
        }

        var progress = new Progress<InstallProgress>(p =>
        {
            _context.ProgressValue = Math.Clamp(p.Percent / 100.0, 0, 1);
            _context.StatusText = p.Message;
        });

        try
        {
            var service = new InstallService();
            await Task.Run(() => service.InstallAsync(_context, progress, CancellationToken.None));

            _completed = true;
            _context.ProgressValue = 1;
            _context.StatusText = LocalizationService.Get("Installer.Progress.DoneStatus");
            Logger.Info($"安装完成：{_context.TargetDirectory}");
        }
        catch (Exception ex)
        {
            _context.StatusText = LocalizationService.Format("Installer.Progress.FailedStatusFormat", ex.Message);
            RetryPanel.Visibility = Visibility.Visible;
            Logger.Error("安装失败", ex);
        }
        finally
        {
            NotifyStateChanged();
        }
    }

    private void OnRetryClick(object sender, RoutedEventArgs e)
    {
        _ = RunInstallAsync();
    }

    private void OnElevateClick(object sender, RoutedEventArgs e)
    {
        if (ElevationHelper.TryRelaunchElevated())
        {
            Window.GetWindow(this)?.Close();
            return;
        }

        MessageBox.Show(
            LocalizationService.Get("Installer.Progress.ElevationFailed"),
            LocalizationService.Get("Installer.WindowTitle"),
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }
}
