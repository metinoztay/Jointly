using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Jointly.Models;

namespace Jointly.Services
{
    public class EventCardPdfService
    {
        public byte[] GenerateEventCardPdf(Event eventItem, string eventUrl, byte[] qrCodeBytes)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            // Load Jointly logo
            var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "logo.png");
            byte[] logoBytes = File.Exists(logoPath) ? File.ReadAllBytes(logoPath) : null;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    // A5 size (148mm x 210mm) - Portrait
                    page.Size(new PageSize(148, 210, Unit.Millimetre));
                    page.Margin(10);
                    page.PageColor(Colors.White);

                    page.Content()
                        // Outer thick border
                        .Border(3)
                        .BorderColor("#B76E79")
                        .Padding(5)
                        // Inner thin border
                        .Border(1)
                        .BorderColor("#E08175")
                        .Padding(10)
                        .AlignMiddle()
                        .Column(column =>
                        {
                            column.Spacing(8);

                            // Decorative top border
                            column.Item().AlignCenter().Width(75).LineHorizontal(1.5f).LineColor("#B76E79");

                            // Event Title (elegant, centered)
                            column.Item().PaddingTop(12).AlignCenter().Text(eventItem.Title)
                                .FontFamily("Georgia")
                                .FontSize(22)
                                .Bold()
                                .FontColor("#2C2C2C");

                            // Decorative divider with Jointly logo
                            if (logoBytes != null)
                            {
                                column.Item().PaddingTop(8).AlignCenter().Width(28).Height(28).Image(logoBytes);
                            }

                            // QR Code - centered with border
                            column.Item().PaddingTop(12).AlignCenter().Column(qrSection =>
                            {
                                qrSection.Item()
                                    .Border(3)
                                    .BorderColor("#B76E79")
                                    .Padding(12)
                                    .Width(140)
                                    .Height(140)
                                    .Image(qrCodeBytes);
                            });

                            // Decorative divider with Jointly logo
                            if (logoBytes != null)
                            {
                                column.Item().PaddingTop(8).AlignCenter().Width(28).Height(28).Image(logoBytes);
                            }

                            // Call to action text
                            column.Item().PaddingTop(12).AlignCenter().Text(text =>
                            {   
                                text.DefaultTextStyle(style => style.FontFamily("Georgia"));
                                text.Span(" ").FontSize(15).FontColor("#555555");
                                text.Span("Çektiğiniz ").FontSize(15).FontColor("#555555");
                                text.Span("video ve fotoğrafları").FontSize(15).Bold().FontColor("#2C2C2C");
                                text.Line("");
                                text.Span("bizimle paylaşmak ve bize ").FontSize(15).FontColor("#555555");
                                text.Span("sesli not").FontSize(15).Bold().FontColor("#2C2C2C");
                                text.Line("");
                                text.Span("bırakmak için QR kodu okutun!").FontSize(15).FontColor("#555555");
                                text.Span(" ").FontSize(15).FontColor("#555555");
                            });

                            // Event details
                            column.Item().PaddingTop(14).AlignCenter().Column(details =>
                            {
                                details.Item().Text(eventItem.EventDate.ToString("dd MMMM yyyy"))
                                    .FontFamily("Georgia")
                                    .FontSize(9)
                                    .Italic()
                                    .FontColor("#999999");
                            });

                            // Footer signature
                            column.Item().PaddingTop(16).AlignCenter().Text("— Jointly—")
                                .FontFamily("Georgia")
                                .FontSize(9)
                                .Italic()
                                .FontColor("#B76E79");

                            // Decorative bottom border
                            column.Item().PaddingTop(8).AlignCenter().Width(75).LineHorizontal(1.5f).LineColor("#B76E79");
                        });
                });
            });

            return document.GeneratePdf();
        }
    }
}
