namespace CareTrack.Application.DTOs.Certificates;

public record CertificateResponse(
    Guid Id,
    string CertificateNumber,
    string ProgrammeName,
    DateTime IssuedAt,
    string? PdfBlobUrl);

public record CertificateTemplateResponse(
    Guid Id,
    string Title,
    string OrganizationName,
    string Tagline,
    string AwardedToLabel,
    string BodyText,
    string DatePrefix,
    string Location,
    string FooterLocation,
    string WebsiteUrl,
    string LeftSignatoryTitle,
    string RightSignatoryTitle,
    string? LogoUrl,
    string? LeftSignatureImageUrl,
    string? RightSignatureImageUrl,
    string PrimaryColor,
    string AccentColor);

public record UpdateCertificateTemplateRequest(
    string Title,
    string OrganizationName,
    string Tagline,
    string AwardedToLabel,
    string BodyText,
    string DatePrefix,
    string Location,
    string FooterLocation,
    string WebsiteUrl,
    string LeftSignatoryTitle,
    string RightSignatoryTitle,
    string? LogoUrl,
    string? LeftSignatureImageUrl,
    string? RightSignatureImageUrl,
    string PrimaryColor,
    string AccentColor);

public record CertificateRenderContext(
    string StudentFullName,
    string ProgrammeName,
    string CertificateNumber,
    DateTime IssuedAt,
    CertificateTemplateResponse Template);
