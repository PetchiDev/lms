using CareTrack.Domain.Entities;
using CareTrack.Infrastructure.Identity;
using CareTrack.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CareTrack.Infrastructure.Persistence.Seed;

public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<CareTrackDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        if (await userManager.FindByEmailAsync("admin@apollo.edu") is null)
        {
            await userManager.CreateAsync(new ApplicationUser
            {
                UserName = "admin@apollo.edu",
                Email = "admin@apollo.edu",
                FirstName = "Apollo",
                LastName = "Admin",
                Role = Domain.Enums.UserRole.ApolloAdmin,
                EmailConfirmed = true,
                Status = Domain.Enums.EnrolmentStatus.Active
            }, "Admin@123");
        }

        if (await userManager.FindByEmailAsync("faculty@apollo.edu") is null)
        {
            await userManager.CreateAsync(new ApplicationUser
            {
                UserName = "faculty@apollo.edu",
                Email = "faculty@apollo.edu",
                FirstName = "Apollo",
                LastName = "Faculty",
                Role = Domain.Enums.UserRole.ApolloFaculty,
                EmailConfirmed = true,
                Status = Domain.Enums.EnrolmentStatus.Active
            }, "Faculty@123");
        }

        if (!await db.Programmes.AnyAsync())
        {
            var programme = new Programme
            {
                Name = "B.Sc Allied Health",
                Code = "BSC-AH",
                Description = "Allied Health Sciences Programme",
                DurationYears = 3
            };

            for (var year = 1; year <= 3; year++)
            {
                var programmeYear = new ProgrammeYear
                {
                    YearNumber = year,
                    Name = $"Year {year}"
                };

                for (var sem = 1; sem <= 2; sem++)
                {
                    var semester = new Semester
                    {
                        SemesterNumber = sem,
                        Name = $"Semester {sem}"
                    };

                    semester.Modules.Add(new Module
                    {
                        Title = year == 1 && sem == 1 ? "Cardiovascular Assessment" : $"Module Y{year}S{sem}",
                        Description = "Core module content",
                        SortOrder = 1
                    });

                    programmeYear.Semesters.Add(semester);
                }

                programme.Years.Add(programmeYear);
            }

            db.Programmes.Add(programme);
            await db.SaveChangesAsync();
        }

        if (!await db.Universities.AnyAsync(u => u.Domain == "meridian.edu"))
        {
            var programme = await db.Programmes.OrderBy(p => p.Code).FirstAsync();
            var university = new University
            {
                Name = "Meridian University",
                Domain = "meridian.edu"
            };
            db.Universities.Add(university);

            db.UniversityProgrammes.Add(new UniversityProgramme
            {
                University = university,
                Programme = programme
            });

            var cohort = new Cohort
            {
                University = university,
                Programme = programme,
                Name = "2026 Intake",
                IntakeYear = 2026,
                CurrentYear = 1,
                CurrentSemester = 1
            };
            db.Cohorts.Add(cohort);
            await db.SaveChangesAsync();

            if (await userManager.FindByEmailAsync("admin@meridian.edu") is null)
            {
                await userManager.CreateAsync(new ApplicationUser
                {
                    UserName = "admin@meridian.edu",
                    Email = "admin@meridian.edu",
                    FirstName = "Meridian",
                    LastName = "Admin",
                    Role = Domain.Enums.UserRole.UniversityAdmin,
                    UniversityId = university.Id,
                    EmailConfirmed = true,
                    Status = Domain.Enums.EnrolmentStatus.Active
                }, "UnivAdmin@123");
            }

            // Seed quiz for first module
            var firstModule = await db.Modules.OrderBy(m => m.CreatedAt).FirstOrDefaultAsync();
            if (firstModule is not null && !await db.Quizzes.AnyAsync())
            {
                var quiz = new Quiz
                {
                    ModuleId = firstModule.Id,
                    Title = $"{firstModule.Title} Assessment",
                    PassPercentage = 60,
                    TimeLimitMinutes = 30
                };

                var q1 = new QuizQuestion
                {
                    QuestionText = "What is the primary purpose of cardiovascular assessment?",
                    SortOrder = 1,
                    Options =
                    [
                        new QuizOption { OptionText = "Evaluate heart and blood vessel function", IsCorrect = true, SortOrder = 1 },
                        new QuizOption { OptionText = "Measure bone density", IsCorrect = false, SortOrder = 2 },
                        new QuizOption { OptionText = "Assess lung capacity only", IsCorrect = false, SortOrder = 3 }
                    ]
                };

                quiz.Questions.Add(q1);
                db.Quizzes.Add(quiz);
                await db.SaveChangesAsync();
            }
        }

        await SeedPhase2Async(db, userManager);
    }

    private static async Task SeedPhase2Async(CareTrackDbContext db, UserManager<ApplicationUser> userManager)
    {
        var university = await db.Universities.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Domain == "meridian.edu");
        if (university is null) return;

        if (!await db.TenantIdpConfigs.IgnoreQueryFilters().AnyAsync(c => c.UniversityId == university.Id))
        {
            db.TenantIdpConfigs.Add(new TenantIdpConfig
            {
                UniversityId = university.Id,
                ProviderType = Domain.Enums.IdpProviderType.Saml,
                MetadataUrl = "https://PLACEHOLDER-meridian-idp.edu/saml/metadata",
                ClientId = "PLACEHOLDER_MERIDIAN_CLIENT_ID",
                ClientSecretPlaceholder = "PLACEHOLDER_MERIDIAN_CLIENT_SECRET",
                IsEnabled = true
            });
        }

        HospitalDepartment? cardiology = null;
        if (!await db.HospitalDepartments.IgnoreQueryFilters().AnyAsync(d => d.UniversityId == university.Id))
        {
            cardiology = new HospitalDepartment
            {
                UniversityId = university.Id,
                Name = "Cardiology",
                Code = "CARD",
                CapacityPerMonth = 40
            };
            db.HospitalDepartments.Add(cardiology);
            await db.SaveChangesAsync();
        }
        else
        {
            cardiology = await db.HospitalDepartments.IgnoreQueryFilters().FirstAsync(d => d.UniversityId == university.Id);
        }

        ApplicationUser? supervisorUser = await userManager.FindByEmailAsync("supervisor@meridian.edu");
        Supervisor? supervisor = null;
        if (supervisorUser is null)
        {
            supervisorUser = new ApplicationUser
            {
                UserName = "supervisor@meridian.edu",
                Email = "supervisor@meridian.edu",
                FirstName = "Dr. Priya",
                LastName = "Sharma",
                Role = Domain.Enums.UserRole.Supervisor,
                UniversityId = university.Id,
                EmailConfirmed = true,
                Status = Domain.Enums.EnrolmentStatus.Active
            };
            await userManager.CreateAsync(supervisorUser, "Supervisor@123");

            supervisor = new Supervisor
            {
                UniversityId = university.Id,
                UserId = supervisorUser.Id,
                HospitalDepartmentId = cardiology.Id,
                Title = "Clinical Supervisor"
            };
            db.Supervisors.Add(supervisor);
            await db.SaveChangesAsync();
            supervisorUser.SupervisorId = supervisor.Id;
            await userManager.UpdateAsync(supervisorUser);
        }
        else
        {
            supervisor = await db.Supervisors.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.UserId == supervisorUser.Id);
        }

        var cohort = await db.Cohorts.IgnoreQueryFilters().FirstAsync(c => c.UniversityId == university.Id);
        Student? student = null;
        if (!await db.Students.AnyAsync())
        {
            var studentUser = await userManager.FindByEmailAsync("student@meridian.edu");
            if (studentUser is null)
            {
                studentUser = new ApplicationUser
                {
                    UserName = "student@meridian.edu",
                    Email = "student@meridian.edu",
                    FirstName = "Arjun",
                    LastName = "Kumar",
                    Role = Domain.Enums.UserRole.Student,
                    UniversityId = university.Id,
                    CohortId = cohort.Id,
                    EmailConfirmed = true,
                    Status = Domain.Enums.EnrolmentStatus.Active
                };
                await userManager.CreateAsync(studentUser, "Student@123");
            }

            student = new Student
            {
                UserId = studentUser.Id,
                FirstName = "Arjun",
                LastName = "Kumar"
            };
            db.Students.Add(student);
            await db.SaveChangesAsync();

            studentUser.StudentId = student.Id;
            await userManager.UpdateAsync(studentUser);

            db.StudentEnrolments.Add(new StudentEnrolment
            {
                UniversityId = university.Id,
                StudentId = student.Id,
                CohortId = cohort.Id,
                Status = Domain.Enums.EnrolmentStatus.Active
            });
            await db.SaveChangesAsync();
        }
        else
        {
            student = await db.Students.FirstAsync();
        }

        if (!await db.Rotations.IgnoreQueryFilters().AnyAsync(r => r.UniversityId == university.Id))
        {
            var rotation = new Rotation
            {
                UniversityId = university.Id,
                HospitalDepartmentId = cardiology.Id,
                CohortId = cohort.Id,
                Name = "Cardiology Posting — Batch A",
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-14)),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(28)),
                WeeksDuration = 6,
                RequiredProcedureCount = 5,
                Status = Domain.Enums.RotationStatus.Active
            };
            db.Rotations.Add(rotation);
            await db.SaveChangesAsync();

            var assignment = new RotationAssignment
            {
                UniversityId = university.Id,
                RotationId = rotation.Id,
                StudentId = student!.Id,
                Status = Domain.Enums.RotationStatus.Active,
                AttendancePercent = 88
            };
            db.RotationAssignments.Add(assignment);
            await db.SaveChangesAsync();

            db.LogbookEntries.Add(new LogbookEntry
            {
                UniversityId = university.Id,
                RotationAssignmentId = assignment.Id,
                StudentId = student.Id,
                EntryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
                Procedure = "ECG Interpretation",
                PatientCount = 3,
                Notes = "Observed and assisted with 12-lead ECG placement",
                Location = "Cardiology Ward — Block B",
                Status = Domain.Enums.LogbookEntryStatus.PendingSignoff,
                SubmittedAt = DateTime.UtcNow.AddHours(-6)
            });
            await db.SaveChangesAsync();
        }

        if (!await db.CalendarEvents.IgnoreQueryFilters().AnyAsync(e => e.UniversityId == university.Id))
        {
            var liveClass = new CalendarEvent
            {
                UniversityId = university.Id,
                CohortId = cohort.Id,
                Title = "Live class — Cardiovascular intro",
                Description = "Interactive session with faculty",
                StartAt = DateTime.UtcNow.Date.AddHours(9),
                EndAt = DateTime.UtcNow.Date.AddHours(10),
                EventType = Domain.Enums.CalendarEventType.LiveClass,
                LiveClassSession = new LiveClassSession
                {
                    JoinUrl = "https://PLACEHOLDER-teams.microsoft.com/meet/caretrack-demo",
                    MinAttendanceMinutes = 45
                }
            };
            db.CalendarEvents.Add(liveClass);
            await db.SaveChangesAsync();
        }
    }
}
