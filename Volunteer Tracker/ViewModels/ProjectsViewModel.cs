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
        private readonly bool _isAdmin;

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
            _isAdmin = user.Role == "admin";
            CanCreateProject = user.Role == "student" || _isAdmin;
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

        private async Task LoadAvailableProjects()
        {
            var today = DateOnly.FromDateTime(DateTime.Now);

            var availableProjects = await _context.Projects
                .Where(p => p.Status == "open" && p.EndDate >= today)
                .OrderBy(p => p.EndDate)
                .ToListAsync();

            var leaderIds = availableProjects.Select(p => p.LeaderId).Distinct().ToList();
            var leaders = await _context.Users
                .Where(u => leaderIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u);

            var userRequests = await _context.ProjectAssignments
                .Where(pa => pa.UserId == _currentUser.Id)
                .ToDictionaryAsync(pa => pa.ProjectId, pa => pa.Status ?? "approved");

            var participantCounts = await _context.ProjectAssignments
                .Where(pa => pa.Status == "approved" || pa.Status == null)
                .GroupBy(pa => pa.ProjectId)
                .Select(g => new { ProjectId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ProjectId, x => x.Count);

            Dictionary<int, List<ParticipantItem>> allParticipants = new();
            if (_isAdmin)
            {
                var assignments = await _context.ProjectAssignments
                    .Include(pa => pa.User)
                    .Where(pa => availableProjects.Select(p => p.Id).Contains(pa.ProjectId))
                    .ToListAsync();

                foreach (var pa in assignments)
                {
                    if (!allParticipants.ContainsKey(pa.ProjectId))
                        allParticipants[pa.ProjectId] = new List<ParticipantItem>();

                    allParticipants[pa.ProjectId].Add(new ParticipantItem
                    {
                        AssignmentId = pa.Id,
                        UserId = pa.User.Id,
                        FullName = $"{pa.User.LastName} {pa.User.FirstName}",
                        Initials = GetInitials(pa.User),
                        PointsEarned = pa.PointsEarned ?? 0,
                        JoinedAt = pa.JoinedAt,
                        Status = pa.Status ?? "approved",
                        CanManageParticipants = _isAdmin
                    });
                }
            }

            Projects = availableProjects.Select(p =>
            {
                var leader = leaders.GetValueOrDefault(p.LeaderId);
                var requestStatus = userRequests.GetValueOrDefault(p.Id);
                var isLeader = p.LeaderId == _currentUser.Id;
                var participants = allParticipants.GetValueOrDefault(p.Id) ?? new List<ParticipantItem>();
                var canSeeParticipants = _isAdmin || requestStatus == "approved" || (requestStatus == null && userRequests.ContainsKey(p.Id));

                return new ProjectItem
                {
                    Id = p.Id,
                    Title = p.Title,
                    ShortDescription = p.ShortDescription ?? (p.Description?.Length > 100 ? p.Description.Substring(0, 100) + "..." : p.Description),
                    EndDate = p.EndDate.ToString("dd.MM.yyyy"),
                    MaxPoints = p.MaxPoints ?? 100,
                    Status = GetStatusText(p),
                    StatusColor = GetStatusColor(p),
                    CanSendRequest = !userRequests.ContainsKey(p.Id) && !isLeader,
                    RequestStatus = requestStatus,
                    IsLeader = isLeader,
                    CanCloseProject = _isAdmin,
                    ShowParticipants = canSeeParticipants,
                    ParticipantsCount = participantCounts.GetValueOrDefault(p.Id, 0),
                    Participants = participants,
                    LeaderId = p.LeaderId,
                    LeaderName = leader != null ? $"{leader.LastName} {leader.FirstName}" : "Неизвестный",
                    LeaderInitials = leader != null ? GetInitials(leader) : "??",
                    CanManageParticipants = _isAdmin || isLeader
                };
            }).ToList();
        }

        private async Task LoadMyProjects()
        {
            var allMyProjects = new List<ProjectItem>();
            var processedProjectIds = new HashSet<int>();

            var myAssignments = await _context.ProjectAssignments
                .Where(pa => pa.UserId == _currentUser.Id)
                .Include(pa => pa.Project)
                .ToListAsync();

            // Получаем все проекты, в которых пользователь участвует (включая созданные)
            var projectsInvolved = myAssignments.Select(pa => pa.Project).Where(p => p != null).ToList();
            // Собираем ID лидеров этих проектов
            var leaderIds = projectsInvolved.Select(p => p!.LeaderId).Distinct().ToList();
            // Загружаем пользователей-лидеров
            var leaders = await _context.Users
                .Where(u => leaderIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u);

            foreach (var pa in myAssignments)
            {
                var project = pa.Project;
                if (project == null) continue;

                var leader = leaders.GetValueOrDefault(project.LeaderId);
                var isLeader = project.LeaderId == _currentUser.Id;

                // Если лидер не найден, но это сам пользователь (создатель), то подставляем его данные
                if (leader == null && isLeader)
                {
                    leader = _currentUser;
                }

                var participants = await GetProjectParticipantsAsync(project.Id);
                foreach (var part in participants)
                {
                    part.CanManageParticipants = _isAdmin || isLeader;
                }

                allMyProjects.Add(new ProjectItem
                {
                    Id = project.Id,
                    Title = project.Title,
                    ShortDescription = project.ShortDescription ?? (project.Description?.Length > 100 ? project.Description.Substring(0, 100) + "..." : project.Description),
                    EndDate = project.EndDate.ToString("dd.MM.yyyy"),
                    MaxPoints = project.MaxPoints ?? 100,
                    PointsEarned = pa.PointsEarned ?? 0,
                    Status = GetStatusText(project),
                    StatusColor = GetStatusColor(project),
                    IsLeader = isLeader,
                    ShowProgress = pa.Status == "approved" || pa.Status == null,
                    ProgressPercent = project.MaxPoints > 0 ? (int)((pa.PointsEarned ?? 0) * 100 / project.MaxPoints) : 0,
                    ProgressText = $"Прогресс: {pa.PointsEarned ?? 0}/{project.MaxPoints ?? 100} баллов",
                    RequestStatus = pa.Status ?? "approved",
                    ShowParticipants = pa.Status == "approved" || pa.Status == null || isLeader || _isAdmin,
                    Participants = participants,
                    ParticipantsCount = participants.Count(p => p.IsApproved),
                    LeaderId = project.LeaderId,
                    LeaderName = leader != null ? $"{leader.LastName} {leader.FirstName}" : "Неизвестный",
                    LeaderInitials = leader != null ? GetInitials(leader) : "??",
                    CanCloseProject = _isAdmin,
                    CanManageParticipants = _isAdmin || isLeader
                });

                processedProjectIds.Add(project.Id);
            }

            if (_isAdmin)
            {
                var allProjects = await _context.Projects
                    .OrderBy(p => p.EndDate)
                    .ToListAsync();

                var allLeaderIds = allProjects.Select(p => p.LeaderId).Distinct().ToList();
                var allLeaders = await _context.Users
                    .Where(u => allLeaderIds.Contains(u.Id))
                    .ToDictionaryAsync(u => u.Id, u => u);

                foreach (var p in allProjects)
                {
                    if (processedProjectIds.Contains(p.Id)) continue;

                    var leader = allLeaders.GetValueOrDefault(p.LeaderId);
                    var participants = await GetProjectParticipantsAsync(p.Id);

                    allMyProjects.Add(new ProjectItem
                    {
                        Id = p.Id,
                        Title = p.Title,
                        ShortDescription = p.ShortDescription ?? (p.Description?.Length > 100 ? p.Description.Substring(0, 100) + "..." : p.Description),
                        EndDate = p.EndDate.ToString("dd.MM.yyyy"),
                        MaxPoints = p.MaxPoints ?? 100,
                        Status = GetStatusText(p),
                        StatusColor = GetStatusColor(p),
                        IsLeader = p.LeaderId == _currentUser.Id,
                        ShowParticipants = true,
                        Participants = participants,
                        ParticipantsCount = participants.Count(p => p.IsApproved),
                        CanCloseProject = _isAdmin,
                        CanManageParticipants = true,
                        LeaderId = p.LeaderId,
                        LeaderName = leader != null ? $"{leader.LastName} {leader.FirstName}" : "Неизвестный",
                        LeaderInitials = leader != null ? GetInitials(leader) : "??"
                    });
                }
            }

            Projects = allMyProjects.OrderBy(p => p.EndDate).ToList();
        }



        private async Task<List<ParticipantItem>> GetProjectParticipantsAsync(int projectId)
        {
            var assignments = await _context.ProjectAssignments
                .Where(pa => pa.ProjectId == projectId)
                .Include(pa => pa.User)
                .OrderByDescending(pa => pa.Status == "pending")
                .ThenBy(pa => pa.JoinedAt)
                .ToListAsync();

            return assignments.Select(pa => new ParticipantItem
            {
                AssignmentId = pa.Id,
                UserId = pa.User.Id,
                FullName = $"{pa.User.LastName} {pa.User.FirstName}",
                Initials = GetInitials(pa.User),
                PointsEarned = pa.PointsEarned ?? 0,
                JoinedAt = pa.JoinedAt,
                Status = pa.Status ?? "approved"
            }).ToList();
        }

        private string GetInitials(User user)
        {
            if (user.FirstName != null && user.LastName != null)
                return $"{user.FirstName[0]}{user.LastName[0]}".ToUpper();
            return "??";
        }

        private string GetStatusText(Project project)
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            if (project.Status == "closed") return "Закрыт";
            if (project.Status == "open" && project.EndDate >= today) return "Открыт";
            if (project.EndDate < today) return "Просрочен";
            return project.Status ?? "Черновик";
        }

        private string GetStatusColor(Project project)
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            if (project.Status == "closed") return "#5F6368";
            if (project.Status == "open" && project.EndDate >= today) return "#34A853";
            if (project.EndDate < today) return "#EA4335";
            return "#FB8C00";
        }

        #region Commands

        [RelayCommand]
        private async Task SendRequest(ProjectItem project)
        {
            try
            {
                // Проверяем, нет ли уже заявки или участия
                var existing = await _context.ProjectAssignments
                    .AnyAsync(pa => pa.ProjectId == project.Id && pa.UserId == _currentUser.Id);
                if (existing) return;

                var assignment = new ProjectAssignment
                {
                    ProjectId = project.Id,
                    UserId = _currentUser.Id,
                    JoinedAt = DateTime.Now,
                    Status = "pending"
                };
                _context.ProjectAssignments.Add(assignment);
                await _context.SaveChangesAsync();
                await LoadProjectsAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка отправки заявки: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task AcceptUser(ParticipantItem participant)
        {
            try
            {
                var assignment = await _context.ProjectAssignments.FindAsync(participant.AssignmentId);
                if (assignment != null)
                {
                    assignment.Status = "approved";
                    await _context.SaveChangesAsync();

                    // Обновляем список участников для конкретного проекта
                    var project = Projects.FirstOrDefault(p => p.Id == assignment.ProjectId);
                    if (project != null)
                    {
                        project.Participants = await GetProjectParticipantsAsync(project.Id);
                        project.ParticipantsCount = project.Participants.Count(p => p.IsApproved);
                        OnPropertyChanged(nameof(Projects));
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка принятия заявки: {ex.Message}");
            }
        }


        [RelayCommand]
        private async Task RejectUser(ParticipantItem participant)
        {
            try
            {
                var assignment = await _context.ProjectAssignments.FindAsync(participant.AssignmentId);
                if (assignment != null)
                {
                    _context.ProjectAssignments.Remove(assignment);
                    await _context.SaveChangesAsync();

                    // Обновляем список участников для конкретного проекта
                    var project = Projects.FirstOrDefault(p => p.Id == assignment.ProjectId);
                    if (project != null)
                    {
                        project.Participants = await GetProjectParticipantsAsync(project.Id);
                        project.ParticipantsCount = project.Participants.Count(p => p.IsApproved);
                        OnPropertyChanged(nameof(Projects));
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка отклонения заявки: {ex.Message}");
            }
        }


        [RelayCommand]
        private async Task KickUser(ParticipantItem participant)
        {
            try
            {
                var assignment = await _context.ProjectAssignments.FindAsync(participant.AssignmentId);
                if (assignment != null && participant.IsApproved) // не удаляем создателя
                {
                    _context.ProjectAssignments.Remove(assignment);
                    await _context.SaveChangesAsync();

                    var project = Projects.FirstOrDefault(p => p.Id == assignment.ProjectId);
                    if (project != null)
                    {
                        project.Participants = await GetProjectParticipantsAsync(project.Id);
                        project.ParticipantsCount = project.Participants.Count(p => p.IsApproved);
                        OnPropertyChanged(nameof(Projects));
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка удаления участника: {ex.Message}");
            }
        }


        [RelayCommand]
        private async Task CloseProject(ProjectItem project)
        {
            try
            {
                var dbProject = await _context.Projects.FindAsync(project.Id);
                if (dbProject != null && dbProject.Status != "closed")
                {
                    dbProject.Status = "closed";
                    dbProject.CompletedAt = DateTime.Now;

                    var assignments = await _context.ProjectAssignments
                        .Where(pa => pa.ProjectId == project.Id && (pa.Status == "approved" || pa.Status == null))
                        .ToListAsync();

                    foreach (var assignment in assignments)
                    {
                        assignment.PointsEarned = project.MaxPoints;

                        var userPoints = await _context.UserPoints
                            .FirstOrDefaultAsync(up => up.UserId == assignment.UserId);

                        if (userPoints == null)
                        {
                            userPoints = new UserPoint { UserId = assignment.UserId };
                            _context.UserPoints.Add(userPoints);
                        }

                        userPoints.TotalPoints = (userPoints.TotalPoints ?? 0) + project.MaxPoints;
                        userPoints.TotalProjectsCompleted = (userPoints.TotalProjectsCompleted ?? 0) + 1;

                        _context.ActivityLogs.Add(new ActivityLog
                        {
                            UserId = assignment.UserId,
                            ActivityType = "project_completed",
                            Title = $"Проект \"{project.Title}\" завершён",
                            PointsChange = project.MaxPoints,
                            CreatedAt = DateTime.Now
                        });
                    }

                    await _context.SaveChangesAsync();
                    await LoadProjectsAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка закрытия проекта: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task OpenLeaderProfile(int leaderId)
        {
            await NavigateToProfile(leaderId);
        }

        [RelayCommand]
        private async Task OpenUserProfile(int userId)
        {
            await NavigateToProfile(userId);
        }

        private async Task NavigateToProfile(int userId)
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

        [RelayCommand]
        private void ToggleParticipants(ProjectItem project)
        {
            // Закрываем все другие панели
            foreach (var p in Projects)
            {
                if (p != project && p.IsParticipantsExpanded)
                    p.IsParticipantsExpanded = false;
            }
            // Переключаем текущую
            project.IsParticipantsExpanded = !project.IsParticipantsExpanded;

            // Если открываем и участники ещё не загружены, загружаем
            if (project.IsParticipantsExpanded && project.Participants.Count == 0)
            {
                _ = LoadParticipantsForProject(project);
            }

            // OnPropertyChanged(nameof(Projects));  // можно убрать, так как ObservableProperty обновляет сам
        }

        private async Task LoadParticipantsForProject(ProjectItem project)
        {
            var participants = await GetProjectParticipantsAsync(project.Id);
            foreach (var p in participants)
            {
                p.CanManageParticipants = project.CanManageParticipants;
            }
            project.Participants = participants;
            project.ParticipantsCount = participants.Count(p => p.IsApproved);
            OnPropertyChanged(nameof(Projects));
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
                            Status = "open",
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now
                        };
                        _context.Projects.Add(newProject);
                        await _context.SaveChangesAsync();

                        var assignment = new ProjectAssignment
                        {
                            ProjectId = newProject.Id,
                            UserId = _currentUser.Id,
                            JoinedAt = DateTime.Now,
                            Status = "approved"
                        };
                        _context.ProjectAssignments.Add(assignment);
                        await _context.SaveChangesAsync();

                        await LoadProjectsAsync();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");
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
        [RelayCommand]
        private async Task SwitchToAvailable()
        {
            IsAvailableSelected = true;
            IsMyProjectsSelected = false;
            await LoadProjectsAsync();
        }

        [RelayCommand]
        private async Task SwitchToMyProjects()
        {
            IsAvailableSelected = false;
            IsMyProjectsSelected = true;
            await LoadProjectsAsync();
        }

        #endregion
    }

    public partial class ProjectItem : ObservableObject
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ShortDescription { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
        public int MaxPoints { get; set; }
        public int PointsEarned { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StatusColor { get; set; } = "#5F6368";

        public bool CanSendRequest { get; set; }
        public string? RequestStatus { get; set; }

        [ObservableProperty]
        private bool isParticipantsExpanded;

        public string RequestStatusText => RequestStatus switch
        {
            "pending" => "⏳ Заявка отправлена",
            "approved" => "✓ Участник",
            "rejected" => "✗ Заявка отклонена",
            _ => ""
        };
        public string RequestStatusColor => RequestStatus switch
        {
            "pending" => "#FB8C00",
            "approved" => "#34A853",
            "rejected" => "#EA4335",
            _ => "#5F6368"
        };
        public bool IsPending => RequestStatus == "pending";
        public bool IsRejected => RequestStatus == "rejected";

        public bool IsLeader { get; set; }
        public bool ShowProgress { get; set; }
        public int ProgressPercent { get; set; }
        public string ProgressText { get; set; } = string.Empty;
        public bool ShowParticipants { get; set; }
        public bool CanManageParticipants { get; set; }
        public bool CanCloseProject { get; set; }
        public int ParticipantsCount { get; set; }
        public List<ParticipantItem> Participants { get; set; } = new();

        public int LeaderId { get; set; }
        public string LeaderName { get; set; } = string.Empty;
        public string LeaderInitials { get; set; } = string.Empty;
    }

    public class ParticipantItem
    {
        public int AssignmentId { get; set; }
        public int UserId { get; set; }

        public bool CanManageParticipants { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Initials { get; set; } = string.Empty;
        public int PointsEarned { get; set; }
        public DateTime? JoinedAt { get; set; }
        public string? Status { get; set; }

        public string StatusText => Status switch
        {
            "pending" => "Ожидает",
            "approved" => "Участник",
            "rejected" => "Отклонена",
            _ => "Участник"
        };
        public string StatusColor => Status switch
        {
            "pending" => "#FB8C00",
            "approved" => "#34A853",
            "rejected" => "#EA4335",
            _ => "#34A853"
        };
        public bool IsPending => Status == "pending";
        public bool IsApproved => Status == "approved" || Status == null;
        public bool IsRejected => Status == "rejected";
        public string JoinedDate => JoinedAt?.ToString("dd.MM.yyyy") ?? "";
    }
}