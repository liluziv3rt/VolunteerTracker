using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Volunteer_Tracker.Models;

namespace Volunteer_Tracker.ViewModels
{
    public partial class RegisterViewModel : ObservableObject
    {
        private readonly PostgresContext _context;

        [ObservableProperty]
        private string _email = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private string _confirmPassword = string.Empty;

        [ObservableProperty]
        private string _firstName = string.Empty;

        [ObservableProperty]
        private string _lastName = string.Empty;

        [ObservableProperty]
        private string _middleName = string.Empty;

        [ObservableProperty]
        private string _groupName = string.Empty;

        [ObservableProperty]
        private string _phone = string.Empty;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _isLoading;

        // События
        public event EventHandler? RegistrationSuccess;
        public event EventHandler? NavigateToLoginRequested;  // 👈 ИЗМЕНЕНО ИМЯ

        public RegisterViewModel()
        {
            _context = new PostgresContext();
        }

        [RelayCommand]
        private async Task Register()
        {
            if (IsLoading) return;

            if (string.IsNullOrWhiteSpace(LastName))
            {
                ErrorMessage = "Введите фамилию";
                return;
            }

            if (string.IsNullOrWhiteSpace(FirstName))
            {
                ErrorMessage = "Введите имя";
                return;
            }

            if (string.IsNullOrWhiteSpace(Email))
            {
                ErrorMessage = "Введите email";
                return;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Введите пароль";
                return;
            }

            if (Password != ConfirmPassword)
            {
                ErrorMessage = "Пароли не совпадают";
                return;
            }

            if (Password.Length < 3)
            {
                ErrorMessage = "Пароль должен содержать минимум 3 символа";
                return;
            }

            IsLoading = true;
            ErrorMessage = string.Empty;

            try
            {
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == Email);

                if (existingUser != null)
                {
                    ErrorMessage = "Пользователь с таким email уже существует";
                    return;
                }

                string passwordHash = BCrypt.Net.BCrypt.HashPassword(Password);

                var newUser = new User
                {
                    Email = Email,
                    PasswordHash = passwordHash,
                    FirstName = FirstName,
                    LastName = LastName,
                    MiddleName = string.IsNullOrWhiteSpace(MiddleName) ? null : MiddleName,
                    GroupName = string.IsNullOrWhiteSpace(GroupName) ? null : GroupName,
                    Phone = string.IsNullOrWhiteSpace(Phone) ? null : Phone,
                    Role = "student",
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                RegistrationSuccess?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка регистрации: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        // 👇 ЭТОТ МЕТОД ГЕНЕРИРУЕТ КОМАНДУ NavigateToLoginCommand
        [RelayCommand]
        private void NavigateToLogin()
        {
            NavigateToLoginRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}