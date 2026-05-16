using Microsoft.Extensions.Logging;
using SmartGreenhouseApp.Services;
using SmartGreenhouseApp.ViewModels;
using SmartGreenhouseApp.Views;

namespace SmartGreenhouseApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // --- Dependency Injection ---
        builder.Services.AddSingleton<DataParser>();
        builder.Services.AddSingleton<BluetoothService>();
        builder.Services.AddSingleton<DashboardViewModel>();
        builder.Services.AddSingleton<DashboardPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
