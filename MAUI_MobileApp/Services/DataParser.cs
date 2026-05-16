using System.Text;

namespace SmartGreenhouseApp.Services;

/// <summary>
/// Stateful buffer-based parser for Bluetooth serial stream.
/// Handles packet fragmentation — accumulates bytes until \r\n delimiter.
/// </summary>
public class DataParser
{
    private readonly StringBuilder _buffer = new();
    private const int MaxBufferSize = 512;

    /// <summary>
    /// Fired when a complete, valid data line is parsed.
    /// </summary>
    public event Action<Models.SensorData>? OnDataParsed;

    /// <summary>
    /// Feed incoming raw bytes from Bluetooth into the parser.
    /// May fire OnDataParsed zero or more times.
    /// </summary>
    public void Feed(string chunk)
    {
        _buffer.Append(chunk);

        // Prevent memory leak from garbage data
        if (_buffer.Length > MaxBufferSize)
        {
            _buffer.Clear();
            return;
        }

        // Process all complete lines
        while (true)
        {
            string current = _buffer.ToString();
            int delimIndex = current.IndexOf('\n');

            if (delimIndex < 0)
                break;

            // Extract the line (strip \r\n)
            string line = current.Substring(0, delimIndex).TrimEnd('\r');
            _buffer.Remove(0, delimIndex + 1);

            // Validate: must contain at least "S:" and "T:"
            if (line.Contains("S:") && line.Contains("T:"))
            {
                var data = Models.SensorData.Parse(line);
                OnDataParsed?.Invoke(data);
            }
        }
    }

    /// <summary>
    /// Reset the internal buffer.
    /// </summary>
    public void Reset()
    {
        _buffer.Clear();
    }
}
