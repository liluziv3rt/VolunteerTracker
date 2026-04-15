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

        public RatingViewModel(User user)
        {
            _currentUser = user;
            _context = new PostgresContext();
            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                // Получаем всех студентов с баллами
                var query = from u in _context.Users
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
                            };

                var rawData = await query.ToListAsync();

                // Получаем количество значков
                var badgesCount = await _context.UserAchievements
                    .GroupBy(ua => ua.UserId)
                    .Select(g => new { UserId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.UserId, x => x.Count);

                // Формируем список лидеров
                var studentsWithData = new List<LeaderboardItem>();
                int rank = 1;
                foreach (var item in rawData)
                {
                    var leaderItem = new LeaderboardItem
                    {
                        UserId = item.Id,
                        FullName = $"{item.LastName} {item.FirstName}",
                        GroupName = item.GroupName ?? "Факультет",
                        Points = item.Points,
                        BadgesCount = badgesCount.GetValueOrDefault(item.Id, 0),
                        Rank = rank,
                        IsCurrentUser = item.Id == _currentUser.Id
                    };

                    var names = leaderItem.FullName.Split(' ');
                    leaderItem.Initials = names.Length >= 2
                        ? $"{names[0][0]}{names[1][0]}".ToUpper()
                        : names[0].Substring(0, Math.Min(2, names[0].Length)).ToUpper();

                    if (rank == 1)
                    {
                        leaderItem.Medal = "🥇";
                        leaderItem.RankColor = "#FFD700";
                        leaderItem.RowBackground = "#FFF9E6";
                    }
                    else if (rank == 2)
                    {
                        leaderItem.Medal = "🥈";
                        leaderItem.RankColor = "#C0C0C0";
                        leaderItem.RowBackground = "#F0F7FF";
                    }
                    else if (rank == 3)
                    {
                        leaderItem.Medal = "🥉";
                        leaderItem.RankColor = "#CD7F32";
                        leaderItem.RowBackground = "#FFF4EB";
                    }
                    else
                    {
                        leaderItem.Medal = "";
                        leaderItem.RankColor = "#5F6368";
                        leaderItem.RowBackground = "Transparent";
                    }

                    if (leaderItem.IsCurrentUser)
                    {
                        leaderItem.RowBackground = "#E8F0FE";
                    }

                    studentsWithData.Add(leaderItem);
                    rank++;
                }

                Leaderboard = studentsWithData.Take(20).ToList();

                // Загружаем достижения (сортировка по порогу)
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
                System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");
                SetDefaultData();
            }
        }

        private void SetDefaultData()
        {
            Leaderboard = new List<LeaderboardItem>
            {
                new() { Rank = 1, FullName = "Иванов Иван", GroupName = "Факультет информатики", Points = 2450, BadgesCount = 5, Medal = "🥇", RankColor = "#FFD700", RowBackground = "#FFF9E6", Initials = "ИИ" },
                new() { Rank = 2, FullName = "Петрова Анна", GroupName = "Факультет экономики", Points = 2120, BadgesCount = 4, IsCurrentUser = true, Medal = "🥈", RankColor = "#C0C0C0", RowBackground = "#E8F0FE", Initials = "ПА" },
                new() { Rank = 3, FullName = "Сидоров Алексей", GroupName = "Факультет медицины", Points = 1980, BadgesCount = 4, Medal = "🥉", RankColor = "#CD7F32", RowBackground = "#FFF4EB", Initials = "СА" },
                new() { Rank = 4, FullName = "Кузнецова Мария", GroupName = "Факультет права", Points = 1845, BadgesCount = 3, Initials = "КМ" },
                new() { Rank = 5, FullName = "Смирнов Дмитрий", GroupName = "Факультет инженерии", Points = 1720, BadgesCount = 3, Initials = "СД" }
            };

            EarnedAchievements = new List<AchievementItem>
            {
                new() { Name = "Первые шаги", Points = 50, Icon = "★", IsEarned = true },
                new() { Name = "Активист", Points = 200, Icon = "⚡", IsEarned = true }
            };

            LockedAchievements = new List<AchievementItem>
            {
                new() { Name = "Лидер", Points = 500, Icon = "👑", IsEarned = false },
                new() { Name = "Волонтёр года", Points = 1000, Icon = "🏆", IsEarned = false },
                new() { Name = "Труженик", Points = 100, Icon = "⏱️", IsEarned = false },
                new() { Name = "Мастер проектов", Points = 3, Icon = "📁", IsEarned = false },
                new() { Name = "Энтузиаст", Points = 5, Icon = "🚀", IsEarned = false }
            };

            AchievementsCount = "2/7";
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