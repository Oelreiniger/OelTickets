using CommunityToolkit.Mvvm.ComponentModel;
using OelTickets.Services;

namespace OelTickets.Pages
{
    public class NavPageVM : ObservableObject
    {
        public FrameNavigationService Navigation { get; set; }
    }
}
