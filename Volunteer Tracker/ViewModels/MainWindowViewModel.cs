using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Volunteer_Tracker.Models;
using Volunteer_Tracker.Views;

namespace Volunteer_Tracker.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        private object? _currentView;

        [ObservableProperty]
        private object? _menuView;

        private User? _currentUser;
        private MainMenuViewModel? _menuViewModel;

        public MainWindowViewModel()
        {
            ShowLogin();
        }

        private void ShowLogin()
        {
            var loginVm = new LoginViewModel();
            loginVm.LoginSuccess += OnLoginSuccess;
            loginVm.NavigateToRegisterRequested += OnNavigateToRegister;
            CurrentView = new LoginView { DataContext = loginVm };
            MenuView = null;
        }

        private void ShowGradebook()
        {
            if (_menuViewModel != null)
            {
                _menuViewModel.ResetSelection();
                _menuViewModel.IsGradebookSelected = true;
            }
            // TODO: ShowGradebook
        }

        private void ShowEvents()
        {
            if (_menuViewModel != null)
            {
                _menuViewModel.ResetSelection();
                _menuViewModel.IsEventsSelected = true;
            }
            // TODO: ShowEvents
        }

        private void ShowVolunteer()
        {
            if (_menuViewModel != null)
            {
                _menuViewModel.ResetSelection();
                _menuViewModel.IsVolunteerSelected = true;
            }
            // TODO: ShowVolunteer
        }

        private void ShowProjects()
        {
            _menuViewModel?.SetSelectedMenuItem("Projects");
            var projectsVm = new ProjectsViewModel(_currentUser!);
            CurrentView = new ProjectsView { DataContext = projectsVm };
        }


        private void ShowRegister()
        {
            var registerVm = new RegisterViewModel();
            registerVm.RegistrationSuccess += OnRegistrationSuccess;
            registerVm.NavigateToLoginRequested += OnNavigateToLogin;
            CurrentView = new RegisterView { DataContext = registerVm };
        }

        private void ShowDashboard()
        {
            _menuViewModel?.SetSelectedMenuItem("Dashboard");
            var dashboardVm = new DashboardViewModel(_currentUser!);
            dashboardVm.NavigateRequested += OnNavigateFromDashboard;
            CurrentView = new DashboardView { DataContext = dashboardVm };
        }



        private async void OnLoginSuccess(object? sender, User user)
        {
            _currentUser = user;

            // Загружаем баллы пользователя из базы данных
            int userPoints = 0;
            try
            {
                using var context = new PostgresContext();
                var pointsEntity = await context.UserPoints
                    .FirstOrDefaultAsync(up => up.UserId == user.Id);

                if (pointsEntity != null)
                {
                    userPoints = pointsEntity.TotalPoints ?? 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки баллов: {ex.Message}");
            }

            _menuViewModel = new MainMenuViewModel(user, userPoints);
            _menuViewModel.NavigateRequested += OnNavigateRequested;
            _menuViewModel.LogoutRequested += OnLogoutRequested;
            MenuView = new MainMenuView { DataContext = _menuViewModel };

            ShowDashboard();
        }

        private void OnNavigateRequested(object? sender, string destination)
        {
            switch (destination)
            {
                case "Dashboard":
                    ShowDashboard();
                    break;
                case "Gradebook":
                    // TODO
                    break;
                case "Events":
                    // TODO
                    break;
                case "Volunteer":
                    // TODO
                    break;
                case "Rating":
                    ShowRating();
                    break;
                case "Projects":
                    ShowProjects();
                    break;
                case "MyProfile":
                    ShowMyProfile();
                    break;
                
            }
        }

        

        private void ShowMyProfile()
        {
            var profileVm = new ProfileViewModel(_currentUser!);
            CurrentView = new ProfileView { DataContext = profileVm };
        }



        private void OnNavigateFromDashboard(object? sender, string destination)
        {
            _menuViewModel?.ResetSelection();
            switch (destination)
            {
                case "Volunteer":
                    _menuViewModel?.NavigateToVolunteerCommand?.Execute(null);
                    break;
                case "Projects":
                    _menuViewModel?.NavigateToProjectsCommand?.Execute(null);
                    break;
                case "Rating":
                    _menuViewModel?.NavigateToRatingCommand?.Execute(null);
                    break;
            }
        }

        private void OnLogoutRequested(object? sender, EventArgs e)
        {
            _currentUser = null;
            ShowLogin();
        }

        private void ShowRating()
        {
            _menuViewModel?.SetSelectedMenuItem("Rating");
            var ratingVm = new RatingViewModel(_currentUser!);
            CurrentView = new RatingView { DataContext = ratingVm };
        }


        private void OnNavigateToRegister(object? sender, EventArgs e) => ShowRegister();
        private void OnNavigateToLogin(object? sender, EventArgs e) => ShowLogin();
        private void OnRegistrationSuccess(object? sender, EventArgs e) => ShowLogin();
    }
}