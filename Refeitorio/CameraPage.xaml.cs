using Refeitorio.Services;
using Camera.MAUI;

namespace Refeitorio;

public partial class CameraPage : ContentPage
{
    private readonly ApiService _apiService;

    public CameraPage(ApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CheckAndRequestCameraPermission();
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        // CORRE��O: Chamamos StopCameraAsync diretamente.
        // � seguro fazer isso mesmo que a c�mara n�o esteja iniciada.
        await cameraView.StopCameraAsync();
    }

    private async Task CheckAndRequestCameraPermission()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.Camera>();
        }

        if (status == PermissionStatus.Granted)
        {
            cameraView.Camera = cameraView.Cameras.FirstOrDefault(c => c.Position == CameraPosition.Front);
            if (cameraView.Camera == null)
            {
                cameraView.Camera = cameraView.Cameras.FirstOrDefault();
            }

            if (cameraView.Camera != null)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await cameraView.StartCameraAsync();
                });
            }
            else
            {
                await DisplayAlert("Erro", "Nenhuma c�mara detetada no dispositivo.", "OK");
                await Navigation.PopAsync();
            }
        }
        else
        {
            await DisplayAlert("Permiss�o Necess�ria", "A permiss�o da c�mara � necess�ria para o reconhecimento facial.", "OK");
            await Navigation.PopAsync();
        }
    }

    private async void OnCaptureClicked(object sender, EventArgs e)
    {
        CaptureButton.IsEnabled = false;
        LoadingIndicator.IsRunning = true;

        try
        {
            var fotoStream = await cameraView.TakePhotoAsync();

            if (fotoStream != null)
            {
                fotoStream.Position = 0;
                var result = await _apiService.IdentificarColaborador(fotoStream);

                if (result.IsSuccess)
                {
                    await DisplayAlert("Sucesso!", $"Refei��o Registada!\n\nColaborador: {result.ColaboradorNome}\nHor�rio: {DateTime.Now:HH:mm:ss}", "OK");
                    await Navigation.PopAsync();
                }
                else
                {
                    await DisplayAlert("Falha na Verifica��o", result.Message, "OK");
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro", $"Ocorreu um erro: {ex.Message}", "OK");
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            CaptureButton.IsEnabled = true;
        }
    }
}