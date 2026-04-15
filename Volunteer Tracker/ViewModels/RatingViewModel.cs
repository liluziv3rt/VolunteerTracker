using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Volunteer_Tracker.Models;

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

        public RatingViewModel(User user)
        {
            _currentUser = user;
            _context = new PostgresContext();
            _ = LoadDataAsync();
        }

        public async Task LoadDataAsync()
        {
            await LoadLeaderboardAsync();
            await LoadAchievementsAsync();
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
                        Points = a.ThresholdValue,
                        Icon = a.BadgeIcon ?? "🏅",
                        IsEarned = true
                    }).ToList();

                var locked = allAchievements
                    .Where(a => !userAchievements.Contains(a.Id))
                    .Select(a => new AchievementItem
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
        public int Points { get; set; }
        public string Icon { get; set; } = string.Empty;
        public bool IsEarned { get; set; }
    }
}