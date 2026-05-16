namespace SmartGreenhouseApp.Models;

/// <summary>
/// Represents a single data packet received from the ATmega32 greenhouse controller.
/// Format: S:{soil}%,T:{temp}C,H:{hum}%,L:{light},P:{pump},F:{fan}\r\n
/// </summary>
public class SensorData
{
    public int SoilMoisture { get; set; }
    public int Temperature { get; set; }
    public int Humidity { get; set; }
    public bool IsLightOn { get; set; }
    public bool IsPumpOn { get; set; }
    public bool IsFanOn { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;

    /// <summary>
    /// Parse a complete data line from the firmware.
    /// Example: "S:72%,T:28C,H:65%,L:1,P:ON,F:OFF"
    /// </summary>
    public static SensorData Parse(string raw)
    {
        var data = new SensorData();

        try
        {
            string[] parts = raw.Split(',');

            foreach (string part in parts)
            {
                string trimmed = part.Trim();

                if (trimmed.StartsWith("S:"))
                {
                    string val = trimmed.Substring(2).Replace("%", "");
                    int.TryParse(val, out int soil);
                    data.SoilMoisture = Math.Clamp(soil, 0, 100);
                }
                else if (trimmed.StartsWith("T:"))
                {
                    string val = trimmed.Substring(2).Replace("C", "");
                    int.TryParse(val, out int temp);
                    data.Temperature = Math.Clamp(temp, -10, 60);
                }
                else if (trimmed.StartsWith("H:"))
                {
                    string val = trimmed.Substring(2).Replace("%", "");
                    int.TryParse(val, out int hum);
                    data.Humidity = Math.Clamp(hum, 0, 100);
                }
                else if (trimmed.StartsWith("L:"))
                {
                    string val = trimmed.Substring(2);
                    data.IsLightOn = val == "1" || val.Equals("ON", StringComparison.OrdinalIgnoreCase);
                }
                else if (trimmed.StartsWith("P:"))
                {
                    data.IsPumpOn = trimmed.Substring(2).Equals("ON", StringComparison.OrdinalIgnoreCase);
                }
                else if (trimmed.StartsWith("F:"))
                {
                    data.IsFanOn = trimmed.Substring(2).Equals("ON", StringComparison.OrdinalIgnoreCase);
                }
            }

            data.Timestamp = DateTime.Now;
        }
        catch
        {
            // Return default data on parse failure
        }

        return data;
    }
}
