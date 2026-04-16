using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Volunteer_Tracker.Models;
using Volunteer_Tracker.Views;

namespace Volunteer_Tracker.ViewModels
{
    public partial class ProjectsViewModel : ObservableObject
    {
        private readonly PostgresContext _context;
        private readonly User _currentUser;

        [ObservableProperty]
        private List<ProjectItem> _projects = new();

        [ObservableProperty]
        private bool _isAvailableSelected = true;

        [ObservableProperty]
        private bool _isMyProjectsSelected;

        [ObservableProperty]
        private bool _canCreateProject;

        public ProjectsViewModel(User user)
        {
            _currentUser = user;
            _context = new PostgresContext();
            CanCreateProject = true;
            _ = LoadProjectsAsync();
        }

        public async Task LoadProjectsAsync()
        {
            try
            {
                if (IsAvailableSelected)
                {
                    await LoadAvailableProjects();
                }
                else
                {
                    await LoadMyProjects();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки проектов: {ex.Message}");
                Projects = new List<ProjectItem>();
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

        private async Task LoadAvailableProjects()
        {
            var today = DateOnly.FromDateTime(DateTime.Now);

            var availableProjects = await _context.Projects
                .Where(p => p.Status == "open" || p.Status == "pending")
                .ToListAsync();

            availableProjects = availableProjects
                .Where(p => p.EndDate >= today)
                .OrderBy(p => p.EndDate)
                .ToList();

            var userAssignments = await _context.ProjectAssignments
                .Where(pa => pa.UserId == _currentUser.Id)
                .Select(pa => pa.ProjectId)
                .ToHashSetAsync();

            var projectItems = new List<ProjectItem>();

            foreach (var p in availableProjects)
            {
                var item = new ProjectItem
                {
                    Id = p.Id,
                    Title = p.Title,
                    ShortDescription = p.ShortDescription ?? (p.Description?.Length > 100 ? p.Description.Substring(0, 100) + "..." : p.Description),
                    EndDate = p.EndDate.ToString("dd.MM.yyyy"),
                    MaxPoints = p.MaxPoints ?? 100,
                    Status = GetStatusText(p),
                    StatusColor = GetStatusColor(p),
                    CanJoin = !userAssignments.Contains(p.Id) && p.Status == "open",
                    CanApprove = _currentUser.Role == "admin" && p.Status == "pending",
                    ShowProgress = false
                };

                // Проверяем, может ли пользователь видеть участников
                if (_currentUser.Role == "admin" || p.LeaderId == _currentUser.Id)
                {
                    item.ShowParticipants = true;

                    // Загружаем участников
                    var participants = await _context.ProjectAssignments
                        .Where(pa => pa.ProjectId == p.Id)
                        .Include(pa => pa.User)
                        .ToListAsync();

                    item.Participants = participants.Select(pa => new ParticipantItem
                    {
                        UserId = pa.User.Id,
                        FullName = $"{pa.User.LastName} {pa.User.FirstName}",
                        Initials = $"{pa.User.FirstName[0]}{pa.User.LastName[0]}".ToUpper(),
                        Points = pa.PointsEarned ?? 0
                    }).ToList();
                }

                projectItems.Add(item);
            }

            Projects = projectItems;
        }

        private async Task LoadMyProjects()
        {
            // Проекты, где пользователь участник
            var myAssignments = await _context.ProjectAssignments
                .Where(pa => pa.UserId == _currentUser.Id)
                .Include(pa => pa.Project)
                .ToListAsync();

            // Проекты, где пользователь создатель
            var myCreatedProjects = await _context.Projects
                .Where(p => p.LeaderId == _currentUser.Id)
                .ToListAsync();

            var allMyProjects = new List<ProjectItem>();

            // Добавляем проекты-участники
            foreach (var pa in myAssignments)
            {
                var item = new ProjectItem
                {
                    Id = pa.Project.Id,
                    Title = pa.Project.Title,
                    ShortDescription = pa.Project.ShortDescription ?? (pa.Project.Description?.Length > 100 ? pa.Project.Description.Substring(0, 100) + "..." : pa.Project.Description),
                    EndDate = pa.Project.EndDate.ToString("dd.MM.yyyy"),
                    MaxPoints = pa.Project.MaxPoints ?? 100,
                    PointsEarned = pa.PointsEarned ?? 0,
                    Status = GetStatusText(pa.Project),
                    StatusColor = GetStatusColor(pa.Project),
                    CanJoin = false,
                    CanApprove = false,
                    ShowProgress = true,
                    ProgressPercent = pa.Project.MaxPoints > 0 ? (int)((pa.PointsEarned ?? 0) * 100 / pa.Project.MaxPoints) : 0,
                    ProgressText = $"Прогресс: {pa.PointsEarned ?? 0}/{pa.Project.MaxPoints ?? 100} баллов"
                };

                // Создатель и админ видят участников
                if (_currentUser.Role == "admin" || pa.Project.LeaderId == _currentUser.Id)
                {
                    item.ShowParticipants = true;

                    var participants = await _context.ProjectAssignments
                        .Where(pa2 => pa2.ProjectId == pa.Project.Id)
                        .Include(pa2 => pa2.User)
                        .ToListAsync();

                    item.Participants = participants.Select(pa2 => new ParticipantItem
                    {
                        UserId = pa2.User.Id,
                        FullName = $"{pa2.User.LastName} {pa2.User.FirstName}",
                        Initials = $"{pa2.User.FirstName[0]}{pa2.User.LastName[0]}".ToUpper(),
                        Points = pa2.PointsEarned ?? 0
                    }).ToList();
                }

                allMyProjects.Add(item);
            }

            // Добавляем проекты-создатели
            foreach (var p in myCreatedProjects)
            {
                var item = new ProjectItem
                {
                    Id = p.Id,
                    Title = p.Title,
                    ShortDescription = p.ShortDescription ?? (p.Description?.Length > 100 ? p.Description.Substring(0, 100) + "..." : p.Description),
                    EndDate = p.EndDate.ToString("dd.MM.yyyy"),
                    MaxPoints = p.MaxPoints ?? 100,
                    PointsEarned = 0,
                    Status = GetStatusText(p),
                    StatusColor = GetStatusColor(p),
                    CanJoin = false,
                    CanApprove = _currentUser.Role == "admin" && p.Status == "pending",
                    ShowProgress = false,
                    ProgressPercent = 0,
                    ProgressText = ""
                };

                // Создатель и админ видят участников
                if (_currentUser.Role == "admin" || p.LeaderId == _currentUser.Id)
                {
                    item.ShowParticipants = true;

                    var participants = await _context.ProjectAssignments
                        .Where(pa => pa.ProjectId == p.Id)
                        .Include(pa => pa.User)
                        .ToListAsync();

                    item.Participants = participants.Select(pa => new ParticipantItem
                    {
                        UserId = pa.User.Id,
                        FullName = $"{pa.User.LastName} {pa.User.FirstName}",
                        Initials = $"{pa.User.FirstName[0]}{pa.User.LastName[0]}".ToUpper(),
                        Points = pa.PointsEarned ?? 0
                    }).ToList();
                }

                allMyProjects.Add(item);
            }

            Projects = allMyProjects.OrderBy(p => p.EndDate).ToList();
        }

        private string GetStatusText(Project project)
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            if (project.Status == "pending") return "Ожидает";
            if (project.Status == "open" && project.EndDate >= today) return "Открыт";
            if (project.Status == "in_progress") return "В процессе";
            if (project.Status == "completed") return "Завершён";
            if (project.EndDate < today) return "Закрыт";
            return project.Status ?? "Черновик";
        }

        private string GetStatusColor(Project project)
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            if (project.Status == "pending") return "#FB8C00";
            if (project.Status == "open" && project.EndDate >= today) return "#34A853";
            if (project.Status == "in_progress") return "#1A73E8";
            if (project.Status == "completed") return "#5F6368";
            if (project.EndDate < today) return "#EA4335";
            return "#FB8C00";
        }

        [RelayCommand]
        private async Task JoinProject(ProjectItem project)
        {
            try
            {
                var assignment = new ProjectAssignment
                {
                    ProjectId = project.Id,
                    UserId = _currentUser.Id,
                    Status = "approved",
                    JoinedAt = DateTime.Now
                };
                _context.ProjectAssignments.Add(assignment);
                await _context.SaveChangesAsync();

                var activity = new ActivityLog
                {
                    UserId = _currentUser.Id,
                    ActivityType = "project_joined",
                    Title = $"Присоединился к проекту \"{project.Title}\"",
                    CreatedAt = DateTime.Now
                };
                _context.ActivityLogs.Add(activity);
                await _context.SaveChangesAsync();

                await LoadProjectsAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка присоединения: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task ApproveProject(ProjectItem project)
        {
            try
            {
                var dbProject = await _context.Projects.FindAsync(project.Id);
                if (dbProject != null)
                {
                    dbProject.Status = "open";
                    dbProject.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                    await LoadProjectsAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка подтверждения проекта: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task CreateProject()
        {
            var dialogViewModel = new CreateProjectDialogViewModel();
            var dialog = new CreateProjectDialog();
            dialog.DataContext = dialogViewModel;

            Window? owner = null;
            if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                owner = desktop.MainWindow;
            }

            dialogViewModel.ProjectCreated += async (sender, project) =>
            {
                if (!string.IsNullOrEmpty(project.Title))
                {
                    try
                    {
                        var newProject = new Project
                        {
                            Title = project.Title,
                            ShortDescription = project.ShortDescription,
                            Description = project.Description,
                            LeaderId = _currentUser.Id,
                            StartDate = DateOnly.FromDateTime(project.StartDate),
                            EndDate = DateOnly.FromDateTime(project.EndDate),
                            MaxPoints = project.MaxPoints,
                            Category = project.Category,
                            Status = "pending",
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now
                        };
                        _context.Projects.Add(newProject);
                        await _context.SaveChangesAsync();
                        await LoadProjectsAsync();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Ошибка создания проекта: {ex.Message}");
                    }
                }
                dialog.Close();
            };

            if (owner != null)
            {
                await dialog.ShowDialog(owner);
            }
            else
            {
                dialog.Show();
            }
        }

        public async Task SwitchToAvailable()
        {
            IsAvailableSelected = true;
            IsMyProjectsSelected = false;
            await LoadProjectsAsync();
        }

        public async Task SwitchToMyProjects()
        {
            IsAvailableSelected = false;
            IsMyProjectsSelected = true;
            await LoadProjectsAsync();
        }
    }

    public class ProjectItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ShortDescription { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
        public int MaxPoints { get; set; }
        public int PointsEarned { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StatusColor { get; set; } = "#5F6368";
        public bool CanJoin { get; set; }
        public bool CanApprove { get; set; }
        public bool ShowProgress { get; set; }
        public int ProgressPercent { get; set; }
        public string ProgressText { get; set; } = string.Empty;
        public bool ShowParticipants { get; set; }
        public List<ParticipantItem> Participants { get; set; } = new();
    }

    public class ParticipantItem
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Initials { get; set; } = string.Empty;
        public int Points { get; set; }
    }
}