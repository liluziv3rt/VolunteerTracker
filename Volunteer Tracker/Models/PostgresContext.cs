using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Volunteer_Tracker.Models;

public partial class PostgresContext : DbContext
{
    public PostgresContext()
    {
    }

    public PostgresContext(DbContextOptions<PostgresContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Achievement> Achievements { get; set; }

    public virtual DbSet<ActivityLog> ActivityLogs { get; set; }

    public virtual DbSet<IntegrationSetting> IntegrationSettings { get; set; }

    public virtual DbSet<Project> Projects { get; set; }

    public virtual DbSet<ProjectAssignment> ProjectAssignments { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<Rental> Rentals { get; set; }

    public virtual DbSet<Resource> Resources { get; set; }

    public virtual DbSet<StudentPortfolio> StudentPortfolios { get; set; }

    public virtual DbSet<StudentRanking> StudentRankings { get; set; }

    public virtual DbSet<SyncLog> SyncLogs { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserAchievement> UserAchievements { get; set; }

    public virtual DbSet<UserPoint> UserPoints { get; set; }

    public virtual DbSet<VolunteerHour> VolunteerHours { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=123");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Achievement>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("achievements_pkey");

            entity.ToTable("achievements", tb => tb.HasComment("Достижения для геймификации"));

            entity.HasIndex(e => new { e.TriggerType, e.ThresholdValue }, "idx_achievements_trigger");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BadgeColor)
                .HasMaxLength(20)
                .HasColumnName("badge_color");
            entity.Property(e => e.BadgeIcon)
                .HasMaxLength(255)
                .HasColumnName("badge_icon");
            entity.Property(e => e.BonusPoints)
                .HasDefaultValue(0)
                .HasColumnName("bonus_points");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.Rarity)
                .HasMaxLength(50)
                .HasDefaultValueSql("'common'::character varying")
                .HasColumnName("rarity");
            entity.Property(e => e.ThresholdValue).HasColumnName("threshold_value");
            entity.Property(e => e.TriggerType)
                .HasMaxLength(50)
                .HasColumnName("trigger_type");
        });

        modelBuilder.Entity<ActivityLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("activity_log_pkey");

            entity.ToTable("activity_log", tb => tb.HasComment("Журнал активности для зачётной книжки"));

            entity.HasIndex(e => e.CreatedAt, "idx_activity_log_date");

            entity.HasIndex(e => e.ActivityType, "idx_activity_log_type");

            entity.HasIndex(e => e.UserId, "idx_activity_log_user");

