using CareTrack.Domain.Entities;
using CareTrack.Domain.Enums;
using CareTrack.Infrastructure.Identity;
using CareTrack.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CareTrack.Infrastructure.Persistence.Seed;

public static class UniversityStudentSeeder
{
  private static readonly (string FirstName, string LastName)[] CoreTeam =
  [
    ("Petchiappan", "P"),
    ("Prasana", "Balaji"),
    ("Prasanna", "Venkatesh"),
    ("Sangeeth", "K"),
    ("Kishore", "K"),
    ("Murali", "M"),
    ("Abdur", "R"),
    ("Vasanth", "V"),
    ("Jaswanth", "J"),
    ("Sakthi", "S"),
  ];

  private static readonly string[] RandomFirstNames =
  [
    "Aarav", "Aditya", "Ananya", "Arun", "Bhavya", "Deepak", "Divya", "Ganesh", "Harini", "Isha",
    "Karthik", "Keerthi", "Lakshmi", "Manoj", "Meena", "Naveen", "Nithya", "Prakash", "Priya", "Rahul",
    "Rajesh", "Ramya", "Ravi", "Revathi", "Sanjay", "Shalini", "Suresh", "Swathi", "Vijay", "Yogesh",
    "Aishwarya", "Balaji", "Charan", "Dinesh", "Eswar", "Farhan", "Gokul", "Hema", "Indira", "Jagan",
  ];

  private static readonly string[] RandomLastNames =
  [
    "Kumar", "Reddy", "Sharma", "Iyer", "Nair", "Pillai", "Rao", "Singh", "Patel", "Das",
    "Menon", "Venkatesh", "Subramanian", "Krishnan", "Babu", "Devi", "Lal", "Chandran", "Murugan", "Selvam",
  ];

  public static async Task SeedAsync(IServiceProvider services)
  {
    var db = services.GetRequiredService<CareTrackDbContext>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var logger = services.GetService<ILoggerFactory>()?.CreateLogger("UniversityStudentSeeder");

    var universities = await db.Universities.AsNoTracking()
      .Where(u => u.IsActive)
      .Select(u => new { u.Id, u.Name, u.Domain })
      .ToListAsync();

    if (universities.Count == 0)
      return;

    var programmes = await db.Programmes.AsNoTracking().OrderBy(p => p.Name).ToListAsync();
    if (programmes.Count == 0)
    {
      logger?.LogWarning("University student seed skipped: no programmes in database.");
      return;
    }

    var defaultProgramme = programmes[0];
    var totalCreated = 0;

    foreach (var university in universities)
    {
      var existingCount = await db.StudentEnrolments.AsNoTracking()
        .CountAsync(e => e.UniversityId == university.Id);

      if (existingCount >= 50)
      {
        logger?.LogInformation("Skipping {University}: already has {Count} students.", university.Name, existingCount);
        continue;
      }

      var targetTotal = Random.Shared.Next(50, 101);
      var cohort = await EnsureCohortAsync(db, university.Id, defaultProgramme.Id);
      var createdHere = 0;

      foreach (var (firstName, lastName) in CoreTeam)
      {
        if (existingCount + createdHere >= targetTotal)
          break;

        var email = BuildEmail(firstName, lastName, university.Domain);
        if (await TryCreateStudentAsync(userManager, db, university.Id, cohort.Id, email, firstName, lastName))
          createdHere++;
      }

      var guard = 0;
      while (existingCount + createdHere < targetTotal && guard < 200)
      {
        guard++;
        var firstName = RandomFirstNames[Random.Shared.Next(RandomFirstNames.Length)];
        var lastName = RandomLastNames[Random.Shared.Next(RandomLastNames.Length)];
        var suffix = Random.Shared.Next(1, 9999);
        var email = $"{firstName.ToLowerInvariant()}.{lastName.ToLowerInvariant()}{suffix}@{university.Domain}";

        if (await TryCreateStudentAsync(userManager, db, university.Id, cohort.Id, email, firstName, lastName))
          createdHere++;
      }

      totalCreated += createdHere;
      logger?.LogInformation(
        "Seeded {Created} students for {University} ({Domain}). Total now ~{Total}.",
        createdHere,
        university.Name,
        university.Domain,
        existingCount + createdHere);
    }

    if (totalCreated > 0)
      logger?.LogInformation("University student seed complete: {Total} new students.", totalCreated);
  }

  private static async Task<Cohort> EnsureCohortAsync(CareTrackDbContext db, Guid universityId, Guid programmeId)
  {
    var cohort = await db.Cohorts
      .FirstOrDefaultAsync(c => c.UniversityId == universityId && c.ProgrammeId == programmeId);

    if (cohort is not null)
      return cohort;

    var hasLink = await db.UniversityProgrammes.AnyAsync(up =>
      up.UniversityId == universityId && up.ProgrammeId == programmeId);

    if (!hasLink)
    {
      db.UniversityProgrammes.Add(new UniversityProgramme
      {
        UniversityId = universityId,
        ProgrammeId = programmeId,
      });
    }

    var year = DateTime.UtcNow.Year;
    cohort = new Cohort
    {
      UniversityId = universityId,
      ProgrammeId = programmeId,
      Name = $"{year} Intake",
      IntakeYear = year,
      CurrentYear = 1,
      CurrentSemester = 1,
      IsActive = true,
    };

    db.Cohorts.Add(cohort);
    await db.SaveChangesAsync();
    return cohort;
  }

  private static string BuildEmail(string firstName, string lastName, string domain)
  {
    var local = firstName.Trim().ToLowerInvariant().Replace(" ", "");
    if (!string.IsNullOrWhiteSpace(lastName) && lastName.Length > 1)
      local += $".{lastName.Trim().ToLowerInvariant().Replace(" ", "")}";
    return $"{local}@{domain.Trim().ToLowerInvariant()}";
  }

  private static string BuildPassword(string firstName)
  {
    var name = firstName.Trim();
    if (string.IsNullOrEmpty(name))
      return "Student@123";

    return char.ToUpperInvariant(name[0]) + name[1..].ToLowerInvariant() + "@123";
  }

  private static async Task<bool> TryCreateStudentAsync(
    UserManager<ApplicationUser> userManager,
    CareTrackDbContext db,
    Guid universityId,
    Guid cohortId,
    string email,
    string firstName,
    string lastName)
  {
    if (await userManager.FindByEmailAsync(email) is not null)
      return false;

    var studentId = Guid.NewGuid();
    var password = BuildPassword(firstName);

    var user = new ApplicationUser
    {
      UserName = email,
      Email = email,
      FirstName = firstName,
      LastName = lastName,
      Role = UserRole.Student,
      UniversityId = universityId,
      CohortId = cohortId,
      StudentId = studentId,
      Status = EnrolmentStatus.Active,
      EmailConfirmed = true,
    };

    var createResult = await userManager.CreateAsync(user, password);
    if (!createResult.Succeeded)
      return false;

    db.Students.Add(new Student
    {
      Id = studentId,
      UserId = user.Id,
      FirstName = firstName,
      LastName = lastName,
    });

    db.StudentEnrolments.Add(new StudentEnrolment
    {
      UniversityId = universityId,
      StudentId = studentId,
      CohortId = cohortId,
      Status = EnrolmentStatus.Active,
      ActivatedAt = DateTime.UtcNow,
      CurrentYear = 1,
      CurrentSemester = 1,
    });

    await db.SaveChangesAsync();
    return true;
  }
}
