using Avalonia.Controls;
using Volunteer_Tracker.ViewModels;

namespace Volunteer_Tracker.Views
{
    public partial class EditProfileDialog : Window
    {
        public EditProfileDialog()
        {
            InitializeComponent();

            DataContextChanged += (_, _) =>
            {
                if (DataContext is EditProfileDialogViewModel vm)
                {
                    vm.RequestClose += (_, result) =>
                    {
                        Close(result);
                    };
                }
            };
        }
    }
}