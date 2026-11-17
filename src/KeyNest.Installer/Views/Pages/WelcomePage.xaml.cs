using KeyNest.UI.Services;

namespace KeyNest.Installer.Views.Pages;

public partial class WelcomePage : WizardPage
{
    public WelcomePage(InstallerContext context)
    {
        InitializeComponent();
        DataContext = context;
    }

    public override string Title => LocalizationService.Get("Installer.Welcome.Title");
}