            entity.HasIndex(e => new { e.UserId, e.CreatedAt }, "idx_activity_log_user_date").IsDescending(false, true);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ActivityType)
                .HasMaxLength(50)
                .HasColumnName("activity_type");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.HoursChange)
                .HasPrecision(5, 2)
                .HasColumnName("hours_change");
            entity.Property(e => e.PointsChange)
                .HasDefaultValue(0)
                .HasColumnName("points_change");
            entity.Property(e => e.ReferenceId).HasColumnName("reference_id");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.ActivityLogs)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("activity_log_user_id_fkey");
        });

        modelBuilder.Entity<IntegrationSetting>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("integration_settings_pkey");

            entity.ToTable("integration_settings", tb => tb.HasComment("Настройки интеграций с внешними системами"));

            entity.HasIndex(e => e.IntegrationName, "idx_integration_name");

            entity.HasIndex(e => e.IntegrationName, "integration_settings_integration_name_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.IntegrationName)
                .HasMaxLength(100)
                .HasColumnName("integration_name");
            entity.Property(e => e.IsEnabled)
                .HasDefaultValue(false)
                .HasColumnName("is_enabled");
            entity.Property(e => e.LastSyncAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("last_sync_at");
            entity.Property(e => e.LastSyncError).HasColumnName("last_sync_error");
            entity.Property(e => e.LastSyncStatus)
                .HasMaxLength(50)
                .HasColumnName("last_sync_status");
            entity.Property(e => e.Settings)
                .HasColumnType("jsonb")
                .HasColumnName("settings");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("projects_pkey");

            entity.ToTable("projects", tb => tb.HasComment("Проекты, в которых участвуют студенты"));

            entity.HasIndex(e => e.Category, "idx_projects_category");

            entity.HasIndex(e => new { e.StartDate, e.EndDate }, "idx_projects_dates");

            entity.HasIndex(e => e.LeaderId, "idx_projects_leader");

            entity.HasIndex(e => e.Status, "idx_projects_status");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Category)
                .HasMaxLength(100)
                .HasColumnName("category");
            entity.Property(e => e.CompletedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("completed_at");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.LeaderId).HasColumnName("leader_id");
            entity.Property(e => e.MaxPoints)
                .HasDefaultValue(100)
                .HasColumnName("max_points");
            entity.Property(e => e.PointsPerHour)
                .HasDefaultValue(10)
                .HasColumnName("points_per_hour");
            entity.Property(e => e.RegistrationDeadline).HasColumnName("registration_deadline");
            entity.Property(e => e.ShortDescription)
                .HasMaxLength(500)
                .HasColumnName("short_description");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValueSql("'draft'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Leader).WithMany(p => p.Projects)
                .HasForeignKey(d => d.LeaderId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("projects_leader_id_fkey");
        });

        modelBuilder.Entity<ProjectAssignment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("project_assignments_pkey");

            entity.ToTable("project_assignments", tb => tb.HasComment("Назначения студентов на проекты"));

            entity.HasIndex(e => e.ProjectId, "idx_project_assignments_project");

            entity.HasIndex(e => e.Status, "idx_project_assignments_status");

            entity.HasIndex(e => e.UserId, "idx_project_assignments_user");

            entity.HasIndex(e => new { e.ProjectId, e.UserId }, "project_assignments_project_id_user_id_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CompletedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("completed_at");
            entity.Property(e => e.JoinedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("joined_at");
            entity.Property(e => e.PointsEarned)
                .HasDefaultValue(0)
                .HasColumnName("points_earned");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.RequestStatus)
                .HasMaxLength(50)
                .HasDefaultValueSql("'pending'::character varying")
                .HasColumnName("request_status");
            entity.Property(e => e.RoleInProject)
                .HasMaxLength(100)
                .HasColumnName("role_in_project");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValueSql("'pending'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Project).WithMany(p => p.ProjectAssignments)
                .HasForeignKey(d => d.ProjectId)
                .HasConstraintName("project_assignments_project_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.ProjectAssignments)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("project_assignments_user_id_fkey");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("refresh_tokens_pkey");

            entity.ToTable("refresh_tokens", tb => tb.HasComment("Refresh токены для JWT аутентификации"));

            entity.HasIndex(e => e.Token, "idx_refresh_tokens_token");

            entity.HasIndex(e => e.UserId, "idx_refresh_tokens_user");

            entity.HasIndex(e => e.Token, "refresh_tokens_token_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpiresAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("expires_at");
            entity.Property(e => e.Revoked)
                .HasDefaultValue(false)
                .HasColumnName("revoked");
            entity.Property(e => e.Token)
                .HasMaxLength(512)
                .HasColumnName("token");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.RefreshTokens)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("refresh_tokens_user_id_fkey");
        });

        modelBuilder.Entity<Rental>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("rentals_pkey");

            entity.ToTable("rentals", tb => tb.HasComment("Аренда ресурсов студентами"));

            entity.HasIndex(e => new { e.UserId, e.ResourceId, e.Status }, "idx_rentals_active").HasFilter("((status)::text = 'active'::text)");

            entity.HasIndex(e => e.ResourceId, "idx_rentals_resource");

            entity.HasIndex(e => e.Status, "idx_rentals_status");

            entity.HasIndex(e => e.UserId, "idx_rentals_user");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.DurationMinutes).HasColumnName("duration_minutes");
            entity.Property(e => e.EndTime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("end_time");
            entity.Property(e => e.ResourceId).HasColumnName("resource_id");
            entity.Property(e => e.StartTime)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("start_time");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValueSql("'active'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Resource).WithMany(p => p.Rentals)
                .HasForeignKey(d => d.ResourceId)
                .HasConstraintName("rentals_resource_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.Rentals)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("rentals_user_id_fkey");
        });

        modelBuilder.Entity<Resource>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("resources_pkey");

            entity.ToTable("resources", tb => tb.HasComment("Ресурсы для аренды (рабочие станции, оборудование)"));

            entity.HasIndex(e => e.IsAvailable, "idx_resources_available");

            entity.HasIndex(e => e.Type, "idx_resources_type");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsAvailable)
                .HasDefaultValue(true)
                .HasColumnName("is_available");
            entity.Property(e => e.Location)
                .HasMaxLength(255)
                .HasColumnName("location");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.SerialNumber)
                .HasMaxLength(100)
                .HasColumnName("serial_number");
            entity.Property(e => e.Type)
                .HasMaxLength(100)
                .HasColumnName("type");
        });

        modelBuilder.Entity<StudentPortfolio>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("student_portfolio");

            entity.Property(e => e.ActivityType)
                .HasMaxLength(50)
                .HasColumnName("activity_type");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.FirstName)
                .HasMaxLength(100)
                .HasColumnName("first_name");
            entity.Property(e => e.GroupName)
                .HasMaxLength(50)
                .HasColumnName("group_name");
            entity.Property(e => e.HoursChange)
                .HasPrecision(5, 2)
                .HasColumnName("hours_change");
            entity.Property(e => e.LastName)
                .HasMaxLength(100)
                .HasColumnName("last_name");
            entity.Property(e => e.PointsChange).HasColumnName("points_change");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");
            entity.Property(e => e.UserId).HasColumnName("user_id");
        });

        modelBuilder.Entity<StudentRanking>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("student_ranking");

            entity.Property(e => e.BadgesCount).HasColumnName("badges_count");
            entity.Property(e => e.FirstName)
                .HasMaxLength(100)
                .HasColumnName("first_name");
            entity.Property(e => e.GroupName)
                .HasMaxLength(50)
                .HasColumnName("group_name");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.LastName)
                .HasMaxLength(100)
                .HasColumnName("last_name");
            entity.Property(e => e.Rank).HasColumnName("rank");
            entity.Property(e => e.TotalHours).HasColumnName("total_hours");
            entity.Property(e => e.TotalPoints).HasColumnName("total_points");
        });

        modelBuilder.Entity<SyncLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("sync_log_pkey");

            entity.ToTable("sync_log", tb => tb.HasComment("Журнал синхронизации с внешними API"));

            entity.HasIndex(e => e.StartedAt, "idx_sync_log_date");

            entity.HasIndex(e => e.IntegrationName, "idx_sync_log_integration");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CompletedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("completed_at");
            entity.Property(e => e.ErrorMessage).HasColumnName("error_message");
            entity.Property(e => e.IntegrationName)
                .HasMaxLength(100)
                .HasColumnName("integration_name");
            entity.Property(e => e.RecordsFailed)
                .HasDefaultValue(0)
                .HasColumnName("records_failed");
            entity.Property(e => e.RecordsProcessed)
                .HasDefaultValue(0)
                .HasColumnName("records_processed");
            entity.Property(e => e.StartedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("started_at");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.SyncType)
                .HasMaxLength(50)
                .HasColumnName("sync_type");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("users_pkey");

            entity.ToTable("users", tb => tb.HasComment("Пользователи системы (студенты, руководители, администраторы)"));

            entity.HasIndex(e => e.AmoCrmId, "idx_users_amo_crm_id");

            entity.HasIndex(e => e.Email, "idx_users_email");

            entity.HasIndex(e => e.GroupName, "idx_users_group");

            entity.HasIndex(e => e.Role, "idx_users_role");

            entity.HasIndex(e => e.Email, "users_email_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AmoCrmId).HasColumnName("amo_crm_id");
            entity.Property(e => e.AvatarUrl).HasColumnName("avatar_url");
            entity.Property(e => e.Bio).HasColumnName("bio");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.ExternalApiId)
                .HasMaxLength(100)
                .HasColumnName("external_api_id");
            entity.Property(e => e.FirstName)
                .HasMaxLength(100)
                .HasColumnName("first_name");
            entity.Property(e => e.Github)
                .HasMaxLength(100)
                .HasColumnName("github");
            entity.Property(e => e.GroupName)
                .HasMaxLength(50)
                .HasColumnName("group_name");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.LastLoginAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("last_login_at");
            entity.Property(e => e.LastName)
                .HasMaxLength(100)
                .HasColumnName("last_name");
            entity.Property(e => e.MiddleName)
                .HasMaxLength(100)
                .HasColumnName("middle_name");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.PhoneVisible)
                .HasDefaultValue(false)
                .HasColumnName("phone_visible");
            entity.Property(e => e.Role)
                .HasMaxLength(50)
                .HasDefaultValueSql("'student'::character varying")
                .HasColumnName("role");
            entity.Property(e => e.Telegram)
                .HasMaxLength(100)
                .HasColumnName("telegram");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");
            entity.Property(e => e.Vk)
                .HasMaxLength(100)
                .HasColumnName("vk");
        });

        modelBuilder.Entity<UserAchievement>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_achievements_pkey");

            entity.ToTable("user_achievements", tb => tb.HasComment("Полученные достижения пользователей"));

            entity.HasIndex(e => e.EarnedAt, "idx_user_achievements_earned");

            entity.HasIndex(e => e.UserId, "idx_user_achievements_user");

            entity.HasIndex(e => new { e.UserId, e.AchievementId }, "user_achievements_user_id_achievement_id_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AchievementId).HasColumnName("achievement_id");
            entity.Property(e => e.EarnedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("earned_at");
            entity.Property(e => e.NotificationSent)
                .HasDefaultValue(false)
                .HasColumnName("notification_sent");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Achievement).WithMany(p => p.UserAchievements)
                .HasForeignKey(d => d.AchievementId)
                .HasConstraintName("user_achievements_achievement_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.UserAchievements)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("user_achievements_user_id_fkey");
        });

        modelBuilder.Entity<UserPoint>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("user_points_pkey");

            entity.ToTable("user_points", tb => tb.HasComment("Суммарная статистика пользователей"));

            entity.Property(e => e.UserId)
                .ValueGeneratedNever()
                .HasColumnName("user_id");
            entity.Property(e => e.CurrentStreakDays)
                .HasDefaultValue(0)
                .HasColumnName("current_streak_days");
            entity.Property(e => e.MaxStreakDays)
                .HasDefaultValue(0)
                .HasColumnName("max_streak_days");
            entity.Property(e => e.TotalPoints)
                .HasDefaultValue(0)
                .HasColumnName("total_points");
            entity.Property(e => e.TotalProjectsCompleted)
                .HasDefaultValue(0)
                .HasColumnName("total_projects_completed");
            entity.Property(e => e.TotalRentalMinutes)
                .HasDefaultValue(0)
                .HasColumnName("total_rental_minutes");
            entity.Property(e => e.TotalVolunteerHours)
                .HasPrecision(8, 2)
                .HasDefaultValueSql("0")
                .HasColumnName("total_volunteer_hours");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.User).WithOne(p => p.UserPoint)
                .HasForeignKey<UserPoint>(d => d.UserId)
                .HasConstraintName("user_points_user_id_fkey");
        });

        modelBuilder.Entity<VolunteerHour>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("volunteer_hours_pkey");

            entity.ToTable("volunteer_hours", tb => tb.HasComment("Учёт волонтёрских часов студентов"));

            entity.HasIndex(e => e.StartTime, "idx_volunteer_hours_date");

            entity.HasIndex(e => e.ProjectId, "idx_volunteer_hours_project");

            entity.HasIndex(e => e.Status, "idx_volunteer_hours_status");

            entity.HasIndex(e => e.UserId, "idx_volunteer_hours_user");

            entity.HasIndex(e => new { e.UserId, e.Status }, "idx_volunteer_hours_user_status");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ConfirmedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("confirmed_at");
            entity.Property(e => e.ConfirmedBy).HasColumnName("confirmed_by");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.EndTime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("end_time");
            entity.Property(e => e.Hours)
                .HasPrecision(5, 2)
                .HasColumnName("hours");
            entity.Property(e => e.PointsAwarded)
                .HasDefaultValue(0)
                .HasColumnName("points_awarded");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.StartTime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("start_time");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValueSql("'pending'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.ConfirmedByNavigation).WithMany(p => p.VolunteerHourConfirmedByNavigations)
                .HasForeignKey(d => d.ConfirmedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("volunteer_hours_confirmed_by_fkey");

            entity.HasOne(d => d.Project).WithMany(p => p.VolunteerHours)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("volunteer_hours_project_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.VolunteerHourUsers)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("volunteer_hours_user_id_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
