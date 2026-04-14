using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Volunteer_Tracker.Models;

namespace Volunteer_Tracker.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly PostgresContext _context;

        [ObservableProperty]
        private string _email = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _isPasswordVisible;

        // События
        public event EventHandler<User?>? LoginSuccess;
        public event EventHandler? NavigateToRegisterRequested;  // 👈 ИЗМЕНЕНО ИМЯ

        public LoginViewModel()
        {
            _context = new PostgresContext();
        }

        [RelayCommand]
        private async Task Login()
        {
            if (IsLoading || string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Заполните email и пароль";
                return;
            }

            IsLoading = true;
            ErrorMessage = string.Empty;

            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == Email && u.IsActive == true);

                if (user == null)
                {
                    ErrorMessage = "Пользователь с таким email не найден";
                    return;
                }

                if (!BCrypt.Net.BCrypt.Verify(Password, user.PasswordHash))
                {
                    ErrorMessage = "Неверный пароль";
                    return;
                }

                user.LastLoginAt = DateTime.Now;
                await _context.SaveChangesAsync();

                LoginSuccess?.Invoke(this, user);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка подключения к базе данных: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void ForgotPassword()
        {
            // TODO: Обработка восстановления пароля
        }

        [RelayCommand]
        private void TogglePasswordVisibility()
        {
            IsPasswordVisible = !IsPasswordVisible;
        }

        // 👇 ЭТОТ МЕТОД ГЕНЕРИРУЕТ КОМАНДУ NavigateToRegisterCommand
        [RelayCommand]
        private void NavigateToRegister()
        {
            NavigateToRegisterRequested?.Invoke(this, EventArgs.Empty);
        }

        public string PasswordChar => IsPasswordVisible ? "" : "●";
    }
}