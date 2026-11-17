using System;
using KeyNest.Core.Models;
using KeyNest.Core.Mvvm;
using KeyNest.Installer.Services;
using KeyNest.UI.Services;

namespace KeyNest.Installer;

/// <summary>安装向导的共享状态，所有页面通过数据绑定读写。</summary>
public sealed class InstallerContext : ObservableObject
{
    private InstallScope _scope = InstallScope.PerUser;
    private string _targetDirectory = InstallerPaths.DefaultPerUserDirectory;
    private bool _licenseAccepted;
    private bool _createDesktopShortcut = true;
    private bool _createStartMenuShortcut = true;
    private bool _enableAutoStart;
    private string _validationMessage = string.Empty;
    private bool _isTargetValid;
    private double _progressValue;
    private string _statusText = string.Empty;
    private string? _installedVersion;
    private AppLanguage _language = AppLanguage.Chinese;

    public InstallerContext()
    {
        _statusText = LocalizationService.Get("Installer.Progress.Preparing");
        InstalledVersion = InstalledVersionReader.GetInstalledVersion();
        RefreshValidation();
    }

    // ===== 语言 =====

    /// <summary>安装程序与键巢首次启动使用的语言，默认简体中文。</summary>
    public AppLanguage Language
    {
        get => _language;
        private set => SetProperty(ref _language, value);
    }

    public bool LanguageChinese
    {
        get => Language == AppLanguage.Chinese;
        set
        {
            if (value)
            {
                SetLanguage(AppLanguage.Chinese);
            }
        }
    }

    public bool LanguageEnglish
    {
        get => Language == AppLanguage.English;
        set
        {
            if (value)
            {
                SetLanguage(AppLanguage.English);
            }
        }
    }

    private void SetLanguage(AppLanguage language)
    {
        if (language == _language)
        {
            return;
        }

        Language = language;
        LocalizationService.SetLanguage(language);

        OnPropertyChanged(nameof(LanguageChinese));
        OnPropertyChanged(nameof(LanguageEnglish));
        OnPropertyChanged(nameof(ScopeNote));
        OnPropertyChanged(nameof(PreviousVersionText));
    }

    public InstallScope Scope
    {
        get => _scope;
        set
        {
            if (!SetProperty(ref _scope, value))
            {
                return;
            }

            TargetDirectory = InstallerPaths.GetDefaultDirectory(value);
            OnPropertyChanged(nameof(ScopeNote));
            OnPropertyChanged(nameof(IsPerMachine));
            OnPropertyChanged(nameof(IsPerUser));
            OnPropertyChanged(nameof(IsPerMachineSelected));
        }
    }

    public bool IsPerMachine => Scope == InstallScope.PerMachine;

    public bool IsPerUser
    {
        get => Scope == InstallScope.PerUser;
        set
        {
            if (value)
            {
                Scope = InstallScope.PerUser;
            }
        }
    }

    public bool IsPerMachineSelected
    {
        get => Scope == InstallScope.PerMachine;
        set
        {
            if (value)
            {
                Scope = InstallScope.PerMachine;
            }
        }
    }

    public string ScopeNote => LocalizationService.Get(Scope == InstallScope.PerMachine
        ? "Installer.Directory.ScopeNotePerMachine"
        : "Installer.Directory.ScopeNotePerUser");

    public string TargetDirectory
    {
        get => _targetDirectory;
        set
        {
            if (SetProperty(ref _targetDirectory, value))
            {
                RefreshValidation();
            }
        }
    }

    public bool LicenseAccepted
    {
        get => _licenseAccepted;
        set => SetProperty(ref _licenseAccepted, value);
    }

    public bool CreateDesktopShortcut
    {
        get => _createDesktopShortcut;
        set => SetProperty(ref _createDesktopShortcut, value);
    }

    public bool CreateStartMenuShortcut
    {
        get => _createStartMenuShortcut;
        set => SetProperty(ref _createStartMenuShortcut, value);
    }

    public bool EnableAutoStart
    {
        get => _enableAutoStart;
        set => SetProperty(ref _enableAutoStart, value);
    }

    public string ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    public bool IsTargetValid
    {
        get => _isTargetValid;
        private set => SetProperty(ref _isTargetValid, value);
    }

    public double ProgressValue
    {
        get => _progressValue;
        set => SetProperty(ref _progressValue, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    /// <summary>检测到的旧版本号；没有安装过则为 null。</summary>
    public string? InstalledVersion
    {
        get => _installedVersion;
        private set
        {
            if (SetProperty(ref _installedVersion, value))
            {
                OnPropertyChanged(nameof(HasPreviousVersion));
                OnPropertyChanged(nameof(PreviousVersionText));
            }
        }
    }

    public bool HasPreviousVersion => !string.IsNullOrWhiteSpace(InstalledVersion);

    public string PreviousVersionText => HasPreviousVersion
        ? LocalizationService.Format("Installer.Directory.PreviousVersionFormat", InstalledVersion)
        : string.Empty;

    public string AppExecutablePath => System.IO.Path.Combine(TargetDirectory, InstallerPaths.AppExeName);

    private void RefreshValidation()
    {
        var result = InstallValidator.Validate(TargetDirectory, Scope);
        IsTargetValid = result.IsValid;
        ValidationMessage = result.Message;
    }
}
