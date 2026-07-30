using MeiHuaPuChess.Core.Enums;

namespace MeiHuaPuChess.Core.Models;

/// <summary>
/// 棋子
/// </summary>
public class ChessPiece
{
    public PieceType Type { get; set; }
    public Side Side { get; set; }
    public int Row { get; set; }
    public int Col { get; set; }
    public bool IsAlive { get; set; } = true;

    /// <summary>
    /// 棋子显示的汉字
    /// </summary>
    public string DisplayChar => Type switch
    {
        PieceType.Shuai  => "帅",
        PieceType.Shi    => "仕",
        PieceType.Xiang  => "相",
        PieceType.Ju     => "車",
        PieceType.Ma     => "馬",
        PieceType.Pao    => "砲",
        PieceType.Bing   => "兵",
        PieceType.Jiang  => "將",
        PieceType.Shi2   => "士",
        PieceType.Xiang2 => "象",
        PieceType.Ju2    => "車",
        PieceType.Ma2    => "馬",
        PieceType.Pao2   => "炮",
        PieceType.Zu     => "卒",
        _ => "?"
    };

    public ChessPiece Clone()
    {
        return new ChessPiece
        {
            Type = Type,
            Side = Side,
            Row = Row,
            Col = Col,
            IsAlive = IsAlive
        };
    }

    public override string ToString()
    {
        return $"{Side} {DisplayChar} ({Row},{Col})";
    }
}
