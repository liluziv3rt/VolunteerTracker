using Avalonia.Controls;
using Avalonia.Input;
using Volunteer_Tracker.ViewModels;

namespace Volunteer_Tracker.Views
{
    public partial class RatingView : UserControl
    {
        public RatingView()
        {
            InitializeComponent();
        }

        private async void OnThisMonthTapped(object? sender, TappedEventArgs e)
        {
            if (DataContext is RatingViewModel vm)
            {
                await vm.SwitchToThisMonth();
            }
        }

        private async void OnAllTimeTapped(object? sender, TappedEventArgs e)
        {
            if (DataContext is RatingViewModel vm)
            {
                await vm.SwitchToAllTime();
            }
        }
    }
}