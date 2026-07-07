using CareTrack.Application;
using CareTrack.Application.Common;
using CareTrack.Domain.Entities;
using CareTrack.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CareTrack.Infrastructure.Persistence;

public class CareTrackDbContext : IdentityDbContext<ApplicationUser>, ICareTrackDbContext
{
    private readonly ITenantContext _tenantContext;

    public CareTrackDbContext(DbContextOptions<CareTrackDbContext> options, ITenantContext tenantContext) : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<University> Universities => Set<University>();
    public DbSet<Programme> Programmes => Set<Programme>();
    public DbSet<UniversityProgramme> UniversityProgrammes => Set<UniversityProgramme>();
    public DbSet<ProgrammeYear> ProgrammeYears => Set<ProgrammeYear>();
    public DbSet<Semester> Semesters => Set<Semester>();
    public DbSet<Module> Modules => Set<Module>();
    public DbSet<ModulePrerequisite> ModulePrerequisites => Set<ModulePrerequisite>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<LessonAsset> LessonAssets => Set<LessonAsset>();
    public DbSet<ContentPublication> ContentPublications => Set<ContentPublication>();
    public DbSet<Cohort> Cohorts => Set<Cohort>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<StudentEnrolment> StudentEnrolments => Set<StudentEnrolment>();
    public DbSet<LessonProgress> LessonProgresses => Set<LessonProgress>();
    public DbSet<ModuleProgress> ModuleProgresses => Set<ModuleProgress>();
    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<QuizQuestion> QuizQuestions => Set<QuizQuestion>();
    public DbSet<QuizOption> QuizOptions => Set<QuizOption>();
    public DbSet<QuizAttempt> QuizAttempts => Set<QuizAttempt>();
    public DbSet<QuizAnswer> QuizAnswers => Set<QuizAnswer>();
    public DbSet<OfflineAssessmentResult> OfflineAssessmentResults => Set<OfflineAssessmentResult>();
    public DbSet<Certificate> Certificates => Set<Certificate>();
    public DbSet<CertificateTemplate> CertificateTemplates => Set<CertificateTemplate>();
    public DbSet<TenantIdpConfig> TenantIdpConfigs => Set<TenantIdpConfig>();
    public DbSet<HospitalDepartment> HospitalDepartments => Set<HospitalDepartment>();
    public DbSet<Supervisor> Supervisors => Set<Supervisor>();
    public DbSet<Rotation> Rotations => Set<Rotation>();
    public DbSet<RotationAssignment> RotationAssignments => Set<RotationAssignment>();
    public DbSet<LogbookEntry> LogbookEntries => Set<LogbookEntry>();
    public DbSet<SignOffEscalation> SignOffEscalations => Set<SignOffEscalation>();
    public DbSet<SupervisorDelegation> SupervisorDelegations => Set<SupervisorDelegation>();
    public DbSet<ContentVersion> ContentVersions => Set<ContentVersion>();
    public DbSet<AssessmentContentVersion> AssessmentContentVersions => Set<AssessmentContentVersion>();
    public DbSet<SisRosterSyncRun> SisRosterSyncRuns => Set<SisRosterSyncRun>();
    public DbSet<SisRosterSyncRecord> SisRosterSyncRecords => Set<SisRosterSyncRecord>();
    public DbSet<GradeSyncRequest> GradeSyncRequests => Set<GradeSyncRequest>();
    public DbSet<GradeSyncRecord> GradeSyncRecords => Set<GradeSyncRecord>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<HospitalAttendanceFeed> HospitalAttendanceFeeds => Set<HospitalAttendanceFeed>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<CalendarEvent> CalendarEvents => Set<CalendarEvent>();
    public DbSet<LiveClassSession> LiveClassSessions => Set<LiveClassSession>();
    public DbSet<DiscussionThread> DiscussionThreads => Set<DiscussionThread>();
    public DbSet<DiscussionPost> DiscussionPosts => Set<DiscussionPost>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<University>(e =>
        {
            e.HasIndex(x => x.Domain).IsUnique();
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Domain).HasMaxLength(100);
            e.Property(x => x.LogoUrl).HasMaxLength(2000);
            e.Property(x => x.EmailInviteSubject).HasMaxLength(300);
            e.Property(x => x.EmailFromName).HasMaxLength(200);
            e.Property(x => x.EmailFromEmail).HasMaxLength(320);
            e.HasOne(x => x.IdpConfig).WithOne(x => x.University).HasForeignKey<TenantIdpConfig>(x => x.UniversityId);
        });

        builder.Entity<TenantIdpConfig>(e =>
        {
            e.HasIndex(x => x.UniversityId).IsUnique();
        });

        builder.Entity<Programme>(e =>
        {
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Code).HasMaxLength(50);
        });

        builder.Entity<UniversityProgramme>(e =>
        {
            e.HasIndex(x => new { x.UniversityId, x.ProgrammeId }).IsUnique();
            e.HasOne(x => x.University).WithMany(u => u.UniversityProgrammes).HasForeignKey(x => x.UniversityId);
            e.HasOne(x => x.Programme).WithMany(p => p.UniversityProgrammes).HasForeignKey(x => x.ProgrammeId);
        });

        builder.Entity<ProgrammeYear>(e =>
        {
            e.HasIndex(x => new { x.ProgrammeId, x.YearNumber }).IsUnique();
            e.HasOne(x => x.Programme).WithMany(p => p.Years).HasForeignKey(x => x.ProgrammeId);
        });

        builder.Entity<Semester>(e =>
        {
            e.HasIndex(x => new { x.ProgrammeYearId, x.SemesterNumber }).IsUnique();
            e.HasOne(x => x.ProgrammeYear).WithMany(y => y.Semesters).HasForeignKey(x => x.ProgrammeYearId);
        });

        builder.Entity<Module>(e =>
        {
            e.HasOne(x => x.Semester).WithMany(s => s.Modules).HasForeignKey(x => x.SemesterId);
            e.Property(x => x.Title).HasMaxLength(300);
        });

        builder.Entity<ModulePrerequisite>(e =>
        {
            e.HasIndex(x => new { x.ModuleId, x.PrerequisiteModuleId }).IsUnique();
            e.HasOne(x => x.Module).WithMany(m => m.Prerequisites).HasForeignKey(x => x.ModuleId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.PrerequisiteModule).WithMany(m => m.RequiredBy).HasForeignKey(x => x.PrerequisiteModuleId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Lesson>(e =>
        {
            e.HasOne(x => x.Module).WithMany(m => m.Lessons).HasForeignKey(x => x.ModuleId);
            e.Property(x => x.Title).HasMaxLength(300);
        });

        builder.Entity<LessonAsset>(e =>
        {
            e.HasOne(x => x.Lesson).WithMany(l => l.Assets).HasForeignKey(x => x.LessonId);
        });

        builder.Entity<ContentPublication>(e =>
        {
            e.HasIndex(x => new { x.LessonId, x.UniversityId });
            e.HasOne(x => x.Lesson).WithMany(l => l.Publications).HasForeignKey(x => x.LessonId);
            e.HasOne(x => x.University).WithMany(u => u.ContentPublications).HasForeignKey(x => x.UniversityId);
        });

        builder.Entity<ContentVersion>(e =>
        {
            e.HasIndex(x => new { x.LessonId, x.VersionNumber }).IsUnique();
            e.HasOne(x => x.Lesson).WithMany(l => l.ContentVersions).HasForeignKey(x => x.LessonId);
        });

        builder.Entity<AssessmentContentVersion>(e =>
        {
            e.HasIndex(x => new { x.QuizId, x.ContentVersionId }).IsUnique();
            e.HasOne(x => x.Quiz).WithMany().HasForeignKey(x => x.QuizId);
            e.HasOne(x => x.ContentVersion).WithMany(v => v.AssessmentLinks).HasForeignKey(x => x.ContentVersionId);
        });

        builder.Entity<Cohort>(e =>
        {
            e.HasIndex(x => new { x.UniversityId, x.Name }).IsUnique();
            e.HasOne(x => x.University).WithMany(u => u.Cohorts).HasForeignKey(x => x.UniversityId);
            e.HasOne(x => x.Programme).WithMany().HasForeignKey(x => x.ProgrammeId);
        });

        builder.Entity<Student>(e =>
        {
            e.HasIndex(x => x.UserId).IsUnique();
        });

        builder.Entity<StudentEnrolment>(e =>
        {
            e.HasIndex(x => new { x.UniversityId, x.StudentId }).IsUnique();
            e.HasIndex(x => new { x.UniversityId, x.CohortId });
            e.HasOne(x => x.University).WithMany(u => u.Enrolments).HasForeignKey(x => x.UniversityId);
            e.HasOne(x => x.Student).WithMany(s => s.Enrolments).HasForeignKey(x => x.StudentId);
            e.HasOne(x => x.Cohort).WithMany(c => c.Enrolments).HasForeignKey(x => x.CohortId);
        });

        builder.Entity<LessonProgress>(e =>
        {
            e.HasIndex(x => new { x.StudentId, x.LessonId }).IsUnique();
            e.HasOne(x => x.Student).WithMany(s => s.LessonProgresses).HasForeignKey(x => x.StudentId);
            e.HasOne(x => x.Lesson).WithMany(l => l.ProgressRecords).HasForeignKey(x => x.LessonId);
        });

        builder.Entity<ModuleProgress>(e =>
        {
            e.HasIndex(x => new { x.StudentId, x.ModuleId }).IsUnique();
            e.HasOne(x => x.Student).WithMany(s => s.ModuleProgresses).HasForeignKey(x => x.StudentId);
            e.HasOne(x => x.Module).WithMany(m => m.ModuleProgresses).HasForeignKey(x => x.ModuleId);
        });

        builder.Entity<Quiz>(e =>
        {
            e.HasOne(x => x.Module).WithMany(m => m.Quizzes).HasForeignKey(x => x.ModuleId);
        });

        builder.Entity<QuizQuestion>(e =>
        {
            e.HasOne(x => x.Quiz).WithMany(q => q.Questions).HasForeignKey(x => x.QuizId);
        });

        builder.Entity<QuizOption>(e =>
        {
            e.HasOne(x => x.Question).WithMany(q => q.Options).HasForeignKey(x => x.QuestionId);
        });

        builder.Entity<QuizAttempt>(e =>
        {
            e.HasOne(x => x.Quiz).WithMany(q => q.Attempts).HasForeignKey(x => x.QuizId);
            e.HasOne(x => x.Student).WithMany(s => s.QuizAttempts).HasForeignKey(x => x.StudentId);
        });

        builder.Entity<QuizAnswer>(e =>
        {
            e.HasOne(x => x.Attempt).WithMany(a => a.Answers).HasForeignKey(x => x.AttemptId);
            e.HasOne(x => x.Question).WithMany().HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.SelectedOption).WithMany().HasForeignKey(x => x.SelectedOptionId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<OfflineAssessmentResult>(e =>
        {
            e.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId);
            e.HasOne(x => x.Module).WithMany().HasForeignKey(x => x.ModuleId);
        });

        builder.Entity<Certificate>(e =>
        {
            e.HasIndex(x => x.CertificateNumber).IsUnique();
            e.HasIndex(x => new { x.StudentId, x.ProgrammeId }).IsUnique();
            e.HasOne(x => x.Student).WithMany(s => s.Certificates).HasForeignKey(x => x.StudentId);
            e.HasOne(x => x.Programme).WithMany().HasForeignKey(x => x.ProgrammeId);
        });

        builder.Entity<CertificateTemplate>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(200);
            e.Property(x => x.OrganizationName).HasMaxLength(200);
            e.HasIndex(x => x.UniversityId).IsUnique();
            e.HasOne(x => x.University).WithMany().HasForeignKey(x => x.UniversityId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<HospitalDepartment>(e =>
        {
            e.HasIndex(x => new { x.UniversityId, x.Code }).IsUnique();
            e.HasOne(x => x.University).WithMany(u => u.HospitalDepartments).HasForeignKey(x => x.UniversityId);
        });

        builder.Entity<Supervisor>(e =>
        {
            e.HasIndex(x => x.UserId).IsUnique();
            e.HasOne(x => x.University).WithMany(u => u.Supervisors).HasForeignKey(x => x.UniversityId);
            e.HasOne(x => x.HospitalDepartment).WithMany(d => d.Supervisors).HasForeignKey(x => x.HospitalDepartmentId);
        });

        builder.Entity<Rotation>(e =>
        {
            e.HasOne(x => x.University).WithMany(u => u.Rotations).HasForeignKey(x => x.UniversityId);
            e.HasOne(x => x.HospitalDepartment).WithMany(d => d.Rotations).HasForeignKey(x => x.HospitalDepartmentId);
            e.HasOne(x => x.Cohort).WithMany().HasForeignKey(x => x.CohortId);
        });

        builder.Entity<RotationAssignment>(e =>
        {
            e.HasIndex(x => new { x.RotationId, x.StudentId }).IsUnique();
            e.HasOne(x => x.University).WithMany().HasForeignKey(x => x.UniversityId);
            e.HasOne(x => x.Rotation).WithMany(r => r.Assignments).HasForeignKey(x => x.RotationId);
            e.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId);
        });

        builder.Entity<LogbookEntry>(e =>
        {
            e.HasOne(x => x.University).WithMany().HasForeignKey(x => x.UniversityId);
            e.HasOne(x => x.RotationAssignment).WithMany(a => a.LogbookEntries).HasForeignKey(x => x.RotationAssignmentId);
            e.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId);
            e.HasOne(x => x.Supervisor).WithMany(s => s.ReviewedEntries).HasForeignKey(x => x.SupervisorId);
            e.HasOne(x => x.Escalation).WithOne(x => x.LogbookEntry).HasForeignKey<SignOffEscalation>(x => x.LogbookEntryId);
        });

        builder.Entity<SignOffEscalation>(e =>
        {
            e.HasIndex(x => x.LogbookEntryId).IsUnique();
            e.HasOne(x => x.University).WithMany().HasForeignKey(x => x.UniversityId);
        });

        builder.Entity<SupervisorDelegation>(e =>
        {
            e.HasOne(x => x.University).WithMany().HasForeignKey(x => x.UniversityId);
            e.HasOne(x => x.Supervisor).WithMany(s => s.DelegationsGiven).HasForeignKey(x => x.SupervisorId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.DelegateSupervisor).WithMany(s => s.DelegationsReceived).HasForeignKey(x => x.DelegateSupervisorId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<SisRosterSyncRun>(e =>
        {
            e.HasOne(x => x.University).WithMany().HasForeignKey(x => x.UniversityId);
        });

        builder.Entity<SisRosterSyncRecord>(e =>
        {
            e.HasOne(x => x.SyncRun).WithMany(r => r.Records).HasForeignKey(x => x.SyncRunId);
        });

        builder.Entity<GradeSyncRequest>(e =>
        {
            e.HasOne(x => x.University).WithMany().HasForeignKey(x => x.UniversityId);
            e.HasOne(x => x.Semester).WithMany().HasForeignKey(x => x.SemesterId);
            e.HasOne(x => x.Cohort).WithMany().HasForeignKey(x => x.CohortId);
        });

        builder.Entity<GradeSyncRecord>(e =>
        {
            e.HasOne(x => x.GradeSyncRequest).WithMany(r => r.Records).HasForeignKey(x => x.GradeSyncRequestId);
            e.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId);
        });

        builder.Entity<AttendanceRecord>(e =>
        {
            e.HasIndex(x => new { x.RotationAssignmentId, x.RecordDate });
            e.HasOne(x => x.University).WithMany().HasForeignKey(x => x.UniversityId);
            e.HasOne(x => x.RotationAssignment).WithMany(a => a.AttendanceRecords).HasForeignKey(x => x.RotationAssignmentId);
            e.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId);
        });

        builder.Entity<HospitalAttendanceFeed>(e =>
        {
            e.HasIndex(x => new { x.UniversityId, x.ExternalRecordId }).IsUnique();
            e.HasOne(x => x.University).WithMany().HasForeignKey(x => x.UniversityId);
        });

        builder.Entity<Notification>(e =>
        {
            e.HasIndex(x => new { x.UserId, x.IsRead });
            e.HasOne(x => x.University).WithMany().HasForeignKey(x => x.UniversityId);
        });

        builder.Entity<CalendarEvent>(e =>
        {
            e.HasOne(x => x.University).WithMany().HasForeignKey(x => x.UniversityId);
            e.HasOne(x => x.Cohort).WithMany().HasForeignKey(x => x.CohortId);
            e.HasOne(x => x.LiveClassSession).WithOne(x => x.CalendarEvent).HasForeignKey<LiveClassSession>(x => x.CalendarEventId);
        });

        builder.Entity<DiscussionThread>(e =>
        {
            e.HasOne(x => x.University).WithMany().HasForeignKey(x => x.UniversityId);
            e.HasOne(x => x.Lesson).WithMany(l => l.DiscussionThreads).HasForeignKey(x => x.LessonId);
        });

        builder.Entity<DiscussionPost>(e =>
        {
            e.HasOne(x => x.Thread).WithMany(t => t.Posts).HasForeignKey(x => x.ThreadId);
            e.HasOne(x => x.ParentPost).WithMany(p => p.Replies).HasForeignKey(x => x.ParentPostId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ApplicationUser>(e =>
        {
            e.HasIndex(x => x.Email).IsUnique();
        });

        TenantQueryFilterExtensions.ApplyTenantFilter<Cohort>(builder, _tenantContext);
        TenantQueryFilterExtensions.ApplyTenantFilter<StudentEnrolment>(builder, _tenantContext);
        TenantQueryFilterExtensions.ApplyTenantFilter<HospitalDepartment>(builder, _tenantContext);
        TenantQueryFilterExtensions.ApplyTenantFilter<Supervisor>(builder, _tenantContext);
        TenantQueryFilterExtensions.ApplyTenantFilter<Rotation>(builder, _tenantContext);
        TenantQueryFilterExtensions.ApplyTenantFilter<RotationAssignment>(builder, _tenantContext);
        TenantQueryFilterExtensions.ApplyTenantFilter<LogbookEntry>(builder, _tenantContext);
        TenantQueryFilterExtensions.ApplyTenantFilter<SignOffEscalation>(builder, _tenantContext);
        TenantQueryFilterExtensions.ApplyTenantFilter<SupervisorDelegation>(builder, _tenantContext);
        TenantQueryFilterExtensions.ApplyTenantFilter<SisRosterSyncRun>(builder, _tenantContext);
        TenantQueryFilterExtensions.ApplyTenantFilter<GradeSyncRequest>(builder, _tenantContext);
        TenantQueryFilterExtensions.ApplyTenantFilter<AttendanceRecord>(builder, _tenantContext);
        TenantQueryFilterExtensions.ApplyTenantFilter<HospitalAttendanceFeed>(builder, _tenantContext);
        TenantQueryFilterExtensions.ApplyTenantFilter<Notification>(builder, _tenantContext);
        TenantQueryFilterExtensions.ApplyTenantFilter<CalendarEvent>(builder, _tenantContext);
        TenantQueryFilterExtensions.ApplyTenantFilter<DiscussionThread>(builder, _tenantContext);
    }
}
