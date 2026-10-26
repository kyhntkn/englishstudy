using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
        string[] heads = { "★", "Tür", "İngilizce", "Türkçe", "Örnek cümle", "Örnek çeviri", "Kategori", "Seviye", "Sözcük türü", "Not", "Kutu" };
        float[] widths = { 0.4f, 0.8f, 2f, 2f, 3f, 3f, 1.2f, 0.8f, 1.1f, 1.8f, 0.5f };
        Document.Create(doc => doc.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(20);
            page.DefaultTextStyle(x => x.FontSize(8.5f));
            page.Header().Text("English Study - Kelime ve Cümle Listesi").FontSize(15).Bold();
            page.Content().PaddingTop(8).Table(t =>
            {
                t.ColumnsDefinition(c => { foreach (var w in widths) c.RelativeColumn(w); });
                t.Header(h =>
                {
                    foreach (var s in heads)
                        h.Cell().Background(Colors.Blue.Lighten4).Border(0.5f).Padding(3).Text(s).Bold();
                });
                foreach (var it in items)
                {
                    string[] v = { it.Fav ? "★" : "", it.Kind, it.Text, it.Translation, it.Example, it.ExampleTr,
                                   it.Category, it.Level, it.Pos, it.Note, it.Box.ToString() };
                    foreach (var s in v) t.Cell().Border(0.5f).Padding(3).Text(s ?? "");
                }
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

public static class Pkg
{
    public const string TaskId = "EnglishDefterimStartup";
    static bool? _p;

    public static bool IsPackaged
    {
        get
        {
            if (_p == null)
            {
                try { _ = Windows.ApplicationModel.Package.Current.Id; _p = true; }
                catch { _p = false; }
            }
            return _p.Value;
        }
    }

    static bool On(Windows.ApplicationModel.StartupTaskState s) =>
        s == Windows.ApplicationModel.StartupTaskState.Enabled || s == Windows.ApplicationModel.StartupTaskState.EnabledByPolicy;

    public static async Task<bool> GetStartupEnabled()
    {
        var t = await Windows.ApplicationModel.StartupTask.GetAsync(TaskId);
        return On(t.State);
    }

    public static async Task<bool> SetStartup(bool enable)
    {
        var t = await Windows.ApplicationModel.StartupTask.GetAsync(TaskId);
        if (enable) return On(await t.RequestEnableAsync());
        t.Disable();
        return true;
    }
}
