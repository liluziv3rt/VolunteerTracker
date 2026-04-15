using Avalonia.Controls;
using Avalonia.Input;
using Volunteer_Tracker.ViewModels;

namespace Volunteer_Tracker.Views
{
    public partial class ProjectsView : UserControl
    {
        public ProjectsView()
        {
            InitializeComponent();
        }

        private async void OnAvailableTapped(object? sender, TappedEventArgs e)
        {
            if (DataContext is ProjectsViewModel vm)
            {
                await vm.SwitchToAvailable();
            }
        }

        private async void OnMyProjectsTapped(object? sender, TappedEventArgs e)
        {
            if (DataContext is ProjectsViewModel vm)
            {
                await vm.SwitchToMyProjects();
            }
        }
    }
}