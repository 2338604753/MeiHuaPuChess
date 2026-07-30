using MeiHuaPuChess.Core.Enums;
using MeiHuaPuChess.Core.Models;

namespace MeiHuaPuChess.Core.Engine;

/// <summary>
/// 合法走法生成器
/// </summary>
public class MoveGenerator
{
    private readonly ChessBoard _board;
    private readonly MoveValidator _validator;

    public MoveGenerator(ChessBoard board)
    {
        _board = board;
        _validator = new MoveValidator(board);
    }

    /// <summary>
    /// 生成某方所有合法走法
    /// </summary>
    public List<Move> GenerateAllLegalMoves(Side side)
    {
        var moves = new List<Move>();
        var pieces = _board.GetAlivePieces(side);

        foreach (var piece in pieces)
        {
            GenerateMovesForPiece(piece, moves);
        }

        return moves;
    }

    /// <summary>
    /// 生成单个棋子的所有合法走法
    /// </summary>
    public List<(int row, int col)> GetLegalMovesForPiece(int row, int col)
    {
        var piece = _board[row, col];
        if (piece == null) return new List<(int, int)>();

        var result = new List<(int, int)>();
        var moves = new List<Move>();
        GenerateMovesForPiece(piece, moves);

        foreach (var move in moves)
        {
            result.Add((move.ToRow, move.ToCol));
        }

        return result;
    }

    private void GenerateMovesForPiece(ChessPiece piece, List<Move> moves)
    {
        int row = piece.Row;
        int col = piece.Col;

        switch (piece.Type)
        {
            case PieceType.Shuai or PieceType.Jiang:
                GenerateKingMoves(piece, row, col, moves);
                break;
            case PieceType.Shi or PieceType.Shi2:
                GenerateAdvisorMoves(piece, row, col, moves);
                break;
            case PieceType.Xiang or PieceType.Xiang2:
                GenerateElephantMoves(piece, row, col, moves);
                break;
            case PieceType.Ma or PieceType.Ma2:
                GenerateKnightMoves(piece, row, col, moves);
                break;
            case PieceType.Ju or PieceType.Ju2:
                GenerateRookMoves(piece, row, col, moves);
                break;
            case PieceType.Pao or PieceType.Pao2:
                GenerateCannonMoves(piece, row, col, moves);
                break;
            case PieceType.Bing or PieceType.Zu:
                GeneratePawnMoves(piece, row, col, moves);
                break;
        }
    }

    private void TryAddMove(ChessPiece piece, int toRow, int toCol, List<Move> moves)
    {
        if (_validator.IsValidFullMove(piece.Row, piece.Col, toRow, toCol))
        {
            var captured = _board[toRow, toCol];
            moves.Add(new Move
            {
                FromRow = piece.Row,
                FromCol = piece.Col,
                ToRow = toRow,
                ToCol = toCol,
                Piece = piece,
                CapturedPiece = captured,
                Notation = GenerateNotation(piece, toRow, toCol, captured != null)
            });
        }
    }

    #region 各棋子走法生成

    private void GenerateKingMoves(ChessPiece piece, int row, int col, List<Move> moves)
    {
        int[] dr = { -1, 1, 0, 0 };
        int[] dc = { 0, 0, -1, 1 };

        for (int i = 0; i < 4; i++)
        {
            int nr = row + dr[i];
            int nc = col + dc[i];

            if (nr >= 0 && nr < ChessBoard.Rows && nc >= 0 && nc < ChessBoard.Cols)
            {
                TryAddMove(piece, nr, nc, moves);
            }
        }
    }

    private void GenerateAdvisorMoves(ChessPiece piece, int row, int col, List<Move> moves)
    {
        int[] dr = { -1, -1, 1, 1 };
        int[] dc = { -1, 1, -1, 1 };

        for (int i = 0; i < 4; i++)
        {
            int nr = row + dr[i];
            int nc = col + dc[i];

            if (nr >= 0 && nr < ChessBoard.Rows && nc >= 0 && nc < ChessBoard.Cols)
            {
                TryAddMove(piece, nr, nc, moves);
            }
        }
    }

    private void GenerateElephantMoves(ChessPiece piece, int row, int col, List<Move> moves)
    {
        int[] dr = { -2, -2, 2, 2 };
        int[] dc = { -2, 2, -2, 2 };

        for (int i = 0; i < 4; i++)
        {
            int nr = row + dr[i];
            int nc = col + dc[i];

            if (nr >= 0 && nr < ChessBoard.Rows && nc >= 0 && nc < ChessBoard.Cols)
            {
                TryAddMove(piece, nr, nc, moves);
            }
        }
    }

    private void GenerateKnightMoves(ChessPiece piece, int row, int col, List<Move> moves)
    {
        // 8个可能的日字位置：(dr, dc, legRow偏移, legCol偏移)
        var knightMoves = new (int dr, int dc, int lr, int lc)[]
        {
            (-2, -1, -1,  0), (-2,  1, -1,  0),
            ( 2, -1,  1,  0), ( 2,  1,  1,  0),
            (-1, -2,  0, -1), (-1,  2,  0,  1),
            ( 1, -2,  0, -1), ( 1,  2,  0,  1),
        };

        foreach (var (dr, dc, lr, lc) in knightMoves)
        {
            int nr = row + dr;
            int nc = col + dc;

            if (nr >= 0 && nr < ChessBoard.Rows && nc >= 0 && nc < ChessBoard.Cols)
            {
                TryAddMove(piece, nr, nc, moves);
            }
        }
    }

