using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace SmartGreenhouseApp.ViewModels;

public class DashboardViewModel : INotifyPropertyChanged
{
    private readonly Services.BluetoothService _bluetooth;
    public event PropertyChangedEventHandler? PropertyChanged;

    // --- Sensor Properties ---
    private int _soilMoisture;
    public int SoilMoisture
    {
        get => _soilMoisture;
        set { _soilMoisture = value; OnPropertyChanged(); OnPropertyChanged(nameof(SoilColor)); }
    }

    private int _temperature;
    public int Temperature
    {
        get => _temperature;
        set { _temperature = value; OnPropertyChanged(); OnPropertyChanged(nameof(TempColor)); }
    }

    private int _humidity;
    public int Humidity
    {
        get => _humidity;
        set { _humidity = value; OnPropertyChanged(); OnPropertyChanged(nameof(HumidityProgress)); }
    }

    private bool _isLightOn;
    public bool IsLightOn
    {
        get => _isLightOn;
        set { _isLightOn = value; OnPropertyChanged(); OnPropertyChanged(nameof(LightStatusText)); OnPropertyChanged(nameof(LightStatusColor)); }
    }

    private bool _isPumpOn;
    public bool IsPumpOn
    {
        get => _isPumpOn;
        set { _isPumpOn = value; OnPropertyChanged(); OnPropertyChanged(nameof(PumpStatusText)); OnPropertyChanged(nameof(PumpStatusColor)); OnPropertyChanged(nameof(PumpButtonText)); }
    }

    private bool _isFanOn;
    public bool IsFanOn
    {
        get => _isFanOn;
        set { _isFanOn = value; OnPropertyChanged(); OnPropertyChanged(nameof(FanStatusText)); OnPropertyChanged(nameof(FanStatusColor)); OnPropertyChanged(nameof(FanButtonText)); }
    }

    // --- Connection Properties ---
    private bool _isConnected;
    public bool IsConnected
    {
        get => _isConnected;
        set { _isConnected = value; OnPropertyChanged(); OnPropertyChanged(nameof(ConnectionColor)); OnPropertyChanged(nameof(ConnectButtonText)); }
    }

