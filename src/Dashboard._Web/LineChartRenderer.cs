using Dashboard._Web.ViewModels;
using SkiaSharp;

namespace Dashboard._Web;

public static class LineChartRenderer
{
    public static byte[] RenderPng(LineChartViewModel model, int width, int height)
    {
        // Handle empty data gracefully
        var points = model.DataPoints;
        if (points.Count == 0)
        {
            return RenderEmpty(width, height, model.Title);
        }

        // Parse numeric series
        var ys = points.Select(p => p.Value).ToList();
        var minY = ys.Min();
        var maxY = ys.Max();
        if (minY == maxY)
        {
            // Avoid divide-by-zero; expand range slightly
            minY -= 1m;
            maxY += 1m;
        }

        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);

        // Layout
        var paddingLeft = 70f;
        var paddingRight = 20f;
        var paddingTop = 45f;
        var paddingBottom = 45f;

        var plotLeft = paddingLeft;
        var plotTop = paddingTop;
        var plotRight = width - paddingRight;
        var plotBottom = height - paddingBottom;

        // Paints
        using var axisPaint = new SKPaint { Color = SKColors.Gray, StrokeWidth = 1, IsAntialias = true };
        using var gridPaint = new SKPaint { Color = new SKColor(230, 230, 230), StrokeWidth = 1, IsAntialias = true };
        using var linePaint = new SKPaint { Color = SKColors.Black, StrokeWidth = 2, IsAntialias = true, Style = SKPaintStyle.Stroke };
#pragma warning disable CS0618 // Type or member is obsolete
        using var textPaint = new SKPaint { Color = SKColors.Black, TextSize = 16, IsAntialias = true };
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
        using var smallTextPaint = new SKPaint { Color = SKColors.Black, TextSize = 12, IsAntialias = true };
#pragma warning restore CS0618 // Type or member is obsolete

        // Title
        if (!string.IsNullOrWhiteSpace(model.Title))
        {
#pragma warning disable CS0618 // Type or member is obsolete
            canvas.DrawText(model.Title, plotLeft, 25, textPaint);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        // Axes
        canvas.DrawLine(plotLeft, plotBottom, plotRight, plotBottom, axisPaint);
        canvas.DrawLine(plotLeft, plotTop, plotLeft, plotBottom, axisPaint);

        // Y grid + labels (5 ticks)
        int yTicks = 5;
        for (int i = 0; i <= yTicks; i++)
        {
            float t = i / (float)yTicks;
            float y = plotBottom - t * (plotBottom - plotTop);

            canvas.DrawLine(plotLeft, y, plotRight, y, gridPaint);

            decimal yVal = minY + (maxY - minY) * (decimal)t;
            var label = FormatValue(yVal, model.Format);
#pragma warning disable CS0618 // Type or member is obsolete
            canvas.DrawText(label, 5, y + 4, smallTextPaint);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        // X mapping (index-based; you can improve to true date scaling later)
        int n = points.Count;
        float PlotX(int idx) =>
            plotLeft + (n == 1 ? 0 : idx * (plotRight - plotLeft) / (n - 1));

        float PlotY(decimal v)
        {
            var frac = (float)((v - minY) / (maxY - minY));
            return plotBottom - frac * (plotBottom - plotTop);
        }

        // Build line path
        using var path = new SKPath();
        for (int i = 0; i < n; i++)
        {
            float x = PlotX(i);
            float y = PlotY(points[i].Value);
            if (i == 0) path.MoveTo(x, y);
            else path.LineTo(x, y);
        }
        canvas.DrawPath(path, linePaint);

        // Optional: draw last value label
        var last = points[^1];
        var lastX = PlotX(n - 1);
        var lastY = PlotY(last.Value);
        canvas.DrawCircle(lastX, lastY, 3, new SKPaint { Color = SKColors.Black, IsAntialias = true });
#pragma warning disable CS0618 // Type or member is obsolete
        canvas.DrawText(FormatValue(last.Value, model.Format), lastX - 60, lastY - 8, smallTextPaint);
#pragma warning restore CS0618 // Type or member is obsolete

        // Encode PNG
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 90);
        return data.ToArray();
    }

    private static byte[] RenderEmpty(int width, int height, string? title)
    {
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);

#pragma warning disable CS0618 // Type or member is obsolete
        using var textPaint = new SKPaint { Color = SKColors.Black, TextSize = 18, IsAntialias = true };
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
        using var smallTextPaint = new SKPaint { Color = SKColors.Gray, TextSize = 14, IsAntialias = true };
#pragma warning restore CS0618 // Type or member is obsolete

        if (!string.IsNullOrWhiteSpace(title))
#pragma warning disable CS0618 // Type or member is obsolete
            canvas.DrawText(title, 20, 30, textPaint);
#pragma warning restore CS0618 // Type or member is obsolete

#pragma warning disable CS0618 // Type or member is obsolete
        canvas.DrawText("No data available for the selected filters.", 20, 80, smallTextPaint);
#pragma warning restore CS0618 // Type or member is obsolete

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 90);
        return data.ToArray();
    }

    private static string FormatValue(decimal value, string? format)
    {
        return format switch
        {
            "currency" => value.ToString("C2"),
            "percentage" => value.ToString("F2") + "%",
            _ => value.ToString("F2")
        };
    }
}