    private void GenerateRookMoves(ChessPiece piece, int row, int col, List<Move> moves)
    {
        // 四个方向
        int[] dr = { -1, 1, 0, 0 };
        int[] dc = { 0, 0, -1, 1 };

        for (int i = 0; i < 4; i++)
        {
            int nr = row + dr[i];
            int nc = col + dc[i];

            while (nr >= 0 && nr < ChessBoard.Rows && nc >= 0 && nc < ChessBoard.Cols)
            {
                var target = _board[nr, nc];
                if (target == null)
                {
                    TryAddMove(piece, nr, nc, moves);
                }
                else
                {
                    if (target.Side != piece.Side)
                    {
                        TryAddMove(piece, nr, nc, moves);
                    }
                    break; // 遇到棋子停止
                }
                nr += dr[i];
                nc += dc[i];
            }
        }
    }

    private void GenerateCannonMoves(ChessPiece piece, int row, int col, List<Move> moves)
    {
        int[] dr = { -1, 1, 0, 0 };
        int[] dc = { 0, 0, -1, 1 };

        for (int i = 0; i < 4; i++)
        {
            int nr = row + dr[i];
            int nc = col + dc[i];
            bool foundPlatform = false;

            while (nr >= 0 && nr < ChessBoard.Rows && nc >= 0 && nc < ChessBoard.Cols)
            {
                var target = _board[nr, nc];

                if (!foundPlatform)
                {
                    if (target == null)
                    {
                        TryAddMove(piece, nr, nc, moves); // 不吃子移动
                    }
                    else
                    {
                        foundPlatform = true; // 找到炮架
                    }
                }
                else
                {
                    if (target != null)
                    {
                        if (target.Side != piece.Side)
                        {
                            TryAddMove(piece, nr, nc, moves); // 吃子
                        }
                        break; // 遇到棋子停止
                    }
                }

                nr += dr[i];
                nc += dc[i];
            }
        }
    }

    private void GeneratePawnMoves(ChessPiece piece, int row, int col, List<Move> moves)
    {
        bool hasCrossed = piece.Side == Side.Red ? row > 4 : row < 5;

        if (piece.Side == Side.Red)
        {
            // 前进
            if (row + 1 < ChessBoard.Rows)
                TryAddMove(piece, row + 1, col, moves);

            if (hasCrossed)
            {
                // 左右
                if (col - 1 >= 0) TryAddMove(piece, row, col - 1, moves);
                if (col + 1 < ChessBoard.Cols) TryAddMove(piece, row, col + 1, moves);
            }
        }
        else
        {
            // 前进（黑方向是 row 减小）
            if (row - 1 >= 0)
                TryAddMove(piece, row - 1, col, moves);

            if (hasCrossed)
            {
                if (col - 1 >= 0) TryAddMove(piece, row, col - 1, moves);
                if (col + 1 < ChessBoard.Cols) TryAddMove(piece, row, col + 1, moves);
            }
        }
    }
    #endregion

    #region 记谱法生成
    /// <summary>
    /// 生成中国传统记谱法字符串
    /// </summary>
    private static string GenerateNotation(ChessPiece piece, int toRow, int toCol, bool isCapture)
    {
        string name = piece.DisplayChar;
        string action;

        if (piece.Side == Side.Red)
        {
            // 红方用中文数字表示列
            string[] redCols = { "九", "八", "七", "六", "五", "四", "三", "二", "一" };
            string fromCol = redCols[piece.Col];
            string toColStr = redCols[toCol];

            if (toRow == piece.Row)
            {
                action = "平";
            }
            else if ((piece.Side == Side.Red && toRow > piece.Row) ||
                     (piece.Side == Side.Black && toRow < piece.Row))
            {
                action = "進";
            }
            else
            {
                action = "退";
            }

            // 对于车马炮兵等直线棋子：名+原列+动作+目标列
            // 对于马象士等斜线棋子：名+原列+动作+目标列
            return $"{name}{fromCol}{action}{toColStr}";
        }
        else
        {
            // 黑方用阿拉伯数字表示列 (黑1路 = 内部Col 0, ...)
            string[] blackCols = { "1", "2", "3", "4", "5", "6", "7", "8", "9" };
            string fromCol = blackCols[piece.Col];
            string toColStr = blackCols[toCol];

            if (toRow == piece.Row)
            {
                action = "平";
            }
            else if ((piece.Side == Side.Black && toRow < piece.Row) ||
                     (piece.Side == Side.Red && toRow > piece.Row))
            {
                action = "進";
            }
            else
            {
                action = "退";
            }

            return $"{name}{fromCol}{action}{toColStr}";
        }
    }
    #endregion
}
