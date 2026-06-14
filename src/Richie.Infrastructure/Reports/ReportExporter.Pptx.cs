using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using Richie.Application.Reports;
using D = DocumentFormat.OpenXml.Drawing;
using P = DocumentFormat.OpenXml.Presentation;

namespace Richie.Infrastructure.Reports;

public sealed partial class ReportExporter
{
    public byte[] ToPptx(ReportContent content)
    {
        using var stream = new MemoryStream();
        using (PresentationDocument doc = PresentationDocument.Create(stream, PresentationDocumentType.Presentation))
        {
            PresentationPart presentationPart = doc.AddPresentationPart();
            presentationPart.Presentation = new P.Presentation();

            SlideMasterPart masterPart = CreateSlideMaster(presentationPart);
            SlideLayoutPart layoutPart = masterPart.GetPartsOfType<SlideLayoutPart>().First();

            var slideIdList = new P.SlideIdList();
            uint slideId = 256;
            uint relIndex = 1;

            // Title slide.
            AddSlide(presentationPart, layoutPart, slideIdList, ref slideId, ref relIndex,
                content.Title, [$"Generated {content.GeneratedUtc.ToLocalTime():g}", content.PeriodLabel]);

            // One slide per section.
            foreach (ReportSection section in content.Sections)
                AddSlide(presentationPart, layoutPart, slideIdList, ref slideId, ref relIndex,
                    section.Heading, BodyLines(section));

            presentationPart.Presentation.Append(
                new P.SlideMasterIdList(new P.SlideMasterId
                {
                    Id = 2147483648U,
                    RelationshipId = presentationPart.GetIdOfPart(masterPart)
                }),
                slideIdList,
                new P.SlideSize { Cx = 9144000, Cy = 6858000 },
                new P.NotesSize { Cx = 6858000, Cy = 9144000 });
            presentationPart.Presentation.Save();
        }

        return stream.ToArray();
    }

    private static IReadOnlyList<string> BodyLines(ReportSection section)
    {
        var lines = new List<string>(section.Lines);
        if (section.Table is { } table)
        {
            lines.Add(string.Join("   |   ", table.Columns));
            foreach (IReadOnlyList<string> row in table.Rows)
                lines.Add(string.Join("   |   ", row));
        }
        return lines.Count > 0 ? lines : [" "];
    }

    private static void AddSlide(
        PresentationPart presentationPart, SlideLayoutPart layoutPart, P.SlideIdList slideIdList,
        ref uint slideId, ref uint relIndex, string title, IReadOnlyList<string> body)
    {
        var slidePart = presentationPart.AddNewPart<SlidePart>($"rIdSlide{relIndex}");
        slidePart.Slide = new P.Slide(
            new P.CommonSlideData(
                new P.ShapeTree(
                    new P.NonVisualGroupShapeProperties(
                        new P.NonVisualDrawingProperties { Id = 1U, Name = "" },
                        new P.NonVisualGroupShapeDrawingProperties(),
                        new P.ApplicationNonVisualDrawingProperties()),
                    new P.GroupShapeProperties(new D.TransformGroup()),
                    TextShape(2U, "Title", title, 457200, 274638, 8229600, 1143000, bold: true, size: 2800),
                    TextShape(3U, "Body", null, 457200, 1600200, 8229600, 4525963, bold: false, size: 1400, body))),
            new P.ColorMapOverride(new D.MasterColorMapping()));
        slidePart.AddPart(layoutPart);

        slideIdList.Append(new P.SlideId
        {
            Id = slideId++,
            RelationshipId = presentationPart.GetIdOfPart(slidePart)
        });
        relIndex++;
    }

    private static P.Shape TextShape(
        uint id, string name, string? singleLine, long x, long y, long cx, long cy,
        bool bold, int size, IReadOnlyList<string>? lines = null)
    {
        var body = new P.TextBody(new D.BodyProperties(), new D.ListStyle());
        IEnumerable<string> content = lines ?? [singleLine ?? ""];
        foreach (string line in content)
        {
            body.Append(new D.Paragraph(
                new D.Run(
                    new D.RunProperties { Bold = bold, FontSize = size, Language = "en-US" },
                    new D.Text(line))));
        }

        return new P.Shape(
            new P.NonVisualShapeProperties(
                new P.NonVisualDrawingProperties { Id = id, Name = name },
                new P.NonVisualShapeDrawingProperties(new D.ShapeLocks { NoGrouping = true }),
                new P.ApplicationNonVisualDrawingProperties()),
            new P.ShapeProperties(
                new D.Transform2D(
                    new D.Offset { X = x, Y = y },
                    new D.Extents { Cx = cx, Cy = cy }),
                new D.PresetGeometry(new D.AdjustValueList()) { Preset = D.ShapeTypeValues.Rectangle }),
            body);
    }

