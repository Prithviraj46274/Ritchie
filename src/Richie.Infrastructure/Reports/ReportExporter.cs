using Richie.Application.Reports;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Richie.Infrastructure.Reports;

public sealed partial class ReportExporter : IReportExporter
{
    static ReportExporter()
    {
        // QuestPDF is free for this use under the Community licence (offline desktop app).
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] ToPdf(ReportContent content)
    {
        return Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Margin(36);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text(content.Title).FontSize(20).SemiBold();
                    col.Item().Text($"Generated {content.GeneratedUtc.ToLocalTime():g}  ·  {content.PeriodLabel}")
                        .FontColor(Colors.Grey.Medium);
                });

                page.Content().PaddingVertical(8).Column(col =>
                {
                    col.Spacing(10);
                    foreach (ReportSection section in content.Sections)
                    {
                        col.Item().PaddingTop(6).Text(section.Heading).FontSize(13).SemiBold();
                        foreach (string line in section.Lines)
                            col.Item().Text(line);
                        if (section.Table is { } table)
                            col.Item().Element(c => RenderTable(c, table));
                    }
                });

                page.Footer().AlignRight().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    private static void RenderTable(IContainer container, ReportTable table)
    {
        container.Table(t =>
        {
            t.ColumnsDefinition(cols =>
            {
                foreach (string _ in table.Columns)
                    cols.RelativeColumn();
            });

            foreach (string column in table.Columns)
                t.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text(column).SemiBold();

            foreach (IReadOnlyList<string> row in table.Rows)
                foreach (string cell in row)
                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(cell);
        });
    }
}
