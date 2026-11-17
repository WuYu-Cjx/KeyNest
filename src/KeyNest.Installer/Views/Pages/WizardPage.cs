using System.Threading.Tasks;
using System.Windows.Controls;
using KeyNest.UI.Services;

namespace KeyNest.Installer.Views.Pages;

/// <summary>安装向导页面基类：由 MainWindow 统一控制“上一步 / 下一步”导航。</summary>
public abstract class WizardPage : UserControl
{
    public abstract string Title { get; }

    public virtual string NextText => LocalizationService.Get("Common.Next");

    public virtual bool ShowBack => true;

    public virtual bool ShowNext => true;

    public virtual bool CanGoNext => true;

    /// <summary>进度页安装完成后为 true，用于放行“下一步”。</summary>
    public virtual bool IsCompleted => false;

    public event System.EventHandler? StateChanged;

    public virtual void OnEnter()
    {
    }

    public virtual Task<bool> OnNextAsync() => Task.FromResult(true);

    protected void NotifyStateChanged() => StateChanged?.Invoke(this, System.EventArgs.Empty);
}
