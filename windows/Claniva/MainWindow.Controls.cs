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

    private static Grid Metric(string title, string amount, Color accent, bool condensed = false)
    {
        // A horizontal StackPanel measures TextBlocks with unbounded width.
        // A two-column Grid constrains long EUR amounts to the actual card.
        var card = new Grid { ColumnSpacing = condensed ? 8 : 14, MinHeight = 64 };
        card.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(5) });
        card.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var accentBar = new Border
        {
            Background = new SolidColorBrush(accent),
            Width = 5, Height = 64, CornerRadius = new CornerRadius(4),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(accentBar, 0);
        card.Children.Add(accentBar);

        var content = new StackPanel { Spacing = 7, VerticalAlignment = VerticalAlignment.Center };
        var label = Text(title, 14);
        label.Foreground = new SolidColorBrush(BrandPalette.Muted);
        content.Children.Add(label);
        var value = Text(amount, condensed ? 14 : 22, true);
        value.Foreground = new SolidColorBrush(BrandPalette.Navy);
        value.TextWrapping = TextWrapping.WrapWholeWords;
        content.Children.Add(value);
        Grid.SetColumn(content, 1);
        card.Children.Add(content);
        return card;
    }
}
