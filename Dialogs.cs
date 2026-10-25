using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace EnglishStudy;

public class ItemDialog : Window
{
    public Item Result;
    readonly ComboBox _kind = new(), _cat = new(), _lvl = new(), _pos = new();
    readonly TextBox _t = new(), _tr = new(), _ex = new(), _extr = new(), _note = new();

    public ItemDialog(Item it, List<Cat> cats)
    {
        Title = it.Id == 0 ? "Yeni kayıt" : "Düzenle";
        Width = 460;
        SizeToContent = SizeToContent.Height;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var sp = new StackPanel { Margin = new Thickness(14) };
        void Add(string label, Control c)
        {
            sp.Children.Add(new TextBlock { Text = label, Margin = new Thickness(0, 8, 0, 2) });
            sp.Children.Add(c);
        }
        _kind.ItemsSource = new[] { "Kelime", "Cümle" };
        _kind.SelectedItem = it.Kind;
        _lvl.ItemsSource = new[] { "", "A1", "A2", "B1", "B2", "C1", "C2" };
        _lvl.SelectedItem = it.Level ?? "";
        _pos.ItemsSource = new[] { "", "noun", "verb", "adjective", "adverb", "preposition", "phrasal verb", "idiom", "other" };
        _pos.SelectedItem = it.Pos ?? "";
        var all = new List<Cat> { new Cat { Id = 0, Name = "(Kategorisiz)" } }.Concat(cats).ToList();
        _cat.ItemsSource = all;
        _cat.DisplayMemberPath = "Name";
        _cat.SelectedItem = all.FirstOrDefault(c => c.Id == it.CategoryId) ?? all[0];
        foreach (var tb in new[] { _t, _tr, _ex, _extr, _note }) { tb.TextWrapping = TextWrapping.Wrap; tb.MinHeight = 28; }
        _t.Text = it.Text; _tr.Text = it.Translation; _ex.Text = it.Example; _extr.Text = it.ExampleTr; _note.Text = it.Note;
        Add("Tür", _kind);
        Add("İngilizce (kelime veya cümle)", _t);
        Add("Türkçe karşılığı", _tr);
        Add("Örnek cümle (İngilizce) - kelimeler için", _ex);
        Add("Örnek cümlenin Türkçesi", _extr);
        Add("Kategori", _cat);
        Add("Seviye", _lvl);
        Add("Sözcük türü", _pos);
        Add("Not", _note);
        var ok = new Button { Content = "Kaydet", IsDefault = true, Padding = new Thickness(16, 4, 16, 4), Margin = new Thickness(0, 0, 8, 0) };
        var cancel = new Button { Content = "İptal", IsCancel = true, Padding = new Thickness(16, 4, 16, 4) };
        ok.Click += (s, e) =>
        {
            if (string.IsNullOrWhiteSpace(_t.Text) || string.IsNullOrWhiteSpace(_tr.Text))
            {
                MessageBox.Show(this, "İngilizce ve Türkçe alanları boş olamaz."); return;
            }
            Result = new Item
            {
                Id = it.Id, Kind = (string)_kind.SelectedItem, Text = _t.Text.Trim(), Translation = _tr.Text.Trim(),
                Example = _ex.Text.Trim(), ExampleTr = _extr.Text.Trim(), CategoryId = ((Cat)_cat.SelectedItem).Id,
                Level = (string)_lvl.SelectedItem ?? "", Pos = (string)_pos.SelectedItem ?? "", Note = _note.Text.Trim()
            };
            DialogResult = true;
        };
        var bp = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 14, 0, 0) };
        bp.Children.Add(ok); bp.Children.Add(cancel);
        sp.Children.Add(bp);
        Content = new ScrollViewer { Content = sp, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        SizeToContent = SizeToContent.Manual;
        Height = 720;
        Loaded += (s, e) => _t.Focus();
    }
}

public static class Prompt
{
    public static string Ask(Window owner, string title)
    {
        var w = new Window
        {
            Title = title, Width = 320, SizeToContent = SizeToContent.Height, Owner = owner,
            WindowStartupLocation = WindowStartupLocation.CenterOwner, ResizeMode = ResizeMode.NoResize
        };
        var tb = new TextBox { Margin = new Thickness(12, 12, 12, 6) };
        var ok = new Button { Content = "Tamam", IsDefault = true, Margin = new Thickness(12, 0, 12, 12), Padding = new Thickness(14, 3, 14, 3), HorizontalAlignment = HorizontalAlignment.Right };
        ok.Click += (s, e) => w.DialogResult = true;
        var sp = new StackPanel();
        sp.Children.Add(tb); sp.Children.Add(ok);
        w.Content = sp;
        w.Loaded += (s, e) => tb.Focus();
        return w.ShowDialog() == true && !string.IsNullOrWhiteSpace(tb.Text) ? tb.Text.Trim() : null;
    }
}

public class SettingsWindow : Window
{
    public SettingsWindow()
    {
        Title = "Ayarlar";
        Width = 380;
        SizeToContent = SizeToContent.Height;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var hours = Enumerable.Range(0, 24).ToList();
        var quiet = new CheckBox { Content = "Sessiz saatlerde bildirim gösterme", IsChecked = Db.GetInt("quiet_on", 0) == 1 };
        var from = new ComboBox { ItemsSource = hours, SelectedItem = Db.GetInt("quiet_from", 22), Width = 60 };
        var to = new ComboBox { ItemsSource = hours, SelectedItem = Db.GetInt("quiet_to", 8), Width = 60 };
        var auto = new CheckBox { Content = "Haftalık otomatik yedek (son 5 yedek saklanır)", IsChecked = Db.GetInt("autobackup", 1) == 1, Margin = new Thickness(0, 14, 0, 0) };
        var goal = new TextBox { Text = Db.GetInt("goal", 20).ToString(), Width = 60, HorizontalAlignment = HorizontalAlignment.Left };
        var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 6, 0, 0) };
        row.Children.Add(new TextBlock { Text = "Başlangıç saati ", VerticalAlignment = VerticalAlignment.Center });
        row.Children.Add(from);
        row.Children.Add(new TextBlock { Text = "   Bitiş saati ", VerticalAlignment = VerticalAlignment.Center });
        row.Children.Add(to);
        var ok = new Button { Content = "Kaydet", IsDefault = true, Padding = new Thickness(16, 4, 16, 4), HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 16, 0, 0) };
        ok.Click += (s, e) =>
        {
            Db.SetSetting("quiet_on", quiet.IsChecked == true ? "1" : "0");
            Db.SetSetting("quiet_from", from.SelectedItem.ToString());
            Db.SetSetting("quiet_to", to.SelectedItem.ToString());
            Db.SetSetting("autobackup", auto.IsChecked == true ? "1" : "0");
            int g = int.TryParse(goal.Text, out var v) ? v : 20;
            Db.SetSetting("goal", System.Math.Clamp(g, 1, 1000).ToString());
            DialogResult = true;
        };
        var sp = new StackPanel { Margin = new Thickness(16) };
        sp.Children.Add(quiet); sp.Children.Add(row); sp.Children.Add(auto);
        sp.Children.Add(new TextBlock { Text = "Günlük hedef (tekrar sayısı)", Margin = new Thickness(0, 14, 0, 2) });
        sp.Children.Add(goal); sp.Children.Add(ok);
        Content = sp;
    }
}
