using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace EnglishStudy;

public class ItemDialog : Window
{
    public Item Result;
    readonly ComboBox _kind = new(), _cat = new();
    readonly TextBox _t = new(), _tr = new(), _ex = new(), _extr = new();

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
        var all = new List<Cat> { new Cat { Id = 0, Name = "(Kategorisiz)" } }.Concat(cats).ToList();
        _cat.ItemsSource = all;
        _cat.SelectedItem = all.FirstOrDefault(c => c.Id == it.CategoryId) ?? all[0];
        foreach (var tb in new[] { _t, _tr, _ex, _extr }) { tb.TextWrapping = TextWrapping.Wrap; tb.MinHeight = 28; }
        _t.Text = it.Text; _tr.Text = it.Translation; _ex.Text = it.Example; _extr.Text = it.ExampleTr;
        Add("Tür", _kind);
        Add("İngilizce (kelime veya cümle)", _t);
        Add("Türkçe karşılığı", _tr);
        Add("Örnek cümle (İngilizce) - kelimeler için", _ex);
        Add("Örnek cümlenin Türkçesi", _extr);
        Add("Kategori", _cat);
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
                Example = _ex.Text.Trim(), ExampleTr = _extr.Text.Trim(), CategoryId = ((Cat)_cat.SelectedItem).Id
            };
            DialogResult = true;
        };
        var bp = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 14, 0, 0) };
        bp.Children.Add(ok); bp.Children.Add(cancel);
        sp.Children.Add(bp);
        Content = sp;
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
