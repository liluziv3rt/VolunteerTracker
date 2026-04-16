using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Volunteer_Tracker.Models;

namespace Volunteer_Tracker.ViewModels
{
    public partial class DashboardViewModel : ObservableObject
    {
        private readonly PostgresContext _context;
        private readonly User _currentUser;

        [ObservableProperty]
        private string _userFirstName = string.Empty;

        [ObservableProperty]
        private string _currentDate = string.Empty;

        [ObservableProperty]
        private decimal _totalHours;

        [ObservableProperty]
        private int _totalPoints;

        [ObservableProperty]
        private int _weeklyPoints;

        [ObservableProperty]
        private int _completedProjects;

        [ObservableProperty]
        private string _nextAchievementText = string.Empty;

        [ObservableProperty]
        private double _achievementProgress;

        [ObservableProperty]
        private List<ActivityItem> _recentActivities = new();

        public event EventHandler<string>? NavigateRequested;

        public DashboardViewModel(User user)
        {
            _currentUser = user;
            _context = new PostgresContext();
            UserFirstName = user.FirstName;
            CurrentDate = DateTime.Now.ToString("dd MMMM yyyy", new System.Globalization.CultureInfo("ru-RU"));

            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== Загрузка данных для UserId: {_currentUser.Id} ===");

                var userPoints = await _context.UserPoints
                    .FirstOrDefaultAsync(up => up.UserId == _currentUser.Id);

                if (userPoints != null)
                {
                    System.Diagnostics.Debug.WriteLine($"userPoints найден: TotalPoints={userPoints.TotalPoints}, TotalHours={userPoints.TotalVolunteerHours}, Projects={userPoints.TotalProjectsCompleted}");

                    TotalPoints = userPoints.TotalPoints ?? 0;
                    TotalHours = userPoints.TotalVolunteerHours ?? 0;
                    CompletedProjects = userPoints.TotalProjectsCompleted ?? 0;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"userPoints НЕ найден для UserId={_currentUser.Id}");

                    var newUserPoints = new UserPoint
                    {
                        UserId = _currentUser.Id,
                        TotalPoints = 0,
                        TotalVolunteerHours = 0,
                        TotalProjectsCompleted = 0,
                        UpdatedAt = DateTime.Now
                    };
                    _context.UserPoints.Add(newUserPoints);
                    await _context.SaveChangesAsync();
                    System.Diagnostics.Debug.WriteLine($"Создана новая запись user_points для UserId={_currentUser.Id}");

                    TotalPoints = 0;
                    TotalHours = 0;
                    CompletedProjects = 0;
                }

                System.Diagnostics.Debug.WriteLine($"Итоговые значения: TotalPoints={TotalPoints}, TotalHours={TotalHours}, CompletedProjects={CompletedProjects}");

                var oneWeekAgo = DateTime.Now.AddDays(-7);
                var weeklyPointsFromLog = await _context.ActivityLogs
                    .Where(al => al.UserId == _currentUser.Id && al.CreatedAt >= oneWeekAgo)
                    .SumAsync(al => al.PointsChange ?? 0);
                WeeklyPoints = weeklyPointsFromLog;
                System.Diagnostics.Debug.WriteLine($"WeeklyPoints: {WeeklyPoints}");

                await LoadAchievementProgress();

