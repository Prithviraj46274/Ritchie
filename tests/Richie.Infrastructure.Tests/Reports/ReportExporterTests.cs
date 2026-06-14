using System.Text;
using DocumentFormat.OpenXml.Packaging;
using Richie.Application.Reports;
using Richie.Infrastructure.Reports;

namespace Richie.Infrastructure.Tests.Reports;

public sealed class ReportExporterTests
{
    private static ReportContent Sample() => new(
        "Test Report", new DateTime(2026, 6, 15, 9, 0, 0, DateTimeKind.Utc), "All data",
        [
            new ReportSection("Summary", ["Total: 1,200.00", "P&L: +200.00"],
                new ReportTable(["Name", "Value"], [["Acme", "1,000"], ["Beta", "200"]])),
            new ReportSection("Notes", ["Just text, no table."])
        ]);

    [Fact]
    public void ToPdf_ProducesAPdf()
    {
        byte[] pdf = new ReportExporter().ToPdf(Sample());

        Assert.True(pdf.Length > 0);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(pdf, 0, 4));   // PDF magic header
    }

    [Fact]
    public void ToPptx_ProducesAValidDeck_WithTitlePlusSectionSlides()
    {
        byte[] pptx = new ReportExporter().ToPptx(Sample());

        Assert.True(pptx.Length > 0);
        Assert.Equal("PK", Encoding.ASCII.GetString(pptx, 0, 2));    // zip/OOXML header

        using var ms = new MemoryStream(pptx);
        using PresentationDocument doc = PresentationDocument.Open(ms, false);
        int slides = doc.PresentationPart!.SlideParts.Count();
        Assert.Equal(3, slides);   // title slide + 2 sections
    }
}
