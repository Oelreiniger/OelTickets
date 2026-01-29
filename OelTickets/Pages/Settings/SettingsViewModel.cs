using CommunityToolkit.Mvvm.Input;
using OelTickets.Pages.Login;

namespace OelTickets.Pages.Settings;

public sealed partial class SettingsViewModel : NavPageVM
{
    public RelayCommand ReturnCommand { get; }

    public SettingsViewModel()
    {
        ReturnCommand = new RelayCommand(Return);
    }

    public sealed record NavigateMessage(string Target);

    private void Return()
    {
        Navigation.Navigate<LoginPage, LoginViewModel>();
    }

    private sealed record LoginResponse(string Token);
}
