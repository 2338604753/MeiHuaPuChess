using MeiHuaPuChess.Core.Engine;
using MeiHuaPuChess.Core.Models;

namespace MeiHuaPuChess.Core.Services;

/// <summary>
/// 游戏服务接口
/// </summary>
public interface IGameService
{
    GameEngine Engine { get; }
    List<MeiHuaPuRecord> AvailableRecords { get; }

    void LoadRecords();
    void StartReview(MeiHuaPuRecord record);
    void StartTraining(MeiHuaPuRecord record);
    MatchResult TryBlackMove(int fromRow, int fromCol, int toRow, int toCol);
    bool UndoLastPair();
    void Restart();
    MeiHuaPuMove? GetHint();
    void PreviousStep();
    void NextStep();
    void GoToFirst();
    void GoToLast();
}

/// <summary>
/// 游戏服务实现
/// </summary>
public class GameService : IGameService
{
    private readonly IMeiHuaPuDataLoader _dataLoader;

    public GameEngine Engine { get; } = new();
    public List<MeiHuaPuRecord> AvailableRecords { get; private set; } = new();

    public GameService(IMeiHuaPuDataLoader dataLoader)
    {
        _dataLoader = dataLoader;
    }

    public void LoadRecords()
    {
        AvailableRecords = _dataLoader.LoadAllRecords();
    }

    public void StartReview(MeiHuaPuRecord record)
    {
        Engine.StartReview(record);
    }

    public void StartTraining(MeiHuaPuRecord record)
    {
        Engine.StartTraining(record);
    }

    public MatchResult TryBlackMove(int fromRow, int fromCol, int toRow, int toCol)
    {
        return Engine.TryBlackMove(fromRow, fromCol, toRow, toCol);
    }

    public bool UndoLastPair()
    {
        return Engine.UndoLastPair();
    }

    public void Restart()
    {
        Engine.Restart();
    }

    public MeiHuaPuMove? GetHint()
    {
        return Engine.GetHint();
    }

    public void PreviousStep()
    {
        Engine.PreviousStep();
    }

    public void NextStep()
    {
        Engine.NextStep();
    }

    public void GoToFirst()
    {
        Engine.GoToFirst();
    }

    public void GoToLast()
    {
        Engine.GoToLast();
    }
}
