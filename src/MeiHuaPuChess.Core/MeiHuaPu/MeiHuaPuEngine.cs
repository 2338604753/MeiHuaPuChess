using MeiHuaPuChess.Core.Engine;
using MeiHuaPuChess.Core.Models;

namespace MeiHuaPuChess.Core.MeiHuaPu;

/// <summary>
/// 梅花谱引擎：管理当前棋局的走法序列匹配
/// </summary>
public class MeiHuaPuEngine
{
    private MeiHuaPuRecord? _currentRecord;
    private int _currentStepIndex = 0;

    /// <summary>
    /// 当前步数索引（0-based，指向下一步）
    /// </summary>
    public int CurrentStepIndex => _currentStepIndex;

    /// <summary>
    /// 加载梅花谱棋局记录
    /// </summary>
    public void LoadRecord(MeiHuaPuRecord record)
    {
        _currentRecord = record;
        _currentStepIndex = 0;
    }

    /// <summary>
    /// 获取红方下一步（用于自动走棋）
    /// </summary>
    /// <returns>梅花谱中红方的下一步走法，如果走完返回 null</returns>
    public MeiHuaPuMove? GetNextRedMove()
    {
        if (_currentRecord == null) return null;

        // 找到当前步数之后第一个红方走法
        var redMove = _currentRecord.Moves
            .FirstOrDefault(m => m.StepNumber == _currentStepIndex + 1 && m.MovingSide == Enums.Side.Red);

        if (redMove != null)
        {
            _currentStepIndex++;
        }

        return redMove;
    }

    /// <summary>
    /// 校验黑方走法是否匹配梅花谱
    /// </summary>
    public MatchResult ValidateBlackMove(MeiHuaPuMove playerMove)
    {
        if (_currentRecord == null) return MatchResult.Error;

        // 找到当前步数之后第一个黑方走法
        var expectedMove = _currentRecord.Moves
            .FirstOrDefault(m => m.StepNumber == _currentStepIndex + 1 && m.MovingSide == Enums.Side.Black);

        if (expectedMove == null)
        {
            return MatchResult.GameComplete;
        }

        // 比较位置
        if (expectedMove.FromRow == playerMove.FromRow &&
            expectedMove.FromCol == playerMove.FromCol &&
            expectedMove.ToRow == playerMove.ToRow &&
            expectedMove.ToCol == playerMove.ToCol)
        {
            _currentStepIndex++;
            return MatchResult.Correct;
        }

        return MatchResult.Incorrect;
    }

    /// <summary>
    /// 获取当前期望的黑方走法（用于提示）
    /// </summary>
    public MeiHuaPuMove? GetExpectedBlackMove()
    {
        if (_currentRecord == null) return null;

        return _currentRecord.Moves
            .FirstOrDefault(m => m.StepNumber == _currentStepIndex + 1 && m.MovingSide == Enums.Side.Black);
    }

    /// <summary>
    /// 获取当前步的提示信息
    /// </summary>
    public List<string>? GetCurrentHints()
    {
        var expected = GetExpectedBlackMove();
        return expected?.Hints;
    }

    /// <summary>
    /// 悔棋：回退一步
    /// </summary>
    public void StepBack()
    {
        if (_currentStepIndex > 0)
        {
            _currentStepIndex--;
        }
    }

    /// <summary>
    /// 直接设置步数索引（用于导航）
    /// </summary>
    public void SetStepIndex(int index)
    {
        if (_currentRecord == null) return;
        _currentStepIndex = Math.Clamp(index, 0, _currentRecord.TotalSteps);
    }

    /// <summary>
    /// 梅花谱是否走完
    /// </summary>
    public bool IsComplete()
    {
        if (_currentRecord == null) return true;
        return _currentStepIndex >= _currentRecord.TotalSteps;
    }

    /// <summary>
    /// 获取进度信息
    /// </summary>
    public (int current, int total) GetProgress()
    {
        if (_currentRecord == null) return (0, 0);
        return (_currentStepIndex, _currentRecord.TotalSteps);
    }

    /// <summary>
    /// 重置到第0步
    /// </summary>
    public void Reset()
    {
        _currentStepIndex = 0;
    }
}
