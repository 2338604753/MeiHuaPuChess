using MeiHuaPuChess.Core.Enums;

namespace MeiHuaPuChess.Core.Models;

/// <summary>
/// 一步走法记录
/// </summary>
public class Move
{
    public int FromRow { get; set; }
    public int FromCol { get; set; }
    public int ToRow { get; set; }
    public int ToCol { get; set; }
    public ChessPiece Piece { get; set; } = null!;
    public ChessPiece? CapturedPiece { get; set; }

    /// <summary>
    /// 中国传统记谱法字符串，如 "炮二平五"
    /// </summary>
    public string Notation { get; set; } = string.Empty;

    /// <summary>
    /// 是否为将军
    /// </summary>
    public bool IsCheck { get; set; }

    /// <summary>
    /// 是否为将死
    /// </summary>
    public bool IsCheckmate { get; set; }

    /// <summary>
    /// 走棋方
    /// </summary>
    public Side Side => Piece.Side;

    public override string ToString()
    {
        var capture = CapturedPiece != null ? $"吃{CapturedPiece.DisplayChar}" : "";
        return $"{Notation}{capture}";
    }
}
