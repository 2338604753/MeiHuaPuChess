using System.Text.Json;
using MeiHuaPuChess.Core.Models;

namespace MeiHuaPuChess.Data;

/// <summary>
/// 用户自定义棋局存储（保存到 %LocalAppData%/MeiHuaPuChess/）
/// </summary>
public class UserRecordStore
{
    private static readonly string DataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MeiHuaPuChess");

    private static readonly string RecordsPath = Path.Combine(DataDir, "user_records.json");
    private static readonly string FavoritesPath = Path.Combine(DataDir, "favorites.json");

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public UserRecordStore()
    {
        Directory.CreateDirectory(DataDir);
    }

    /// <summary>加载所有用户棋局</summary>
    public List<MeiHuaPuRecord> LoadAll()
    {
        if (!File.Exists(RecordsPath)) return new();
        try
        {
            var json = File.ReadAllText(RecordsPath);
            var wrapper = JsonSerializer.Deserialize<UserRecordWrapper>(json, JsonOpts);
            var records = wrapper?.Records ?? new();
            records.ForEach(r => r.Source = RecordSource.User);
            return records;
        }
        catch { return new(); }
    }

    /// <summary>保存所有用户棋局</summary>
    public void SaveAll(List<MeiHuaPuRecord> records)
    {
        var wrapper = new UserRecordWrapper { Records = records };
        var json = JsonSerializer.Serialize(wrapper, JsonOpts);
        File.WriteAllText(RecordsPath, json);
    }

    /// <summary>添加棋局（自动分配 ID）</summary>
    public MeiHuaPuRecord Add(MeiHuaPuRecord record)
    {
        var records = LoadAll();
        int maxId = 0;
        foreach (var r in records)
        {
            if (r.Id.StartsWith("MY-") && int.TryParse(r.Id[3..], out var n))
                maxId = Math.Max(maxId, n);
        }
        record.Id = $"MY-{maxId + 1:D3}";
        record.Source = RecordSource.User;
        records.Add(record);
        SaveAll(records);
        return record;
    }

    /// <summary>更新棋局</summary>
    public void Update(MeiHuaPuRecord record)
    {
        var records = LoadAll();
        var idx = records.FindIndex(r => r.Id == record.Id);
        if (idx >= 0)
        {
            records[idx] = record;
            SaveAll(records);
        }
    }

    /// <summary>删除棋局</summary>
    public void Delete(string id)
    {
        var records = LoadAll();
        records.RemoveAll(r => r.Id == id);
        SaveAll(records);
    }

    /// <summary>加载收藏列表（梅花谱 ID 列表）</summary>
    public HashSet<string> LoadFavorites()
    {
        if (!File.Exists(FavoritesPath)) return new();
        try
        {
            var json = File.ReadAllText(FavoritesPath);
            return JsonSerializer.Deserialize<HashSet<string>>(json, JsonOpts) ?? new();
        }
        catch { return new(); }
    }

    /// <summary>保存收藏列表</summary>
    public void SaveFavorites(HashSet<string> ids)
    {
        var json = JsonSerializer.Serialize(ids, JsonOpts);
        File.WriteAllText(FavoritesPath, json);
    }
}

internal class UserRecordWrapper
{
    public List<MeiHuaPuRecord> Records { get; set; } = new();
}
