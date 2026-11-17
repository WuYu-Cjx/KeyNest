using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using KeyNest.Installer.Views.Pages;
using KeyNest.UI.Interop;
using KeyNest.UI.Services;

namespace KeyNest.Installer.Views;

public partial class MainWindow : Window
{
    private readonly InstallerContext _context = new();
    private readonly List<WizardPage> _pages;
    private int _index;
    private bool _busy;

    public MainWindow()
    {
        InitializeComponent();

        _context.PropertyChanged += OnContextPropertyChanged;

        _pages = new List<WizardPage>
        {
            new WelcomePage(_context),
            new LicensePage(_context),
            new DirectoryPage(_context),
            new OptionsPage(_context),
            new ProgressPage(_context),
            new FinishPage(_context)
        };

        foreach (var page in _pages)
        {
            page.StateChanged += (_, _) => UpdateChrome();
        }

        LocalizationService.LanguageChanged += OnLanguageChanged;

        Navigate(0);
    }

    private void OnLanguageChanged(object? sender, EventArgs e) => UpdateChrome();

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        WindowEffects.TryEnableRoundedCorners(this);
    }

    private void OnContextPropertyChanged(object? sender, PropertyChangedEventArgs e) => UpdateChrome();

    private void Navigate(int index)
    {
        if (index < 0 || index >= _pages.Count)
        {
            return;
        }

        _index = index;
        var page = _pages[index];
        PageHost.Content = page;
        page.OnEnter();
        UpdateChrome();
    }

    private void UpdateChrome()
    {
        if (_pages is null || _index >= _pages.Count)
        {
            return;
        }

        var page = _pages[_index];

        StepText.Text = LocalizationService.Format("Installer.StepFormat", _index + 1, _pages.Count);

        BackButton.Visibility = page.ShowBack ? Visibility.Visible : Visibility.Collapsed;
        BackButton.IsEnabled = _index > 0 && !_busy;

        NextButton.Visibility = page.ShowNext ? Visibility.Visible : Visibility.Collapsed;
        NextButton.Content = page.NextText;
        NextButton.IsEnabled = !_busy && (page.CanGoNext || page.IsCompleted);
    }

    private async void OnNextClick(object sender, RoutedEventArgs e)
    {
        if (_index >= _pages.Count)
        {
            return;
        }

        var page = _pages[_index];

        if (!page.CanGoNext && !page.IsCompleted)
        {
            return;
        }

        _busy = true;
        UpdateChrome();

        try
        {
            var canContinue = await page.OnNextAsync();
            if (!canContinue)
            {
                return;
            }

            if (_index == _pages.Count - 1)
            {
                Close();
                return;
            }

            Navigate(_index + 1);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                LocalizationService.Format("Installer.ContinueError", Environment.NewLine + ex.Message),
                LocalizationService.Get("Installer.WindowTitle"),
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            _busy = false;
            UpdateChrome();
        }
    }

    private void OnBackClick(object sender, RoutedEventArgs e)
    {
        if (_index <= 0)
        {
            return;
        }

        Navigate(_index - 1);
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        var page = _pages[_index];
        if (page is ProgressPage { IsCompleted: false, InstallStarted: true })
        {
            var confirm = MessageBox.Show(
                LocalizationService.Get("Installer.CancelBusyConfirm"),
                LocalizationService.Get("Installer.WindowTitle"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }
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
            // 鼠标已释放，忽略。
        }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => OnCancelClick(sender, e);
}
