using System.Windows;
using System.Windows.Controls;

namespace OelTickets.Pages
{
    /// <summary>
    /// Interaktionslogik für LoginPage.xaml
    /// </summary>
    public partial class LoginPage : Page
    {
        public LoginPage()
        {
            InitializeComponent();
        }

        private void GoSettings_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new SettingsPage());
        }
    }
}
