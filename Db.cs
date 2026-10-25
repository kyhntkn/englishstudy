using System;
using System.Collections.Generic;
using System.IO;
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
}

public class Cat
{
    public long Id;
    public string Name = "";
    public override string ToString() => Name;
}

public static class Db
{
    public static readonly string Dir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EnglishStudy");
    public static readonly string FilePath = Path.Combine(Dir, "data.db");

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

    public static void Init()
    {
        Directory.CreateDirectory(Dir);
        Exec(@"CREATE TABLE IF NOT EXISTS Categories(Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL UNIQUE);");
        Exec(@"CREATE TABLE IF NOT EXISTS Items(
                Id INTEGER PRIMARY KEY AUTOINCREMENT, Kind TEXT NOT NULL, Text TEXT NOT NULL,
                Translation TEXT NOT NULL, Example TEXT, ExampleTr TEXT, CategoryId INTEGER DEFAULT 0);");
        Exec(@"CREATE TABLE IF NOT EXISTS Settings(Key TEXT PRIMARY KEY, Value TEXT);");
    }

    public static string GetSetting(string key, string def)
    {
        using var c = Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT Value FROM Settings WHERE Key=@k";
        cmd.Parameters.AddWithValue("@k", key);
        var r = cmd.ExecuteScalar();
        return r == null || r is DBNull ? def : r.ToString();
    }

    public static void SetSetting(string key, string value) =>
        Exec("INSERT OR REPLACE INTO Settings(Key,Value) VALUES(@k,@v)", ("@k", key), ("@v", value));

    public static List<Cat> GetCats()
    {
        var list = new List<Cat>();
        using var c = Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT Id,Name FROM Categories ORDER BY Name COLLATE NOCASE";
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(new Cat { Id = r.GetInt64(0), Name = r.GetString(1) });
        return list;
    }

    public static void AddCat(string name) =>
        Exec("INSERT OR IGNORE INTO Categories(Name) VALUES(@n)", ("@n", name));

    public static void DeleteCat(long id)
    {
        Exec("UPDATE Items SET CategoryId=0 WHERE CategoryId=@i", ("@i", id));
        Exec("DELETE FROM Categories WHERE Id=@i", ("@i", id));
    }

    public static List<Item> Search(string q, string kind, long catId)
    {
        var list = new List<Item>();
        using var c = Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = @"SELECT i.Id,i.Kind,i.Text,i.Translation,IFNULL(i.Example,''),IFNULL(i.ExampleTr,''),
                i.CategoryId,IFNULL(c.Name,'') FROM Items i LEFT JOIN Categories c ON c.Id=i.CategoryId
            WHERE (@q='' OR i.Text LIKE @l OR i.Translation LIKE @l OR i.Example LIKE @l OR i.ExampleTr LIKE @l)
              AND (@k='' OR i.Kind=@k) AND (@c=0 OR i.CategoryId=@c)
            ORDER BY i.Id DESC";
        q = q ?? "";
        cmd.Parameters.AddWithValue("@q", q);
        cmd.Parameters.AddWithValue("@l", "%" + q + "%");
        cmd.Parameters.AddWithValue("@k", kind ?? "");
        cmd.Parameters.AddWithValue("@c", catId);
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new Item
            {
                Id = r.GetInt64(0), Kind = r.GetString(1), Text = r.GetString(2), Translation = r.GetString(3),
                Example = r.GetString(4), ExampleTr = r.GetString(5), CategoryId = r.GetInt64(6), Category = r.GetString(7)
            });
        return list;
    }

    public static void Save(Item it)
    {
        if (it.Id == 0)
            Exec("INSERT INTO Items(Kind,Text,Translation,Example,ExampleTr,CategoryId) VALUES(@k,@t,@tr,@e,@et,@c)",
                ("@k", it.Kind), ("@t", it.Text), ("@tr", it.Translation), ("@e", it.Example), ("@et", it.ExampleTr), ("@c", it.CategoryId));
        else
            Exec("UPDATE Items SET Kind=@k,Text=@t,Translation=@tr,Example=@e,ExampleTr=@et,CategoryId=@c WHERE Id=@i",
                ("@k", it.Kind), ("@t", it.Text), ("@tr", it.Translation), ("@e", it.Example), ("@et", it.ExampleTr), ("@c", it.CategoryId), ("@i", it.Id));
    }

    public static void Delete(long id) => Exec("DELETE FROM Items WHERE Id=@i", ("@i", id));

    public static void Backup(string path)
    {
        if (File.Exists(path)) File.Delete(path);
        Exec("VACUUM INTO '" + path.Replace("'", "''") + "'");
    }

    public static void Restore(string path)
    {
        SqliteConnection.ClearAllPools();
        File.Copy(path, FilePath, true);
        Init();
    }
}
