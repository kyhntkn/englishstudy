using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace EnglishStudy;

public class PopupWindow : Window
{
    readonly DispatcherTimer _close = new() { Interval = TimeSpan.FromSeconds(45) };
    bool _done;

    public PopupWindow(Item it, Action<string> callback)
    {
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        Topmost = true;
        ShowInTaskbar = false;
        ShowActivated = false;
        SizeToContent = SizeToContent.Height;
        Width = 380;
        Background = Brushes.White;

        void Finish(string act) { if (_done) return; _done = true; if (act != "") callback(act); Close(); }

        var meta = new[] { it.Kind, it.Pos, it.Level, it.Category }.Where(x => !string.IsNullOrEmpty(x));
        var sp = new StackPanel { Margin = new Thickness(16) };
        sp.Children.Add(new TextBlock { Text = string.Join("  •  ", meta) + (it.Fav ? "  ★" : ""), Foreground = Brushes.Gray, FontSize = 12 });
        sp.Children.Add(new TextBlock { Text = it.Text, FontSize = 22, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 6, 0, 8) });

        var answer = new StackPanel { Visibility = Visibility.Collapsed };
        answer.Children.Add(new TextBlock { Text = it.Translation, FontSize = 18, Foreground = new SolidColorBrush(Color.FromRgb(0x25, 0x63, 0xEB)), TextWrapping = TextWrapping.Wrap });
        if (it.Example != "")
        {
            answer.Children.Add(new TextBlock { Text = it.Example, FontStyle = FontStyles.Italic, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 10, 0, 0) });
            if (it.ExampleTr != "")
                answer.Children.Add(new TextBlock { Text = it.ExampleTr, Foreground = Brushes.Gray, TextWrapping = TextWrapping.Wrap });
        }
        if (it.Note != "")
            answer.Children.Add(new TextBlock { Text = "Not: " + it.Note, Foreground = Brushes.DimGray, FontSize = 12, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0) });
        var know = new Button { Content = "Biliyorum", Padding = new Thickness(12, 4, 12, 4), Margin = new Thickness(0, 0, 8, 0), Background = new SolidColorBrush(Color.FromRgb(0xD1, 0xFA, 0xE5)) };
        var dont = new Button { Content = "Bilmiyorum", Padding = new Thickness(12, 4, 12, 4), Background = new SolidColorBrush(Color.FromRgb(0xFE, 0xE2, 0xE2)) };
        know.Click += (s, e) => Finish("know");
        dont.Click += (s, e) => Finish("dont");
        var kp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 12, 0, 0) };
        kp.Children.Add(know); kp.Children.Add(dont);
        answer.Children.Add(kp);
        sp.Children.Add(answer);

        var show = new Button { Content = "Cevabı göster", Padding = new Thickness(10, 4, 10, 4), Margin = new Thickness(0, 0, 6, 0) };
        var speak = new Button { Content = "🔊", Padding = new Thickness(8, 4, 8, 4), Margin = new Thickness(0, 0, 6, 0), ToolTip = "Seslendir" };
        var snooze = new Button { Content = "Ertele (5 dk)", Padding = new Thickness(10, 4, 10, 4), Margin = new Thickness(0, 0, 6, 0) };
        var closeBtn = new Button { Content = "Kapat", Padding = new Thickness(10, 4, 10, 4) };
        show.Click += (s, e) => { answer.Visibility = Visibility.Visible; show.IsEnabled = false; _close.Stop(); _close.Interval = TimeSpan.FromSeconds(90); _close.Start(); };
        speak.Click += (s, e) => Speaker.Say(it.Text);
        snooze.Click += (s, e) => Finish("snooze");
        closeBtn.Click += (s, e) => Finish("");
        var bp = new WrapPanel { Margin = new Thickness(0, 14, 0, 0) };
        bp.Children.Add(show); bp.Children.Add(speak); bp.Children.Add(snooze); bp.Children.Add(closeBtn);
        sp.Children.Add(bp);

        Content = new Border { BorderBrush = new SolidColorBrush(Color.FromRgb(0x25, 0x63, 0xEB)), BorderThickness = new Thickness(2), Child = sp };

        SizeChanged += (s, e) => Place();
        Loaded += (s, e) => { Place(); _close.Start(); };
        _close.Tick += (s, e) => { _close.Stop(); Finish(""); };
        Closed += (s, e) => _close.Stop();
    }

    void Place()
    {
        var wa = SystemParameters.WorkArea;
        Left = wa.Right - Width - 12;
        Top = wa.Bottom - ActualHeight - 12;
    }
}
