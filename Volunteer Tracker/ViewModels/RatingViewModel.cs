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
        private int _currentUserRank;

        [ObservableProperty]
        private string _currentUserInitials = string.Empty;

        [ObservableProperty]
        private string _currentUserFullName = string.Empty;

        [ObservableProperty]
        private int _currentUserPoints;

        [ObservableProperty]
        private string _rankChange = string.Empty;

        [ObservableProperty]
        private string _rankChangeColor = string.Empty;

        [ObservableProperty]
        private double _averagePoints;

        [ObservableProperty]
        private int _totalStudents;

        [ObservableProperty]
        private int _maxPoints;

        [ObservableProperty]
        private int _pointsToNextRank;

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
                // Получаем всех студентов с их баллами (без использования ? в LINQ)
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

                // Формируем LeaderboardItem уже после загрузки данных (на клиенте)
                var studentsWithPoints = rawData.Select((item, index) => new LeaderboardItem
                {
                    UserId = item.Id,
                    FullName = $"{item.LastName} {item.FirstName}",
                    Initials = GetInitials(item.FirstName, item.LastName),
                    GroupName = item.GroupName ?? "—",
                    Points = item.Points,
                    Rank = index + 1
                }).ToList();

                // Добавляем медали и цвета
                foreach (var item in studentsWithPoints)
                {
                    if (item.Rank == 1) item.Medal = "🥇";
                    else if (item.Rank == 2) item.Medal = "🥈";
                    else if (item.Rank == 3) item.Medal = "🥉";
                    else item.Medal = "";

                    if (item.Rank == 1) item.RankColor = "#FFD700";
                    else if (item.Rank == 2) item.RankColor = "#C0C0C0";
                    else if (item.Rank == 3) item.RankColor = "#CD7F32";
                    else item.RankColor = "#5F6368";

                    if (item.UserId == _currentUser.Id)
                    {
                        item.BackgroundColor = "#E8F0FE";
                        CurrentUserRank = item.Rank;
                        CurrentUserFullName = item.FullName;
                        CurrentUserInitials = item.Initials;
                        CurrentUserPoints = item.Points;
                    }
                }

                Leaderboard = studentsWithPoints.Take(20).ToList();
                TotalStudents = studentsWithPoints.Count;
                MaxPoints = studentsWithPoints.FirstOrDefault()?.Points ?? 0;
                AveragePoints = studentsWithPoints.Any() ? studentsWithPoints.Average(x => x.Points) : 0;

                // Баллы до следующего места
                var currentUserItem = studentsWithPoints.FirstOrDefault(x => x.UserId == _currentUser.Id);
                if (currentUserItem != null && currentUserItem.Rank > 1)
                {
                    var nextRankItem = studentsWithPoints.FirstOrDefault(x => x.Rank == currentUserItem.Rank - 1);
                    if (nextRankItem != null)
                    {
                        PointsToNextRank = nextRankItem.Points - currentUserItem.Points;
                    }
                }

                // Изменение места (для демо)
                RankChange = "▲ +2 места за месяц";
                RankChangeColor = "#34A853";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки рейтинга: {ex.Message}");
            }
        }

        private string GetInitials(string firstName, string lastName)
        {
            if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName))
                return "??";

            char firstInitial = firstName.Length > 0 ? firstName[0] : '?';
            char lastInitial = lastName.Length > 0 ? lastName[0] : '?';

            return $"{firstInitial}{lastInitial}".ToUpper();
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
        public string Medal { get; set; } = string.Empty;
        public string RankColor { get; set; } = "#5F6368";
        public string BackgroundColor { get; set; } = "Transparent";
    }
}