using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Volunteer_Tracker.Models;

namespace Volunteer_Tracker.ViewModels
{
    public partial class MainMenuViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _userFullName = string.Empty;

        [ObservableProperty]
        private string _userInitials = string.Empty;

        [ObservableProperty]
        private string _userRole = string.Empty;

        [ObservableProperty]
        private int _userPoints;

        [ObservableProperty]
        private bool _isDashboardSelected = true;

        [ObservableProperty]
        private bool _isGradebookSelected;

        [ObservableProperty]
        private bool _isEventsSelected;

        [ObservableProperty]
        private bool _isVolunteerSelected;

        [ObservableProperty]
        private bool _isRatingSelected;

        [ObservableProperty]
        private bool _isProjectsSelected;

        [ObservableProperty]
        private bool _isAdminPanelSelected;

        public bool IsAdmin => UserRole == "Администратор";

        public event EventHandler<string>? NavigateRequested;
        public event EventHandler? LogoutRequested;

        public MainMenuViewModel(User user, int points)
        {
            UserFullName = $"{user.LastName} {user.FirstName}";
            UserInitials = $"{user.FirstName?[0]}{user.LastName?[0]}".ToUpper();
            UserRole = user.Role switch
            {
                "student" => "Студент",
                "project_leader" => "Руководитель",
                "admin" => "Администратор",
                _ => "Пользователь"
            };
            UserPoints = points;
        }

        public void SetSelectedMenuItem(string menuItem)
        {
            ResetSelection();
            switch (menuItem)
            {
                case "Dashboard":
                    IsDashboardSelected = true;
                    break;
                case "Gradebook":
                    IsGradebookSelected = true;
                    break;
                case "Events":
                    IsEventsSelected = true;
                    break;
                case "Volunteer":
                    IsVolunteerSelected = true;
                    break;
                case "Rating":
                    IsRatingSelected = true;
                    break;
                case "Projects":
                    IsProjectsSelected = true;
                    break;
            }
        }

        public void ResetSelection()
        {
            IsDashboardSelected = false;
            IsGradebookSelected = false;
            IsEventsSelected = false;
            IsVolunteerSelected = false;
            IsRatingSelected = false;
            IsProjectsSelected = false;
        }

        [RelayCommand]
        private void NavigateToAdminPanel()
        {
            ResetSelection();
            IsAdminPanelSelected = true;
            NavigateRequested?.Invoke(this, "AdminPanel");
        }

        [RelayCommand]
        private void NavigateToDashboard()
        {
            ResetSelection();
            IsDashboardSelected = true;
            NavigateRequested?.Invoke(this, "Dashboard");
        }

        [RelayCommand]
        private void NavigateToGradebook()
        {
            ResetSelection();
            IsGradebookSelected = true;
            NavigateRequested?.Invoke(this, "Gradebook");
        }

        [RelayCommand]
        private void NavigateToEvents()
        {
            ResetSelection();
            IsEventsSelected = true;
            NavigateRequested?.Invoke(this, "Events");
        }

        [RelayCommand]
        private void NavigateToVolunteer()
        {
            ResetSelection();
            IsVolunteerSelected = true;
            NavigateRequested?.Invoke(this, "Volunteer");
        }

        [RelayCommand]
        private void NavigateToRating()
        {
            ResetSelection();
            IsRatingSelected = true;
            NavigateRequested?.Invoke(this, "Rating");
        }

        [RelayCommand]
        private void NavigateToProjects()
        {
            ResetSelection();
            IsProjectsSelected = true;
            NavigateRequested?.Invoke(this, "Projects");
        }

        [RelayCommand]
        private void OpenMyProfile()
        {
            NavigateRequested?.Invoke(this, "MyProfile");
        }

        [RelayCommand]
        private void Logout()
        {
            LogoutRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}