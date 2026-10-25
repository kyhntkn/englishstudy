using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;

namespace EnglishStudy;

public class Item
{
    public long Id { get; set; }
    public string Kind { get; set; } = "Kelime";
    public string Text { get; set; } = "";
    public string Translation { get; set; } = "";
    public string Example { get; set; } = "";
    public string ExampleTr { get; set; } = "";
    public long CategoryId { get; set; }
    public string Category { get; set; } = "";
    public string Level { get; set; } = "";
    public string Pos { get; set; } = "";
    public string Note { get; set; } = "";
    public int Box { get; set; } = 1;
    public string Next { get; set; } = "";
    public int Correct { get; set; }
    public int Wrong { get; set; }
    public bool Fav { get; set; }
    public string FavMark => Fav ? "★" : "";

    public bool IsDue(DateTime now) =>
        !DateTime.TryParse(Next, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) || d <= now;
}

public class Cat
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public override string ToString() => Name;
}

public static class Db
{
    public static readonly string Dir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EnglishStudy");
    public static readonly string FilePath = Path.Combine(Dir, "data.db");

    const string Cols = @"i.Id,i.Kind,i.Text,i.Translation,IFNULL(i.Example,''),IFNULL(i.ExampleTr,''),i.CategoryId,IFNULL(c.Name,''),
        IFNULL(i.Level,''),IFNULL(i.Pos,''),IFNULL(i.Note,''),IFNULL(i.Box,1),IFNULL(i.Next,''),IFNULL(i.Correct,0),IFNULL(i.Wrong,0),IFNULL(i.Fav,0)";

    static SqliteConnection Open()
    {
        var c = new SqliteConnection("Data Source=" + FilePath);
        c.Open();
        return c;
    }

