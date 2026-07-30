using MeiHuaPuChess.Core.Enums;
using MeiHuaPuChess.Core.Models;

namespace MeiHuaPuChess.Core.Engine;

/// <summary>
/// 匹配结果
/// </summary>
public enum MatchResult
{
    Correct,
    Incorrect,
    GameComplete,
    Error,
    ModeMismatch
}

/// <summary>
/// 游戏主引擎 — 支持两种模式：Review（观看）和 Training（训练）
/// </summary>
public class GameEngine
{
    private readonly MeiHuaPu.MeiHuaPuEngine _meiHuaPuEngine;

    public ChessBoard Board { get; private set; } = new();
    public GamePhase Phase { get; private set; } = GamePhase.NotStarted;
    public MeiHuaPuRecord? CurrentRecord { get; private set; }
    public List<Move> MoveHistory { get; private set; } = new();
    public int CurrentStepIndex => _meiHuaPuEngine.CurrentStepIndex;

    /// <summary>当前应用模式</summary>
    public AppMode CurrentMode { get; private set; } = AppMode.Review;

    /// <summary>当前导航到的步数（0 = 初始局面）</summary>
    public int NavigationStep { get; private set; }

    /// <summary>是否可以回退（仅 Review 模式）</summary>
    public bool CanGoPrevious =>
        CurrentMode == AppMode.Review
        && CurrentRecord != null
        && NavigationStep > 0;

    /// <summary>是否可以前进（仅 Review 模式）</summary>
    public bool CanGoForward =>
        CurrentMode == AppMode.Review
        && CurrentRecord != null
        && NavigationStep < CurrentRecord.TotalSteps;

    /// <summary>是否可以悔棋（仅 Training 模式）</summary>
    public bool CanUndo =>
        CurrentMode == AppMode.Training
        && Phase == GamePhase.BlackTurn
        && MoveHistory.Count >= 2;

    /// <summary>训练是否已完成</summary>
    public bool IsTrainingComplete =>
        CurrentMode == AppMode.Training
        && Phase == GamePhase.GameOver;

    // 事件
    public event Action<Move>? OnRedMoveCompleted;
    public event Action<MatchResult, List<string>?>? OnBlackMoveValidated;
    public event Action<string>? OnGameOver;
    public event Action? OnStateChanged;

    public GameEngine()
    {
        _meiHuaPuEngine = new MeiHuaPu.MeiHuaPuEngine();
    }

    // ================================================================
    //  模式入口
    // ================================================================

    /// <summary>开始观看模式：重置棋盘，不走棋</summary>
    public void StartReview(MeiHuaPuRecord record)
    {
        CurrentMode = AppMode.Review;
        Board = new ChessBoard();
        MoveHistory.Clear();
        CurrentRecord = record;
        _meiHuaPuEngine.LoadRecord(record);
        NavigationStep = 0;
        Phase = GamePhase.NotStarted;
        OnStateChanged?.Invoke();
    }

    /// <summary>开始训练模式：重置棋盘，红方自动走第一步</summary>
    public void StartTraining(MeiHuaPuRecord record)
    {
        CurrentMode = AppMode.Training;
        Board = new ChessBoard();
        MoveHistory.Clear();
        CurrentRecord = record;
        _meiHuaPuEngine.LoadRecord(record);
        NavigationStep = 0;
        Phase = GamePhase.RedTurn;
        OnStateChanged?.Invoke();
        ExecuteRedMove();
    }

    // ================================================================
    //  训练模式 — 红方自动走棋
    // ================================================================

