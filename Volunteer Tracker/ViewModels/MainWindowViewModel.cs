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

        public MainWindowViewModel()
        {
            ShowLogin();
        }

        private void ShowLogin()
        {
            var loginVm = new LoginViewModel();
            loginVm.LoginSuccess += OnLoginSuccess;
            loginVm.NavigateToRegisterRequested += OnNavigateToRegister;  // 👈 ИЗМЕНЕНО
            CurrentView = new LoginView { DataContext = loginVm };
        }

        private void ShowRegister()
        {
            var registerVm = new RegisterViewModel();
            registerVm.RegistrationSuccess += OnRegistrationSuccess;
            registerVm.NavigateToLoginRequested += OnNavigateToLogin;  // 👈 ИЗМЕНЕНО
            CurrentView = new RegisterView { DataContext = registerVm };
        }

        private void OnNavigateToRegister(object? sender, EventArgs e)
        {
            ShowRegister();
        }

        private void OnNavigateToLogin(object? sender, EventArgs e)
        {
            ShowLogin();
        }

        private void OnLoginSuccess(object? sender, User? user)
        {
            // TODO: Переход на дашборд
        }

        private void OnRegistrationSuccess(object? sender, EventArgs e)
        {
            ShowLogin();
        }
    }
}