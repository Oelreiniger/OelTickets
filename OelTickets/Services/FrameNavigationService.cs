using OelTickets.Pages;
using System.Windows.Controls;

namespace OelTickets.Services
{
    public class FrameNavigationService
    {
        private readonly Frame Frame;

        public FrameNavigationService(Frame frame)
        {
            Frame = frame;
        }

        public void Navigate<TPage, TPageVM>() where TPage : Page, new() where TPageVM : NavPageVM, new()
        {
            var page = new TPage();
            var vm = new TPageVM
            {
                Navigation = this
            };

            page.DataContext = vm;
            Frame.Navigate(page);
        }
    }
}
