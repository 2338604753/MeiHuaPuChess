using System.Text.Json;
using MeiHuaPuChess.Core.Models;
using MeiHuaPuChess.Core.Services;

namespace MeiHuaPuChess.Data;

/// <summary>
/// 梅花谱数据加载器
/// </summary>
public class MeiHuaPuDataLoader : IMeiHuaPuDataLoader
{
    /// <summary>
    /// 从 JSON 加载所有梅花谱记录
    /// </summary>
    public List<MeiHuaPuRecord> LoadAllRecords()
    {
        // 从嵌入资源加载 JSON
        var json = GetEmbeddedJson();
        return ParseJson(json);
    }

    /// <summary>
    /// 获取嵌入的 JSON 数据
    /// </summary>
    private string GetEmbeddedJson()
    {
        // 尝试从文件系统加载
        var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "meiHuaPu_records.json");
        if (File.Exists(filePath))
        {
            return File.ReadAllText(filePath);
        }

        // 尝试从相对路径加载（开发环境）
        var devPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..",
                                   "MeiHuaPuChess.Data", "meiHuaPu_records.json");
        if (File.Exists(devPath))
        {
            return File.ReadAllText(devPath);
        }

        // 回退：使用硬编码数据
        return GetFallbackData();
    }

    private List<MeiHuaPuRecord> ParseJson(string json)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var wrapper = JsonSerializer.Deserialize<MeiHuaPuDataWrapper>(json, options);
        return wrapper?.Records ?? new List<MeiHuaPuRecord>();
    }

    /// <summary>
    /// 兜底数据：硬编码一局经典梅花谱
    /// </summary>
    private string GetFallbackData()
    {
        return @"{
  ""records"": [
    {
      ""id"": ""MH-001"",
      ""title"": ""屏风马破当头炮·巡河车"",
      ""category"": ""卷上·第一局"",
      ""description"": ""红方当头炮巡河车，黑方屏风马应对。此局展示屏风马如何应对巡河车的标准走法。"",
      ""moves"": [
        {""step"":1,""side"":""Red"",""notation"":""炮二平五"",""fromRow"":7,""fromCol"":1,""toRow"":7,""toCol"":4},
        {""step"":2,""side"":""Black"",""notation"":""馬8進7"",""fromRow"":9,""fromCol"":1,""toRow"":7,""toCol"":2,""hints"":[""跳马保中卒，这是屏风马的关键第一步"",""应走馬8進7""]},
        {""step"":3,""side"":""Red"",""notation"":""馬二進三"",""fromRow"":7,""fromCol"":7,""toRow"":5,""toCol"":6},
        {""step"":4,""side"":""Black"",""notation"":""車9平8"",""fromRow"":9,""fromCol"":0,""toRow"":9,""toCol"":1,""hints"":[""出车抓炮抢先手"",""应走車9平8""]},
        {""step"":5,""side"":""Red"",""notation"":""車一平二"",""fromRow"":9,""fromCol"":8,""toRow"":9,""toCol"":7},
        {""step"":6,""side"":""Black"",""notation"":""卒7進1"",""fromRow"":6,""fromCol"":6,""toRow"":5,""toCol"":6,""hints"":[""挺7卒活通马路"",""应走卒7進1""]},
        {""step"":7,""side"":""Red"",""notation"":""車二進六"",""fromRow"":9,""fromCol"":7,""toRow"":4,""toCol"":7},
        {""step"":8,""side"":""Black"",""notation"":""馬2進3"",""fromRow"":9,""fromCol"":7,""toRow"":7,""toCol"":6,""hints"":[""跳右马形成屏风马阵型"",""应走馬2進3""]},
        {""step"":9,""side"":""Red"",""notation"":""兵七進一"",""fromRow"":7,""fromCol"":6,""toRow"":6,""toCol"":6},
        {""step"":10,""side"":""Black"",""notation"":""卒7進1"",""fromRow"":5,""fromCol"":6,""toRow"":4,""toCol"":6,""hints"":[""卒7進1过河威胁红车"",""应走卒7進1""]},
        {""step"":11,""side"":""Red"",""notation"":""車二平三"",""fromRow"":4,""fromCol"":7,""toRow"":4,""toCol"":6},
        {""step"":12,""side"":""Black"",""notation"":""馬7進6"",""fromRow"":7,""fromCol"":2,""toRow"":5,""toCol"":3,""hints"":[""马7进6踩车反先"",""应走馬7進6""]},
        {""step"":13,""side"":""Red"",""notation"":""車三退二"",""fromRow"":4,""fromCol"":6,""toRow"":6,""toCol"":6},
        {""step"":14,""side"":""Black"",""notation"":""炮8平7"",""fromRow"":7,""fromCol"":1,""toRow"":7,""toCol"":0,""hints"":[""炮8平7打车，准备反击"",""应走炮8平7""]}
      ]
    }
  ]
}";
    }
}

/// <summary>
/// JSON 反序列化包装类
/// </summary>
internal class MeiHuaPuDataWrapper
{
    public List<MeiHuaPuRecord> Records { get; set; } = new();
}
