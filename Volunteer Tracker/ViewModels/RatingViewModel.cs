using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volunteer_Tracker.Models;
using Volunteer_Tracker.Views;

namespace Volunteer_Tracker.ViewModels
{
    public partial class RatingViewModel : ObservableObject
    {
        private readonly PostgresContext _context;
        private readonly User _currentUser;

        [ObservableProperty]
        private List<LeaderboardItem> _leaderboard = new();

        [ObservableProperty]
        private List<AchievementItem> _earnedAchievements = new();

        [ObservableProperty]
        private List<AchievementItem> _lockedAchievements = new();

        [ObservableProperty]
        private string _achievementsCount = "0/0";

        [ObservableProperty]
        private bool _isThisMonthSelected = true;

        [ObservableProperty]
        private bool _isAllTimeSelected;

        [ObservableProperty]
        private int _currentUserRank;

        [ObservableProperty]
        private string _currentUserFullName = string.Empty;

        [ObservableProperty]
        private string _currentUserInitials = string.Empty;

        [ObservableProperty]
        private int _currentUserPoints;

        [ObservableProperty]
        private int _currentUserBadgesCount;

        public RatingViewModel(User user)
        {
            _currentUser = user;
            _context = new PostgresContext();
            _ = CheckAndAwardAchievements(user.Id);
            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                // Получаем всех студентов с баллами
                var studentsQuery = await (from u in _context.Users
                                           join up in _context.UserPoints on u.Id equals up.UserId into pointsJoin
                                           from up in pointsJoin.DefaultIfEmpty()
                                           where u.Role == "student" && u.IsActive == true
                                           orderby (up.TotalPoints ?? 0) descending
                                           select new
                                           {
                                               u.Id,
                                               u.LastName,
                                               u.FirstName,
                                               u.GroupName,
                                               Points = up.TotalPoints ?? 0
                                           })
                                           .ToListAsync();

                // Получаем количество значков для всех пользователей
                var badgesCount = await _context.UserAchievements
                    .GroupBy(ua => ua.UserId)
                    .Select(g => new { UserId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.UserId, x => x.Count);

                var studentsWithPoints = new List<LeaderboardItem>();
                int rank = 1;

                foreach (var item in studentsQuery)
                {
                    var leaderItem = new LeaderboardItem
                    {
                        UserId = item.Id,
                        FullName = $"{item.LastName} {item.FirstName}",
                        GroupName = item.GroupName ?? "Факультет",
                        Points = item.Points,
                        BadgesCount = badgesCount.GetValueOrDefault(item.Id, 0),
                        Rank = rank,
                        IsCurrentUser = item.Id == _currentUser.Id,
                        Initials = item.FirstName != null && item.LastName != null
                            ? $"{item.FirstName[0]}{item.LastName[0]}".ToUpper()
                            : "??"
                    };

                    if (rank == 1) leaderItem.Medal = "🥇";
                    else if (rank == 2) leaderItem.Medal = "🥈";
                    else if (rank == 3) leaderItem.Medal = "🥉";
                    else leaderItem.Medal = "";

                    if (rank == 1) leaderItem.RankColor = "#FFD700";
                    else if (rank == 2) leaderItem.RankColor = "#C0C0C0";
                    else if (rank == 3) leaderItem.RankColor = "#CD7F32";
                    else leaderItem.RankColor = "#5F6368";

                    if (rank == 1) leaderItem.RowBackground = "#FFF9E6";
                    else if (rank == 2) leaderItem.RowBackground = "#F0F7FF";
                    else if (rank == 3) leaderItem.RowBackground = "#FFF4EB";
                    else leaderItem.RowBackground = "Transparent";

                    if (leaderItem.IsCurrentUser && rank > 3)
                    {
                        leaderItem.RowBackground = "#E8F0FE";
                    }

                    if (leaderItem.IsCurrentUser)
                    {
                        CurrentUserRank = rank;
                        CurrentUserFullName = leaderItem.FullName;
                        CurrentUserInitials = leaderItem.Initials;
                        CurrentUserPoints = leaderItem.Points;
                        CurrentUserBadgesCount = leaderItem.BadgesCount;
                    }

                    studentsWithPoints.Add(leaderItem);
                    rank++;
                }

                Leaderboard = studentsWithPoints.Take(20).ToList();

                await LoadAchievementsAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки рейтинга: {ex.Message}");
                Leaderboard = new List<LeaderboardItem>();
            }
        }

