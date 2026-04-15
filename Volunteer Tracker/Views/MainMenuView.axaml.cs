using Avalonia.Controls;
using Avalonia.Input;
using Volunteer_Tracker.ViewModels;

namespace Volunteer_Tracker.Views;

public partial class MainMenuView : UserControl
{
    public MainMenuView()
    {
        InitializeComponent();
    }

    private void OnProfileTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is MainMenuViewModel vm)
        {
            vm.OpenMyProfileCommand.Execute(null);
        }
    }
}