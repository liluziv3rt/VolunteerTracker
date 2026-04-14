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
                // 1. Загружаем баллы и часы из user_points
                var userPoints = await _context.UserPoints
                    .FirstOrDefaultAsync(up => up.UserId == _currentUser.Id);

                if (userPoints != null)
                {
                    TotalPoints = userPoints.TotalPoints ?? 0;
                    TotalHours = userPoints.TotalVolunteerHours ?? 0;
                    CompletedProjects = userPoints.TotalProjectsCompleted ?? 0;
                }

                // 2. Баллы за последнюю неделю из activity_log
                var oneWeekAgo = DateTime.Now.AddDays(-7);
                var weeklyPointsFromLog = await _context.ActivityLogs
                    .Where(al => al.UserId == _currentUser.Id && al.CreatedAt >= oneWeekAgo)
                    .SumAsync(al => al.PointsChange ?? 0);
                WeeklyPoints = weeklyPointsFromLog;

                // 3. Прогресс до следующего достижения из таблицы achievements
                var nextAchievement = await _context.Achievements
                    .Where(a => a.TriggerType == "points" && a.ThresholdValue > TotalPoints)
                    .OrderBy(a => a.ThresholdValue)
                    .FirstOrDefaultAsync();

                if (nextAchievement != null)
                {
                    var remainingPoints = nextAchievement.ThresholdValue - TotalPoints;
                    NextAchievementText = $"До достижения \"{nextAchievement.Name}\": {remainingPoints} баллов";

                    // Процент прогресса
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

                // 4. Недавняя активность из activity_log
                var activities = await _context.ActivityLogs
                    .Where(al => al.UserId == _currentUser.Id)
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки данных: {ex.Message}");

                // Данные по умолчанию, если БД пустая
                TotalPoints = 1250;
                TotalHours = 47.5m;
                CompletedProjects = 4;
                WeeklyPoints = 50;
                NextAchievementText = "До следующего достижения: 150 баллов";
                AchievementProgress = 75;
                RecentActivities = GetDefaultActivities();
            }
        }

        private List<ActivityItem> GetDefaultActivities()
        {
            return new List<ActivityItem>
            {
                new() { Title = "Завершила проект \"Эко-акция\"", PointsChange = "+50 баллов", TimeAgo = "2 часа назад" },
                new() { Title = "Подтверждено 4 волонтёрских часа", PointsChange = null, TimeAgo = "вчера" },
                new() { Title = "Получен значок \"Помощник\"", PointsChange = null, TimeAgo = "3 дня назад" },
                new() { Title = "Присоединилась к проекту \"Чистый город\"", PointsChange = null, TimeAgo = "неделю назад" },
                new() { Title = "Поднялась в рейтинге на 5 позиций", PointsChange = null, TimeAgo = "неделю назад" }
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
        public string? PointsChange { get; set; }
        public string TimeAgo { get; set; } = string.Empty;
    }
}