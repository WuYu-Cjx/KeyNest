using KeyNest.UI.Services;

namespace KeyNest.Installer.Views.Pages;

public partial class LicensePage : WizardPage
{
    private readonly InstallerContext _context;

    public LicensePage(InstallerContext context)
    {
        _context = context;
        InitializeComponent();
        DataContext = context;
    }

    public override string Title => LocalizationService.Get("Installer.License.Title");

    public override bool CanGoNext => _context.LicenseAccepted;
}
