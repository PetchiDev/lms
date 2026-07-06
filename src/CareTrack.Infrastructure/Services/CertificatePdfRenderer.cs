using CareTrack.Application.DTOs.Certificates;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CareTrack.Infrastructure.Services;

public static class CertificatePdfRenderer
{
    private static readonly string DefaultLogoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "apollo_logo.png");

    public static byte[] Render(CertificateRenderContext ctx, Func<string?, byte[]?> loadImage)
    {
        var template = ctx.Template;
        var primary = ParseColor(template.PrimaryColor, "#003366");
        var accent = ParseColor(template.AccentColor, "#C9A227");
        var body = template.BodyText
            .Replace("{ProgrammeName}", ctx.ProgrammeName, StringComparison.OrdinalIgnoreCase)
            .Replace("{StudentName}", ctx.StudentFullName, StringComparison.OrdinalIgnoreCase);
        var dateLine = $"{template.DatePrefix} {template.Location} on {FormatIssuedDate(ctx.IssuedAt)}.";
        var logoBytes = loadImage(template.LogoUrl) ?? loadImage(null);

        using var stream = new MemoryStream();
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(28);
                page.DefaultTextStyle(x => x.FontFamily("Times New Roman").FontColor(primary));

                page.Background().Background("#FFFDF8");

                page.Content().Element(c => DrawCertificate(c, ctx, template, primary, accent, body, dateLine, logoBytes, loadImage));
            });
        }).GeneratePdf(stream);

        return stream.ToArray();
    }

    private static void DrawCertificate(
        IContainer container,
        CertificateRenderContext ctx,
        CertificateTemplateResponse template,
        string primary,
        string accent,
        string body,
        string dateLine,
        byte[]? logoBytes,
        Func<string?, byte[]?> loadImage)
    {
        container
            .Border(3).BorderColor(accent)
            .Padding(6)
            .Border(1).BorderColor(accent)
            .Padding(24)
            .Column(col =>
            {
                col.Item().AlignCenter().Element(c => DrawHeader(c, template, logoBytes, primary, accent));
                col.Item().PaddingTop(10).AlignCenter().Text(template.Title).FontSize(30).Bold().FontColor(primary).LetterSpacing(0.8f);
                col.Item().PaddingTop(18).AlignCenter().Text(template.AwardedToLabel).FontSize(14).Italic().FontColor(primary);
                col.Item().PaddingTop(6).AlignCenter().Text(ctx.StudentFullName.ToUpperInvariant()).FontSize(36).Bold().FontColor(primary);
                col.Item().PaddingTop(16).PaddingHorizontal(40).AlignCenter().Text(body).FontSize(13).LineHeight(1.5f).FontColor(Colors.Black);
                col.Item().PaddingTop(12).AlignCenter().Text(dateLine).FontSize(12).FontColor(Colors.Black);
                col.Item().PaddingTop(8).AlignCenter().Text($"Certificate No: {ctx.CertificateNumber}").FontSize(9).FontColor(Colors.Grey.Darken2);

                col.Item().PaddingTop(28).Row(row =>
                {
                    row.ConstantItem(120).Element(c => DrawSeal(c, accent, logoBytes));
                    row.RelativeItem().PaddingHorizontal(16).Column(inner =>
                    {
                        inner.Item().Row(sigRow =>
                        {
                            sigRow.RelativeItem().Element(c => DrawSignature(c, loadImage(template.LeftSignatureImageUrl), template.LeftSignatoryTitle, primary));
                            sigRow.ConstantItem(40);
                            sigRow.RelativeItem().Element(c => DrawSignature(c, loadImage(template.RightSignatureImageUrl), template.RightSignatoryTitle, primary));
                        });
                        inner.Item().PaddingTop(14).AlignCenter().Text(template.FooterLocation).FontSize(10).Bold().FontColor(primary);
                        inner.Item().AlignCenter().Text(template.WebsiteUrl).FontSize(9).FontColor(Colors.Grey.Darken1);
                    });
                });
            });
    }

    private static void DrawHeader(IContainer container, CertificateTemplateResponse template, byte[]? logoBytes, string primary, string accent)
    {
        container.Column(col =>
        {
            if (logoBytes is not null)
                col.Item().AlignCenter().Height(44).Image(logoBytes).FitHeight();
            else
                col.Item().AlignCenter().Text(template.OrganizationName).FontSize(22).Bold().FontColor(primary);

            col.Item().AlignCenter().Text(template.Tagline).FontSize(11).LetterSpacing(1.2f).FontColor(accent);
        });
    }

    private static void DrawSeal(IContainer container, string accent, byte[]? logoBytes)
    {
        container.AlignLeft().AlignBottom().Width(100).Height(100)
            .Border(3).BorderColor(accent)
            .Background(Colors.White)
            .AlignCenter().AlignMiddle().Column(col =>
            {
                col.Item().AlignCenter().Text("CERTIFIED").FontSize(8).Bold().FontColor(accent);
                if (logoBytes is not null)
                    col.Item().PaddingVertical(4).Height(36).Image(logoBytes).FitHeight();
                col.Item().AlignCenter().Text("COMPLETION").FontSize(8).Bold().FontColor(accent);
            });
    }

    private static void DrawSignature(IContainer container, byte[]? signatureBytes, string title, string primary)
    {
        container.Column(col =>
        {
            if (signatureBytes is not null)
                col.Item().AlignCenter().Height(36).Image(signatureBytes).FitHeight();
            else
                col.Item().AlignCenter().PaddingBottom(4).Text(" ").FontSize(20);

            col.Item().BorderBottom(1).BorderColor(primary).PaddingBottom(2);
            col.Item().PaddingTop(6).AlignCenter().Text(title).FontSize(8).FontColor(Colors.Grey.Darken2);
        });
    }

    private static string FormatIssuedDate(DateTime date)
    {
        var day = date.Day;
        var suffix = day switch
        {
            1 or 21 or 31 => "st",
            2 or 22 => "nd",
            3 or 23 => "rd",
            _ => "th"
        };
        return $"This {day}{suffix} Day of {date:MMMM, yyyy}";
    }

    private static string ParseColor(string? value, string fallback)
        => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
}
