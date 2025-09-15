using Camera.MAUI;
using Refeitorio.Services;
using Microsoft.Extensions.Logging;

namespace Refeitorio;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCameraView()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        string apiBaseUrl = "http://10.1.0.51:8090";

        builder.Services.AddSingleton(new HttpClient
        {
            BaseAddress = new Uri(apiBaseUrl)
        });

        builder.Services.AddSingleton<ApiService>();
        builder.Services.AddSingleton<MainPage>();
        builder.Services.AddTransient<CameraPage>();

        return builder.Build();
    }
}