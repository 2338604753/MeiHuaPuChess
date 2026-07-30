using MeiHuaPuChess.Core.Enums;
using MeiHuaPuChess.Core.Models;

namespace MeiHuaPuChess.Core.Engine;

/// <summary>
/// 将军检测器
/// </summary>
public class CheckDetector
{
    private readonly ChessBoard _board;
    private readonly MoveValidator _validator;

    public CheckDetector(ChessBoard board)
    {
        _board = board;
        _validator = new MoveValidator(board);
    }

    /// <summary>
    /// 判断某方是否被将军
    /// </summary>
    public bool IsInCheck(Side side)
    {
        var king = _board.FindKing(side);
        if (king == null) return true; // 将被吃掉了（不应该发生）

        return IsSquareAttacked(king.Row, king.Col, GetOppositeSide(side));
    }

    /// <summary>
    /// 判断某方是否被将死
    /// </summary>
    public bool IsCheckmated(Side side)
    {
        if (!IsInCheck(side)) return false;

        // 检查是否有合法走法可以解除将军
        var moveGenerator = new MoveGenerator(_board);
        var moves = moveGenerator.GenerateAllLegalMoves(side);

        return moves.Count == 0;
    }

    /// <summary>
    /// 判断某个位置是否被指定方攻击
    /// </summary>
    public bool IsSquareAttacked(int row, int col, Side attackerSide)
    {
        var enemyPieces = _board.GetAlivePieces(attackerSide);

        foreach (var piece in enemyPieces)
        {
            if (_validator.IsValidBasicMove(piece.Row, piece.Col, row, col))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 判断是否"对面"（两个将/帅在同一列且之间无子遮挡）
    /// </summary>
    public bool IsKingsFacing()
    {
        var redKing = _board.FindKing(Side.Red);
        var blackKing = _board.FindKing(Side.Black);

        if (redKing == null || blackKing == null) return false;

        // 必须在同一列
        if (redKing.Col != blackKing.Col) return false;

        // 检查之间是否有棋子
        int minRow = Math.Min(redKing.Row, blackKing.Row);
        int maxRow = Math.Max(redKing.Row, blackKing.Row);

        for (int r = minRow + 1; r < maxRow; r++)
        {
            if (_board[redKing.Col, r] != null) return false;
        }

        return true; // 对面了！
    }

    private static Side GetOppositeSide(Side side)
    {
        return side == Side.Red ? Side.Black : Side.Red;
    }
}
