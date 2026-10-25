using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace EnglishStudy;

public class StatsWindow : Window
{
    static long Cnt(string sql) => Convert.ToInt64(Db.Rows(sql)[0][0]);

    public StatsWindow()
    {
        Title = "İstatistikler";
        Width = 440; Height = 560;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var sb = new StringBuilder();
        string today = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        sb.AppendLine("Toplam kayıt: " + Cnt("SELECT COUNT(*) FROM Items"));
        sb.AppendLine("Favoriler: " + Cnt("SELECT COUNT(*) FROM Items WHERE Fav=1"));
        sb.AppendLine();
        sb.AppendLine("Öğrenme kutuları (5. kutu = iyi biliniyor):");
        for (int b = 1; b <= 5; b++)
            sb.AppendLine($"   Kutu {b}: " + Cnt($"SELECT COUNT(*) FROM Items WHERE IFNULL(Box,1)={b}"));

        var days = Db.Rows("SELECT Day,Correct,Wrong FROM Reviews ORDER BY Day DESC");
        var set = new HashSet<string>(days.Where(r => Convert.ToInt64(r[1]) + Convert.ToInt64(r[2]) > 0).Select(r => r[0].ToString()));
        var d = DateTime.Now.Date;
        if (!set.Contains(d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))) d = d.AddDays(-1);
        int streak = 0;
        while (set.Contains(d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))) { streak++; d = d.AddDays(-1); }

        var t = days.FirstOrDefault(r => r[0].ToString() == today);
        long tc = t == null ? 0 : Convert.ToInt64(t[1]), tw = t == null ? 0 : Convert.ToInt64(t[2]);
        sb.AppendLine();
        sb.AppendLine($"Bugün: {tc + tw} tekrar (doğru {tc}, yanlış {tw}) / hedef {Db.GetInt("goal", 20)}");
        sb.AppendLine($"Seri: {streak} gün");
        sb.AppendLine();
        sb.AppendLine("Son 7 gün:");
        foreach (var r in days.Take(7))
            sb.AppendLine($"   {r[0]}:  doğru {r[1]}, yanlış {r[2]}");
        sb.AppendLine();
        sb.AppendLine("En çok yanlış yapılanlar:");
        var wrong = Db.Rows("SELECT Text,Translation,Wrong FROM Items WHERE IFNULL(Wrong,0)>0 ORDER BY Wrong DESC LIMIT 10");
        if (wrong.Count == 0) sb.AppendLine("   (henüz yok)");
        foreach (var r in wrong) sb.AppendLine($"   {r[0]} = {r[1]}  ({r[2]} yanlış)");

        Content = new ScrollViewer { Content = new TextBlock { Text = sb.ToString(), Margin = new Thickness(16), FontSize = 14, TextWrapping = TextWrapping.Wrap } };
    }
}
