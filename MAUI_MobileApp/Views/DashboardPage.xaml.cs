using SmartGreenhouseApp.Controls;
using SmartGreenhouseApp.ViewModels;

namespace SmartGreenhouseApp.Views;

public partial class DashboardPage : ContentPage
{
    private readonly DashboardViewModel _vm;
    private readonly GaugeDrawable _tempGauge;
    private readonly GaugeDrawable _humGauge;
    private readonly GaugeDrawable _soilGauge;

    public DashboardPage(DashboardViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        _vm = vm;

        // Setup Temperature Gauge
        _tempGauge = new GaugeDrawable
        {
            MinValue = 0,
            MaxValue = 50,
            Unit = "°C",
            Label = "Temp",
            ThemeColor = Color.FromArgb("#E67E22") // Orange
        };
        TempGauge.Drawable = _tempGauge;

        // Setup Humidity Gauge
        _humGauge = new GaugeDrawable
        {
            MinValue = 0,
            MaxValue = 100,
            Unit = "%",
            Label = "Humid",
            ThemeColor = Color.FromArgb("#3498DB") // Blue
        };
        HumGauge.Drawable = _humGauge;

        // Setup Soil Moisture Gauge
        _soilGauge = new GaugeDrawable
        {
            MinValue = 0,
            MaxValue = 100,
            Unit = "%",
            Label = "Soil",
            ThemeColor = Color.FromArgb("#2ECC71") // Green
        };
        SoilGauge.Drawable = _soilGauge;

        // Listen for property changes to update gauges
        _vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(DashboardViewModel.Temperature))
            {
                _tempGauge.Value = _vm.Temperature;
                TempGauge.Invalidate();
            }
            else if (e.PropertyName == nameof(DashboardViewModel.Humidity))
            {
                _humGauge.Value = _vm.Humidity;
                HumGauge.Invalidate();
            }
            else if (e.PropertyName == nameof(DashboardViewModel.SoilMoisture))
            {
                _soilGauge.Value = _vm.SoilMoisture;
                SoilGauge.Invalidate();
            }
        };
    }
}
