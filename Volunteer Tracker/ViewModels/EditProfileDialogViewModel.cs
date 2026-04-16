using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Volunteer_Tracker.Models;

namespace Volunteer_Tracker.ViewModels
{
    public partial class EditProfileDialogViewModel : ObservableObject
    {
        private readonly PostgresContext _context;
        private readonly User _user;

        [ObservableProperty]
        private string _fullName = string.Empty;

        [ObservableProperty]
        private string _userInitials = string.Empty;

        [ObservableProperty]
        private string _groupName = string.Empty;

        [ObservableProperty]
        private string _bio = string.Empty;

        [ObservableProperty]
        private string _telegram = string.Empty;

        [ObservableProperty]
        private string _vk = string.Empty;

        [ObservableProperty]
        private string _github = string.Empty;

        [ObservableProperty]
        private bool _phoneVisible;

        [ObservableProperty]
        private string _phone = string.Empty;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        public event EventHandler<bool>? RequestClose;

        public EditProfileDialogViewModel(User user)
        {
            _user = user;
            _context = new PostgresContext();

            FullName = $"{user.LastName} {user.FirstName}";
            UserInitials = $"{user.FirstName[0]}{user.LastName[0]}".ToUpper();
            GroupName = user.GroupName ?? "";
            Bio = user.Bio ?? "";
            Telegram = user.Telegram ?? "";
            Vk = user.Vk ?? "";
            Github = user.Github ?? "";
            PhoneVisible = user.PhoneVisible ?? false;
            Phone = user.Phone ?? "";
        }

        [RelayCommand]
        private async Task Save()
        {
            try
            {
                var dbUser = await _context.Users.FindAsync(_user.Id);
                if (dbUser != null)
                {
                    dbUser.GroupName = string.IsNullOrWhiteSpace(GroupName) ? null : GroupName;
                    dbUser.Bio = string.IsNullOrWhiteSpace(Bio) ? null : Bio;
                    dbUser.Telegram = string.IsNullOrWhiteSpace(Telegram) ? null : Telegram;
                    dbUser.Vk = string.IsNullOrWhiteSpace(Vk) ? null : Vk;
                    dbUser.Github = string.IsNullOrWhiteSpace(Github) ? null : Github;
                    dbUser.PhoneVisible = PhoneVisible;
                    dbUser.Phone = string.IsNullOrWhiteSpace(Phone) ? null : Phone;
                    dbUser.UpdatedAt = DateTime.Now;

                    await _context.SaveChangesAsync();
                    RequestClose?.Invoke(this, true);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка: {ex.Message}";
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            RequestClose?.Invoke(this, false);
        }
    }
}