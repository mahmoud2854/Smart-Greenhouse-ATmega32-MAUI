using SmartGreenhouseApp.Controls;
using SmartGreenhouseApp.ViewModels;

namespace SmartGreenhouseApp.Views;

public partial class DashboardPage : ContentPage
{
    private readonly DashboardViewModel _vm;
    private readonly GaugeDrawable _tempGauge;
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
            Label = "Temperature",
            StartColor = Color.FromArgb("#3498DB"),   // Blue (cold)
            EndColor = Color.FromArgb("#E74C3C")      // Red (hot)
        };
        TempGauge.Drawable = _tempGauge;

        // Setup Soil Moisture Gauge
        _soilGauge = new GaugeDrawable
        {
            MinValue = 0,
            MaxValue = 100,
            Unit = "%",
            Label = "Soil Moisture",
            StartColor = Color.FromArgb("#E74C3C"),   // Red (dry)
            EndColor = Color.FromArgb("#2ECC71")      // Green (wet)
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
            else if (e.PropertyName == nameof(DashboardViewModel.SoilMoisture))
            {
                _soilGauge.Value = _vm.SoilMoisture;
                SoilGauge.Invalidate();
            }
        };
    }
}
