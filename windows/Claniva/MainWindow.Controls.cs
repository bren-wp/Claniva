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
        Child = child,
        Background = new SolidColorBrush(BrandPalette.White),
        CornerRadius = new CornerRadius(16),
        BorderBrush = new SolidColorBrush(BrandPalette.Stroke),
        BorderThickness = new Thickness(1),
        Padding = new Thickness(16),
        HorizontalAlignment = HorizontalAlignment.Stretch
    };

    private static Button Action(string title, RoutedEventHandler handler)
    {
        var b = new Button
        {
            Content = title,
            HorizontalAlignment = HorizontalAlignment.Left,
            MinHeight = 40,
            Padding = new Thickness(16, 8, 16, 8),
            Background = new SolidColorBrush(BrandPalette.Blue),
            Foreground = new SolidColorBrush(BrandPalette.White),
            BorderBrush = new SolidColorBrush(BrandPalette.Blue),
            CornerRadius = new CornerRadius(10)
        };
        b.Click += handler;
        return b;
    }

    private static StackPanel Metric(string title, string amount, Color accent)
    {
        var card = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 14,
            VerticalAlignment = VerticalAlignment.Center,
            MinHeight = 64
        };
        card.Children.Add(new Border
        {
            Background = new SolidColorBrush(accent),
            Width = 5, Height = 64, CornerRadius = new CornerRadius(4)
        });
        var content = new StackPanel
        {
            Spacing = 7, VerticalAlignment = VerticalAlignment.Center
        };
        var label = Text(title, 14);
        label.Foreground = new SolidColorBrush(BrandPalette.Muted);
        content.Children.Add(label);
        var value = Text(amount, 22, true);
        value.Foreground = new SolidColorBrush(BrandPalette.Navy);
        content.Children.Add(value);
        card.Children.Add(content);
        return card;
    }
}
