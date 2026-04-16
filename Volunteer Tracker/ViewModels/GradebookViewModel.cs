using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Volunteer_Tracker.Models;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Volunteer_Tracker.ViewModels
{
    public partial class GradebookViewModel : ObservableObject
    {
        private readonly PostgresContext _context;
        private readonly User _profileUser;

        [ObservableProperty]
        private string _userInitials = string.Empty;

        [ObservableProperty]
        private string _userFullName = string.Empty;

        [ObservableProperty]
        private string _userGroup = string.Empty;

        [ObservableProperty]
        private string _joinDate = string.Empty;

        [ObservableProperty]
        private decimal _totalHours;

        [ObservableProperty]
        private int _totalPoints;

        [ObservableProperty]
        private int _totalProjects;

        [ObservableProperty]
        private int _totalBadges;

        [ObservableProperty]
        private bool _hasNoActivities = true;

        [ObservableProperty]
        private List<GradebookActivityItem> _activities = new();

        [ObservableProperty]
        private List<string> _activityTypes = new();

        [ObservableProperty]
        private string _selectedActivityType = "Все";

        [ObservableProperty]
        private DateTimeOffset? _startDate = DateTimeOffset.Now.AddMonths(-1);

        [ObservableProperty]
        private DateTimeOffset? _endDate = DateTimeOffset.Now;

        [ObservableProperty]
        private string _startMonth = "Март";

        [ObservableProperty]
        private string _endMonth = "Апрель";

        [ObservableProperty]
        private List<string> _availableMonths = new()
{
    "Январь", "Февраль", "Март", "Апрель", "Май", "Июнь",
    "Июль", "Август", "Сентябрь", "Октябрь", "Ноябрь", "Декабрь"
};

        public GradebookViewModel(User currentUser, int? userId = null)
        {
            _profileUser = userId.HasValue && userId.Value != currentUser.Id
                ? _context.Users.Find(userId.Value)
                : currentUser;

            _context = new PostgresContext();

            UserFullName = $"{_profileUser.LastName} {_profileUser.FirstName}";
            UserInitials = $"{_profileUser.FirstName?[0]}{_profileUser.LastName?[0]}".ToUpper();
            UserGroup = _profileUser.GroupName ?? "—";
            JoinDate = _profileUser.CreatedAt?.ToString("dd.MM.yyyy") ?? "—";

            ActivityTypes = new List<string> { "Все", "Волонтёрство", "Проекты", "Достижения" };
            StartDate = DateTime.Now.AddMonths(-1);
            EndDate = DateTime.Now;

            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                // Статистика
                var userPoints = await _context.UserPoints
                    .FirstOrDefaultAsync(up => up.UserId == _profileUser.Id);

                if (userPoints != null)
                {
                    TotalPoints = userPoints.TotalPoints ?? 0;
                    TotalHours = userPoints.TotalVolunteerHours ?? 0;
                    TotalProjects = userPoints.TotalProjectsCompleted ?? 0;
                }

                TotalBadges = await _context.UserAchievements
                    .CountAsync(ua => ua.UserId == _profileUser.Id);

                await LoadActivitiesAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки: {ex.Message}");
            }
        }

        private async Task LoadActivitiesAsync()
        {
            var query = _context.ActivityLogs
                .Where(al => al.UserId == _profileUser.Id);

            // Фильтр по датам
            if (StartDate.HasValue)
                query = query.Where(al => al.CreatedAt >= StartDate.Value.DateTime);
            if (EndDate.HasValue)
                query = query.Where(al => al.CreatedAt <= EndDate.Value.DateTime.AddDays(1));

            var activities = await query
                .OrderByDescending(al => al.CreatedAt)
                .ToListAsync();

            HasNoActivities = Activities == null || Activities.Count == 0;

            var items = new List<GradebookActivityItem>();

            foreach (var a in activities)
            {
                // Фильтр по типу
                if (SelectedActivityType != "Все")
                {
                    if (SelectedActivityType == "Волонтёрство" && a.ActivityType != "volunteer_hours" && a.ActivityType != "volunteer")
                        continue;
                    if (SelectedActivityType == "Проекты" && a.ActivityType != "project_completed" && a.ActivityType != "project_joined")
                        continue;
                    if (SelectedActivityType == "Достижения" && a.ActivityType != "achievement")
                        continue;
                }

                items.Add(new GradebookActivityItem
                {
                    Title = a.Title ?? a.ActivityType ?? "Действие",
                    Subtitle = GetSubtitleByType(a),
                    Date = a.CreatedAt?.ToString("dd MMMM yyyy") ?? "",
                    PointsChange = a.PointsChange > 0 ? $"+{a.PointsChange}" : "",
                    Icon = GetIconByType(a.ActivityType),
                    IconBackground = GetIconBackgroundByType(a.ActivityType),
                    SubtitleColor = GetSubtitleColorByType(a.ActivityType)
                });
            }

            Activities = items;
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

        private string GetIconByType(string? type)
        {
            return type switch
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

        private string GetIconBackgroundByType(string? type)
        {
            return type switch
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

        private string GetSubtitleColorByType(string? type)
        {
            return type switch
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

        [RelayCommand]
        private async Task ApplyFilter()
        {
            // Преобразуем месяцы в даты (примерно)
            var year = DateTime.Now.Year;

            StartDate = GetDateFromMonth(StartMonth, year);
            EndDate = GetDateFromMonth(EndMonth, year).AddMonths(1).AddDays(-1); // конец месяца

            await LoadActivitiesAsync();
        }

        private DateTimeOffset GetDateFromMonth(string monthName, int year)
        {
            var months = new Dictionary<string, int>
    {
        {"Январь", 1}, {"Февраль", 2}, {"Март", 3}, {"Апрель", 4}, {"Май", 5}, {"Июнь", 6},
        {"Июль", 7}, {"Август", 8}, {"Сентябрь", 9}, {"Октябрь", 10}, {"Ноябрь", 11}, {"Декабрь", 12}
    };

            int month = months.GetValueOrDefault(monthName, DateTime.Now.Month);
            return new DateTimeOffset(year, month, 1, 0, 0, 0, TimeSpan.Zero);
        }

        [RelayCommand]
        private async Task ExportPdf()
        {
            try
            {
                // Простой экспорт в HTML (можно потом конвертировать в PDF)
                var sb = new StringBuilder();
                sb.AppendLine("<html><head><meta charset='utf-8'><title>Зачётная книжка</title>");
                sb.AppendLine("<style>");
                sb.AppendLine("body { font-family: Arial, sans-serif; margin: 40px; }");
                sb.AppendLine("h1 { color: #202124; }");
                sb.AppendLine(".header { margin-bottom: 30px; }");
                sb.AppendLine(".stats { display: flex; gap: 20px; margin-bottom: 30px; }");
                sb.AppendLine(".stat { background: #F8F9FA; padding: 15px; border-radius: 12px; text-align: center; }");
                sb.AppendLine(".activity { border-bottom: 1px solid #F0F0F0; padding: 12px 0; }");
                sb.AppendLine("</style></head><body>");

                sb.AppendLine($"<h1>Зачётная книжка завода</h1>");
                sb.AppendLine($"<p><strong>{UserFullName}</strong><br/>Группа: {UserGroup}<br/>Дата вступления: {JoinDate}</p>");

                sb.AppendLine("<div class='stats'>");
                sb.AppendLine($"<div class='stat'>🤲<br/><strong>{TotalHours:F1}</strong><br/>Часов</div>");
                sb.AppendLine($"<div class='stat'>📁<br/><strong>{TotalProjects}</strong><br/>Проектов</div>");
                sb.AppendLine($"<div class='stat'>⭐<br/><strong>{TotalPoints}</strong><br/>Баллов</div>");
                sb.AppendLine($"<div class='stat'>🏅<br/><strong>{TotalBadges}</strong><br/>Значков</div>");
                sb.AppendLine("</div>");

                sb.AppendLine("<h2>Активность</h2>");
                foreach (var a in Activities)
                {
                    sb.AppendLine($"<div class='activity'>");
                    sb.AppendLine($"<strong>{a.Title}</strong><br/>");
                    sb.AppendLine($"<span>{a.Subtitle}</span> <span style='color:#9AA0A6'>{a.Date}</span>");
                    if (!string.IsNullOrEmpty(a.PointsChange))
                        sb.AppendLine($"<br/><span style='color:#34A853'>{a.PointsChange}</span>");
                    sb.AppendLine("</div>");
                }

                sb.AppendLine("</body></html>");

                var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"Gradebook_{_profileUser.LastName}_{DateTime.Now:yyyyMMdd}.html");
                await File.WriteAllTextAsync(filePath, sb.ToString());

                Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка экспорта: {ex.Message}");
            }
        }
    }

    public class GradebookActivityItem
    {
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string PointsChange { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string IconBackground { get; set; } = "#F5F5F5";
        public string SubtitleColor { get; set; } = "#5F6368";
    }
}