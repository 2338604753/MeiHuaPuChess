using MeiHuaPuChess.Core.Enums;

namespace MeiHuaPuChess.Core.Models;

/// <summary>
/// 梅花谱棋局记录
/// </summary>
public class MeiHuaPuRecord
{
    /// <summary>
    /// 唯一标识，如 "MH-001"
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 棋局标题，如 "屏风马破当头炮·巡河车"
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 分类，如 "卷上"
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// 棋局说明
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 走法序列
    /// </summary>
    public List<MeiHuaPuMove> Moves { get; set; } = new();

    /// <summary>
    /// 总步数
    /// </summary>
    public int TotalSteps => Moves.Count;

    /// <summary>
    /// 来源：MeiHuaPu 或 User
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public RecordSource Source { get; set; } = RecordSource.MeiHuaPu;

    /// <summary>
    /// 是否已收藏
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsFavorite { get; set; }

    public override string ToString()
    {
        return $"[{Id}] {Title} ({TotalSteps}步)";
    }
}

public enum RecordSource
{
    MeiHuaPu,
    User
}

/// <summary>
/// 梅花谱中单步走法
/// </summary>
public class MeiHuaPuMove
{
    /// <summary>
    /// 步数序号（从1开始）
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("step")]
    public int StepNumber { get; set; }

    /// <summary>
    /// 走棋方
    /// </summary>
    public string Side { get; set; } = string.Empty; // "Red" or "Black" (from JSON)

    /// <summary>
    /// 走棋方（解析后）
    /// </summary>
    public Enums.Side MovingSide => Side == "Red" ? Enums.Side.Red : Enums.Side.Black;

    /// <summary>
    /// 传统记谱法，如 "炮二平五"
    /// </summary>
    public string Notation { get; set; } = string.Empty;

    /// <summary>
    /// 起始行
    /// </summary>
    public int FromRow { get; set; }

    /// <summary>
    /// 起始列
    /// </summary>
    public int FromCol { get; set; }

    /// <summary>
    /// 目标行
    /// </summary>
    public int ToRow { get; set; }

    /// <summary>
    /// 目标列
    /// </summary>
    public int ToCol { get; set; }

    /// <summary>
    /// 提示语（仅黑方走法有，供走错时提示）
    /// </summary>
    public List<string>? Hints { get; set; }

    public override string ToString()
    {
        return $"第{StepNumber}步 {MovingSide} {Notation} ({FromRow},{FromCol})→({ToRow},{ToCol})";
    }
}
