using System;
using System.Drawing;
using System.Windows.Forms;
using KeyNest.App.Branding;
using KeyNest.UI.Services;

namespace KeyNest.App.Tray;

/// <summary>
/// 托盘图标与原生右键菜单。使用 .NET 8 自带的 WinForms NotifyIcon，
/// 菜单沿用系统原生可访问性，不自绘。菜单文本随界面语言切换。
/// </summary>
public sealed class TrayIconHost : IDisposable
{
    private NotifyIcon? _notifyIcon;
    private ContextMenuStrip? _menu;
    private Icon? _ownerIcon;

    private ToolStripMenuItem? _enableItem;
    private ToolStripMenuItem? _disableItem;
    private ToolStripMenuItem? _panelItem;
    private ToolStripMenuItem? _settingsItem;
    private ToolStripMenuItem? _autoStartItem;
    private ToolStripMenuItem? _exitItem;

    private bool _listening;
    private bool _panelVisible;
    private bool _autoStart;
    private bool _disposed;

    public event EventHandler? EnableRequested;
    public event EventHandler? DisableRequested;
    public event EventHandler? TogglePanelRequested;
    public event EventHandler? SettingsRequested;
    public event EventHandler<bool>? AutoStartToggleRequested;
    public event EventHandler? ExitRequested;

    public TrayIconHost()
    {
        _ownerIcon = BrandIconFactory.CreateIcon(32);
        _menu = BuildMenu();

        _notifyIcon = new NotifyIcon
        {
            Icon = _ownerIcon,
            Text = LocalizationService.Get("Tray.TitleIdle"),
            Visible = true,
            ContextMenuStrip = _menu
        };

        _notifyIcon.DoubleClick += (_, _) => TogglePanelRequested?.Invoke(this, EventArgs.Empty);
    }

    public void SetListening(bool listening)
    {
        _listening = listening;
        ApplyState();
    }

    public void SetPanelVisible(bool visible)
    {
        _panelVisible = visible;

        if (_panelItem is not null)
        {
            _panelItem.Text = LocalizationService.Get(visible ? "Tray.HidePanel" : "Tray.ShowPanel");
        }
    }

    public void SetAutoStart(bool enabled)
    {
        _autoStart = enabled;

        if (_autoStartItem is not null)
        {
            _autoStartItem.Checked = enabled;
        }
    }

    /// <summary>语言切换后刷新托盘提示与所有菜单项文本。</summary>
    public void ApplyLanguage()
    {
        if (_menu is not null)
        {
            _enableItem!.Text = LocalizationService.Get("Tray.Enable");
            _disableItem!.Text = LocalizationService.Get("Tray.Disable");
            _panelItem!.Text = LocalizationService.Get(_panelVisible ? "Tray.HidePanel" : "Tray.ShowPanel");
            _settingsItem!.Text = LocalizationService.Get("Tray.Settings");
            _autoStartItem!.Text = LocalizationService.Get("Tray.AutoStart");
            _exitItem!.Text = LocalizationService.Get("Tray.Exit");
        }

        ApplyState();
    }

    public void ShowNotification(string title, string message)
    {
        if (_notifyIcon is null)
        {
            return;
        }

        _notifyIcon.BalloonTipTitle = title;
        _notifyIcon.BalloonTipText = message;
        _notifyIcon.BalloonTipIcon = ToolTipIcon.None;
        _notifyIcon.ShowBalloonTip(3000);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_notifyIcon is not null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }

        _menu?.Dispose();
        _menu = null;

        _ownerIcon?.Dispose();
        _ownerIcon = null;
    }

    private void ApplyState()
    {
        if (_notifyIcon is not null)
        {
            _notifyIcon.Text = LocalizationService.Get(_listening ? "Tray.TitleListening" : "Tray.TitleIdle");
        }

        if (_enableItem is not null)
        {
            _enableItem.Enabled = !_listening;
        }

        if (_disableItem is not null)
        {
            _disableItem.Enabled = _listening;
        }
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip
        {
            ShowImageMargin = false,
            Font = new Font("Segoe UI", 9f)
        };

        _enableItem = new ToolStripMenuItem(LocalizationService.Get("Tray.Enable"));
        _enableItem.Click += (_, _) => EnableRequested?.Invoke(this, EventArgs.Empty);

        _disableItem = new ToolStripMenuItem(LocalizationService.Get("Tray.Disable"));
        _disableItem.Click += (_, _) => DisableRequested?.Invoke(this, EventArgs.Empty);

        _panelItem = new ToolStripMenuItem(LocalizationService.Get("Tray.ShowPanel"));
        _panelItem.Click += (_, _) => TogglePanelRequested?.Invoke(this, EventArgs.Empty);

        _settingsItem = new ToolStripMenuItem(LocalizationService.Get("Tray.Settings"));
        _settingsItem.Click += (_, _) => SettingsRequested?.Invoke(this, EventArgs.Empty);

        _autoStartItem = new ToolStripMenuItem(LocalizationService.Get("Tray.AutoStart"))
        {
            CheckOnClick = false
        };
        _autoStartItem.Click += (_, _) =>
        {
            var target = !_autoStartItem.Checked;
            AutoStartToggleRequested?.Invoke(this, target);
        };

        _exitItem = new ToolStripMenuItem(LocalizationService.Get("Tray.Exit"));
        _exitItem.Click += (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty);

        menu.Items.Add(_enableItem);
        menu.Items.Add(_disableItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_panelItem);
        menu.Items.Add(_settingsItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_autoStartItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_exitItem);

        return menu;
    }
}
