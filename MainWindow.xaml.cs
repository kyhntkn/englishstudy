using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ClosedXML.Excel;
using Microsoft.Win32;

namespace EnglishStudy;

public partial class MainWindow : Window
{
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    readonly int[] _mins = { 0, 1, 5, 10, 15, 30, 60 };
    readonly DispatcherTimer _timer = new();
    readonly DispatcherTimer _snooze = new() { Interval = TimeSpan.FromMinutes(5) };
    readonly Random _rnd = new();
    System.Windows.Forms.NotifyIcon _tray;
    PopupWindow _popup;
    Item _snoozeItem;
    long _lastId = -1;
    bool _ready, _exiting;

    public MainWindow()
    {
        InitializeComponent();
        Db.Init();
        try { if (Db.GetInt("autobackup", 1) == 1) Db.AutoBackup(); } catch { }
        KindFilter.ItemsSource = new[] { "Tümü", "Kelime", "Cümle" };
        KindFilter.SelectedIndex = 0;
        LevelFilter.ItemsSource = new[] { "Tüm seviyeler", "A1", "A2", "B1", "B2", "C1", "C2" };
        LevelFilter.SelectedIndex = 0;
        IntervalBox.ItemsSource = new[] { "Kapalı", "1 dk", "5 dk", "10 dk", "15 dk", "30 dk", "60 dk" };
        IntervalBox.SelectedIndex = Math.Max(0, Array.IndexOf(_mins, Db.GetInt("interval", 0)));
        _timer.Tick += (s, e) => { if (!InQuiet()) ShowRandom(); };
        _snooze.Tick += (s, e) => { _snooze.Stop(); if (_snoozeItem != null) ShowPopup(_snoozeItem); };
        LoadCats();
        AutoStart.IsChecked = Registry.CurrentUser.OpenSubKey(RunKey)?.GetValue("EnglishStudy") != null;
        SetupTray();
        _ready = true;
        ApplyTimer();
        Reload();
        if (Environment.GetCommandLineArgs().Contains("--tray")) Loaded += (s, e) => Hide();
    }

    static long SelCatId(ComboBox cb) => (cb.SelectedItem as Cat)?.Id ?? 0;

    void LoadCats()
    {
        long f = SelCatId(CatFilter), r = SelCatId(RemCat);
        var list = new List<Cat> { new Cat { Id = 0, Name = "Tüm kategoriler" } };
        list.AddRange(Db.GetCats());
        bool was = _ready;
        _ready = false;
        CatFilter.ItemsSource = list;
        RemCat.ItemsSource = list.ToList();
        CatFilter.SelectedItem = list.FirstOrDefault(c => c.Id == f) ?? list[0];
        RemCat.SelectedItem = ((List<Cat>)RemCat.ItemsSource).FirstOrDefault(c => c.Id == r) ?? ((List<Cat>)RemCat.ItemsSource)[0];
        _ready = was;
    }

    void Reload()
    {
        if (!_ready) return;
        string kind = KindFilter.SelectedIndex <= 0 ? "" : (string)KindFilter.SelectedItem;
        var items = Db.Search(SearchBox.Text.Trim(), kind, SelCatId(CatFilter), FavOnly.IsChecked == true,
            LevelFilter.SelectedIndex <= 0 ? "" : (string)LevelFilter.SelectedItem);
        Grid.ItemsSource = items;
        CountText.Text = items.Count + " kayıt";
    }

    void Filter_Changed(object s, RoutedEventArgs e) => Reload();

    void Add_Click(object s, RoutedEventArgs e)
    {
        var dlg = new ItemDialog(new Item { CategoryId = SelCatId(CatFilter) }, Db.GetCats()) { Owner = this };
        if (dlg.ShowDialog() == true) { Db.Save(dlg.Result); Reload(); }
    }

