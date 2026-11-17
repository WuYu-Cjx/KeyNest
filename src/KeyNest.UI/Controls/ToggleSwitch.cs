using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace KeyNest.UI.Controls;

/// <summary>
/// 自绘开关控件。仅负责外观与动画，不含业务逻辑；
/// 系统关闭“显示动画”时（减少动画设置）直接跳到目标位置。
/// </summary>
public class ToggleSwitch : ToggleButton
{
    private const double ThumbSize = 18;
    private const double ThumbMargin = 3;
    private const double FallbackTravel = 20;

    private TranslateTransform? _thumbTransform;

    static ToggleSwitch()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(ToggleSwitch),
            new FrameworkPropertyMetadata(typeof(ToggleSwitch)));
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _thumbTransform = GetTemplateChild("ThumbTransform") as TranslateTransform
                          ?? (GetTemplateChild("Thumb") as Border)?.RenderTransform as TranslateTransform;

        UpdateThumb(animate: false);
    }

    protected override void OnChecked(RoutedEventArgs e)
    {
        base.OnChecked(e);
        UpdateThumb(animate: true);
    }

    protected override void OnUnchecked(RoutedEventArgs e)
    {
        base.OnUnchecked(e);
        UpdateThumb(animate: true);
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        UpdateThumb(animate: false);
    }

    private void UpdateThumb(bool animate)
    {
        if (_thumbTransform is null)
        {
            return;
        }

        var travel = ActualWidth > ThumbSize + ThumbMargin * 2
            ? ActualWidth - ThumbSize - ThumbMargin * 2
            : FallbackTravel;

        var target = IsChecked == true ? travel : 0d;

        if (!animate || !SystemParameters.ClientAreaAnimation)
        {
            _thumbTransform.BeginAnimation(TranslateTransform.XProperty, null);
            _thumbTransform.X = target;
            return;
        }

        var animation = new DoubleAnimation
        {
            To = target,
            Duration = TimeSpan.FromMilliseconds(140),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        _thumbTransform.BeginAnimation(TranslateTransform.XProperty, animation);
    }
}