    private static SlideMasterPart CreateSlideMaster(PresentationPart presentationPart)
    {
        var masterPart = presentationPart.AddNewPart<SlideMasterPart>("rIdMaster");
        var layoutPart = masterPart.AddNewPart<SlideLayoutPart>("rIdLayout");

        layoutPart.SlideLayout = new P.SlideLayout(
            new P.CommonSlideData(new P.ShapeTree(
                new P.NonVisualGroupShapeProperties(
                    new P.NonVisualDrawingProperties { Id = 1U, Name = "" },
                    new P.NonVisualGroupShapeDrawingProperties(),
                    new P.ApplicationNonVisualDrawingProperties()),
                new P.GroupShapeProperties(new D.TransformGroup()))),
            new P.ColorMapOverride(new D.MasterColorMapping()))
        { Type = P.SlideLayoutValues.Blank };

        masterPart.SlideMaster = new P.SlideMaster(
            new P.CommonSlideData(new P.ShapeTree(
                new P.NonVisualGroupShapeProperties(
                    new P.NonVisualDrawingProperties { Id = 1U, Name = "" },
                    new P.NonVisualGroupShapeDrawingProperties(),
                    new P.ApplicationNonVisualDrawingProperties()),
                new P.GroupShapeProperties(new D.TransformGroup()))),
            new P.ColorMap
            {
                Background1 = D.ColorSchemeIndexValues.Light1,
                Text1 = D.ColorSchemeIndexValues.Dark1,
                Background2 = D.ColorSchemeIndexValues.Light2,
                Text2 = D.ColorSchemeIndexValues.Dark2,
                Accent1 = D.ColorSchemeIndexValues.Accent1,
                Accent2 = D.ColorSchemeIndexValues.Accent2,
                Accent3 = D.ColorSchemeIndexValues.Accent3,
                Accent4 = D.ColorSchemeIndexValues.Accent4,
                Accent5 = D.ColorSchemeIndexValues.Accent5,
                Accent6 = D.ColorSchemeIndexValues.Accent6,
                Hyperlink = D.ColorSchemeIndexValues.Hyperlink,
                FollowedHyperlink = D.ColorSchemeIndexValues.FollowedHyperlink
            },
            new P.SlideLayoutIdList(new P.SlideLayoutId
            {
                Id = 2147483649U,
                RelationshipId = masterPart.GetIdOfPart(layoutPart)
            }));

        masterPart.SlideMaster.Append(MinimalTheme());
        AddTheme(masterPart);
        return masterPart;
    }

    // A slide master needs a theme part; create a minimal one.
    private static void AddTheme(SlideMasterPart masterPart)
    {
        var themePart = masterPart.AddNewPart<ThemePart>("rIdTheme");
        themePart.Theme = new D.Theme(
            new D.ThemeElements(
                new D.ColorScheme(
                    new D.Dark1Color(new D.SystemColor { Val = D.SystemColorValues.WindowText }),
                    new D.Light1Color(new D.SystemColor { Val = D.SystemColorValues.Window }),
                    new D.Dark2Color(new D.RgbColorModelHex { Val = "1F497D" }),
                    new D.Light2Color(new D.RgbColorModelHex { Val = "EEECE1" }),
                    new D.Accent1Color(new D.RgbColorModelHex { Val = "4F81BD" }),
                    new D.Accent2Color(new D.RgbColorModelHex { Val = "C0504D" }),
                    new D.Accent3Color(new D.RgbColorModelHex { Val = "9BBB59" }),
                    new D.Accent4Color(new D.RgbColorModelHex { Val = "8064A2" }),
                    new D.Accent5Color(new D.RgbColorModelHex { Val = "4BACC6" }),
                    new D.Accent6Color(new D.RgbColorModelHex { Val = "F79646" }),
                    new D.Hyperlink(new D.RgbColorModelHex { Val = "0000FF" }),
                    new D.FollowedHyperlinkColor(new D.RgbColorModelHex { Val = "800080" }))
                { Name = "Office" },
                new D.FontScheme(
                    new D.MajorFont(new D.LatinFont { Typeface = "Calibri" }, new D.EastAsianFont { Typeface = "" }, new D.ComplexScriptFont { Typeface = "" }),
                    new D.MinorFont(new D.LatinFont { Typeface = "Calibri" }, new D.EastAsianFont { Typeface = "" }, new D.ComplexScriptFont { Typeface = "" }))
                { Name = "Office" },
                new D.FormatScheme(
                    new D.FillStyleList(new D.SolidFill(new D.SchemeColor { Val = D.SchemeColorValues.PhColor }), new D.SolidFill(new D.SchemeColor { Val = D.SchemeColorValues.PhColor }), new D.SolidFill(new D.SchemeColor { Val = D.SchemeColorValues.PhColor })),
                    new D.LineStyleList(new D.Outline(new D.SolidFill(new D.SchemeColor { Val = D.SchemeColorValues.PhColor })), new D.Outline(new D.SolidFill(new D.SchemeColor { Val = D.SchemeColorValues.PhColor })), new D.Outline(new D.SolidFill(new D.SchemeColor { Val = D.SchemeColorValues.PhColor }))),
                    new D.EffectStyleList(new D.EffectStyle(new D.EffectList()), new D.EffectStyle(new D.EffectList()), new D.EffectStyle(new D.EffectList())),
                    new D.BackgroundFillStyleList(new D.SolidFill(new D.SchemeColor { Val = D.SchemeColorValues.PhColor }), new D.SolidFill(new D.SchemeColor { Val = D.SchemeColorValues.PhColor }), new D.SolidFill(new D.SchemeColor { Val = D.SchemeColorValues.PhColor })))
                { Name = "Office" }))
        { Name = "Office Theme" };
    }

    private static P.TextStyles MinimalTheme() => new(new P.TitleStyle(), new P.BodyStyle(), new P.OtherStyle());
}
