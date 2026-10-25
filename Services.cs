using System.Collections.Generic;
using System.Globalization;
using System.Speech.Synthesis;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EnglishStudy;

public static class Speaker
{
    static SpeechSynthesizer _s;

    public static void Say(string text)
    {
        try
        {
            if (_s == null)
            {
                _s = new SpeechSynthesizer();
                try { _s.SelectVoiceByHints(VoiceGender.NotSet, VoiceAge.NotSet, 0, new CultureInfo("en-US")); } catch { }
            }
            _s.SpeakAsyncCancelAll();
            _s.SpeakAsync(text);
        }
        catch { }
    }
}

public static class Exporter
{
    public static void Pdf(List<Item> items, string path)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        Document.Create(doc => doc.Page(page =>
        {
            page.Margin(28);
            page.DefaultTextStyle(x => x.FontSize(10));
            page.Header().Text("English Study - Kelime ve Cümle Listesi").FontSize(16).Bold();
            page.Content().PaddingTop(10).Table(t =>
            {
                t.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(2); c.RelativeColumn(2); c.RelativeColumn(3); c.RelativeColumn(3); c.RelativeColumn(1.2f);
                });
                t.Header(h =>
                {
                    foreach (var s in new[] { "İngilizce", "Türkçe", "Örnek cümle", "Örnek çeviri", "Kategori" })
                        h.Cell().Background(Colors.Blue.Lighten4).Border(0.5f).Padding(4).Text(s).Bold();
                });
                foreach (var it in items)
                    foreach (var s in new[] { it.Text, it.Translation, it.Example, it.ExampleTr, it.Category })
                        t.Cell().Border(0.5f).Padding(4).Text(s ?? "");
            });
            page.Footer().AlignCenter().Text(x =>
            {
                x.CurrentPageNumber();
                x.Span(" / ");
                x.TotalPages();
            });
        })).GeneratePdf(path);
    }
}
