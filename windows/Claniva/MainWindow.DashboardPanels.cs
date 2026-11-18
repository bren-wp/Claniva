using Claniva.Data;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;

namespace Claniva;

public sealed partial class MainWindow
{
    // Two real-data panels from the Windows dashboard design concept. No
    // fabricated graph coordinates or invented paid-member counts.
    private void ShowReferenceAnalytics(Summary summary)
    {
        var payments = _store.Ledger("payment");
        var history = DashboardAnalytics.LastSixMonths(payments, DateTime.Now);
        var chart = Panel(BuildMonthlyChart(history));
        var status = Panel(BuildCollectionStatus(summary));
        var wide = (ContentScroller?.ActualWidth ?? 0) >= 760;
        var row = new Grid { ColumnSpacing = 14, RowSpacing = 14 };
        row.ColumnDefinitions.Add(new ColumnDefinition
        {
            Width = new GridLength(wide ? 1.6 : 1, GridUnitType.Star)
        });
        if (wide)
            row.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(1, GridUnitType.Star)
            });
        row.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        if (!wide)
            row.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        Grid.SetColumn(chart, 0);
        Grid.SetRow(chart, 0);
        row.Children.Add(chart);
        Grid.SetColumn(status, wide ? 1 : 0);
        Grid.SetRow(status, wide ? 0 : 1);
        row.Children.Add(status);
        PageContent.Children.Add(row);
    }

    private static StackPanel BuildMonthlyChart(IReadOnlyList<DashboardAnalytics.MonthlyTotal> buckets)
    {
        var root = new StackPanel { Spacing = 10 };
        var heading = Text("Uplate po mjesecima", 21, true);
        heading.Foreground = new SolidColorBrush(BrandPalette.Navy);
        root.Children.Add(heading);

        var max = Math.Max(1L, buckets.Max(x => x.Cents));
        const double top = 24;
        const double bottom = 144;
        const double startX = 26;
        const double gap = 70;
        var graph = new Canvas { Width = 410, Height = 188 };
        var axis = new Line
        {
            X1 = startX, Y1 = bottom, X2 = startX + gap * 5,
            Y2 = bottom, Stroke = new SolidColorBrush(BrandPalette.Stroke), StrokeThickness = 1.5
        };
        graph.Children.Add(axis);

        var line = new Polyline
        {
            Stroke = new SolidColorBrush(BrandPalette.Teal),
            StrokeThickness = 3,
            StrokeLineJoin = PenLineJoin.Round
        };
        for (int i = 0; i < buckets.Count; i++)
        {
            var x = startX + gap * i;
            var y = bottom - (bottom - top) * (double)buckets[i].Cents / max;
            line.Points.Add(new Point(x, y));
        }
        graph.Children.Add(line);
        for (int i = 0; i < buckets.Count; i++)
        {
            var x = startX + gap * i;
            var y = bottom - (bottom - top) * (double)buckets[i].Cents / max;
            var dot = new Ellipse
            {
                Width = 8, Height = 8,
                Fill = new SolidColorBrush(BrandPalette.Teal),
                Stroke = new SolidColorBrush(BrandPalette.White), StrokeThickness = 1.5
            };
            Canvas.SetLeft(dot, x - 4);
            Canvas.SetTop(dot, y - 4);
            graph.Children.Add(dot);
            var caption = Text(buckets[i].Label, 12);
            caption.Foreground = new SolidColorBrush(BrandPalette.Muted);
            Canvas.SetLeft(caption, x - 14);
            Canvas.SetTop(caption, 156);
            graph.Children.Add(caption);
        }
        var view = new Viewbox
        {
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            MaxHeight = 188,
            Child = graph
        };
        root.Children.Add(view);
        var explanation = Text(
            buckets.All(x => x.Cents == 0)
                ? "Nema evidentiranih uplata u posljednjih šest mjeseci."
                : "Posljednjih šest mjeseci · evidentirane, ne bankovno potvrđene uplate.", 12);
        explanation.Foreground = new SolidColorBrush(BrandPalette.Muted);
        root.Children.Add(explanation);
        return root;
    }

    private static StackPanel BuildCollectionStatus(Summary summary)
    {
        var root = new StackPanel { Spacing = 12 };
        var heading = Text("Status članarina", 21, true);
        heading.Foreground = new SolidColorBrush(BrandPalette.Navy);
        root.Children.Add(heading);

        var percent = DashboardAnalytics.CoveredPercent(summary);
        const double size = 164;
        const double center = size / 2;
        const double radius = 62;
        var ring = new Grid { Width = size, Height = size, HorizontalAlignment = HorizontalAlignment.Center };
        ring.Children.Add(new Ellipse
        {
            Width = radius * 2, Height = radius * 2,
            Stroke = new SolidColorBrush(BrandPalette.Stroke),
            StrokeThickness = 18,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        });
        if (percent >= 99.999)
        {
            ring.Children.Add(new Ellipse
            {
                Width = radius * 2, Height = radius * 2,
                Stroke = new SolidColorBrush(BrandPalette.Teal),
                StrokeThickness = 18,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            });
        }
        else if (percent > 0)
        {
            var radians = (percent / 100 * 2 * Math.PI) - Math.PI / 2;
            var figure = new PathFigure { StartPoint = new Point(center, center - radius) };
            figure.Segments.Add(new ArcSegment
            {
                Point = new Point(center + radius * Math.Cos(radians),
                                  center + radius * Math.Sin(radians)),
                Size = new Size(radius, radius),
                SweepDirection = SweepDirection.Clockwise,
                IsLargeArc = percent > 50
            });
            var geometry = new PathGeometry();
            geometry.Figures.Add(figure);
            ring.Children.Add(new Microsoft.UI.Xaml.Shapes.Path
            {
                Data = geometry,
                Stroke = new SolidColorBrush(BrandPalette.Teal),
                StrokeThickness = 18,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            });
        }
        var value = Text(percent.ToString("0.0", System.Globalization.CultureInfo.GetCultureInfo("hr-HR")) + " %", 26, true);
        value.Foreground = new SolidColorBrush(BrandPalette.Navy);
        value.HorizontalAlignment = HorizontalAlignment.Center;
        value.VerticalAlignment = VerticalAlignment.Center;
        ring.Children.Add(value);
        root.Children.Add(ring);

        var paid = Math.Clamp(summary.ChargedCents - summary.OutstandingCents, 0, Math.Max(0, summary.ChargedCents));
        root.Children.Add(Text("Pokriveno zaduženje: " + LocalStore.Money(paid), 14, true));
        root.Children.Add(Text("Nepodmireno: " + LocalStore.Money(summary.OutstandingCents), 14));
        root.Children.Add(Text("Preplate: " + LocalStore.Money(summary.CreditCents), 14));
        var context = Text(
            summary.ChargedCents == 0
                ? "Nema zaduženja za obračun postotka."
                : "Postotak temeljen na internim evidencijama, nije bankovna potvrda.", 12);
        context.Foreground = new SolidColorBrush(BrandPalette.Muted);
        root.Children.Add(context);
        return root;
    }
}
