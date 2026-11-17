using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using KeyNest.Installer.Services;
using KeyNest.UI.Services;
using Microsoft.Win32;

namespace KeyNest.Installer.Views.Pages;

public partial class DirectoryPage : WizardPage
{
    private readonly InstallerContext _context;

    public DirectoryPage(InstallerContext context)
    {
        _context = context;
        InitializeComponent();
        DataContext = context;
    }

    public override string Title => LocalizationService.Get("Installer.Directory.Title");

    public override bool CanGoNext => _context.IsTargetValid;

    private void OnBrowseClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = LocalizationService.Get("Installer.Directory.BrowseTitle"),
            Multiselect = false
        };

        if (Directory.Exists(_context.TargetDirectory))
        {
            dialog.InitialDirectory = _context.TargetDirectory;
        }

        if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.FolderName))
        {
            _context.TargetDirectory = dialog.FolderName;
        }
    }
}
