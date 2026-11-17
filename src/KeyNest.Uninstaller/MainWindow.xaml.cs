using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using KeyNest.UI.Interop;
using KeyNest.UI.Services;
using KeyNest.Uninstaller.Services;

namespace KeyNest.Uninstaller;

public partial class MainWindow : Window
{
    private readonly string _installDirectory = UninstallerPaths.InstallDirectory;
    private bool _busy;
    private bool _uninstalled;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    public bool KeepUserConfig { get; set; }

    public string InstallDirectory => _installDirectory;

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        WindowEffects.TryEnableRoundedCorners(this);
    }

    private async void OnUninstallClick(object sender, RoutedEventArgs e)
    {
        if (_busy)
        {
            return;
        }

        var confirm = MessageBox.Show(
            LocalizationService.Get("Uninstaller.Confirm"),
            LocalizationService.Get("Uninstaller.WindowTitle"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        _busy = true;
        UninstallButton.IsEnabled = false;
        CancelButton.IsEnabled = false;
        OptionsPanel.Visibility = Visibility.Collapsed;
        ProgressPanel.Visibility = Visibility.Visible;

        var progress = new Progress<string>(message => StatusText.Text = message);

        try
        {
            await Task.Run(() => new UninstallService().Run(KeepUserConfig, progress));
            _uninstalled = true;

            MessageBox.Show(
                LocalizationService.Get("Uninstaller.Done"),
                LocalizationService.Get("Uninstaller.WindowTitle"),
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                LocalizationService.Format("Uninstaller.ErrorFormat", ex.Message),
                LocalizationService.Get("Uninstaller.WindowTitle"),
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            StatusText.Text = LocalizationService.Format("Uninstaller.FailedStatusFormat", ex.Message);
            UninstallButton.IsEnabled = true;
            CancelButton.IsEnabled = true;
            _busy = false;
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        // 必须在卸载器窗口关闭后再安排自删除，否则卸载器自身占用 exe 会导致目录残留。
        if (_uninstalled)
        {
            UninstallService.ScheduleSelfDestruct(_installDirectory);
        }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        if (_busy)
        {
            return;
        }

        Close();
    }

    private void OnTitleBarMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        try
        {
            DragMove();
        }
        catch (InvalidOperationException)
        {
            // 忽略。
        }
    }
}
