using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace EnglishStudy;

public class QuizWindow : Window
{
    readonly Random _rnd = new();
    readonly List<Item> _all = Db.Search("", "", 0);
    List<Item> _qs = new();
    int _i, _score, _mode;
    string _correct = "";
    readonly ComboBox _modeBox = new();
    readonly TextBlock _info = new(), _q = new(), _fb = new();
    readonly StackPanel _opts = new();
    readonly TextBox _ans = new();
    readonly Button _check = new(), _next = new();

    public QuizWindow()
    {
        Title = "Quiz";
        Width = 540; Height = 560;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var sp = new StackPanel { Margin = new Thickness(16) };
        _modeBox.ItemsSource = new[]
        {
            "İngilizce → Türkçe (çoktan seçmeli)", "Türkçe → İngilizce (çoktan seçmeli)",
            "Türkçe → İngilizce (yazarak)", "Boşluk doldurma (örnek cümleden)"
        };
        _modeBox.SelectedIndex = 0;
        var start = new Button { Content = "Başlat (10 soru)", Margin = new Thickness(0, 8, 0, 12), Padding = new Thickness(12, 4, 12, 4), HorizontalAlignment = HorizontalAlignment.Left };
        start.Click += (s, e) => Start();
        _info.Foreground = Brushes.Gray;
        _q.FontSize = 22; _q.FontWeight = FontWeights.SemiBold; _q.TextWrapping = TextWrapping.Wrap; _q.Margin = new Thickness(0, 8, 0, 12);
        _q.Text = "Bir mod seçip Başlat'a bas. Zamanı gelen kayıtlar önce sorulur.";
        _fb.FontSize = 16; _fb.TextWrapping = TextWrapping.Wrap; _fb.Margin = new Thickness(0, 10, 0, 6);
        _ans.FontSize = 16; _ans.Visibility = Visibility.Collapsed;
        _ans.KeyDown += (s, e) => { if (e.Key == Key.Enter && _check.IsEnabled) Answer(_ans.Text); };
        _check.Content = "Kontrol et"; _check.Visibility = Visibility.Collapsed; _check.Margin = new Thickness(0, 8, 0, 0); _check.Padding = new Thickness(12, 4, 12, 4); _check.HorizontalAlignment = HorizontalAlignment.Left;
        _check.Click += (s, e) => Answer(_ans.Text);
        _next.Content = "Sonraki"; _next.IsEnabled = false; _next.Padding = new Thickness(12, 4, 12, 4); _next.HorizontalAlignment = HorizontalAlignment.Left; _next.Margin = new Thickness(0, 8, 0, 0);
        _next.Click += (s, e) => Next();
        foreach (UIElement u in new UIElement[] { _modeBox, start, _info, _q, _opts, _ans, _check, _fb, _next }) sp.Children.Add(u);
        Content = new ScrollViewer { Content = sp };
    }

    void Start()
    {
        _mode = _modeBox.SelectedIndex;
        if (_mode <= 1 && _all.Count < 4) { MessageBox.Show(this, "Çoktan seçmeli için en az 4 kayıt gerekir."); return; }
        IEnumerable<Item> pool = _all;
        if (_mode == 3) pool = pool.Where(x => x.Example != "" && x.Example.IndexOf(x.Text, StringComparison.OrdinalIgnoreCase) >= 0);
        var now = DateTime.Now;
        _qs = pool.OrderBy(x => x.IsDue(now) ? 0 : 1).ThenBy(_ => _rnd.Next()).Take(10).ToList();
        if (_qs.Count == 0) { MessageBox.Show(this, "Bu mod için uygun kayıt yok (örnek cümlesi olan kelimeler gerekir)."); return; }
        _i = 0; _score = 0;
        Show();
    }

    void Show()
    {
        var it = _qs[_i];
        _fb.Text = ""; _next.IsEnabled = false; _next.Content = "Sonraki";
        _info.Text = $"Soru {_i + 1}/{_qs.Count}   Puan: {_score}";
        _opts.Children.Clear(); _ans.Text = "";
        bool choice = _mode <= 1;
        _opts.Visibility = choice ? Visibility.Visible : Visibility.Collapsed;
        _ans.Visibility = _check.Visibility = choice ? Visibility.Collapsed : Visibility.Visible;
        _ans.IsEnabled = _check.IsEnabled = true;
        switch (_mode)
        {
            case 0: _q.Text = it.Text; _correct = it.Translation; break;
            case 1: _q.Text = it.Translation; _correct = it.Text; break;
            case 2: _q.Text = it.Translation; _correct = it.Text; break;
            default:
                _q.Text = Regex.Replace(it.Example, Regex.Escape(it.Text), "_____", RegexOptions.IgnoreCase)
                          + "\n(" + (it.ExampleTr != "" ? it.ExampleTr : it.Translation) + ")";
                _correct = it.Text; break;
        }
        if (choice)
        {
            var others = _all.Where(x => x.Id != it.Id).Select(x => _mode == 0 ? x.Translation : x.Text)
                .Where(x => !string.Equals(x, _correct, StringComparison.OrdinalIgnoreCase))
                .Distinct().OrderBy(_ => _rnd.Next()).Take(3).ToList();
            others.Add(_correct);
            foreach (var o in others.OrderBy(_ => _rnd.Next()).ToList())
            {
                var b = new Button { Content = o, Margin = new Thickness(0, 4, 0, 0), Padding = new Thickness(8), HorizontalContentAlignment = HorizontalAlignment.Left };
                b.Click += (s, e) => Answer(o);
                _opts.Children.Add(b);
            }
        }
        else _ans.Focus();
    }

    void Answer(string given)
    {
        var it = _qs[_i];
        bool ok = string.Equals((given ?? "").Trim(), _correct, StringComparison.OrdinalIgnoreCase);
        Db.Review(it, ok);
        if (ok) _score++;
        _fb.Text = ok ? "✔ Doğru!" : "✘ Doğrusu: " + _correct;
        _fb.Foreground = ok ? Brushes.Green : Brushes.Crimson;
        foreach (Button b in _opts.Children) b.IsEnabled = false;
        _check.IsEnabled = _ans.IsEnabled = false;
        _next.IsEnabled = true;
        _next.Content = _i == _qs.Count - 1 ? "Bitir" : "Sonraki";
        _next.Focus();
        _info.Text = $"Soru {_i + 1}/{_qs.Count}   Puan: {_score}";
    }

    void Next()
    {
        _i++;
        if (_i >= _qs.Count)
        {
            _q.Text = $"Bitti! Sonuç: {_score}/{_qs.Count}";
            _info.Text = ""; _fb.Text = "";
            _opts.Children.Clear();
            _ans.Visibility = _check.Visibility = Visibility.Collapsed;
            _next.IsEnabled = false;
            return;
        }
        Show();
    }
}
