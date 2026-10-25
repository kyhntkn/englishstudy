using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace EnglishStudy;

public class PopupWindow : Window
{
    readonly DispatcherTimer _close = new() { Interval = TimeSpan.FromSeconds(40) };

    public PopupWindow(Item it)
    {
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        Topmost = true;
        ShowInTaskbar = false;
        ShowActivated = false;
        SizeToContent = SizeToContent.Height;
        Width = 360;
        Background = Brushes.White;

        var sp = new StackPanel { Margin = new Thickness(16) };
        sp.Children.Add(new TextBlock { Text = it.Kind + (it.Category != "" ? "  •  " + it.Category : ""), Foreground = Brushes.Gray, FontSize = 12 });
        sp.Children.Add(new TextBlock { Text = it.Text, FontSize = 22, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 6, 0, 8) });

        var answer = new StackPanel { Visibility = Visibility.Collapsed };
        answer.Children.Add(new TextBlock { Text = it.Translation, FontSize = 18, Foreground = new SolidColorBrush(Color.FromRgb(0x25, 0x63, 0xEB)), TextWrapping = TextWrapping.Wrap });
        if (it.Example != "")
        {
            answer.Children.Add(new TextBlock { Text = it.Example, FontStyle = FontStyles.Italic, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 10, 0, 0) });
            if (it.ExampleTr != "")
                answer.Children.Add(new TextBlock { Text = it.ExampleTr, Foreground = Brushes.Gray, TextWrapping = TextWrapping.Wrap });
        }
        sp.Children.Add(answer);

        var show = new Button { Content = "Cevabı göster", Padding = new Thickness(12, 4, 12, 4), Margin = new Thickness(0, 0, 8, 0) };
        var closeBtn = new Button { Content = "Kapat", Padding = new Thickness(12, 4, 12, 4) };
        show.Click += (s, e) => { answer.Visibility = Visibility.Visible; show.IsEnabled = false; _close.Interval = TimeSpan.FromSeconds(60); _close.Stop(); _close.Start(); };
        closeBtn.Click += (s, e) => Close();
        var bp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 14, 0, 0) };
        bp.Children.Add(show); bp.Children.Add(closeBtn);
        sp.Children.Add(bp);

        Content = new Border { BorderBrush = new SolidColorBrush(Color.FromRgb(0x25, 0x63, 0xEB)), BorderThickness = new Thickness(2), Child = sp };

        SizeChanged += (s, e) => Place();
        Loaded += (s, e) => { Place(); _close.Start(); };
        _close.Tick += (s, e) => { _close.Stop(); Close(); };
        Closed += (s, e) => _close.Stop();
    }

    void Place()
    {
        var wa = SystemParameters.WorkArea;
        Left = wa.Right - Width - 12;
        Top = wa.Bottom - ActualHeight - 12;
    }
}
