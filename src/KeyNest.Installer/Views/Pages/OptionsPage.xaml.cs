using KeyNest.UI.Services;

namespace KeyNest.Installer.Views.Pages;

public partial class OptionsPage : WizardPage
{
    public OptionsPage(InstallerContext context)
    {
        InitializeComponent();
        DataContext = context;
    }

    public override string Title => LocalizationService.Get("Installer.Options.Title");
}
