namespace SmartGreenhouseApp.Controls;

/// <summary>
/// Custom circular gauge drawable for temperature and soil moisture.
/// </summary>
public class GaugeDrawable : IDrawable
{
    public float Value { get; set; }
    public float MinValue { get; set; }
    public float MaxValue { get; set; } = 100;
    public string Unit { get; set; } = "%";
    public string Label { get; set; } = "";
    public Color StartColor { get; set; } = Colors.Green;
    public Color EndColor { get; set; } = Colors.Red;
    public Color TrackColor { get; set; } = Color.FromArgb("#1E2A3A");
    public Color BackgroundColor { get; set; } = Color.FromArgb("#0D1117");

    private const float StartAngle = 135f;
    private const float SweepAngle = 270f;
    private const float ArcThickness = 14f;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        float size = Math.Min(dirtyRect.Width, dirtyRect.Height);
        float cx = dirtyRect.Width / 2;
        float cy = dirtyRect.Height / 2;
        float radius = (size / 2) - ArcThickness - 4;

        // --- Background Track Arc ---
        canvas.StrokeColor = TrackColor;
        canvas.StrokeSize = ArcThickness;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.DrawArc(
            cx - radius, cy - radius,
            radius * 2, radius * 2,
            StartAngle, StartAngle + SweepAngle,
            false, false);

        // --- Value Arc ---
        float percentage = Math.Clamp((Value - MinValue) / (MaxValue - MinValue), 0, 1);
        float valueSweep = SweepAngle * percentage;

        if (valueSweep > 0.5f)
        {
            // Gradient effect via color interpolation
            float r = StartColor.Red + (EndColor.Red - StartColor.Red) * percentage;
            float g = StartColor.Green + (EndColor.Green - StartColor.Green) * percentage;
            float b = StartColor.Blue + (EndColor.Blue - StartColor.Blue) * percentage;
            var valueColor = new Color(r, g, b);

            canvas.StrokeColor = valueColor;
            canvas.StrokeSize = ArcThickness;
            canvas.StrokeLineCap = LineCap.Round;
            canvas.DrawArc(
                cx - radius, cy - radius,
                radius * 2, radius * 2,
                StartAngle, StartAngle + valueSweep,
                false, false);
        }

        // --- Center Value Text ---
        canvas.FontColor = Colors.White;
        canvas.FontSize = size * 0.18f;
        canvas.Font = Microsoft.Maui.Graphics.Font.Default;
        string valueText = $"{(int)Value}{Unit}";
        canvas.DrawString(valueText, cx, cy - 4, HorizontalAlignment.Center);

        // --- Label Text ---
        canvas.FontColor = Color.FromArgb("#8B95A5");
        canvas.FontSize = size * 0.09f;
        canvas.DrawString(Label, cx, cy + size * 0.16f, HorizontalAlignment.Center);
    }
}
