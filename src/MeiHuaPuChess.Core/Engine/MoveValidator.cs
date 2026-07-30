using MeiHuaPuChess.Core.Enums;
using MeiHuaPuChess.Core.Models;

namespace MeiHuaPuChess.Core.Engine;

/// <summary>
/// 走法合法性校验器
/// </summary>
public class MoveValidator
{
    private readonly ChessBoard _board;

    public MoveValidator(ChessBoard board)
    {
        _board = board;
    }

    /// <summary>
    /// 校验某步走法是否合法（不考虑应将和送将）
    /// </summary>
    public bool IsValidBasicMove(int fromRow, int fromCol, int toRow, int toCol)
    {
        var piece = _board[fromRow, fromCol];
        if (piece == null) return false;

        // 不能走到己方棋子位置
        var target = _board[toRow, toCol];
        if (target != null && target.Side == piece.Side) return false;

        // 不能原地不动
        if (fromRow == toRow && fromCol == toCol) return false;

        return piece.Type switch
        {
            PieceType.Shuai or PieceType.Jiang => IsValidKingMove(piece, fromRow, fromCol, toRow, toCol),
            PieceType.Shi or PieceType.Shi2     => IsValidAdvisorMove(piece, fromRow, fromCol, toRow, toCol),
            PieceType.Xiang or PieceType.Xiang2 => IsValidElephantMove(piece, fromRow, fromCol, toRow, toCol),
            PieceType.Ma or PieceType.Ma2       => IsValidKnightMove(fromRow, fromCol, toRow, toCol),
            PieceType.Ju or PieceType.Ju2       => IsValidRookMove(fromRow, fromCol, toRow, toCol),
            PieceType.Pao or PieceType.Pao2     => IsValidCannonMove(fromRow, fromCol, toRow, toCol),
            PieceType.Bing or PieceType.Zu      => IsValidPawnMove(piece, fromRow, fromCol, toRow, toCol),
            _ => false
        };
    }

    /// <summary>
    /// 校验走法是否完全合法（包括不能送将和应将）
    /// </summary>
    public bool IsValidFullMove(int fromRow, int fromCol, int toRow, int toCol)
    {
        var piece = _board[fromRow, fromCol];
        if (piece == null) return false;

        // 基本走法检查
        if (!IsValidBasicMove(fromRow, fromCol, toRow, toCol))
            return false;

        // 模拟走一步，检查走后己方是否被将军（不能送将）
        var captured = _board.MovePiece(fromRow, fromCol, toRow, toCol);

        var checker = new CheckDetector(_board);
        bool isInCheck = checker.IsInCheck(piece.Side);

        // 撤销
        _board.UndoMove(fromRow, fromCol, toRow, toCol, captured);

        return !isInCheck;
    }

    #region 将/帅
    private bool IsValidKingMove(ChessPiece piece, int fromRow, int fromCol, int toRow, int toCol)
    {
        // 必须在九宫内
        if (!IsInPalace(piece.Side, toRow, toCol)) return false;

        // 只能走一步（上下左右）
        int dr = Math.Abs(toRow - fromRow);
        int dc = Math.Abs(toCol - fromCol);

        return (dr + dc) == 1;
    }

    private static bool IsInPalace(Side side, int row, int col)
    {
        if (col < 3 || col > 5) return false;

        return side == Side.Red
            ? (row >= 0 && row <= 2)   // 红方九宫: Row 0-2
            : (row >= 7 && row <= 9);  // 黑方九宫: Row 7-9
    }
    #endregion

    #region 士/仕
    private bool IsValidAdvisorMove(ChessPiece piece, int fromRow, int fromCol, int toRow, int toCol)
    {
        // 必须在九宫内
        if (!IsInPalace(piece.Side, toRow, toCol)) return false;

        // 斜走一步
        int dr = Math.Abs(toRow - fromRow);
        int dc = Math.Abs(toCol - fromCol);

        return dr == 1 && dc == 1;
    }
    #endregion

    #region 象/相
    private bool IsValidElephantMove(ChessPiece piece, int fromRow, int fromCol, int toRow, int toCol)
    {
        // 不能过河
        if (!IsInOwnSide(piece.Side, toRow)) return false;

        // 田字走法：斜走两步
        int dr = Math.Abs(toRow - fromRow);
        int dc = Math.Abs(toCol - fromCol);

        if (dr != 2 || dc != 2) return false;

        // 检查象眼是否被堵
        int eyeRow = (fromRow + toRow) / 2;
        int eyeCol = (fromCol + toCol) / 2;

        return _board[eyeRow, eyeCol] == null; // 象眼为空
    }