    private string _statusMessage = "🔌 Enter COM port (e.g. COM3) and press Connect";
    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }

    private string _portEntry = "";
    public string PortEntry
    {
        get => _portEntry;
        set { _portEntry = value; OnPropertyChanged(); }
    }

    private List<string> _availablePorts = new();
    public List<string> AvailablePorts
    {
        get => _availablePorts;
        set { _availablePorts = value; OnPropertyChanged(); }
    }

    private string? _selectedPort;
    public string? SelectedPort
    {
        get => _selectedPort;
        set { _selectedPort = value; OnPropertyChanged(); }
    }

    // --- Computed Properties ---
    public string PumpStatusText => IsPumpOn ? "ON" : "OFF";
    public string FanStatusText => IsFanOn ? "ON" : "OFF";
    public string LightStatusText => IsLightOn ? "ON" : "OFF";
    public double HumidityProgress => Humidity / 100.0;
    public Color PumpStatusColor => IsPumpOn ? Color.FromArgb("#2ECC71") : Color.FromArgb("#E74C3C");
    public Color FanStatusColor => IsFanOn ? Color.FromArgb("#2ECC71") : Color.FromArgb("#E74C3C");
    public Color LightStatusColor => IsLightOn ? Color.FromArgb("#F39C12") : Color.FromArgb("#636E72");
    public Color ConnectionColor => IsConnected ? Color.FromArgb("#2ECC71") : Color.FromArgb("#E74C3C");
    public string ConnectButtonText => IsConnected ? "Disconnect" : "Connect";
    public string PumpButtonText => IsPumpOn ? "⏹ Pump OFF" : "▶ Pump ON";
    public string FanButtonText => IsFanOn ? "⏹ Fan OFF" : "▶ Fan ON";
    public string LightButtonText => IsLightOn ? "⏹ Light OFF" : "▶ Light ON";

    public Color SoilColor
    {
        get
        {
            if (SoilMoisture < 35) return Color.FromArgb("#E74C3C");
            if (SoilMoisture < 60) return Color.FromArgb("#F39C12");
            return Color.FromArgb("#2ECC71");
        }
    }

    public Color TempColor
    {
        get
        {
            if (Temperature >= 45) return Color.FromArgb("#E74C3C");
            if (Temperature >= 35) return Color.FromArgb("#F39C12");
            return Color.FromArgb("#3498DB");
        }
    }

    // --- Commands ---
    public ICommand ConnectCommand { get; }
    public ICommand TogglePumpCommand { get; }
    public ICommand ToggleFanCommand { get; }
    public ICommand ToggleLightCommand { get; }
    public ICommand ResetAutoCommand { get; }
    public ICommand RefreshPortsCommand { get; }

    public DashboardViewModel(Services.BluetoothService bluetooth)
    {
        _bluetooth = bluetooth;

        _bluetooth.OnDataReceived += data =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                SoilMoisture = data.SoilMoisture;
                Temperature = data.Temperature;
                Humidity = data.Humidity;
                IsLightOn = data.IsLightOn;
                IsPumpOn = data.IsPumpOn;
                IsFanOn = data.IsFanOn;
                StatusMessage = $"📡 Live — Soil:{data.SoilMoisture}% Temp:{data.Temperature}°C — {data.Timestamp:HH:mm:ss}";
            });
        };

        _bluetooth.OnStatusChanged += msg =>
            MainThread.BeginInvokeOnMainThread(() => StatusMessage = msg);

        _bluetooth.OnError += msg =>
            MainThread.BeginInvokeOnMainThread(() => StatusMessage = $"⚠ {msg}");

        ConnectCommand = new Command(async () => await ToggleConnectionAsync());
        TogglePumpCommand = new Command(() => TogglePump());
        ToggleFanCommand = new Command(() => ToggleFan());
        ToggleLightCommand = new Command(() => ToggleLight());
        ResetAutoCommand = new Command(() => ResetAuto());
        RefreshPortsCommand = new Command(async () => await RefreshPortsAsync());

        // Perform initial check gracefully without locking UI
        _ = RefreshPortsAsync();
    }

    private async Task RefreshPortsAsync()
    {
#if ANDROID
        try
        {
            var permissionStatus = await Permissions.CheckStatusAsync<Permissions.Bluetooth>();
            if (permissionStatus != PermissionStatus.Granted)
            {
                MainThread.BeginInvokeOnMainThread(() => StatusMessage = "🔒 Requesting Bluetooth permissions...");
                permissionStatus = await Permissions.RequestAsync<Permissions.Bluetooth>();
                if (permissionStatus != PermissionStatus.Granted)
                {
                    MainThread.BeginInvokeOnMainThread(() => StatusMessage = "⚠ Bluetooth permission denied. Cannot discover devices.");
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(() => StatusMessage = $"⚠ Permission check failed: {ex.Message}");
        }
#endif

        var ports = _bluetooth.GetAvailablePorts();
        MainThread.BeginInvokeOnMainThread(() =>
        {
            AvailablePorts = ports;
            if (AvailablePorts.Count > 0)
            {
                SelectedPort = AvailablePorts[0];
                StatusMessage = $"🔍 Found: {string.Join(", ", AvailablePorts)} — select and connect";
            }
            else
            {
                StatusMessage = "📝 Type COM port below (e.g. COM3) then press Connect";
            }
        });
    }

    private async Task ToggleConnectionAsync()
    {
        if (IsConnected)
        {
            _bluetooth.Disconnect();
            IsConnected = false;
            return;
        }

#if ANDROID
        try
        {
            var permissionStatus = await Permissions.CheckStatusAsync<Permissions.Bluetooth>();
            if (permissionStatus != PermissionStatus.Granted)
            {
                StatusMessage = "🔒 Granting Bluetooth permissions required to connect...";
                permissionStatus = await Permissions.RequestAsync<Permissions.Bluetooth>();
                if (permissionStatus != PermissionStatus.Granted)
                {
                    StatusMessage = "⚠ Permission denied. Connection cancelled.";
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"⚠ Setup error: {ex.Message}";
            return;
        }
#endif

        // Use typed port or selected port
        string? port = !string.IsNullOrWhiteSpace(PortEntry) ? PortEntry.Trim().ToUpper() : SelectedPort;

        if (string.IsNullOrEmpty(port))
        {
            StatusMessage = "⚠ Select a device or type a port name first.";
            return;
        }

        // Auto-add COM prefix if user just typed a number on Windows targets
        if (int.TryParse(port, out _))
            port = "COM" + port;

        StatusMessage = $"⏳ Connecting to {port}...";
        bool success = await _bluetooth.ConnectAsync(port);
        IsConnected = success;
        if (!success)
            StatusMessage = $"❌ Failed — check connection to {port}";
    }

    private void TogglePump()
    {
        string cmd = IsPumpOn ? "P:OFF" : "P:ON";
        _bluetooth.SendCommand(cmd);
    }

    private void ToggleFan()
    {
        string cmd = IsFanOn ? "F:OFF" : "F:ON";
        _bluetooth.SendCommand(cmd);
    }

    private void ToggleLight()
    {
        string cmd = IsLightOn ? "L:OFF" : "L:ON";
        _bluetooth.SendCommand(cmd);
    }

    private void ResetAuto()
    {
        _bluetooth.SendCommand("A:AUTO");
        StatusMessage = "🔄 Switched to AUTO mode";
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
