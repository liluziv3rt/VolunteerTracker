using System;
using CommunityToolkit.Mvvm.ComponentModel;
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

        private void ShowRegister()
        {
            var registerVm = new RegisterViewModel();
            registerVm.RegistrationSuccess += OnRegistrationSuccess;
            registerVm.NavigateToLoginRequested += OnNavigateToLogin;
            CurrentView = new RegisterView { DataContext = registerVm };
        }

        private void ShowDashboard()
        {
            var dashboardVm = new DashboardViewModel(_currentUser!);
            dashboardVm.NavigateRequested += OnNavigateFromDashboard;
            CurrentView = new DashboardView { DataContext = dashboardVm };
        }

        private void OnLoginSuccess(object? sender, User user)
        {
            _currentUser = user;

            _menuViewModel = new MainMenuViewModel(user, 1250);
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
                    // TODO
                    break;
                case "Projects":
                    // TODO
                    break;
            }
        }

        private void OnNavigateFromDashboard(object? sender, string destination)
        {
            // Подсвечиваем соответствующий пункт меню
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

        private void OnNavigateToRegister(object? sender, EventArgs e) => ShowRegister();
        private void OnNavigateToLogin(object? sender, EventArgs e) => ShowLogin();
        private void OnRegistrationSuccess(object? sender, EventArgs e) => ShowLogin();
    }
}