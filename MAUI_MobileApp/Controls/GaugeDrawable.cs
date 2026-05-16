namespace SmartGreenhouseApp.Controls;

/// <summary>
/// Custom circular gauge drawable for temperature, humidity, and soil moisture.
/// </summary>
public class GaugeDrawable : IDrawable
{
    public float Value { get; set; }
    public float MinValue { get; set; }
    public float MaxValue { get; set; } = 100;
    public string Unit { get; set; } = "%";
    public string Label { get; set; } = "";
    public Color ThemeColor { get; set; } = Colors.Green;
    public Color TrackColor { get; set; } = Color.FromArgb("#161B22");

    private const float StartAngle = 135f;
    private const float SweepAngle = 270f;
    private const float ArcThickness = 8f;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        float size = Math.Min(dirtyRect.Width, dirtyRect.Height);
        float cx = dirtyRect.Width / 2;
        float cy = dirtyRect.Height / 2;
        float radius = (size / 2) - 20; // Room for glow

        // Background Track Arc
        canvas.StrokeColor = TrackColor;
        canvas.StrokeSize = ArcThickness;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.DrawArc(cx - radius, cy - radius, radius * 2, radius * 2, StartAngle, StartAngle + SweepAngle, false, false);

        // Dashed inner arc
        float innerRadius = radius - 15;
        canvas.StrokeColor = Color.FromArgb("#30363D");
        canvas.StrokeSize = 2f;
        canvas.StrokeDashPattern = new float[] { 2, 4 };
        canvas.DrawArc(cx - innerRadius, cy - innerRadius, innerRadius * 2, innerRadius * 2, StartAngle, StartAngle + SweepAngle, false, false);

        // Value Arc Calculation
        float percentage = Math.Clamp((Value - MinValue) / (MaxValue - MinValue), 0, 1);
        float valueSweep = SweepAngle * percentage;

        if (valueSweep > 0.1f)
        {
            // Draw Glow
            canvas.StrokeDashPattern = null; // Reset dash
            for (int i = 6; i > 0; i--)
            {
                canvas.StrokeColor = ThemeColor.WithAlpha(0.05f * (7 - i));
                canvas.StrokeSize = ArcThickness + (i * 4);
                canvas.StrokeLineCap = LineCap.Round;
                canvas.DrawArc(cx - radius, cy - radius, radius * 2, radius * 2, StartAngle, StartAngle + valueSweep, false, false);
            }

            // Draw Core Arc
            canvas.StrokeColor = ThemeColor;
            canvas.StrokeSize = ArcThickness;
            canvas.DrawArc(cx - radius, cy - radius, radius * 2, radius * 2, StartAngle, StartAngle + valueSweep, false, false);
            
            // Draw Inner Value Arc
            canvas.StrokeColor = ThemeColor;
            canvas.StrokeSize = 2f;
            canvas.StrokeDashPattern = new float[] { 2, 4 };
            canvas.DrawArc(cx - innerRadius, cy - innerRadius, innerRadius * 2, innerRadius * 2, StartAngle, StartAngle + valueSweep, false, false);
        }

        // Center Value Text
        canvas.FontColor = Colors.White;
        canvas.FontSize = size * 0.22f;
        canvas.Font = Microsoft.Maui.Graphics.Font.DefaultBold;
        string valueText = $"{(int)Value}{Unit}";
        canvas.DrawString(valueText, cx, cy + 5, HorizontalAlignment.Center);

        // Label Text
        canvas.FontColor = Color.FromArgb("#8B95A5");
        canvas.FontSize = size * 0.08f;
        canvas.Font = Microsoft.Maui.Graphics.Font.Default;
        canvas.DrawString(Label.ToUpper(), cx, cy - size * 0.18f, HorizontalAlignment.Center);
    }
}
