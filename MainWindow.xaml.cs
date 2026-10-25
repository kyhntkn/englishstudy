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
    readonly Random _rnd = new();
    System.Windows.Forms.NotifyIcon _tray;
    List<Item> _bag = new();
    PopupWindow _popup;
    bool _ready, _exiting;

    public MainWindow()
    {
        InitializeComponent();
        Db.Init();
        KindFilter.ItemsSource = new[] { "Tümü", "Kelime", "Cümle" };
        KindFilter.SelectedIndex = 0;
        IntervalBox.ItemsSource = new[] { "Kapalı", "1 dk", "5 dk", "10 dk", "15 dk", "30 dk", "60 dk" };
        int saved = int.TryParse(Db.GetSetting("interval", "0"), out var m) ? m : 0;
        IntervalBox.SelectedIndex = Math.Max(0, Array.IndexOf(_mins, saved));
        _timer.Tick += (s, e) => ShowRandom();
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
        RemCat.ItemsSource = list;
        CatFilter.SelectedItem = list.FirstOrDefault(c => c.Id == f) ?? list[0];
        RemCat.SelectedItem = list.FirstOrDefault(c => c.Id == r) ?? list[0];
        _ready = was;
        _bag.Clear();
    }

    void Reload()
    {
        if (!_ready) return;
        string kind = KindFilter.SelectedIndex <= 0 ? "" : (string)KindFilter.SelectedItem;
        var items = Db.Search(SearchBox.Text.Trim(), kind, SelCatId(CatFilter));
        Grid.ItemsSource = items;
        CountText.Text = items.Count + " kayıt";
    }

    void Filter_Changed(object s, RoutedEventArgs e) => Reload();

    void Add_Click(object s, RoutedEventArgs e)
    {
        var dlg = new ItemDialog(new Item { CategoryId = SelCatId(CatFilter) }, Db.GetCats()) { Owner = this };
        if (dlg.ShowDialog() == true) { Db.Save(dlg.Result); _bag.Clear(); Reload(); }
    }

    void Edit_Click(object s, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not Item it) return;
        var dlg = new ItemDialog(it, Db.GetCats()) { Owner = this };
        if (dlg.ShowDialog() == true) { Db.Save(dlg.Result); _bag.Clear(); Reload(); }
    }

    void Delete_Click(object s, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not Item it) return;
        if (MessageBox.Show(this, $"'{it.Text}' silinsin mi?", "Sil", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
        Db.Delete(it.Id); _bag.Clear(); Reload();
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
        if (id == 0) { MessageBox.Show(this, "Önce soldaki listeden silinecek kategoriyi seçin."); return; }
        if (MessageBox.Show(this, "Kategori silinsin mi? Kayıtlar silinmez, kategorisiz kalır.", "Sil", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
        Db.DeleteCat(id); LoadCats(); Reload();
    }

    // ---- Hatırlatma ----
    void ApplyTimer()
    {
        int m = _mins[Math.Max(0, IntervalBox.SelectedIndex)];
        _timer.Stop();
        if (m > 0) { _timer.Interval = TimeSpan.FromMinutes(m); _timer.Start(); }
        Db.SetSetting("interval", m.ToString());
    }

    void Interval_Changed(object s, SelectionChangedEventArgs e) { if (_ready) ApplyTimer(); }
    void RemCat_Changed(object s, SelectionChangedEventArgs e) { if (_ready) _bag.Clear(); }
    void ShowNow_Click(object s, RoutedEventArgs e) => ShowRandom();

    void ShowRandom()
    {
        if (_bag.Count == 0)
            _bag = Db.Search("", "", SelCatId(RemCat)).OrderBy(_ => _rnd.Next()).ToList();
        if (_bag.Count == 0) return;
        var it = _bag[^1];
        _bag.RemoveAt(_bag.Count - 1);
        _popup?.Close();
        var p = new PopupWindow(it);
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

    // ---- Dışa aktarma / yedek ----
    void Export_Click(object s, RoutedEventArgs e)
    {
        var dlg = new SaveFileDialog { Filter = "Excel (*.xlsx)|*.xlsx", FileName = "kelimeler.xlsx" };
        if (dlg.ShowDialog(this) != true) return;
        var items = Db.Search("", "", 0);
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Kayıtlar");
        string[] heads = { "Tür", "İngilizce", "Türkçe", "Örnek cümle", "Örnek çeviri", "Kategori" };
        for (int i = 0; i < heads.Length; i++) { ws.Cell(1, i + 1).Value = heads[i]; ws.Cell(1, i + 1).Style.Font.Bold = true; }
        for (int r = 0; r < items.Count; r++)
        {
            var it = items[r];
            ws.Cell(r + 2, 1).Value = it.Kind; ws.Cell(r + 2, 2).Value = it.Text; ws.Cell(r + 2, 3).Value = it.Translation;
            ws.Cell(r + 2, 4).Value = it.Example; ws.Cell(r + 2, 5).Value = it.ExampleTr; ws.Cell(r + 2, 6).Value = it.Category;
        }
        ws.Columns().AdjustToContents();
        wb.SaveAs(dlg.FileName);
        MessageBox.Show(this, "Excel dosyası kaydedildi.");
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

    void ShowMain() { Show(); WindowState = WindowState.Normal; Activate(); }

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
