using CareTrack.Domain.Entities;
using CareTrack.Domain.Enums;
using CareTrack.Application.Interfaces;
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
        var blobStorage = services.GetRequiredService<IBlobStorageService>();

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

        await SeedProgrammeCatalogueAsync(db, blobStorage);
        await SeedModuleQuizzesAsync(db);
    }

    private static async Task SeedProgrammeCatalogueAsync(CareTrackDbContext db, IBlobStorageService blobStorage)
    {
        // This seeding is intended for a clean database (after purge).
        if (await db.Programmes.AsNoTracking().AnyAsync())
            return;

        var programmeSpecs = new[]
        {
            new ProgrammeSpec("B.AOTT", "Anaesthesia & OT Technology", 4, null),
            new ProgrammeSpec("B.DTT", "Dialysis Therapy Technology", 4, null),
            new ProgrammeSpec("BPT", "Bachelor of Physiotherapy", 4, "4.5 Years"),
            new ProgrammeSpec("B.PA", "Physician Associates", 4, null),
            new ProgrammeSpec("B.MLS", "Medical Laboratory Science", 4, null),
            new ProgrammeSpec("B.RT", "Respiratory Therapy", 4, null),
            new ProgrammeSpec("B.EMT", "Emergency Medical Technologist", 4, null),
            new ProgrammeSpec("B.Optom", "Bachelor of Optometry", 4, null),
            new ProgrammeSpec("B.CCT", "Critical Care Technology", 4, null),
            new ProgrammeSpec("B.CVT", "Cardiovascular Technology", 4, null),
            new ProgrammeSpec("B.BS", "Biomedical Science", 4, null),
            new ProgrammeSpec("B.MRIT", "Medical Radiology & Imaging Technology", 4, null),
            new ProgrammeSpec("MBA", "Health Service Management", 4, null),
            new ProgrammeSpec("BScNMT", "Nuclear Medicine Technology", 4, null),
            new ProgrammeSpec("B.ND", "Nutrition & Dietetics – Honours", 4, null),
        };

        // Upload the sample PDF once to blob and reuse the URL for all lesson assets.
        var assetBlobUrl = await EnsureSamplePdfUploadedAsync(blobStorage);

        var programmes = programmeSpecs.Select(spec => new Programme
        {
            Code = spec.Code,
            Name = spec.Name,
            Description = spec.DurationLabel is null ? $"{spec.Name} programme" : $"{spec.Name} programme ({spec.DurationLabel})",
            DurationYears = spec.DurationYears
        }).ToList();

        db.Programmes.AddRange(programmes);
        await db.SaveChangesAsync();

        var years = new List<ProgrammeYear>();
        foreach (var p in programmes)
            for (var y = 1; y <= 4; y++)
                years.Add(new ProgrammeYear { ProgrammeId = p.Id, YearNumber = y, Name = $"Year {y}" });
        db.ProgrammeYears.AddRange(years);
        await db.SaveChangesAsync();

        var semesters = new List<Semester>();
        foreach (var y in years)
            for (var s = 1; s <= 2; s++)
                semesters.Add(new Semester { ProgrammeYearId = y.Id, SemesterNumber = s, Name = $"Semester {s}" });
        db.Semesters.AddRange(semesters);
        await db.SaveChangesAsync();

        var modules = new List<Module>();
        foreach (var p in programmes)
        {
            foreach (var y in years.Where(x => x.ProgrammeId == p.Id))
            {
                foreach (var s in semesters.Where(x => x.ProgrammeYearId == y.Id))
                {
                    modules.Add(new Module
                    {
                        SemesterId = s.Id,
                        Title = $"{p.Code} Y{y.YearNumber}S{s.SemesterNumber}",
                        Description = "Seeded module content",
                        SortOrder = 1
                    });
                }
            }
        }
        db.Modules.AddRange(modules);
        await db.SaveChangesAsync();

        var lessons = new List<Lesson>();
        foreach (var m in modules)
        {
            lessons.Add(new Lesson
            {
                ModuleId = m.Id,
                Title = "Sample PDF Lesson",
                Description = "Seeded lesson content (PDF).",
                Status = ContentStatus.Published,
                SortOrder = 1,
                CreatedByUserId = null
            });
        }
        db.Lessons.AddRange(lessons);
        await db.SaveChangesAsync();

        db.LessonAssets.AddRange(lessons.Select(l => new LessonAsset
        {
            LessonId = l.Id,
            AssetType = AssetType.Pdf,
            FileName = "SampleNursingPDF.pdf",
            BlobUrl = assetBlobUrl,
            ContentType = "application/pdf",
            FileSizeBytes = 0
        }));

        db.ContentPublications.AddRange(lessons.Select(l => new ContentPublication
        {
            LessonId = l.Id,
            UniversityId = null,
            PublishedByUserId = null
        }));

        await db.SaveChangesAsync();
    }

    private static async Task SaveChangesWithRetryAsync(CareTrackDbContext db)
    {
        const int maxAttempts = 3;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await db.SaveChangesAsync();
                return;
            }
            catch (DbUpdateConcurrencyException) when (attempt < maxAttempts)
            {
                // Very rare during seeding; retry to avoid startup crash.
                await Task.Delay(200 * attempt);
            }
        }
    }

    private static async Task SeedModuleQuizzesAsync(CareTrackDbContext db)
    {
        var moduleIds = await db.Modules.AsNoTracking().Select(m => m.Id).ToListAsync();
        foreach (var moduleId in moduleIds)
        {
            if (await db.Quizzes.AnyAsync(q => q.ModuleId == moduleId))
                continue;

            var moduleTitle = await db.Modules.AsNoTracking().Where(m => m.Id == moduleId).Select(m => m.Title).FirstAsync();
            var quiz = new Quiz
            {
                ModuleId = moduleId,
                Title = $"{moduleTitle} Assessment",
                PassPercentage = 60,
                TimeLimitMinutes = 30
            };

            quiz.Questions.Add(new QuizQuestion
            {
                QuestionText = "Which is the correct statement?",
                SortOrder = 1,
                Options =
                [
                    new QuizOption { OptionText = "Option A (Correct)", IsCorrect = true, SortOrder = 1 },
                    new QuizOption { OptionText = "Option B", IsCorrect = false, SortOrder = 2 },
                    new QuizOption { OptionText = "Option C", IsCorrect = false, SortOrder = 3 },
                    new QuizOption { OptionText = "Option D", IsCorrect = false, SortOrder = 4 }
                ]
            });

            quiz.Questions.Add(new QuizQuestion
            {
                QuestionText = "Pick the best answer.",
                SortOrder = 2,
                Options =
                [
                    new QuizOption { OptionText = "Best (Correct)", IsCorrect = true, SortOrder = 1 },
                    new QuizOption { OptionText = "Wrong", IsCorrect = false, SortOrder = 2 },
                    new QuizOption { OptionText = "Wrong", IsCorrect = false, SortOrder = 3 },
                    new QuizOption { OptionText = "Wrong", IsCorrect = false, SortOrder = 4 }
                ]
            });

            db.Quizzes.Add(quiz);
            await db.SaveChangesAsync();
        }
    }

    private static async Task<string> EnsureSamplePdfUploadedAsync(IBlobStorageService blobStorage)
    {
        // Try to locate the sample PDF in repo or API content root.
        var cwd = Directory.GetCurrentDirectory();
        var candidates = new[]
        {
            Path.Combine(cwd, "src", "CareTrack.Api", "uploads", "SampleNursingPDF.pdf"),
            Path.Combine(cwd, "uploads", "SampleNursingPDF.pdf"),
            Path.Combine(cwd, "src", "CareTrack.Api", "uploads", "SampleNursingPDF.pdf".ToLowerInvariant()),
        };

        var path = candidates.FirstOrDefault(File.Exists)
            ?? throw new FileNotFoundException("Sample PDF not found.", candidates[0]);

        await using var stream = File.OpenRead(path);
        return await blobStorage.UploadAsync(stream, "SampleNursingPDF.pdf", "application/pdf", "media/library");
    }

    private sealed record ProgrammeSpec(string Code, string Name, int DurationYears, string? DurationLabel);
}