    /// <summary>红方自动走棋（仅 Training 模式）</summary>
    public void ExecuteRedMove()
    {
        if (CurrentMode != AppMode.Training) return;

        var expectedMove = _meiHuaPuEngine.GetNextRedMove();
        if (expectedMove == null)
        {
            Phase = GamePhase.GameOver;
            OnGameOver?.Invoke("梅花谱走完！练习完成！");
            OnStateChanged?.Invoke();
            return;
        }

        var piece = Board[expectedMove.FromRow, expectedMove.FromCol];
        if (piece == null)
        {
            OnGameOver?.Invoke($"错误：梅花谱数据异常，第{expectedMove.StepNumber}步红方位置无棋子");
            return;
        }

        var captured = Board.MovePiece(expectedMove.FromRow, expectedMove.FromCol,
                                        expectedMove.ToRow, expectedMove.ToCol);

        var move = new Move
        {
            FromRow = expectedMove.FromRow,
            FromCol = expectedMove.FromCol,
            ToRow = expectedMove.ToRow,
            ToCol = expectedMove.ToCol,
            Piece = piece,
            CapturedPiece = captured,
            Notation = expectedMove.Notation
        };

        MoveHistory.Add(move);
        NavigationStep = expectedMove.StepNumber;

        var checker = new CheckDetector(Board);
        if (checker.IsInCheck(Side.Black))
        {
            move.IsCheck = true;
            if (checker.IsCheckmated(Side.Black))
            {
                move.IsCheckmate = true;
                Phase = GamePhase.GameOver;
                OnRedMoveCompleted?.Invoke(move);
                OnGameOver?.Invoke("红方将死黑方！棋局结束。");
                OnStateChanged?.Invoke();
                return;
            }
        }

        OnRedMoveCompleted?.Invoke(move);

        Phase = GamePhase.BlackTurn;
        OnStateChanged?.Invoke();
    }

    // ================================================================
    //  训练模式 — 黑方走棋（玩家操作）
    // ================================================================

    /// <summary>黑方尝试走棋（仅 Training 模式）</summary>
    public MatchResult TryBlackMove(int fromRow, int fromCol, int toRow, int toCol)
    {
        if (CurrentMode != AppMode.Training)
            return MatchResult.ModeMismatch;

        if (Phase != GamePhase.BlackTurn)
            return MatchResult.Error;

        var piece = Board[fromRow, fromCol];
        if (piece == null || piece.Side != Side.Black)
            return MatchResult.Error;

        var validator = new MoveValidator(Board);
        if (!validator.IsValidFullMove(fromRow, fromCol, toRow, toCol))
            return MatchResult.Incorrect;

        var meiHuaPuMove = new MeiHuaPuMove
        {
            FromRow = fromRow,
            FromCol = fromCol,
            ToRow = toRow,
            ToCol = toCol
        };

        var result = _meiHuaPuEngine.ValidateBlackMove(meiHuaPuMove);
        var hints = _meiHuaPuEngine.GetCurrentHints();

        if (result == MatchResult.Correct)
        {
            var captured = Board.MovePiece(fromRow, fromCol, toRow, toCol);

            var move = new Move
            {
                FromRow = fromRow,
                FromCol = fromCol,
                ToRow = toRow,
                ToCol = toCol,
                Piece = piece,
                CapturedPiece = captured,
                Notation = _meiHuaPuEngine.GetExpectedBlackMove()?.Notation ?? ""
            };

            MoveHistory.Add(move);
            NavigationStep = _meiHuaPuEngine.CurrentStepIndex;

            var checker = new CheckDetector(Board);
            if (checker.IsInCheck(Side.Red))
            {
                move.IsCheck = true;
                if (checker.IsCheckmated(Side.Red))
                {
                    move.IsCheckmate = true;
                    Phase = GamePhase.GameOver;
                    OnBlackMoveValidated?.Invoke(MatchResult.Correct, null);
                    OnGameOver?.Invoke("黑方将死红方！");
                    OnStateChanged?.Invoke();
                    return MatchResult.Correct;
                }
            }

            OnBlackMoveValidated?.Invoke(MatchResult.Correct, null);
            OnStateChanged?.Invoke();

            if (_meiHuaPuEngine.IsComplete())
            {
                Phase = GamePhase.GameOver;
                OnGameOver?.Invoke("🎉 梅花谱走完！练习完成！");
                OnStateChanged?.Invoke();
                return MatchResult.Correct;
            }

            Phase = GamePhase.RedTurn;
            OnStateChanged?.Invoke();
            return MatchResult.Correct;
        }
        else
        {
            OnBlackMoveValidated?.Invoke(MatchResult.Incorrect, hints);
            return MatchResult.Incorrect;
        }
    }

