using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using KeyNest.Core.Models;
using KeyNest.Core.Mvvm;
using KeyNest.Core.Services;
using KeyNest.UI.Services;

namespace KeyNest.App.ViewModels;

/// <summary>悬浮窗中的一条快捷键。</summary>
public sealed class ShortcutRow : ObservableObject
{
    private bool _isHighlighted;

    public ShortcutRow(ShortcutItem item, AppLanguage language)
    {
        Item = item;
        Action = item.ActionText(language);
        NoteText = item.NotesText(language);
        HasNote = item.HasNotes && !string.IsNullOrWhiteSpace(NoteText);
    }

    public ShortcutItem Item { get; }

    public string Display => Item.Display;

    public string Action { get; }

    public string NoteText { get; }

    public bool HasNote { get; }

    public string OsText => Item.Os;

    public bool HasOsBadge => Item.HasOsBadge;

    public bool IsHighlighted
    {
        get => _isHighlighted;
        set => SetProperty(ref _isHighlighted, value);
    }
}

/// <summary>悬浮窗中的分组标题行。</summary>
public sealed class ShortcutSectionHeader
{
    public ShortcutSectionHeader(string title, string countText)
    {
        Title = title;
        CountText = countText;
    }

    public string Title { get; }

    public string CountText { get; }
}

public sealed class FloatingViewModel : ObservableObject
{
    private string _prefixLabel = string.Empty;
    private string _prefixTitle = string.Empty;
    private string _footerNote = string.Empty;

    public ObservableCollection<object> Rows { get; } = new();

    /// <summary>标题栏中的短前缀，例如 “Ctrl+Alt”。</summary>
    public string PrefixLabel
    {
        get => _prefixLabel;
        private set => SetProperty(ref _prefixLabel, value);
    }

    /// <summary>标题栏主标题，例如 “Ctrl+Alt 相关快捷键”。</summary>
    public string PrefixTitle
    {
        get => _prefixTitle;
        private set => SetProperty(ref _prefixTitle, value);
    }

    public string FooterNote
    {
        get => _footerNote;
        private set => SetProperty(ref _footerNote, value);
    }

    public bool HasRows => Rows.Count > 0;

    public string EmptyText => LocalizationService.Get("Floating.Empty");

    /// <summary>按当前修饰键集合展示（P 非空）。</summary>
    public void Load(ModifierMask prefix, IReadOnlyList<ShortcutSection> sections)
    {
        Fill(sections);

        var label = prefix.ToLabel();
        PrefixLabel = label;
        PrefixTitle = LocalizationService.Format("Floating.PrefixTitleFormat", label);
    }

    /// <summary>托盘手动打开的全量概览。</summary>
    public void LoadOverview(IReadOnlyList<ShortcutSection> sections)
    {
        Fill(sections);

        PrefixLabel = LocalizationService.Get("Floating.OverviewTag");
        PrefixTitle = LocalizationService.Get("Floating.OverviewTitle");
    }

    public void Clear()
    {
        Rows.Clear();
        OnPropertyChanged(nameof(HasRows));
    }

    /// <summary>按下主键时高亮对应条目，仅改变视觉，不执行任何操作。</summary>
    public void Highlight(int virtualKey)
    {
        foreach (var row in Rows.OfType<ShortcutRow>())
        {
            row.IsHighlighted = KeyNames.Matches(row.Item.Key, virtualKey);
        }
    }

    private void Fill(IReadOnlyList<ShortcutSection> sections)
    {
        Rows.Clear();

        var language = LocalizationService.Current;

        foreach (var section in sections)
        {
            if (section.Items.Count == 0)
            {
                continue;
            }

            var title = section.Mask == ModifierMask.None
                ? LocalizationService.Get("Floating.NoModifiers")
                : section.Label;

            var count = LocalizationService.Format("Floating.CountFormat", section.Items.Count);

            Rows.Add(new ShortcutSectionHeader(title, count));

            foreach (var item in section.Items)
            {
                Rows.Add(new ShortcutRow(item, language));
            }
        }

        FooterNote = LocalizationService.Get("Floating.Footer");

        OnPropertyChanged(nameof(HasRows));
        OnPropertyChanged(nameof(EmptyText));
    }
}
