using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Volunteer_Tracker.Models;

namespace Volunteer_Tracker.ViewModels
{
    public partial class ProfileViewModel : ObservableObject
    {
        private readonly PostgresContext _context;
        private readonly User _currentUser;
        private readonly User _profileUser;

        [ObservableProperty]
        private string _userInitials = string.Empty;

        [ObservableProperty]
        private string _userFullName = string.Empty;

        [ObservableProperty]
        private string _userRole = string.Empty;

        [ObservableProperty]
        private string _userGroup = string.Empty;

        [ObservableProperty]
        private int _userPoints;

        [ObservableProperty]
        private int _userBadges;

        [ObservableProperty]
        private int _userRank;

        [ObservableProperty]
        private string _bio = string.Empty;

        [ObservableProperty]
        private string _telegram = string.Empty;

        [ObservableProperty]
        private string _vk = string.Empty;

        [ObservableProperty]
        private string _github = string.Empty;

        [ObservableProperty]
        private string _phone = string.Empty;

        [ObservableProperty]
        private string _showPhone = string.Empty;

        [ObservableProperty]
        private int _projectsCount;

        [ObservableProperty]
        private decimal _volunteerHours;

        [ObservableProperty]
        private bool _isOwnProfile;

        [ObservableProperty]
        private bool _noSocials;

        public ProfileViewModel(User currentUser, int? profileUserId = null)
        {
            _currentUser = currentUser;
            _context = new PostgresContext();
            _profileUser = profileUserId.HasValue && profileUserId.Value != currentUser.Id
                ? _context.Users.Find(profileUserId.Value)
                : currentUser;

            IsOwnProfile = _profileUser.Id == currentUser.Id;
            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                UserFullName = $"{_profileUser.LastName} {_profileUser.FirstName}";
                UserInitials = $"{_profileUser.FirstName?[0]}{_profileUser.LastName?[0]}".ToUpper();
                UserRole = _profileUser.Role switch
                {
                    "student" => "Студент",
                    "project_leader" => "Руководитель проектов",
                    "admin" => "Администратор",
                    _ => "Пользователь"
                };
                UserGroup = _profileUser.GroupName ?? "";
                Bio = _profileUser.Bio ?? "";
                Telegram = _profileUser.Telegram ?? "";
                Vk = _profileUser.Vk ?? "";
                Github = _profileUser.Github ?? "";
                Phone = _profileUser.Phone ?? "";

                ShowPhone = (_profileUser.PhoneVisible == true || IsOwnProfile) ? Phone : "";
                NoSocials = string.IsNullOrEmpty(Telegram) && string.IsNullOrEmpty(Vk) && string.IsNullOrEmpty(Github);

                // Статистика
                var userPoints = await _context.UserPoints.FirstOrDefaultAsync(up => up.UserId == _profileUser.Id);
                UserPoints = userPoints?.TotalPoints ?? 0;
                VolunteerHours = userPoints?.TotalVolunteerHours ?? 0;
                ProjectsCount = userPoints?.TotalProjectsCompleted ?? 0;

                UserBadges = await _context.UserAchievements.CountAsync(ua => ua.UserId == _profileUser.Id);

                // Ранг пользователя
                var allPoints = await _context.UserPoints
                    .Where(up => up.TotalPoints > 0)
                    .OrderByDescending(up => up.TotalPoints)
                    .Select(up => up.UserId)
                    .ToListAsync();

                UserRank = allPoints.IndexOf(_profileUser.Id) + 1;
                if (UserRank == 0) UserRank = allPoints.Count + 1;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки профиля: {ex.Message}");
            }
        }

        [RelayCommand]
        private void EditProfile()
        {
            // TODO: Открыть диалог редактирования профиля
        }

        [RelayCommand]
        private void OpenTelegram()
        {
            if (!string.IsNullOrEmpty(Telegram))
                Process.Start(new ProcessStartInfo($"https://t.me/{Telegram}") { UseShellExecute = true });
        }

        [RelayCommand]
        private void OpenVk()
        {
            if (!string.IsNullOrEmpty(Vk))
                Process.Start(new ProcessStartInfo(Vk) { UseShellExecute = true });
        }

        [RelayCommand]
        private void OpenGithub()
        {
            if (!string.IsNullOrEmpty(Github))
                Process.Start(new ProcessStartInfo(Github) { UseShellExecute = true });
        }
    }
}