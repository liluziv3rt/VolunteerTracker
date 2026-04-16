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
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Volunteer_Tracker.ViewModels
{
    public partial class GradebookViewModel : ObservableObject
    {
        private readonly PostgresContext _context;
        private readonly User _profileUser;

        [ObservableProperty] private string _userInitials = string.Empty;
        [ObservableProperty] private string _userFullName = string.Empty;
        [ObservableProperty] private string _userGroup = string.Empty;
        [ObservableProperty] private string _joinDate = string.Empty;
        [ObservableProperty] private decimal _totalHours;
        [ObservableProperty] private int _totalPoints;
        [ObservableProperty] private int _totalProjects;
        [ObservableProperty] private int _totalBadges;
        [ObservableProperty] private bool _hasNoActivities = true;
        [ObservableProperty] private List<GradebookActivityItem> _activities = new();
        [ObservableProperty] private List<string> _activityTypes = new();
        [ObservableProperty] private string _selectedActivityType = "Все";
        [ObservableProperty] private DateTimeOffset? _startDate = DateTimeOffset.Now.AddMonths(-1);
        [ObservableProperty] private DateTimeOffset? _endDate = DateTimeOffset.Now;
        [ObservableProperty] private string _startMonth = "Март";
        [ObservableProperty] private string _endMonth = "Апрель";
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
            var query = _context.ActivityLogs.Where(al => al.UserId == _profileUser.Id);

            if (StartDate.HasValue)
                query = query.Where(al => al.CreatedAt >= StartDate.Value.DateTime);
            if (EndDate.HasValue)
                query = query.Where(al => al.CreatedAt <= EndDate.Value.DateTime.AddDays(1));

            var activities = await query.OrderByDescending(al => al.CreatedAt).ToListAsync();

            var items = new List<GradebookActivityItem>();

            foreach (var a in activities)
            {
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
            HasNoActivities = Activities.Count == 0;
        }

        // Методы GetSubtitleByType, GetIconByType и т.д. оставил без изменений
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

        private string GetIconBackgroundByType(string? type) => "#F5F5F5";

        private string GetSubtitleColorByType(string? type)
        {
            return type switch
            {
                "project_completed" => "#34A853",
                "project_joined" => "#1A73E8",
                "volunteer_hours" => "#5F6368",
                "achievement" => "#FB8C00",
                _ => "#5F6368"
            };
        }

        [RelayCommand]
        private async Task ApplyFilter()
        {
            var year = DateTime.Now.Year;
            StartDate = GetDateFromMonth(StartMonth, year);
            EndDate = GetDateFromMonth(EndMonth, year).AddMonths(1).AddDays(-1);
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
                var filePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    $"Зачётная_книжка_{_profileUser.LastName}_{DateTime.Now:yyyyMMdd_HHmm}.pdf");

                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(40);
                        page.DefaultTextStyle(TextStyle.Default.FontFamily("Arial").FontSize(11));

                        page.Header().Element(ComposeHeader);
                        page.Content().Element(ComposeContent);
                        page.Footer().AlignCenter().Text(text =>
                        {
                            text.Span("Страница ");
                            text.CurrentPageNumber();
                            text.Span(" из ");
                            text.TotalPages();
                        });
                    });
                });

                document.GeneratePdf(filePath);

                Process.Start(new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });

                Debug.WriteLine($"PDF успешно создан: {filePath}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка экспорта в PDF: {ex.Message}");
            }
        }

        private void ComposeHeader(IContainer container)
        {
            container.Column(column =>
            {
                column.Item().Text("Зачётная книжка завода").FontSize(24).Bold().FontColor("#202124");
                column.Item().Text("Цифровое портфолио студента").FontSize(13).FontColor("#5F6368");
                column.Item().PaddingTop(8).LineHorizontal(1).LineColor("#DADCE0");
            });
        }

        private void ComposeContent(IContainer container)
        {
            container.Column(column =>
            {
                // Информация о студенте
                column.Item().PaddingTop(20).Text(UserFullName).FontSize(18).Bold();
                column.Item().Text($"Группа: {UserGroup}     Дата вступления: {JoinDate}")
                      .FontSize(12).FontColor("#5F6368");

                // Статистика
                column.Item().PaddingVertical(20).Row(row =>
                {
                    row.RelativeItem().Component(new StatComponent("🕒", TotalHours.ToString("F1"), "Часов", "#1E88E5"));
                    row.RelativeItem().Component(new StatComponent("📁", TotalProjects.ToString(), "Проектов", "#34A853"));
                    row.RelativeItem().Component(new StatComponent("⭐", TotalPoints.ToString(), "Баллов", "#8E24AA"));
                    row.RelativeItem().Component(new StatComponent("🏅", TotalBadges.ToString(), "Значков", "#FB8C00"));
                });

                // Заголовок "Вся активность" с отступом снизу
                column.Item().PaddingBottom(12).Text("Вся активность")
                      .FontSize(16).Bold();

                foreach (var activity in Activities)
                {
                    column.Item().BorderBottom(1).BorderColor("#E0E0E0").PaddingVertical(10).Row(row =>
                    {
                        row.ConstantItem(50).Text(activity.Icon).FontSize(28).AlignCenter();

                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text(activity.Title).Bold().FontSize(13);
                            col.Item().Text(activity.Subtitle).FontColor(activity.SubtitleColor);
                        });

                        row.ConstantItem(140).Column(col =>
                        {
                            col.Item().AlignRight().Text(activity.Date).FontColor("#9AA0A6").FontSize(12);
                            if (!string.IsNullOrEmpty(activity.PointsChange))
                            {
                                col.Item().AlignRight().Text(activity.PointsChange)
                                    .FontColor("#34A853").Bold().FontSize(14);
                            }
                        });
                    });
                }

                if (Activities.Count == 0)
                {
                    column.Item().Padding(40).AlignCenter()
                          .Text("Нет активности за выбранный период")
                          .FontColor("#9AA0A6").FontSize(14);
                }
            });
        }

        // Компонент карточки статистики
        private class StatComponent : IComponent
        {
            private readonly string _icon;
            private readonly string _value;
            private readonly string _label;
            private readonly string _color;

            public StatComponent(string icon, string value, string label, string color)
            {
                _icon = icon;
                _value = value;
                _label = label;
                _color = color;
            }

            public void Compose(IContainer container)
            {
                container.Border(1).BorderColor("#E0E0E0").CornerRadius(8).Padding(12).Column(column =>
                {
                    column.Item().AlignCenter().Text(_icon).FontSize(32);
                    column.Item().AlignCenter().Text(_value).FontSize(20).Bold().FontColor(_color);
                    column.Item().AlignCenter().Text(_label).FontSize(11).FontColor("#5F6368");
                });
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