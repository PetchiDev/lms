using CareTrack.Application.DTOs.Certificates;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CareTrack.Infrastructure.Services;

public record MarksheetRenderContext(
    string StudentFullName,
    string ProgrammeName,
    string AssessmentTitle,
    int ScorePercent,
    int PassPercentage,
    string? CertificateNumber,
    DateTime SubmittedAt,
    CertificateTemplateResponse Template);

public static class MarksheetPdfRenderer
{
    public static byte[] Render(MarksheetRenderContext ctx, Func<string?, byte[]?> loadImage)
    {
        var template = ctx.Template;
        var primary = ParseColor(template.PrimaryColor, "#003366");
        var accent = ParseColor(template.AccentColor, "#C9A227");
        var logoBytes = loadImage(template.LogoUrl);
        var passed = ctx.ScorePercent >= Math.Max(60, ctx.PassPercentage);
        var statusLabel = passed ? "PASS" : "ATTEMPTED";

        using var stream = new MemoryStream();
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(x => x.FontFamily("Segoe UI").FontColor(Colors.Black).FontSize(11));
                page.Background().Background("#F7F8FA");
                page.Content().Element(c => DrawMarksheet(c, ctx, template, primary, accent, logoBytes, statusLabel, passed, loadImage));
            });
        }).GeneratePdf(stream);

        return stream.ToArray();
    }

    private static void DrawMarksheet(
        IContainer container,
        MarksheetRenderContext ctx,
        CertificateTemplateResponse template,
        string primary,
        string accent,
        byte[]? logoBytes,
        string statusLabel,
        bool passed,
        Func<string?, byte[]?> loadImage)
    {
        container
            .Border(1).BorderColor(Colors.Grey.Lighten2)
            .Background(Colors.White)
            .Padding(0)
            .Column(col =>
            {
                // Accent top bar
                col.Item().Height(6).Background(primary);

                col.Item().Padding(28).Column(body =>
                {
                    body.Item().Element(c => DrawHeader(c, ctx, template, primary, accent, logoBytes, statusLabel, passed));
                    body.Item().PaddingTop(22).Element(c => DrawTitleBand(c, primary, accent));
                    body.Item().PaddingTop(20).Element(c => DrawDetailsTable(c, ctx, primary));
                    body.Item().PaddingTop(18).Element(c => DrawScoreBanner(c, ctx, primary, accent, passed));
                    body.Item().PaddingTop(28).Element(c => DrawSignatures(c, template, primary, loadImage));
                    body.Item().PaddingTop(22).Element(c => DrawFooter(c, template, ctx));
                });

                // Accent bottom bar
                col.Item().Height(4).Background(accent);
            });
    }

    private static void DrawHeader(
        IContainer container,
        MarksheetRenderContext ctx,
        CertificateTemplateResponse template,
        string primary,
        string accent,
        byte[]? logoBytes,
        string statusLabel,
        bool passed)
    {
        container.Row(row =>
        {
            row.RelativeItem().Row(left =>
            {
                if (logoBytes is not null)
                {
                    left.ConstantItem(64)
                        .Height(56)
                        .AlignMiddle()
                        .Image(logoBytes)
                        .FitArea();
                }

                left.RelativeItem()
                    .PaddingLeft(logoBytes is not null ? 14 : 0)
                    .AlignMiddle()
                    .Column(col =>
                    {
                        col.Item().Text(template.OrganizationName)
                            .FontSize(18)
                            .Bold()
                            .FontColor(primary);

                        if (!string.IsNullOrWhiteSpace(template.Tagline))
                        {
                            col.Item().PaddingTop(2).Text(template.Tagline)
                                .FontSize(10)
                                .FontColor(accent);
                        }

                        col.Item().PaddingTop(4).Text($"Issued on {ctx.SubmittedAt:dd MMMM yyyy}")
                            .FontSize(9)
                            .FontColor(Colors.Grey.Darken1);
                    });
            });

            row.ConstantItem(110).AlignRight().AlignMiddle().Element(badge =>
            {
                var bg = passed ? "#ECFDF5" : "#FFF7ED";
                var fg = passed ? Colors.Green.Darken2 : Colors.Orange.Darken2;
                var border = passed ? Colors.Green.Lighten2 : Colors.Orange.Lighten2;

                badge
                    .Border(1).BorderColor(border)
                    .Background(bg)
                    .PaddingHorizontal(14)
                    .PaddingVertical(8)
                    .AlignCenter()
                    .Text(statusLabel)
                    .FontSize(11)
                    .Bold()
                    .FontColor(fg);
            });
        });
    }

    private static void DrawTitleBand(IContainer container, string primary, string accent)
    {
        container
            .Background("#F0F4F8")
            .Border(1).BorderColor(Colors.Grey.Lighten3)
            .PaddingVertical(12)
            .PaddingHorizontal(16)
            .Row(row =>
            {
                row.ConstantItem(4).Background(accent);
                row.RelativeItem().PaddingLeft(12).AlignMiddle()
                    .Text("OFFICIAL MARKSHEET")
                    .FontSize(16)
                    .Bold()
                    .FontColor(primary);
            });
    }

    private static void DrawDetailsTable(IContainer container, MarksheetRenderContext ctx, string primary)
    {
        container
            .Border(1).BorderColor(Colors.Grey.Lighten2)
            .Column(col =>
            {
                DetailRow(col, "Student Name", ctx.StudentFullName, primary, true);
                DetailRow(col, "Course / Programme", ctx.ProgrammeName, primary, false);
                DetailRow(col, "Assessment", ctx.AssessmentTitle, primary, true);
                DetailRow(
                    col,
                    "Reference No.",
                    string.IsNullOrWhiteSpace(ctx.CertificateNumber) ? "—" : ctx.CertificateNumber!,
                    primary,
                    false,
                    isLast: true);
            });
    }

    private static void DetailRow(
        ColumnDescriptor col,
        string label,
        string value,
        string primary,
        bool shaded,
        bool isLast = false)
    {
        var item = col.Item();
        if (!isLast)
            item = item.BorderBottom(1).BorderColor(Colors.Grey.Lighten3);

        item
            .Background(shaded ? "#FAFBFC" : Colors.White)
            .PaddingVertical(12)
            .PaddingHorizontal(14)
            .Row(row =>
            {
                row.ConstantItem(160)
                    .AlignMiddle()
                    .Text(label)
                    .FontSize(10)
                    .SemiBold()
                    .FontColor(Colors.Grey.Darken1);

                row.RelativeItem()
                    .AlignMiddle()
                    .Text(value)
                    .FontSize(12)
                    .SemiBold()
                    .FontColor(primary);
            });
    }

    private static void DrawScoreBanner(
        IContainer container,
        MarksheetRenderContext ctx,
        string primary,
        string accent,
        bool passed)
    {
        container
            .Border(1).BorderColor(Colors.Grey.Lighten2)
            .Background("#F8FAFC")
            .Padding(16)
            .Row(row =>
            {
                row.RelativeItem().Column(left =>
                {
                    left.Item().Text("SCORE ACHIEVED")
                        .FontSize(9)
                        .SemiBold()
                        .FontColor(Colors.Grey.Darken1);

                    left.Item().PaddingTop(4).Text($"{ctx.ScorePercent}%")
                        .FontSize(34)
                        .Bold()
                        .FontColor(primary);

                    left.Item().PaddingTop(8).Height(8).Row(bar =>
                    {
                        var filled = Math.Clamp(ctx.ScorePercent, 0, 100);
                        var empty = 100 - filled;
                        if (filled > 0)
                            bar.RelativeItem(filled).Background(primary);
                        if (empty > 0)
                            bar.RelativeItem(empty).Background(Colors.Grey.Lighten3);
                    });
                });

                row.ConstantItem(140).AlignRight().AlignMiddle().Column(right =>
                {
                    right.Item().AlignRight().Text("Pass Mark")
                        .FontSize(9)
                        .SemiBold()
                        .FontColor(Colors.Grey.Darken1);

                    right.Item().PaddingTop(4).AlignRight().Text($"{ctx.PassPercentage}%")
                        .FontSize(18)
                        .Bold()
                        .FontColor(Colors.Black);

                    right.Item().PaddingTop(8).AlignRight()
                        .Text(passed ? "Result: Passed" : "Result: Not Passed")
                        .FontSize(10)
                        .SemiBold()
                        .FontColor(passed ? Colors.Green.Darken2 : Colors.Orange.Darken2);
                });
            });
    }

    private static void DrawSignatures(
        IContainer container,
        CertificateTemplateResponse template,
        string primary,
        Func<string?, byte[]?> loadImage)
    {
        container.Row(row =>
        {
            row.RelativeItem().Element(c => SignatureBlock(
                c,
                loadImage(template.LeftSignatureImageUrl),
                string.IsNullOrWhiteSpace(template.LeftSignatoryTitle)
                    ? "Controller of Examinations"
                    : template.LeftSignatoryTitle,
                primary));

            row.ConstantItem(28);

            row.RelativeItem().Element(c => SignatureBlock(
                c,
                loadImage(template.RightSignatureImageUrl),
                string.IsNullOrWhiteSpace(template.RightSignatoryTitle)
                    ? "Head of Department"
                    : template.RightSignatoryTitle,
                primary));
        });
    }

    private static void SignatureBlock(IContainer container, byte[]? signatureBytes, string title, string primary)
    {
        container
            .Border(1).BorderColor(Colors.Grey.Lighten2)
            .Background(Colors.White)
            .Padding(14)
            .MinHeight(110)
            .Column(col =>
            {
                col.Item().Height(42).AlignCenter().AlignMiddle().Element(c =>
                {
                    if (signatureBytes is not null)
                        c.Height(40).Image(signatureBytes).FitHeight();
                    else
                        c.Text(" ").FontSize(18);
                });

                col.Item().PaddingTop(8).BorderBottom(1.5f).BorderColor(primary);

                col.Item().PaddingTop(8).AlignCenter()
                    .Text(title)
                    .FontSize(9)
                    .SemiBold()
                    .FontColor(Colors.Grey.Darken2);
            });
    }

    private static void DrawFooter(IContainer container, CertificateTemplateResponse template, MarksheetRenderContext ctx)
    {
        container
            .BorderTop(1).BorderColor(Colors.Grey.Lighten2)
            .PaddingTop(12)
            .Row(row =>
            {
                row.RelativeItem().Text(
                        string.IsNullOrWhiteSpace(template.FooterLocation)
                            ? template.OrganizationName
                            : template.FooterLocation)
                    .FontSize(8)
                    .FontColor(Colors.Grey.Darken1);

                row.RelativeItem().AlignRight()
                    .Text("Generated by CareTrack Learning Platform")
                    .FontSize(8)
                    .FontColor(Colors.Grey.Darken1);
            });
    }

    private static string ParseColor(string? value, string fallback)
        => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
}