    private static bool IsInOwnSide(Side side, int row)
    {
        return side == Side.Red
            ? (row >= 0 && row <= 4)   // 红方: Row 0-4
            : (row >= 5 && row <= 9);  // 黑方: Row 5-9
    }
    #endregion

    #region 馬
    private bool IsValidKnightMove(int fromRow, int fromCol, int toRow, int toCol)
    {
        int dr = Math.Abs(toRow - fromRow);
        int dc = Math.Abs(toCol - fromCol);

        // 日字走法
        if (!((dr == 2 && dc == 1) || (dr == 1 && dc == 2)))
            return false;

        // 检查蹩脚
        int legRow, legCol;

        if (dr == 2)
        {
            // 竖直日字，蹩脚在竖直方向的中点
            legRow = fromRow + (toRow > fromRow ? 1 : -1);
            legCol = fromCol;
        }
        else
        {
            // 水平日字，蹩脚在水平方向的中点
            legRow = fromRow;
            legCol = fromCol + (toCol > fromCol ? 1 : -1);
        }

        return _board[legRow, legCol] == null;
    }
    #endregion

    #region 車
    private bool IsValidRookMove(int fromRow, int fromCol, int toRow, int toCol)
    {
        // 直线走
        if (fromRow != toRow && fromCol != toCol)
            return false;

        // 检查路径上是否有阻挡
        return !HasPieceBetween(fromRow, fromCol, toRow, toCol);
    }
    #endregion

    #region 砲/炮
    private bool IsValidCannonMove(int fromRow, int fromCol, int toRow, int toCol)
    {
        // 直线走
        if (fromRow != toRow && fromCol != toCol)
            return false;

        bool isCapture = _board[toRow, toCol] != null;
        int piecesBetween = CountPiecesBetween(fromRow, fromCol, toRow, toCol);

        if (isCapture)
        {
            // 吃子：必须隔一个子（炮架）
            return piecesBetween == 1;
        }
        else
        {
            // 不吃子：路径必须为空
            return piecesBetween == 0;
        }
    }
    #endregion

    #region 兵/卒
    private bool IsValidPawnMove(ChessPiece piece, int fromRow, int fromCol, int toRow, int toCol)
    {
        int dr = toRow - fromRow;
        int dc = Math.Abs(toCol - fromCol);

        bool hasCrossedRiver = piece.Side == Side.Red
            ? fromRow > 4   // 红方过河：Row > 4
            : fromRow < 5;  // 黑方过河：Row < 5

        if (piece.Side == Side.Red)
        {
            // 红兵：只能前进（Row递增）
            if (dr <= 0) return false;

            if (hasCrossedRiver)
            {
                // 过河后：可前进或左右横移
                return (dr == 1 && dc == 0) || (dr == 0 && dc == 1);
            }
            else
            {
                // 未过河：只能前进
                return dr == 1 && dc == 0;
            }
        }
        else
        {
            // 黑卒：只能前进（Row递减）
            if (dr >= 0) return false;

            if (hasCrossedRiver)
            {
                // 过河后：可前进或左右横移
                return (dr == -1 && dc == 0) || (dr == 0 && dc == 1);
            }
            else
            {
                // 未过河：只能前进
                return dr == -1 && dc == 0;
            }
        }
    }
    #endregion

    #region 辅助方法
    /// <summary>
    /// 检查两点之间是否有棋子
    /// </summary>
    private bool HasPieceBetween(int fromRow, int fromCol, int toRow, int toCol)
    {
        int dr = Math.Sign(toRow - fromRow);
        int dc = Math.Sign(toCol - fromCol);

        int r = fromRow + dr;
        int c = fromCol + dc;

        while (r != toRow || c != toCol)
        {
            if (_board[r, c] != null) return true;
            r += dr;
            c += dc;
        }

        return false;
    }

    /// <summary>
    /// 计算两点之间的棋子数量
    /// </summary>
    private int CountPiecesBetween(int fromRow, int fromCol, int toRow, int toCol)
    {
        int dr = Math.Sign(toRow - fromRow);
        int dc = Math.Sign(toCol - fromCol);

        int count = 0;
        int r = fromRow + dr;
        int c = fromCol + dc;

        while (r != toRow || c != toCol)
        {
            if (_board[r, c] != null) count++;
            r += dr;
            c += dc;
        }

        return count;
    }
    #endregion
}
