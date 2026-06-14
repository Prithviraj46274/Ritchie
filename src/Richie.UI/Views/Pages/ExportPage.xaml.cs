using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using Richie.Application.Reports;
using Richie.UI.Services;

namespace Richie.UI.Views.Pages;

public partial class ExportPage : Page
{
    public ExportPage() => InitializeComponent();

    private void OnExportPdf(object sender, RoutedEventArgs e) => Export("pdf");

    private void OnExportPptx(object sender, RoutedEventArgs e) => Export("pptx");

    private void Export(string format)
    {
        var services = ((App)System.Windows.Application.Current).Services;
        ReportContent content = services.GetRequiredService<IReportService>()
            .Build(new ReportRequest(ReportType.FullPortfolio, null, null, IncludeUnmaskedPasswords: false));

        var exporter = services.GetRequiredService<IReportExporter>();
        byte[] bytes = format == "pdf" ? exporter.ToPdf(content) : exporter.ToPptx(content);

        var dialog = new SaveFileDialog
        {
            FileName = $"richie-full-portfolio-{DateTime.Now:yyyyMMdd}.{format}",
            Filter = format == "pdf" ? "PDF document|*.pdf" : "PowerPoint|*.pptx"
        };
        if (dialog.ShowDialog(Window.GetWindow(this)) == true)
        {
            File.WriteAllBytes(dialog.FileName, bytes);
            services.GetRequiredService<ToastService>().Success($"{format.ToUpperInvariant()} exported.");
        }
    }
}