    static void Exec(string sql, params (string, object)[] p)
    {
        using var c = Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (n, v) in p) cmd.Parameters.AddWithValue(n, v ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public static List<object[]> Rows(string sql)
    {
        var l = new List<object[]>();
        using var c = Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = sql;
        using var r = cmd.ExecuteReader();
        while (r.Read()) { var a = new object[r.FieldCount]; r.GetValues(a); l.Add(a); }
        return l;
    }

    static bool HasCol(string table, string col)
    {
        foreach (var r in Rows($"PRAGMA table_info({table})"))
            if (string.Equals(r[1]?.ToString(), col, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    static void AddCol(string table, string col, string def)
    {
        if (!HasCol(table, col)) Exec($"ALTER TABLE {table} ADD COLUMN {col} {def}");
    }

    public static void Init()
    {
        Directory.CreateDirectory(Dir);
        Exec("CREATE TABLE IF NOT EXISTS Categories(Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL UNIQUE);");
        Exec(@"CREATE TABLE IF NOT EXISTS Items(
                Id INTEGER PRIMARY KEY AUTOINCREMENT, Kind TEXT NOT NULL, Text TEXT NOT NULL,
                Translation TEXT NOT NULL, Example TEXT, ExampleTr TEXT, CategoryId INTEGER DEFAULT 0);");
        Exec("CREATE TABLE IF NOT EXISTS Settings(Key TEXT PRIMARY KEY, Value TEXT);");
        Exec("CREATE TABLE IF NOT EXISTS Reviews(Day TEXT PRIMARY KEY, Correct INTEGER DEFAULT 0, Wrong INTEGER DEFAULT 0);");
        AddCol("Items", "Level", "TEXT DEFAULT ''");
        AddCol("Items", "Pos", "TEXT DEFAULT ''");
        AddCol("Items", "Note", "TEXT DEFAULT ''");
        AddCol("Items", "Box", "INTEGER DEFAULT 1");
        AddCol("Items", "Next", "TEXT DEFAULT ''");
        AddCol("Items", "Correct", "INTEGER DEFAULT 0");
        AddCol("Items", "Wrong", "INTEGER DEFAULT 0");
        AddCol("Items", "Fav", "INTEGER DEFAULT 0");
    }

    public static string GetSetting(string key, string def)
    {
        var r = Rows($"SELECT Value FROM Settings WHERE Key='{key.Replace("'", "''")}'");
        return r.Count == 0 || r[0][0] is DBNull ? def : r[0][0].ToString();
    }

    public static int GetInt(string key, int def) =>
        int.TryParse(GetSetting(key, def.ToString()), out var v) ? v : def;

    public static void SetSetting(string key, string value) =>
        Exec("INSERT OR REPLACE INTO Settings(Key,Value) VALUES(@k,@v)", ("@k", key), ("@v", value));

    public static List<Cat> GetCats() =>
        Rows("SELECT Id,Name FROM Categories ORDER BY Name COLLATE NOCASE")
            .Select(r => new Cat { Id = Convert.ToInt64(r[0]), Name = r[1].ToString() }).ToList();

    public static void AddCat(string name) =>
        Exec("INSERT OR IGNORE INTO Categories(Name) VALUES(@n)", ("@n", name));

    public static long EnsureCat(string name)
    {
        AddCat(name);
        return GetCats().First(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)).Id;
    }

    public static void DeleteCat(long id)
    {
        Exec("UPDATE Items SET CategoryId=0 WHERE CategoryId=@i", ("@i", id));
        Exec("DELETE FROM Categories WHERE Id=@i", ("@i", id));
    }

    public static List<Item> Search(string q, string kind, long catId, bool favOnly = false)
    {
        var list = new List<Item>();
        using var c = Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT " + Cols + @" FROM Items i LEFT JOIN Categories c ON c.Id=i.CategoryId
            WHERE (@q='' OR i.Text LIKE @l OR i.Translation LIKE @l OR i.Example LIKE @l OR i.ExampleTr LIKE @l OR i.Note LIKE @l)
              AND (@k='' OR i.Kind=@k) AND (@c=0 OR i.CategoryId=@c) AND (@f=0 OR i.Fav=1)
            ORDER BY i.Id DESC";
        q = q ?? "";
        cmd.Parameters.AddWithValue("@q", q);
        cmd.Parameters.AddWithValue("@l", "%" + q + "%");
        cmd.Parameters.AddWithValue("@k", kind ?? "");
        cmd.Parameters.AddWithValue("@c", catId);
        cmd.Parameters.AddWithValue("@f", favOnly ? 1 : 0);
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new Item
            {
                Id = r.GetInt64(0), Kind = r.GetString(1), Text = r.GetString(2), Translation = r.GetString(3),
                Example = r.GetString(4), ExampleTr = r.GetString(5), CategoryId = r.GetInt64(6), Category = r.GetString(7),
                Level = r.GetString(8), Pos = r.GetString(9), Note = r.GetString(10), Box = (int)r.GetInt64(11),
                Next = r.GetString(12), Correct = (int)r.GetInt64(13), Wrong = (int)r.GetInt64(14), Fav = r.GetInt64(15) != 0
            });
        return list;
    }

    public static bool Exists(string kind, string text)
    {
        using var c = Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Items WHERE Kind=@k AND Text=@t COLLATE NOCASE";
        cmd.Parameters.AddWithValue("@k", kind);
        cmd.Parameters.AddWithValue("@t", text);
        return Convert.ToInt64(cmd.ExecuteScalar()) > 0;
    }

    public static void Save(Item it)
    {
        if (it.Id == 0)
            Exec(@"INSERT INTO Items(Kind,Text,Translation,Example,ExampleTr,CategoryId,Level,Pos,Note,Box,Next,Correct,Wrong,Fav)
                   VALUES(@k,@t,@tr,@e,@et,@c,@lv,@p,@n,1,'',0,0,0)",
                ("@k", it.Kind), ("@t", it.Text), ("@tr", it.Translation), ("@e", it.Example), ("@et", it.ExampleTr),
                ("@c", it.CategoryId), ("@lv", it.Level ?? ""), ("@p", it.Pos ?? ""), ("@n", it.Note ?? ""));
        else
            Exec(@"UPDATE Items SET Kind=@k,Text=@t,Translation=@tr,Example=@e,ExampleTr=@et,CategoryId=@c,Level=@lv,Pos=@p,Note=@n WHERE Id=@i",
                ("@k", it.Kind), ("@t", it.Text), ("@tr", it.Translation), ("@e", it.Example), ("@et", it.ExampleTr),
                ("@c", it.CategoryId), ("@lv", it.Level ?? ""), ("@p", it.Pos ?? ""), ("@n", it.Note ?? ""), ("@i", it.Id));
    }

    public static void SetFav(long id, bool fav) =>
        Exec("UPDATE Items SET Fav=@f WHERE Id=@i", ("@f", fav ? 1 : 0), ("@i", id));

    public static void Delete(long id) => Exec("DELETE FROM Items WHERE Id=@i", ("@i", id));

    // Leitner: doğru -> bir üst kutu, yanlış -> 1. kutu
    public static void Review(Item it, bool ok)
    {
        int[] days = { 0, 0, 1, 3, 7, 14 };
        int box = ok ? Math.Min(5, it.Box + 1) : 1;
        var next = ok ? DateTime.Now.AddDays(days[box]) : DateTime.Now;
        string nextS = next.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        Exec("UPDATE Items SET Box=@b,Next=@n,Correct=IFNULL(Correct,0)+@c,Wrong=IFNULL(Wrong,0)+@w WHERE Id=@i",
            ("@b", box), ("@n", nextS), ("@c", ok ? 1 : 0), ("@w", ok ? 0 : 1), ("@i", it.Id));
        string day = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        Exec("INSERT OR IGNORE INTO Reviews(Day,Correct,Wrong) VALUES(@d,0,0)", ("@d", day));
        Exec("UPDATE Reviews SET Correct=Correct+@c,Wrong=Wrong+@w WHERE Day=@d", ("@c", ok ? 1 : 0), ("@w", ok ? 0 : 1), ("@d", day));
        it.Box = box; it.Next = nextS;
        if (ok) it.Correct++; else it.Wrong++;
    }

    public static void Backup(string path)
    {
        if (File.Exists(path)) File.Delete(path);
        Exec("VACUUM INTO '" + path.Replace("'", "''") + "'");
    }

    public static void AutoBackup()
    {
        var dir = Path.Combine(Dir, "Backups");
        Directory.CreateDirectory(dir);
        var files = new DirectoryInfo(dir).GetFiles("auto-*.db").OrderByDescending(f => f.CreationTime).ToList();
        if (files.Count > 0 && (DateTime.Now - files[0].CreationTime).TotalDays < 7) return;
        Backup(Path.Combine(dir, $"auto-{DateTime.Now:yyyyMMdd-HHmm}.db"));
        foreach (var f in files.Skip(4)) { try { f.Delete(); } catch { } }
    }

    public static void Restore(string path)
    {
        SqliteConnection.ClearAllPools();
        File.Copy(path, FilePath, true);
        Init();
    }
}
