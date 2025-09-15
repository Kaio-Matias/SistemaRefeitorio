namespace Refeitorio;

public partial class MainPage : ContentPage
{
    private readonly IServiceProvider _serviceProvider;

    public MainPage(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
    }

    private async void OnScanButtonClicked(object sender, TappedEventArgs e)
    {
        var cameraPage = _serviceProvider.GetService<CameraPage>();
        if (cameraPage != null)
        {
            await Navigation.PushAsync(cameraPage);
        }
    }
}