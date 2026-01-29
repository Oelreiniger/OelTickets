using MahApps.Metro.Controls;
using OelTickets.Pages;
using OelTickets.Pages.Login;
using OelTickets.Services;

namespace OelTickets;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : MetroWindow
{
    public MainWindow()
    {
        InitializeComponent();
        FrameNavigationService frame = new FrameNavigationService(MainFrame);
        frame.Navigate<LoginPage, LoginViewModel>();
    }
}