    /// <summary>悔棋（撤销红方+黑方各一步，仅 Training 模式）</summary>
    public bool UndoLastPair()
    {
        if (CurrentMode != AppMode.Training) return false;
        if (MoveHistory.Count < 2) return false;

        var blackMove = MoveHistory[^1];
        Board.UndoMove(blackMove.FromRow, blackMove.FromCol, blackMove.ToRow, blackMove.ToCol, blackMove.CapturedPiece);
        MoveHistory.RemoveAt(MoveHistory.Count - 1);
        _meiHuaPuEngine.StepBack();

        var redMove = MoveHistory[^1];
        Board.UndoMove(redMove.FromRow, redMove.FromCol, redMove.ToRow, redMove.ToCol, redMove.CapturedPiece);
        MoveHistory.RemoveAt(MoveHistory.Count - 1);
        _meiHuaPuEngine.StepBack();

        NavigationStep = _meiHuaPuEngine.CurrentStepIndex;
        Phase = GamePhase.BlackTurn;
        OnStateChanged?.Invoke();
        return true;
    }

    /// <summary>重新开始（沿用当前模式）</summary>
    public void Restart()
    {
        if (CurrentRecord == null) return;
        if (CurrentMode == AppMode.Review)
            StartReview(CurrentRecord);
        else
            StartTraining(CurrentRecord);
    }

    /// <summary>获取当前期望的黑方走法（提示用）</summary>
    public MeiHuaPuMove? GetHint()
    {
        if (CurrentMode != AppMode.Training) return null;
        return _meiHuaPuEngine.GetExpectedBlackMove();
    }

    // ================================================================
    //  观看模式 — 导航
    // ================================================================

    /// <summary>导航到指定步数（仅 Review 模式）</summary>
    public void NavigateToStep(int stepIndex)
    {
        if (CurrentMode != AppMode.Review) return;
        if (CurrentRecord == null) return;

        int targetStep = Math.Clamp(stepIndex, 0, CurrentRecord.TotalSteps);

        Board = new ChessBoard();
        MoveHistory.Clear();
        _meiHuaPuEngine.SetStepIndex(0);

        foreach (var mhpMove in CurrentRecord.Moves
                     .Where(m => m.StepNumber <= targetStep)
                     .OrderBy(m => m.StepNumber))
        {
            var piece = Board[mhpMove.FromRow, mhpMove.FromCol];
            if (piece == null) break;

            var captured = Board.MovePiece(mhpMove.FromRow, mhpMove.FromCol,
                                           mhpMove.ToRow, mhpMove.ToCol);

            var move = new Move
            {
                FromRow = mhpMove.FromRow,
                FromCol = mhpMove.FromCol,
                ToRow = mhpMove.ToRow,
                ToCol = mhpMove.ToCol,
                Piece = piece,
                CapturedPiece = captured,
                Notation = mhpMove.Notation
            };
            MoveHistory.Add(move);
            _meiHuaPuEngine.SetStepIndex(mhpMove.StepNumber);
        }

        NavigationStep = targetStep;

        if (targetStep >= CurrentRecord.TotalSteps)
        {
            Phase = GamePhase.GameOver;
        }
        else
        {
            var nextMove = CurrentRecord.Moves
                .FirstOrDefault(m => m.StepNumber == targetStep + 1);
            Phase = nextMove?.MovingSide == Side.Red
                ? GamePhase.RedTurn
                : GamePhase.BlackTurn;
        }

        OnStateChanged?.Invoke();
    }

    /// <summary>上一步</summary>
    public void PreviousStep()
    {
        if (CanGoPrevious)
            NavigateToStep(NavigationStep - 1);
    }

    /// <summary>下一步</summary>
    public void NextStep()
    {
        if (CanGoForward)
            NavigateToStep(NavigationStep + 1);
    }

    /// <summary>跳到第一步</summary>
    public void GoToFirst()
    {
        if (CurrentMode == AppMode.Review)
            NavigateToStep(0);
    }

    /// <summary>跳到最后一步</summary>
    public void GoToLast()
    {
        if (CurrentMode == AppMode.Review && CurrentRecord != null)
            NavigateToStep(CurrentRecord.TotalSteps);
    }
}
