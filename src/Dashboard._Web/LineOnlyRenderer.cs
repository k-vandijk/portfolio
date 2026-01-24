using Dashboard.Application.Dtos;
using SkiaSharp;

namespace Dashboard._Web;

public static class LineOnlyRenderer
{
    public static byte[] RenderPng(
        IReadOnlyList<DataPointDto>? points,
        int width,
        int height,
        int padding,
        bool transparentBackground)
    {
        points ??= Array.Empty<DataPointDto>();
        if (points.Count < 2)
            return RenderEmpty(width, height, transparentBackground);

        // Extract Y values
        var ys = points.Select(p => p.Value).ToArray();
        decimal min = ys.Min();
        decimal max = ys.Max();

        // Avoid flatline divide-by-zero; give tiny range
        if (min == max)
        {
            min -= 1m;
            max += 1m;
        }

        // Add a bit of "breathing room" so the line doesn't touch edges.
        // This is purely visual, not an axis.
        var range = max - min;
        min -= range * 0.05m;
        max += range * 0.05m;

        var w = width;
        var h = height;
        var left = padding;
        var top = padding;
        var right = w - padding;
        var bottom = h - padding;

        // Safety if padding is extreme
        if (right <= left) { left = 0; right = w; }
        if (bottom <= top) { top = 0; bottom = h; }

        using var bitmap = new SKBitmap(w, h, true);
        using var canvas = new SKCanvas(bitmap);

        canvas.Clear(transparentBackground ? SKColors.Transparent : SKColors.White);

        // Map idx->x, value->y
        int n = points.Count;
        float X(int i) => left + (n == 1 ? 0 : i * (right - left) / (float)(n - 1));

        float Y(decimal v)
        {
            var t = (float)((v - min) / (max - min));  // 0..1
            // invert because y grows downward
            return bottom - t * (bottom - top);
        }

        // Build a smoothed path (Catmull-Rom converted to cubic Bézier)
        // This produces a “nice” curve without showing any labels/axes.
        var path = BuildSmoothPath(points, X, Y);

        // --- Styling choices ---
        // You can tune these; they are purely visual.
        var strokeWidth = Math.Clamp(height / 18f, 3f, 10f);

        // Main line paint
        using var linePaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = strokeWidth,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round,
            Color = new SKColor(20, 40, 80), // dark-ish blue (change if you want)
        };

        // Subtle glow behind the line (optional but “nice”)
        using var glowPaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = strokeWidth + 6f,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round,
            Color = new SKColor(20, 40, 80, 40), // same hue, low alpha
            MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 8f)
        };

        // Draw glow then line
        canvas.DrawPath(path, glowPaint);
        canvas.DrawPath(path, linePaint);

        // Optional: draw a small end-cap dot (still “only the line” visually)
        // Comment out if you want a pure stroke only.
        var endX = X(n - 1);
        var endY = Y(points[n - 1].Value);
        using var endDot = new SKPaint { IsAntialias = true, Color = linePaint.Color };
        canvas.DrawCircle(endX, endY, strokeWidth * 0.65f, endDot);

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 90);
        return data.ToArray();
    }

    private static SKPath BuildSmoothPath(
        IReadOnlyList<DataPointDto> points,
        Func<int, float> x,
        Func<decimal, float> y)
    {
        // Catmull-Rom to cubic Bézier
        // Reference: standard spline conversion. Produces smooth curves through points.
        var pts = points.Select((p, i) => new SKPoint(x(i), y(p.Value))).ToArray();

        var path = new SKPath();
        path.MoveTo(pts[0]);

        for (int i = 0; i < pts.Length - 1; i++)
        {
            var p0 = i == 0 ? pts[i] : pts[i - 1];
            var p1 = pts[i];
            var p2 = pts[i + 1];
            var p3 = (i + 2 < pts.Length) ? pts[i + 2] : pts[i + 1];

            // Tension factor; 0.5 is classic Catmull-Rom
            const float t = 0.5f;

            var c1 = new SKPoint(
                p1.X + (p2.X - p0.X) * t / 3f,
                p1.Y + (p2.Y - p0.Y) * t / 3f);

            var c2 = new SKPoint(
                p2.X - (p3.X - p1.X) * t / 3f,
                p2.Y - (p3.Y - p1.Y) * t / 3f);

            path.CubicTo(c1, c2, p2);
        }

        return path;
    }

    private static byte[] RenderEmpty(int width, int height, bool transparentBackground)
    {
        using var bitmap = new SKBitmap(width, height, true);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(transparentBackground ? SKColors.Transparent : SKColors.White);

        // Return a fully blank image (no text, no axes, nothing)
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 90);
        return data.ToArray();
    }
}