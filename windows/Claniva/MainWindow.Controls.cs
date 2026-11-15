using Claniva.Data;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Windowing;
using Windows.Graphics;
using Windows.UI;

namespace Claniva;

public sealed partial class MainWindow
{
    private static SolidColorBrush Brush(byte r, byte g, byte b) =>
        new(Color.FromArgb(255, r, g, b));

    private static TextBlock Text(string value, int size = 16, bool bold = false) => new()
    {
        Text = value, FontSize = size,
        FontWeight = bold ? Microsoft.UI.Text.FontWeights.Bold : Microsoft.UI.Text.FontWeights.Normal,
        TextWrapping = TextWrapping.Wrap,
        Foreground = Brush(23, 36, 58)
    };

    private static Border Panel(UIElement child) => new()
    {
        Child = child, Background = Brush(255, 255, 255),
        CornerRadius = new CornerRadius(16), BorderBrush = Brush(220, 229, 240),
        BorderThickness = new Thickness(1), Padding = new Thickness(12)
    };

    private static Button Action(string title, RoutedEventHandler handler)
    {
        var b = new Button { Content = title, HorizontalAlignment = HorizontalAlignment.Left };
        b.Click += handler;
        return b;
    }

    private static StackPanel Metric(string title, string amount, Color accent)
    {
        var panel = new StackPanel { Spacing = 10, Padding = new Thickness(13) };
        panel.Children.Add(Text(title, 15));
        var value = Text(amount, 23, true);
        value.Foreground = new SolidColorBrush(accent);
        panel.Children.Add(value);
        return panel;
    }
}
