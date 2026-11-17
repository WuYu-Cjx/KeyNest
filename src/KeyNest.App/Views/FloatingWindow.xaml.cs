using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using KeyNest.App.ViewModels;
using KeyNest.Core.Models;
using KeyNest.UI.Interop;

namespace KeyNest.App.Views;

/// <summary>
/// 悬浮窗：Topmost、不抢焦点、可拖动、圆角半透明。
/// 仅展示快捷键说明，不转发或执行任何快捷键。
/// </summary>
public partial class FloatingWindow : Window
{
    private const double DefaultCardWidth = 424;
    private readonly FloatingViewModel _viewModel;

    private bool _dragging;
    private Point _dragStartCursor;
    private double _dragStartLeft;
    private double _dragStartTop;
    private double _dpiScaleX = 1;
    private double _dpiScaleY = 1;

    public FloatingWindow(FloatingViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

        InitializeComponent();

        DataContext = _viewModel;
    }

    /// <summary>拖动结束，由控制器负责持久化位置。</summary>
    public event EventHandler? DragCompleted;

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        if (PresentationSource.FromVisual(this) is HwndSource source)
        {
            WindowEffects.MakeNoActivateToolWindow(source.Handle);
        }
    }

    public void ShowPanel(AppSettings settings)
    {
        var targetOpacity = Math.Clamp(settings.Floating.Opacity, 0.75, 1.0);

        if (IsVisible)
        {
            BeginAnimation(OpacityProperty, null);
            Opacity = targetOpacity;
            return;
        }

        Opacity = 0;
        Show();
        UpdateLayout();

        if (!TryRestorePosition(settings))
        {
            PlaceDefault();
        }

        FadeTo(targetOpacity);
    }

    public void HidePanel()
    {
        if (!IsVisible)
        {
            return;
        }

        BeginAnimation(OpacityProperty, null);
        Opacity = 0;
        Hide();
    }

    public void ApplySettings(AppSettings settings)
    {
        if (!IsVisible)
        {
            return;
        }

        BeginAnimation(OpacityProperty, null);
        Opacity = Math.Clamp(settings.Floating.Opacity, 0.75, 1.0);
    }

    /// <summary>位置重置：回到屏幕底部居中。</summary>
    public void ResetPositionToDefault()
    {
        if (IsVisible)
        {
            PlaceDefault();
        }
    }

    private bool TryRestorePosition(AppSettings settings)
    {
        var position = settings.Position;
        if (position is null || !position.HasValue)
        {
            return false;
        }

        var left = position.Left;
        var top = position.Top;

        var virtualLeft = SystemParameters.VirtualScreenLeft;
        var virtualTop = SystemParameters.VirtualScreenTop;
        var virtualRight = virtualLeft + SystemParameters.VirtualScreenWidth;
        var virtualBottom = virtualTop + SystemParameters.VirtualScreenHeight;

        var minVisible = 80d;
        if (left + minVisible < virtualLeft || left > virtualRight - minVisible)
        {
            return false;
        }

        if (top + minVisible < virtualTop || top > virtualBottom - minVisible)
        {
            return false;
        }

        Left = left;
        Top = top;
        return true;
    }

    private void PlaceDefault()
    {
        Width = DefaultCardWidth;

        var workAreaLeft = SystemParameters.VirtualScreenLeft;
        var workAreaTop = SystemParameters.VirtualScreenTop;
        var workAreaWidth = SystemParameters.VirtualScreenWidth;
        var workAreaHeight = SystemParameters.VirtualScreenHeight;

        var height = ActualHeight > 0 ? ActualHeight : 320;
        var width = ActualWidth > 0 ? ActualWidth : DefaultCardWidth;

        Left = workAreaLeft + Math.Max(0, (workAreaWidth - width) / 2);
        Top = workAreaTop + Math.Max(0, workAreaHeight - height - 72);
    }

    private void FadeTo(double target)
    {
        if (!SystemParameters.ClientAreaAnimation)
        {
            BeginAnimation(OpacityProperty, null);
            Opacity = target;
            return;
        }

        var animation = new DoubleAnimation
        {
            To = target,
            Duration = TimeSpan.FromMilliseconds(130),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };

        BeginAnimation(OpacityProperty, animation);
    }

    private void OnHeaderMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        if (!WindowEffects.TryGetCursorPosition(out _dragStartCursor))
        {
            return;
        }

        var dpi = VisualTreeHelper.GetDpi(this);
        _dpiScaleX = dpi.DpiScaleX <= 0 ? 1 : dpi.DpiScaleX;
        _dpiScaleY = dpi.DpiScaleY <= 0 ? 1 : dpi.DpiScaleY;

        _dragStartLeft = Left;
        _dragStartTop = Top;
        _dragging = true;

        if (sender is UIElement element)
        {
            element.CaptureMouse();
        }

        e.Handled = true;
    }

    private void OnHeaderMouseMove(object sender, MouseEventArgs e)
    {
        if (!_dragging || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        if (!WindowEffects.TryGetCursorPosition(out var current))
        {
            return;
        }

        Left = _dragStartLeft + (current.X - _dragStartCursor.X) / _dpiScaleX;
        Top = _dragStartTop + (current.Y - _dragStartCursor.Y) / _dpiScaleY;
    }

    private void OnHeaderMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_dragging)
        {
            return;
        }

        _dragging = false;

        if (sender is UIElement element)
        {
            element.ReleaseMouseCapture();
        }

        DragCompleted?.Invoke(this, EventArgs.Empty);
    }
}
