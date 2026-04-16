using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Volunteer_Tracker.Models;

namespace Volunteer_Tracker.ViewModels
{
    public partial class AdminPanelViewModel : ObservableObject
    {
        private readonly PostgresContext _context;

        [ObservableProperty]
        private ObservableCollection<UserItem> _users = new();

        [ObservableProperty]
        private ObservableCollection<ProjectItemForAdmin> _projects = new();

        [ObservableProperty]
        private bool _isUsersTabSelected = true;

        [ObservableProperty]
        private bool _isProjectsTabSelected = false;

        public ObservableCollection<string> Roles { get; } = new()
        {
            "student", "project_leader", "admin"
        };

        public ObservableCollection<string> ProjectStatuses { get; } = new()
        {
            "open", "closed", "completed"
        };

        public AdminPanelViewModel()
        {
            _context = new PostgresContext();
            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                // Загружаем пользователей
                var dbUsers = await _context.Users
                    .OrderBy(u => u.Id)
                    .ToListAsync();

                // Загружаем баллы пользователей
                var userPoints = await _context.UserPoints
                    .ToDictionaryAsync(x => x.UserId);

                Users = new ObservableCollection<UserItem>(dbUsers.Select(u =>
                {
                    var points = userPoints.GetValueOrDefault(u.Id);
                    return new UserItem
                    {
                        Id = u.Id,
                        FullName = $"{u.LastName} {u.FirstName}",
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        Email = u.Email,
                        GroupName = u.GroupName ?? "",
                        Role = u.Role,
                        IsActive = u.IsActive ?? true,
                        Initials = u.FirstName != null && u.LastName != null
                            ? $"{u.FirstName[0]}{u.LastName[0]}".ToUpper()
                            : "??",
                        TotalPoints = points?.TotalPoints ?? 0,
                        CompletedProjects = points?.TotalProjectsCompleted ?? 0
                    };
                }));

                // Загружаем проекты
                var dbProjects = await _context.Projects
                    .OrderBy(p => p.Id)
                    .ToListAsync();

                Projects = new ObservableCollection<ProjectItemForAdmin>(dbProjects.Select(p => new ProjectItemForAdmin
                {
                    Id = p.Id,
                    Title = p.Title,
                    Category = p.Category ?? "",
                    Status = p.Status ?? "open",
                    MaxPoints = p.MaxPoints ?? 100,
                    EndDate = p.EndDate.ToDateTime(TimeOnly.MinValue)
                }));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки данных: {ex.Message}");
            }
        }

        [RelayCommand]
        private void ShowUsers()
        {
            IsUsersTabSelected = true;
            IsProjectsTabSelected = false;
        }

        [RelayCommand]
        private void ShowProjects()
        {
            IsUsersTabSelected = false;
            IsProjectsTabSelected = true;
        }

        [RelayCommand]
        private async Task SaveUsers()
        {
            try
            {
                foreach (var userItem in Users)
                {
                    var dbUser = await _context.Users.FindAsync(userItem.Id);
                    if (dbUser != null)
                    {
                        dbUser.Role = userItem.Role;
                        dbUser.GroupName = string.IsNullOrWhiteSpace(userItem.GroupName) ? null : userItem.GroupName;
                        dbUser.IsActive = userItem.IsActive;
                    }

                    // Сохраняем баллы
                    var userPoints = await _context.UserPoints
                        .FirstOrDefaultAsync(x => x.UserId == userItem.Id);

                    if (userPoints == null)
                    {
                        userPoints = new UserPoint { UserId = userItem.Id };
                        _context.UserPoints.Add(userPoints);
                    }

                    userPoints.TotalPoints = userItem.TotalPoints;
                    userPoints.TotalProjectsCompleted = userItem.CompletedProjects;
                    userPoints.UpdatedAt = DateTime.Now;
                }
                await _context.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine("Пользователи сохранены");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка сохранения: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task SaveProjects()
        {
            try
            {
                foreach (var projectItem in Projects)
                {
                    var dbProject = await _context.Projects.FindAsync(projectItem.Id);
                    if (dbProject != null)
                    {
                        dbProject.Title = projectItem.Title;
                        dbProject.Category = projectItem.Category;
                        dbProject.Status = projectItem.Status;
                        dbProject.MaxPoints = projectItem.MaxPoints;
                        dbProject.EndDate = DateOnly.FromDateTime(projectItem.EndDate);
                        dbProject.UpdatedAt = DateTime.Now;
                    }
                }
                await _context.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine("Проекты сохранены");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка сохранения: {ex.Message}");
            }
        }
    }

    public partial class UserItem : ObservableObject
    {
        public int Id { get; set; }

        [ObservableProperty] private string fullName = string.Empty;
        [ObservableProperty] private string firstName = string.Empty;
        [ObservableProperty] private string lastName = string.Empty;
        [ObservableProperty] private string email = string.Empty;
        [ObservableProperty] private string groupName = string.Empty;
        [ObservableProperty] private string role = "student";
        [ObservableProperty] private bool isActive = true;
        [ObservableProperty] private string initials = string.Empty;
        [ObservableProperty] private int totalPoints;
        [ObservableProperty] private int completedProjects;
    }

    public partial class ProjectItemForAdmin : ObservableObject
    {
        public int Id { get; set; }

        [ObservableProperty] private string title = string.Empty;
        [ObservableProperty] private string category = string.Empty;
        [ObservableProperty] private string status = "open";
        [ObservableProperty] private int maxPoints = 100;
        [ObservableProperty] private DateTime endDate;
    }
}