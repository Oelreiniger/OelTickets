using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OelTickets.Pages.Settings;
using System.Net.Http;
using System.Net.Http.Json;

namespace OelTickets.Pages.Login;

public sealed partial class LoginViewModel : NavPageVM
{
    private static readonly HttpClient _http = new()
    {
        BaseAddress = new Uri("http://localhost:32770/")
    };

    [ObservableProperty]
    private string email = "admin@mail.com";

    [ObservableProperty]
    private string password = "#Admin4dminpw";

    [ObservableProperty]
    private bool isBusy;

    public IAsyncRelayCommand LoginCommand { get; }

    public LoginViewModel()
    {
        LoginCommand = new AsyncRelayCommand(LoginAsync);
    }

    private async Task LoginAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;

        try
        {
            var response = await _http.PostAsJsonAsync("api/auth/login", new
            {
                email = Email,
                password = Password
            });

            if (!response.IsSuccessStatusCode)
            {
                return;
            }

            var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
            if (result is null || string.IsNullOrWhiteSpace(result.Token))
            {
                return;
            }

            Navigation.Navigate<SettingsPage, SettingsViewModel>();
        }
        catch
        {

        }
        finally
        {
            IsBusy = false;
        }
    }

    private sealed record LoginResponse(string Token);
}