    void Edit_Click(object s, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not Item it) return;
        var dlg = new ItemDialog(it, Db.GetCats()) { Owner = this };
        if (dlg.ShowDialog() == true) { Db.Save(dlg.Result); Reload(); }
    }

    void Delete_Click(object s, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not Item it) return;
        if (MessageBox.Show(this, $"'{it.Text}' silinsin mi?", "Sil", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
        Db.Delete(it.Id); Reload();
    }

    void Fav_Click(object s, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not Item it) return;
        Db.SetFav(it.Id, !it.Fav); Reload();
    }

    void Speak_Click(object s, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is Item it) Speaker.Say(it.Text);
    }

    void AddCat_Click(object s, RoutedEventArgs e)
    {
        var name = Prompt.Ask(this, "Yeni kategori adı");
        if (name == null) return;
        Db.AddCat(name); LoadCats(); Reload();
    }

    void DelCat_Click(object s, RoutedEventArgs e)
    {
        long id = SelCatId(CatFilter);
        if (id == 0) { MessageBox.Show(this, "Önce üstteki kategori listesinden silinecek kategoriyi seçin."); return; }
        if (MessageBox.Show(this, "Kategori silinsin mi? Kayıtlar silinmez, kategorisiz kalır.", "Sil", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
        Db.DeleteCat(id); LoadCats(); Reload();
    }

    void Quiz_Click(object s, RoutedEventArgs e) { new QuizWindow { Owner = this }.ShowDialog(); Reload(); }
    void Stats_Click(object s, RoutedEventArgs e) => new StatsWindow { Owner = this }.ShowDialog();
    void Settings_Click(object s, RoutedEventArgs e) => new SettingsWindow { Owner = this }.ShowDialog();

    // ---- Hatırlatma ----
    bool InQuiet()
    {
        if (Db.GetInt("quiet_on", 0) != 1) return false;
        int f = Db.GetInt("quiet_from", 22), t = Db.GetInt("quiet_to", 8), h = DateTime.Now.Hour;
        return f <= t ? (h >= f && h < t) : (h >= f || h < t);
    }

    void ApplyTimer()
    {
        int m = _mins[Math.Max(0, IntervalBox.SelectedIndex)];
        _timer.Stop();
        if (m == 0) { _snooze.Stop(); _snoozeItem = null; }
        if (m > 0) { _timer.Interval = TimeSpan.FromMinutes(m); _timer.Start(); }
        Db.SetSetting("interval", m.ToString());
    }

    void Interval_Changed(object s, SelectionChangedEventArgs e) { if (_ready) ApplyTimer(); }
    void RemCat_Changed(object s, SelectionChangedEventArgs e) { }
    void ShowNow_Click(object s, RoutedEventArgs e) => ShowRandom();

    // Zamanı gelen (aralıklı tekrar) kayıtlar önce, yoksa rastgele
    void ShowRandom()
    {
        var all = Db.Search("", "", SelCatId(RemCat));
        if (all.Count == 0) return;
        var now = DateTime.Now;
        var cand = all.Where(i => i.Id != _lastId).ToList();
        if (cand.Count == 0) cand = all;
        var due = cand.Where(i => i.IsDue(now)).ToList();
        var pool = due.Count > 0 ? due : cand;
        var it = pool[_rnd.Next(pool.Count)];
        _lastId = it.Id;
        ShowPopup(it);
    }

    void ShowPopup(Item it)
    {
        _popup?.Close();
        PopupWindow p = null;
        p = new PopupWindow(it, act =>
        {
            if (act == "know") Db.Review(it, true);
            else if (act == "dont") Db.Review(it, false);
            else if (act == "snooze") { _snoozeItem = it; _snooze.Stop(); _snooze.Start(); }
            Reload();
        });
        p.Closed += (s, e) => { if (_popup == p) _popup = null; };
        _popup = p;
        p.Show();
    }

    // ---- Windows ile başlat ----
    void AutoStart_Changed(object s, RoutedEventArgs e)
    {
        if (!_ready) return;
        using var k = Registry.CurrentUser.OpenSubKey(RunKey, true);
        if (AutoStart.IsChecked == true) k.SetValue("EnglishStudy", "\"" + Environment.ProcessPath + "\" --tray");
        else k.DeleteValue("EnglishStudy", false);
    }

    // ---- Dışa / içe aktarma, yedek ----
    List<Item> Shown() => Grid.ItemsSource as List<Item> ?? new List<Item>();

    void Export_Click(object s, RoutedEventArgs e)
    {
        var dlg = new SaveFileDialog { Filter = "Excel (*.xlsx)|*.xlsx", FileName = "kelimeler.xlsx" };
        if (dlg.ShowDialog(this) != true) return;
        var items = Shown();
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Kayıtlar");
        string[] heads = { "Tür", "İngilizce", "Türkçe", "Örnek cümle", "Örnek çeviri", "Kategori", "Seviye", "Sözcük türü", "Not" };
        for (int i = 0; i < heads.Length; i++) { ws.Cell(1, i + 1).Value = heads[i]; ws.Cell(1, i + 1).Style.Font.Bold = true; }
        for (int r = 0; r < items.Count; r++)
        {
            var it = items[r];
            string[] v = { it.Kind, it.Text, it.Translation, it.Example, it.ExampleTr, it.Category, it.Level, it.Pos, it.Note };
            for (int c = 0; c < v.Length; c++) ws.Cell(r + 2, c + 1).Value = v[c];
        }
        ws.Columns().AdjustToContents();
        wb.SaveAs(dlg.FileName);
        MessageBox.Show(this, $"{items.Count} kayıt Excel dosyasına aktarıldı (ekranda görünen liste).");
    }

    void Pdf_Click(object s, RoutedEventArgs e)
    {
        var dlg = new SaveFileDialog { Filter = "PDF (*.pdf)|*.pdf", FileName = "kelimeler.pdf" };
        if (dlg.ShowDialog(this) != true) return;
        try { Exporter.Pdf(Shown(), dlg.FileName); MessageBox.Show(this, "PDF kaydedildi (ekranda görünen liste)."); }
        catch (Exception ex) { MessageBox.Show(this, "PDF hatası: " + ex.Message); }
    }

    void Import_Click(object s, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = "Excel (*.xlsx)|*.xlsx" };
        if (dlg.ShowDialog(this) != true) return;
        int added = 0, skipped = 0;
        try
        {
            using var wb = new XLWorkbook(dlg.FileName);
            var ws = wb.Worksheet(1);
            foreach (var row in ws.RowsUsed().Skip(1))
            {
                string S(int c) => row.Cell(c).GetString().Trim();
                string text = S(2), tr = S(3);
                if (text == "" || tr == "") { skipped++; continue; }
                string kind = S(1).StartsWith("C", StringComparison.OrdinalIgnoreCase) ? "Cümle" : "Kelime";
                if (Db.Exists(kind, text)) { skipped++; continue; }
                long cat = S(6) == "" ? 0 : Db.EnsureCat(S(6));
                Db.Save(new Item { Kind = kind, Text = text, Translation = tr, Example = S(4), ExampleTr = S(5), CategoryId = cat, Level = S(7), Pos = S(8), Note = S(9) });
                added++;
            }
        }
        catch (Exception ex) { MessageBox.Show(this, "İçe aktarma hatası: " + ex.Message); return; }
        LoadCats(); Reload();
        MessageBox.Show(this, $"{added} kayıt eklendi, {skipped} satır atlandı (boş veya zaten var).\nSütun sırası: Tür, İngilizce, Türkçe, Örnek cümle, Örnek çeviri, Kategori, Seviye, Sözcük türü, Not");
    }

    void Backup_Click(object s, RoutedEventArgs e)
    {
        var dlg = new SaveFileDialog { Filter = "Yedek (*.db)|*.db", FileName = $"english-study-yedek-{DateTime.Now:yyyyMMdd}.db" };
        if (dlg.ShowDialog(this) != true) return;
        Db.Backup(dlg.FileName);
        MessageBox.Show(this, "Yedek alındı.");
    }

    void Restore_Click(object s, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = "Yedek (*.db)|*.db" };
        if (dlg.ShowDialog(this) != true) return;
        if (MessageBox.Show(this, "Mevcut veriler yedekteki verilerle değiştirilecek. Devam edilsin mi?", "Geri yükle", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
        Db.Restore(dlg.FileName); LoadCats(); Reload();
    }

    // ---- Sistem tepsisi ----
    void SetupTray()
    {
        _tray = new System.Windows.Forms.NotifyIcon
        {
            Icon = System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath),
            Visible = true, Text = "English Study"
        };
        var menu = new System.Windows.Forms.ContextMenuStrip();
        menu.Items.Add("Aç", null, (s, e) => ShowMain());
        menu.Items.Add("Şimdi göster", null, (s, e) => ShowRandom());
        menu.Items.Add("Çıkış", null, (s, e) => Exit());
        _tray.ContextMenuStrip = menu;
        _tray.DoubleClick += (s, e) => ShowMain();
    }

    public void ShowMain() { Show(); WindowState = WindowState.Normal; Activate(); }

    void Exit()
    {
        _exiting = true;
        _tray.Visible = false;
        _tray.Dispose();
        Application.Current.Shutdown();
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (!_exiting) { e.Cancel = true; Hide(); }
        base.OnClosing(e);
    }
}
