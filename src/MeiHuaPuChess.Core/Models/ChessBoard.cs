using MeiHuaPuChess.Core.Enums;

namespace MeiHuaPuChess.Core.Models;

/// <summary>
/// 棋盘状态，管理所有棋子的位置
/// 棋盘坐标体系：Row 0-9（红方底线到黑方底线），Col 0-8（红九路到红一路）
/// </summary>
public class ChessBoard
{
    public const int Rows = 10;
    public const int Cols = 9;

    /// <summary>
    /// 棋盘上的所有棋子（包括已死的）
    /// </summary>
    public List<ChessPiece> AllPieces { get; private set; } = new();

    /// <summary>
    /// 按位置索引的棋子（快速查找），null 表示空格
    /// </summary>
    private ChessPiece?[,] _grid = new ChessPiece?[Rows, Cols];

    public ChessPiece? this[int row, int col] => _grid[row, col];

    public ChessBoard()
    {
        InitializeBoard();
    }

    /// <summary>
    /// 初始化棋盘为开局状态
    /// </summary>
    private void InitializeBoard()
    {
        AllPieces.Clear();
        _grid = new ChessPiece?[Rows, Cols];

        // 红方棋子（底线 Row 0）
        AddPiece(PieceType.Ju,    Side.Red, 0, 0);
        AddPiece(PieceType.Ma,    Side.Red, 0, 1);
        AddPiece(PieceType.Xiang, Side.Red, 0, 2);
        AddPiece(PieceType.Shi,   Side.Red, 0, 3);
        AddPiece(PieceType.Shuai, Side.Red, 0, 4);
        AddPiece(PieceType.Shi,   Side.Red, 0, 5);
        AddPiece(PieceType.Xiang, Side.Red, 0, 6);
        AddPiece(PieceType.Ma,    Side.Red, 0, 7);
        AddPiece(PieceType.Ju,    Side.Red, 0, 8);

        // 红方炮（Row 2）
        AddPiece(PieceType.Pao, Side.Red, 2, 1);
        AddPiece(PieceType.Pao, Side.Red, 2, 7);

        // 红方兵（Row 3）
        AddPiece(PieceType.Bing, Side.Red, 3, 0);
        AddPiece(PieceType.Bing, Side.Red, 3, 2);
        AddPiece(PieceType.Bing, Side.Red, 3, 4);
        AddPiece(PieceType.Bing, Side.Red, 3, 6);
        AddPiece(PieceType.Bing, Side.Red, 3, 8);

        // 黑方棋子（底线 Row 9）
        AddPiece(PieceType.Ju2,    Side.Black, 9, 0);
        AddPiece(PieceType.Ma2,    Side.Black, 9, 1);
        AddPiece(PieceType.Xiang2, Side.Black, 9, 2);
        AddPiece(PieceType.Shi2,   Side.Black, 9, 3);
        AddPiece(PieceType.Jiang,  Side.Black, 9, 4);
        AddPiece(PieceType.Shi2,   Side.Black, 9, 5);
        AddPiece(PieceType.Xiang2, Side.Black, 9, 6);
        AddPiece(PieceType.Ma2,    Side.Black, 9, 7);
        AddPiece(PieceType.Ju2,    Side.Black, 9, 8);

        // 黑方炮（Row 7）
        AddPiece(PieceType.Pao2, Side.Black, 7, 1);
        AddPiece(PieceType.Pao2, Side.Black, 7, 7);

        // 黑方卒（Row 6）
        AddPiece(PieceType.Zu, Side.Black, 6, 0);
        AddPiece(PieceType.Zu, Side.Black, 6, 2);
        AddPiece(PieceType.Zu, Side.Black, 6, 4);
        AddPiece(PieceType.Zu, Side.Black, 6, 6);
        AddPiece(PieceType.Zu, Side.Black, 6, 8);
    }

    private void AddPiece(PieceType type, Side side, int row, int col)
    {
        var piece = new ChessPiece { Type = type, Side = side, Row = row, Col = col };
        AllPieces.Add(piece);
        _grid[row, col] = piece;
    }

    /// <summary>
    /// 在棋盘上移动棋子
    /// </summary>
    public ChessPiece? MovePiece(int fromRow, int fromCol, int toRow, int toCol)
    {
        var piece = _grid[fromRow, fromCol];
        if (piece == null) return null;

        var captured = _grid[toRow, toCol];
        if (captured != null)
        {
            captured.IsAlive = false;
        }

        _grid[fromRow, fromCol] = null;
        _grid[toRow, toCol] = piece;
        piece.Row = toRow;
        piece.Col = toCol;

        return captured;
    }

    /// <summary>
    /// 撤销一步走法
    /// </summary>
    public void UndoMove(int fromRow, int fromCol, int toRow, int toCol, ChessPiece? captured)
    {
        var piece = _grid[toRow, toCol];
        if (piece == null) return;

        _grid[toRow, toCol] = captured;
        _grid[fromRow, fromCol] = piece;
        piece.Row = fromRow;
        piece.Col = fromCol;

        if (captured != null)
        {
            captured.IsAlive = true;
            captured.Row = toRow;
            captured.Col = toCol;
        }
    }

    /// <summary>
    /// 获取某方的所有存活棋子
    /// </summary>
    public List<ChessPiece> GetAlivePieces(Side side)
    {
        return AllPieces.Where(p => p.Side == side && p.IsAlive).ToList();
    }

    /// <summary>
    /// 获取所有存活棋子
    /// </summary>
    public List<ChessPiece> GetAllAlivePieces()
    {
        return AllPieces.Where(p => p.IsAlive).ToList();
    }

    /// <summary>
    /// 查找将/帅的位置
    /// </summary>
    public ChessPiece? FindKing(Side side)
    {
        var kingType = side == Side.Red ? PieceType.Shuai : PieceType.Jiang;
        return AllPieces.FirstOrDefault(p => p.Type == kingType && p.IsAlive);
    }

    /// <summary>
    /// 深拷贝棋盘
    /// </summary>
    public ChessBoard Clone()
    {
        var clone = new ChessBoard();
        clone.AllPieces.Clear();
        clone._grid = new ChessPiece?[Rows, Cols];

        foreach (var piece in AllPieces)
        {
            var clonedPiece = piece.Clone();
            clone.AllPieces.Add(clonedPiece);
            if (clonedPiece.IsAlive)
            {
                clone._grid[clonedPiece.Row, clonedPiece.Col] = clonedPiece;
            }
        }

        return clone;
    }
}