        public async Task CheckAndAwardAchievements(int userId)
        {
            using var context = new PostgresContext();

            // Получаем данные пользователя
            var userPoints = await context.UserPoints.FirstOrDefaultAsync(up => up.UserId == userId);
            if (userPoints == null) return;

            int totalPoints = userPoints.TotalPoints ?? 0;
            decimal totalHours = userPoints.TotalVolunteerHours ?? 0;
            int totalProjects = userPoints.TotalProjectsCompleted ?? 0;
            int totalRentals = userPoints.TotalRentalMinutes ?? 0;  // 👈 ИСПРАВЛЕНО

            // Получаем все достижения
            var allAchievements = await context.Achievements.ToListAsync();

            // Получаем уже полученные достижения пользователя
            var earnedIds = await context.UserAchievements
                .Where(ua => ua.UserId == userId)
                .Select(ua => ua.AchievementId)
                .ToListAsync();

            bool anyNew = false;

            foreach (var achievement in allAchievements)
            {
                if (earnedIds.Contains(achievement.Id)) continue;

                bool earned = false;

                switch (achievement.TriggerType)
                {
                    case "points":
                        earned = totalPoints >= achievement.ThresholdValue;
                        break;
                    case "hours":
                        earned = totalHours >= achievement.ThresholdValue;
                        break;
                    case "projects":
                        earned = totalProjects >= achievement.ThresholdValue;
                        break;
                    case "rentals":
                        earned = totalRentals >= achievement.ThresholdValue;
                        break;
                }

                if (earned)
                {
                    context.UserAchievements.Add(new UserAchievement
                    {
                        UserId = userId,
                        AchievementId = achievement.Id,
                        EarnedAt = DateTime.Now
                    });

                    // Добавляем бонусные баллы
                    if (achievement.BonusPoints > 0)
                    {
                        userPoints.TotalPoints = (userPoints.TotalPoints ?? 0) + achievement.BonusPoints;
                    }

                    // Добавляем запись в активность
                    context.ActivityLogs.Add(new ActivityLog
                    {
                        UserId = userId,
                        ActivityType = "achievement",
                        Title = $"Получено достижение \"{achievement.Name}\"",
                        PointsChange = achievement.BonusPoints,
                        CreatedAt = DateTime.Now
                    });

                    anyNew = true;
                }
            }

            if (anyNew)
            {
                await context.SaveChangesAsync();
            }
        }

        public async Task LoadLeaderboardAsync()
        {
            try
            {
                var oneMonthAgo = DateTime.Now.AddMonths(-1);
                List<(int UserId, int Points)> userPointsList;

                if (IsThisMonthSelected)
                {
                    // Баллы за этот месяц из activity_log
                    var monthlyPoints = await _context.ActivityLogs
                        .Where(al => al.CreatedAt >= oneMonthAgo && al.PointsChange > 0)
                        .GroupBy(al => al.UserId)
                        .Select(g => new { UserId = g.Key, Points = g.Sum(x => x.PointsChange ?? 0) })
                        .ToDictionaryAsync(x => x.UserId, x => x.Points);

                    var users = await _context.Users
                        .Where(u => u.Role == "student" && u.IsActive == true)
                        .ToListAsync();

                    userPointsList = users.Select(u => (u.Id, monthlyPoints.GetValueOrDefault(u.Id, 0))).ToList();
                }
                else
                {
                    // Баллы за всё время из таблицы user_points
                    var userPoints = await _context.UserPoints
                        .Where(up => up.TotalPoints > 0)
                        .ToDictionaryAsync(up => up.UserId, up => up.TotalPoints ?? 0);

                    var users = await _context.Users
                        .Where(u => u.Role == "student" && u.IsActive == true)
                        .ToListAsync();

                    userPointsList = users.Select(u => (u.Id, userPoints.GetValueOrDefault(u.Id, 0))).ToList();
                }

                // Получаем количество значков для каждого пользователя
                var badgesCount = await _context.UserAchievements
                    .GroupBy(ua => ua.UserId)
                    .Select(g => new { UserId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.UserId, x => x.Count);

                // Получаем информацию о пользователях
                var usersInfo = await _context.Users
                    .Where(u => u.Role == "student" && u.IsActive == true)
                    .ToDictionaryAsync(u => u.Id, u => u);

                // Формируем список лидеров, сортируем по баллам, берём ТОП-10
                var leaderboardList = new List<LeaderboardItem>();
                int rank = 1;

                foreach (var (userId, points) in userPointsList.OrderByDescending(x => x.Points))
                {
                    if (!usersInfo.ContainsKey(userId)) continue;

                    var user = usersInfo[userId];
                    var item = new LeaderboardItem
                    {
                        UserId = userId,
                        FullName = $"{user.LastName} {user.FirstName}",
                        GroupName = user.GroupName ?? "Факультет",
                        Points = points,
                        BadgesCount = badgesCount.GetValueOrDefault(userId, 0),
                        Rank = rank,
                        IsCurrentUser = userId == _currentUser.Id
                    };

                    var names = item.FullName.Split(' ');
                    item.Initials = names.Length >= 2
                        ? $"{names[0][0]}{names[1][0]}".ToUpper()
                        : names[0].Substring(0, Math.Min(2, names[0].Length)).ToUpper();

                    // Оформление 1, 2, 3 места
                    if (rank == 1)
                    {
                        item.Medal = "🥇";
                        item.RankColor = "#FFD700";
                        item.RowBackground = "#FFF9E6";
                    }
                    else if (rank == 2)
                    {
                        item.Medal = "🥈";
                        item.RankColor = "#C0C0C0";
                        item.RowBackground = "#F0F7FF";
                    }
                    else if (rank == 3)
                    {
                        item.Medal = "🥉";
                        item.RankColor = "#CD7F32";
                        item.RowBackground = "#FFF4EB";
                    }
                    else
                    {
                        item.Medal = "";
                        item.RankColor = "#5F6368";
                        item.RowBackground = "Transparent";
                    }

                    if (item.IsCurrentUser && rank > 3)
                    {
                        item.RowBackground = "#E8F0FE";
                    }

                    leaderboardList.Add(item);
                    rank++;
                    if (rank > 10) break;
                }

                Leaderboard = leaderboardList;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки лидеров: {ex.Message}");
                Leaderboard = new List<LeaderboardItem>();
            }
        }

        [RelayCommand]
        private async Task OpenProfile(int userId)
        {
            var profileVm = new ProfileViewModel(_currentUser, userId);

            if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindow = desktop.MainWindow;
                if (mainWindow?.DataContext is MainWindowViewModel mainVm)
                {
                    mainVm.CurrentView = new ProfileView { DataContext = profileVm };
                }
            }
        }

