using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Volunteer_Tracker.Models;
using Volunteer_Tracker.Views;

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
        private List<AchievementDisplayItem> _earnedAchievements = new();

        [ObservableProperty]
        private List<AchievementDisplayItem> _lockedAchievements = new();

        [ObservableProperty]
        private string _achievementsCount = "0/0";

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

                var userPoints = await _context.UserPoints.FirstOrDefaultAsync(up => up.UserId == _profileUser.Id);
                UserPoints = userPoints?.TotalPoints ?? 0;
                VolunteerHours = userPoints?.TotalVolunteerHours ?? 0;
                ProjectsCount = userPoints?.TotalProjectsCompleted ?? 0;

                UserBadges = await _context.UserAchievements.CountAsync(ua => ua.UserId == _profileUser.Id);

                var allPoints = await _context.UserPoints
                    .Where(up => up.TotalPoints > 0)
                    .OrderByDescending(up => up.TotalPoints)
                    .Select(up => up.UserId)
                    .ToListAsync();

                UserRank = allPoints.IndexOf(_profileUser.Id) + 1;
                if (UserRank == 0) UserRank = allPoints.Count + 1;
                await LoadRecentActivities();

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки профиля: {ex.Message}");
            }
        }


        [ObservableProperty]
        private bool _phoneVisible;

        [RelayCommand]
        private async Task EditProfile()
        {
            var dialogVm = new EditProfileDialogViewModel(_profileUser);
            var dialog = new EditProfileDialog();
            dialog.DataContext = dialogVm;

            var owner = Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;

            if (owner != null)
            {
                var result = await dialog.ShowDialog<bool>(owner);
                if (result)
                {
                    _context.ChangeTracker.Clear();

                    var freshUser = await _context.Users
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.Id == _profileUser.Id);

                    if (freshUser != null)
                    {
                        _profileUser.GroupName = freshUser.GroupName;
                        _profileUser.Bio = freshUser.Bio;
                        _profileUser.Telegram = freshUser.Telegram;
                        _profileUser.Vk = freshUser.Vk;
                        _profileUser.Github = freshUser.Github;
                        _profileUser.PhoneVisible = freshUser.PhoneVisible;
                        _profileUser.Phone = freshUser.Phone;

                        await LoadDataAsync();
                    }
                }
            }
        }

        [ObservableProperty]
        private List<ActivityItem> _recentActivities = new();

        private async Task LoadRecentActivities()
        {
            var activities = await _context.ActivityLogs
                .Where(al => al.UserId == _profileUser.Id)
                .OrderByDescending(al => al.CreatedAt)
                .Take(10)
                .ToListAsync();

            RecentActivities = activities.Select(a => new ActivityItem
            {
                Title = a.Title ?? a.ActivityType ?? "Действие",
                PointsChange = a.PointsChange > 0 ? $"+{a.PointsChange} баллов" : null,
                TimeAgo = GetTimeAgo(a.CreatedAt ?? DateTime.Now)
            }).ToList();
        }

        private string GetTimeAgo(DateTime date)
        {
            var diff = DateTime.Now - date;
            if (diff.TotalMinutes < 1) return "только что";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} мин назад";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} ч назад";
            if (diff.TotalDays < 7) return $"{(int)diff.TotalDays} дн назад";
            if (diff.TotalDays < 30) return $"{(int)(diff.TotalDays / 7)} нед назад";
            return date.ToString("dd.MM.yyyy");
        }

        private async Task LoadAchievementsAsync()
        {
            try
            {
                var allAchievements = await _context.Achievements
                    .OrderBy(a => a.ThresholdValue)
                    .ToListAsync();

                var userAchievements = await _context.UserAchievements
                    .Where(ua => ua.UserId == _profileUser.Id)
                    .Select(ua => ua.AchievementId)
                    .ToListAsync();

                var earned = allAchievements
                    .Where(a => userAchievements.Contains(a.Id))
                    .Select(a => new AchievementDisplayItem
                    {
                        Name = a.Name,
                        Points = a.ThresholdValue,
                        Icon = a.BadgeIcon ?? "🏅",
                        IsEarned = true
                    }).ToList();

                var locked = allAchievements
                    .Where(a => !userAchievements.Contains(a.Id))
                    .Select(a => new AchievementDisplayItem
                    {
                        Name = a.Name,
                        Points = a.ThresholdValue,
                        Icon = a.BadgeIcon ?? "🔒",
                        IsEarned = false
                    }).ToList();

                EarnedAchievements = earned;
                LockedAchievements = locked;
                AchievementsCount = $"{earned.Count}/{allAchievements.Count}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки достижений: {ex.Message}");
            }
        }


        [RelayCommand]
        private void OpenTelegram()
        {
            if (!string.IsNullOrEmpty(Telegram))
            {
                string url = Telegram.StartsWith("http") ? Telegram : $"https://t.me/{Telegram}";
                OpenUrl(url);
            }
        }

        [RelayCommand]
        private void OpenVk()
        {
            if (!string.IsNullOrEmpty(Vk))
            {
                string url = Vk.StartsWith("http") ? Vk : $"https://vk.com/{Vk}";
                OpenUrl(url);
            }
        }

        [RelayCommand]
        private void OpenGithub()
        {
            if (!string.IsNullOrEmpty(Github))
            {
                string url = Github.StartsWith("http") ? Github : $"https://github.com/{Github}";
                OpenUrl(url);
            }
        }

        private void OpenUrl(string url)
        {
            try
            {
                if (Uri.TryCreate(url, UriKind.Absolute, out Uri? uriResult) &&
                    (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
                {
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Неверный URL: {url}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка открытия ссылки: {ex.Message}");
            }
        }

        public class AchievementDisplayItem
        {
            public string Name { get; set; } = string.Empty;
            public int Points { get; set; }
            public string Icon { get; set; } = string.Empty;
            public bool IsEarned { get; set; }
        }

        public class ActivityItem
        {
            public string Title { get; set; } = string.Empty;
            public string? PointsChange { get; set; }
            public string TimeAgo { get; set; } = string.Empty;
        }
    }
}