                await LoadRecentActivities();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ОШИБКА загрузки: {ex.Message}");
                SetDefaultValues();
            }
        }

        private async Task LoadAchievementProgress()
        {
            var nextAchievement = await _context.Achievements
                .Where(a => a.TriggerType == "points" && a.ThresholdValue > TotalPoints)
                .OrderBy(a => a.ThresholdValue)
                .FirstOrDefaultAsync();

            if (nextAchievement != null)
            {
                var remainingPoints = nextAchievement.ThresholdValue - TotalPoints;
                NextAchievementText = $"До достижения \"{nextAchievement.Name}\": {remainingPoints} баллов";

                var previousThreshold = await _context.Achievements
                    .Where(a => a.TriggerType == "points" && a.ThresholdValue <= TotalPoints)
                    .OrderByDescending(a => a.ThresholdValue)
                    .Select(a => a.ThresholdValue)
                    .FirstOrDefaultAsync();

                var totalNeeded = nextAchievement.ThresholdValue - previousThreshold;
                var earned = TotalPoints - previousThreshold;
                AchievementProgress = totalNeeded > 0 ? (double)earned / totalNeeded * 100 : 0;
            }
            else
            {
                NextAchievementText = "Все достижения получены!";
                AchievementProgress = 100;
            }
        }

        private async Task LoadRecentActivities()
        {
            var activities = await _context.ActivityLogs
                .Where(al => al.UserId == _currentUser.Id)
                .OrderByDescending(al => al.CreatedAt)
                .Take(10)
                .ToListAsync();

            if (activities.Any())
            {
                RecentActivities = activities.Select(a => new ActivityItem
                {
                    Title = a.Title ?? a.ActivityType ?? "Действие",
                    Subtitle = GetSubtitleByType(a),
                    PointsChange = a.PointsChange > 0 ? $"+{a.PointsChange}" : null,
                    TimeAgo = GetTimeAgo(a.CreatedAt ?? DateTime.Now),
                    ActivityType = a.ActivityType ?? "other"
                }).ToList();
            }
            else
            {
                RecentActivities = GetDefaultActivities();
            }
        }

        private string GetSubtitleByType(ActivityLog log)
        {
            return log.ActivityType switch
            {
                "project_completed" => "Завершён проект",
                "project_joined" => "Присоединился к проекту",
                "volunteer_hours" => "Подтверждены часы",
                "achievement" => "Новое достижение",
                "rating_up" => "Поднялся в рейтинге",
                "points_earned" => "Начислены баллы",
                _ => log.Description ?? ""
            };
        }


        private void SetDefaultValues()
        {
            TotalPoints = 0;
            TotalHours = 0;
            CompletedProjects = 0;
            WeeklyPoints = 0;
            NextAchievementText = "Загрузка данных...";
            AchievementProgress = 0;
            RecentActivities = GetDefaultActivities();
        }

        private List<ActivityItem> GetDefaultActivities()
        {
            return new List<ActivityItem>
            {
                new() { Title = "Добро пожаловать! Начните волонтёрство", PointsChange = null, TimeAgo = "только что" }
            };
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

        [RelayCommand]
        private void StartVolunteer() => NavigateRequested?.Invoke(this, "Volunteer");

        [RelayCommand]
        private void JoinProject() => NavigateRequested?.Invoke(this, "Projects");

        [RelayCommand]
        private void NavigateToVolunteer() => NavigateRequested?.Invoke(this, "Volunteer");

        [RelayCommand]
        private void NavigateToRating() => NavigateRequested?.Invoke(this, "Rating");

        [RelayCommand]
        private void NavigateToProjects() => NavigateRequested?.Invoke(this, "Projects");
    }

    public class ActivityItem
    {
        public string Title { get; set; } = string.Empty;
        public string? Subtitle { get; set; }
        public string? PointsChange { get; set; }
        public string TimeAgo { get; set; } = string.Empty;
        public string ActivityType { get; set; } = string.Empty;

        public string Icon => GetIcon();
        public string IconBackground => GetIconBackground();
        public string SubtitleColor => GetSubtitleColor();

        private string GetIcon()
        {
            return ActivityType switch
            {
                "project_completed" => "🎉",
                "project_joined" => "📋",
                "volunteer_hours" => "🤲",
                "achievement" => "🏅",
                "rating_up" => "📈",
                "points_earned" => "⭐",
                _ => "📌"
            };
        }

        private string GetIconBackground()
        {
            return ActivityType switch
            {
                "project_completed" => "#E8F5E9",
                "project_joined" => "#E3F2FD",
                "volunteer_hours" => "#E6F4EA",
                "achievement" => "#FFF3E0",
                "rating_up" => "#E8EAF6",
                "points_earned" => "#FFF8E1",
                _ => "#F5F5F5"
            };
        }

        private string GetSubtitleColor()
        {
            return ActivityType switch
            {
                "project_completed" => "#34A853",
                "project_joined" => "#1A73E8",
                "volunteer_hours" => "#5F6368",
                "achievement" => "#FB8C00",
                "rating_up" => "#34A853",
                "points_earned" => "#FB8C00",
                _ => "#5F6368"
            };
        }
    }
}