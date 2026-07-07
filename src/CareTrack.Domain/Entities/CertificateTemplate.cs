using CareTrack.Domain.Common;

namespace CareTrack.Domain.Entities;

public class CertificateTemplate : BaseEntity
{
    public Guid? UniversityId { get; set; }
    public string Title { get; set; } = "CERTIFICATE OF COMPLETION";
    public string OrganizationName { get; set; } = "Institute";
    public string Tagline { get; set; } = "Learning Platform";
    public string AwardedToLabel { get; set; } = "Awarded To";
    public string BodyText { get; set; } =
        "In recognition of successful completion of the training program: {ProgrammeName}, demonstrating proficiency and dedication during the course.";
    public string DatePrefix { get; set; } = "Given at";
    public string Location { get; set; } = "Chennai, India";
    public string FooterLocation { get; set; } = "APOLLO HOSPITALS, Chennai, Tamil Nadu";
    public string WebsiteUrl { get; set; } = "www.apollohospitals.com";
    public string LeftSignatoryTitle { get; set; } = "Director of Medical Education, Apollo Hospitals";
    public string RightSignatoryTitle { get; set; } = "Chief Operating Officer, Apollo Hospitals";
    public string? LogoUrl { get; set; }
    public string? LeftSignatureImageUrl { get; set; }
    public string? RightSignatureImageUrl { get; set; }
    public string PrimaryColor { get; set; } = "#003366";
    public string AccentColor { get; set; } = "#C9A227";

    public University? University { get; set; }
}