        private async Task LoadAchievementsAsync()
        {
            try
            {
                var allAchievements = await _context.Achievements
                    .OrderBy(a => a.ThresholdValue)
                    .ToListAsync();

                var userAchievements = await _context.UserAchievements
                    .Where(ua => ua.UserId == _currentUser.Id)
                    .Select(ua => ua.AchievementId)
                    .ToListAsync();

                var earned = allAchievements
                    .Where(a => userAchievements.Contains(a.Id))
                    .Select(a => new AchievementItem
                    {
                        Name = a.Name,
                        Description = a.Description ?? a.TriggerType switch
                        {
                            "points" => $"Набрать {a.ThresholdValue} баллов",
                            "hours" => $"Отработать {a.ThresholdValue} волонтёрских часов",
                            "projects" => $"Завершить {a.ThresholdValue} проектов",
                            "rentals" => $"Взять в аренду ресурс {a.ThresholdValue} раз",
                            _ => $"Достичь {a.ThresholdValue}"
                        },
                        Icon = a.BadgeIcon ?? "🏅",
                        IsEarned = true
                    }).ToList();

                var locked = allAchievements
                    .Where(a => !userAchievements.Contains(a.Id))
                    .Select(a => new AchievementItem
                    {
                        Name = a.Name,
                        Description = a.Description ?? a.TriggerType switch
                        {
                            "points" => $"Набрать {a.ThresholdValue} баллов",
                            "hours" => $"Отработать {a.ThresholdValue} волонтёрских часов",
                            "projects" => $"Завершить {a.ThresholdValue} проектов",
                            "rentals" => $"Взять в аренду ресурс {a.ThresholdValue} раз",
                            _ => $"Достичь {a.ThresholdValue}"
                        },
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
                EarnedAchievements = new List<AchievementItem>();
                LockedAchievements = new List<AchievementItem>();
                AchievementsCount = "0/0";
            }
        }

        public async Task SwitchToThisMonth()
        {
            IsThisMonthSelected = true;
            IsAllTimeSelected = false;
            await LoadLeaderboardAsync();
        }

        public async Task SwitchToAllTime()
        {
            IsThisMonthSelected = false;
            IsAllTimeSelected = true;
            await LoadLeaderboardAsync();
        }
    }

    public class LeaderboardItem
    {
        public int UserId { get; set; }
        public int Rank { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Initials { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public int Points { get; set; }
        public int BadgesCount { get; set; }
        public string Medal { get; set; } = string.Empty;
        public string RankColor { get; set; } = "#5F6368";
        public string RowBackground { get; set; } = "Transparent";
        public bool IsCurrentUser { get; set; }
    }

    public class AchievementItem
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Points { get; set; }
        public string Icon { get; set; } = string.Empty;
        public bool IsEarned { get; set; }
    }
}