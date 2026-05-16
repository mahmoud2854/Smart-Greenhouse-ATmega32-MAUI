using System.Text;

namespace SmartGreenhouseApp.Services;

/// <summary>
/// Dual-Backend Bluetooth SPP service for connecting to HC-05 module.
/// On Windows Target: utilizes System.IO.Ports (standard COM port).
/// On Android Target: utilizes Android native Bluetooth Classic SPP sockets.
/// </summary>
public class BluetoothService : IDisposable
{
#if ANDROID
    private Android.Bluetooth.BluetoothSocket? _socket;
#else
    private System.IO.Ports.SerialPort? _serialPort;
#endif

    private CancellationTokenSource? _cts;
    private readonly DataParser _parser;
    private bool _disposed;

    public event Action<Models.SensorData>? OnDataReceived;
    public event Action<string>? OnStatusChanged;
    public event Action<string>? OnError;

#if ANDROID
    public bool IsConnected => _socket?.IsConnected ?? false;
#else
    public bool IsConnected => _serialPort?.IsOpen ?? false;
#endif

    public string? CurrentPort { get; private set; }

    public BluetoothService(DataParser parser)
    {
        _parser = parser;
        _parser.OnDataParsed += data => OnDataReceived?.Invoke(data);
    }

    /// <summary>
    /// Retrieve available connection endpoints.
    /// On Windows: lists registered local COM ports.
    /// On Android: lists paired Bluetooth Classic devices.
    /// </summary>
    public List<string> GetAvailablePorts()
    {
        var ports = new List<string>();
        try
        {
#if ANDROID
            var adapter = Android.Bluetooth.BluetoothAdapter.DefaultAdapter;
            if (adapter != null && adapter.IsEnabled)
            {
                var pairedDevices = adapter.BondedDevices;
                if (pairedDevices != null)
                {
                    foreach (var device in pairedDevices)
                    {
                        if (!string.IsNullOrWhiteSpace(device.Name))
                            ports.Add(device.Name);
                        else if (!string.IsNullOrWhiteSpace(device.Address))
                            ports.Add(device.Address);
                    }
                }
            }
#else
            string[] comPorts = System.IO.Ports.SerialPort.GetPortNames();
            if (comPorts.Length > 0)
            {
                ports.AddRange(comPorts);
            }
#endif
        }
        catch (Exception ex)
        {
            OnError?.Invoke($"Scan error: {ex.Message}");
        }
        return ports;
    }

    /// <summary>
    /// Connect to target endpoint asynchronously.
    /// </summary>
    public async Task<bool> ConnectAsync(string portName)
    {
        try
        {
            Disconnect();

#if ANDROID
            var adapter = Android.Bluetooth.BluetoothAdapter.DefaultAdapter;
            if (adapter == null || !adapter.IsEnabled)
            {
                OnError?.Invoke("⚠ Bluetooth adapter is not enabled on this phone.");
                return false;
            }

            Android.Bluetooth.BluetoothDevice? targetDevice = null;
            var pairedDevices = adapter.BondedDevices;
            if (pairedDevices != null)
            {
                foreach (var d in pairedDevices)
                {
                    if (d.Name?.Equals(portName, StringComparison.OrdinalIgnoreCase) == true ||
                        d.Address?.Equals(portName, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        targetDevice = d;
                        break;
                    }
                }
            }

            if (targetDevice == null)
            {
                OnError?.Invoke($"⚠ Device '{portName}' not found in paired list. Please pair HC-05 first.");
                return false;
            }

            OnStatusChanged?.Invoke($"⏳ Connecting securely to {targetDevice.Name}...");

            // Standard Serial Port Profile (SPP) UUID
            var sppUuid = Java.Util.UUID.FromString("00001101-0000-1000-8000-00805F9B34FB");
            
            await Task.Run(() =>
            {
                _socket = targetDevice.CreateRfcommSocketToServiceRecord(sppUuid);
                _socket?.Connect();
            });

#else
            await Task.Run(() =>
            {
                _serialPort = new System.IO.Ports.SerialPort(portName, 9600)
                {
                    DataBits = 8,
                    Parity = System.IO.Ports.Parity.None,
                    StopBits = System.IO.Ports.StopBits.One,
                    ReadTimeout = 2000,
                    WriteTimeout = 1000,
                    DtrEnable = false,
                    RtsEnable = false
                };

                _serialPort.Open();
            });
#endif

            CurrentPort = portName;
            OnStatusChanged?.Invoke($"✅ Connected to {portName}");

            _cts = new CancellationTokenSource();
            _ = Task.Run(() => ReadLoopAsync(_cts.Token));

            return true;
        }
        catch (Exception ex)
        {
            OnError?.Invoke($"Connection failed: {ex.Message}");
            Disconnect();
            return false;
        }
    }

    /// <summary>
    /// Continuous background stream reader loop.
    /// </summary>
    private async Task ReadLoopAsync(CancellationToken ct)
    {
        byte[] buffer = new byte[256];

#if ANDROID
        while (!ct.IsCancellationRequested && _socket?.IsConnected == true)
        {
            try
            {
                var stream = _socket.InputStream;
                if (stream != null)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, ct);
                    if (bytesRead > 0)
                    {
                        string chunk = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                        _parser.Feed(chunk);
                    }
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception) { await Task.Delay(100, ct); }
        }
#else
        while (!ct.IsCancellationRequested && _serialPort?.IsOpen == true)
        {
            try
            {
                var stream = _serialPort.BaseStream;
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, ct);
                if (bytesRead > 0)
                {
                    string chunk = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    _parser.Feed(chunk);
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception) { await Task.Delay(100, ct); }
        }
#endif
    }

    /// <summary>
    /// Transmit override commands safely to the hardware stream.
    /// </summary>
    public bool SendCommand(string command)
    {
        try
        {
            string cmdText = command.EndsWith("\n") ? command : command + "\n";
            byte[] bytes = Encoding.ASCII.GetBytes(cmdText);

#if ANDROID
            if (_socket?.IsConnected == true)
            {
                _socket.OutputStream?.Write(bytes, 0, bytes.Length);
                OnStatusChanged?.Invoke($"📤 Sent: {command.Trim()}");
                return true;
            }
#else
            if (_serialPort?.IsOpen == true)
            {
                _serialPort.Write(cmdText);
                OnStatusChanged?.Invoke($"📤 Sent: {command.Trim()}");
                return true;
            }
#endif
            OnError?.Invoke("⚠ Not connected to module!");
            return false;
        }
        catch (Exception ex)
        {
            OnError?.Invoke($"Transmission failed: {ex.Message}");
            return false;
        }
    }

    public void Disconnect()
    {
        _cts?.Cancel();

#if ANDROID
        if (_socket != null)
        {
            try { _socket.Close(); } catch { }
            _socket.Dispose();
            _socket = null;
        }
#else
        if (_serialPort?.IsOpen == true)
        {
            try { _serialPort.Close(); } catch { }
        }
        _serialPort?.Dispose();
        _serialPort = null;
#endif

        _parser.Reset();
        CurrentPort = null;
        OnStatusChanged?.Invoke("🔌 Disconnected");
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Disconnect();
            _disposed = true;
        }
    }
}
