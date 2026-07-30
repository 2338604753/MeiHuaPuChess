using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MeiHuaPuChess.Core.Engine;
using MeiHuaPuChess.Core.Enums;
using MeiHuaPuChess.Core.Models;
using MeiHuaPuChess.Core.Services;
using MeiHuaPuChess.Data;

namespace MeiHuaPuChess.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IGameService _gameService;
    private readonly UserRecordStore _userStore = new();

    public GameEngine Engine => _gameService.Engine;

    // ================================================================
    //  模式
    // ================================================================

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsReviewMode))]
    [NotifyPropertyChangedFor(nameof(IsTrainingMode))]
    [NotifyPropertyChangedFor(nameof(IsBoardReadOnly))]
    [NotifyPropertyChangedFor(nameof(AutoPlayButtonText))]
    private AppMode _currentMode = AppMode.Review;

    public bool IsReviewMode => CurrentMode == AppMode.Review;
    public bool IsTrainingMode => CurrentMode == AppMode.Training;
    public bool IsBoardReadOnly => CurrentMode == AppMode.Review;

    [RelayCommand]
    public void SwitchToReviewMode()
    {
        if (CurrentMode == AppMode.Review) return;
        StopAutoPlay();
        if (Engine.CurrentRecord != null)
            _gameService.StartReview(Engine.CurrentRecord);
        else
            CurrentMode = AppMode.Review;
        ClearAllState();
        UpdateProgress();
        UpdateCapturedPieces();
    }

    [RelayCommand]
    public void SwitchToTrainingMode()
    {
        if (CurrentMode == AppMode.Training) return;
        StopAutoPlay();
        if (Engine.CurrentRecord != null)
            _gameService.StartTraining(Engine.CurrentRecord);
        else
            CurrentMode = AppMode.Training;
        ClearAllState();
        IsGameActive = true;
        CurrentSide = "红方走棋...";
        IsRedThinking = true;
        UpdateProgress();
        UpdateCapturedPieces();
    }

    // ================================================================
    //  棋局来源切换（梅花谱 / 我的棋局）
    // ================================================================

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMeiHuaPu))]
    [NotifyPropertyChangedFor(nameof(IsUserSource))]
    [NotifyPropertyChangedFor(nameof(DisplayRecords))]
    private RecordSource _currentSource = RecordSource.MeiHuaPu;

    public bool IsMeiHuaPu => CurrentSource == RecordSource.MeiHuaPu;
    public bool IsUserSource => CurrentSource == RecordSource.User;

    public ObservableCollection<MeiHuaPuRecord> MeiHuaPuRecords { get; } = new();
    public ObservableCollection<MeiHuaPuRecord> UserRecords { get; } = new();

    public IEnumerable<MeiHuaPuRecord> DisplayRecords =>
        CurrentSource == RecordSource.MeiHuaPu ? MeiHuaPuRecords : UserRecords;

    private HashSet<string> _favoriteIds = new();

    [ObservableProperty]
    private MeiHuaPuRecord? _selectedRecord;

    [RelayCommand]
    public void SwitchToMeiHuaPu()
    {
        if (CurrentSource == RecordSource.MeiHuaPu) return;
        CurrentSource = RecordSource.MeiHuaPu;
        SelectedRecord = MeiHuaPuRecords.FirstOrDefault();
    }

    [RelayCommand]
    public void SwitchToUserRecords()
    {
        if (CurrentSource == RecordSource.User) return;
        var records = _userStore.LoadAll();
        _favoriteIds = _userStore.LoadFavorites();
        UserRecords.Clear();
        foreach (var r in records)
        {
            r.IsFavorite = _favoriteIds.Contains(r.Id);
            UserRecords.Add(r);
        }
        CurrentSource = RecordSource.User;
        SelectedRecord = UserRecords.FirstOrDefault();
    }

    /// <summary>通知 DisplayRecords 变更</summary>
    partial void OnCurrentSourceChanged(RecordSource value)
    {
        OnPropertyChanged(nameof(DisplayRecords));
        OnPropertyChanged(nameof(IsMeiHuaPu));
        OnPropertyChanged(nameof(IsUserSource));
    }

    // ================================================================
    //  收藏
    // ================================================================

    [RelayCommand]
    public void ToggleFavorite()
    {
        if (SelectedRecord == null) return;
        var id = SelectedRecord.Id;
        if (_favoriteIds.Contains(id))
            _favoriteIds.Remove(id);
        else
            _favoriteIds.Add(id);
        _userStore.SaveFavorites(_favoriteIds);

        SelectedRecord.IsFavorite = _favoriteIds.Contains(id);
        // 触发 UI 刷新
        var rec = SelectedRecord;
        SelectedRecord = null;
        SelectedRecord = rec;
    }

    public bool IsCurrentFavorite => SelectedRecord?.IsFavorite ?? false;

    partial void OnSelectedRecordChanged(MeiHuaPuRecord? value)
    {
        OnPropertyChanged(nameof(IsCurrentFavorite));
    }

    // ================================================================
    //  用户棋局操作
    // ================================================================

    [RelayCommand]
    public void NewUserRecord()
    {
        var record = new MeiHuaPuRecord
        {
            Title = "新棋局",
            Category = "",
            Description = "",
            Source = RecordSource.User
        };
        record = _userStore.Add(record);
        UserRecords.Add(record);
        SelectedRecord = record;
    }

    [RelayCommand]
    public void DeleteUserRecord()
    {
        if (SelectedRecord == null || CurrentSource != RecordSource.User) return;
        var id = SelectedRecord.Id;
        _userStore.Delete(id);
        UserRecords.Remove(SelectedRecord);
        SelectedRecord = UserRecords.FirstOrDefault();
    }

    /// <summary>保存当前用户棋局（标题、描述等）</summary>
    public void SaveCurrentUserRecord()
    {
        if (SelectedRecord == null || CurrentSource != RecordSource.User) return;
        _userStore.Update(SelectedRecord);
    }

    // ================================================================
    //  游戏状态
    // ================================================================

    [ObservableProperty]
    private string _gameStatus = "请选择棋局开始";

    [ObservableProperty]
    private string _stepProgress = "第 0 / 0 步";

    [ObservableProperty]
    private string _currentSide = "--";

    [ObservableProperty]
    private bool _isGameActive;

    [ObservableProperty]
    private bool _isTrainingComplete;

    public bool CanGoPrevious =>
        CurrentMode == AppMode.Review
        && Engine.CurrentRecord != null
        && Engine.NavigationStep > 0;

    public bool CanGoForward =>
        CurrentMode == AppMode.Review
        && Engine.CurrentRecord != null
        && Engine.NavigationStep < Engine.CurrentRecord.TotalSteps;

    public bool CanUndo =>
        CurrentMode == AppMode.Training
        && IsGameActive
        && Engine.MoveHistory.Count >= 2;

    public ObservableCollection<MoveViewModel> MoveHistoryDisplay { get; } = new();

    [ObservableProperty]
    private string _hintMessage = "";

    [ObservableProperty]
    private bool _hasHint;

    [ObservableProperty]
    private bool _isHintError;

    [ObservableProperty]
    private string _capturedByRed = "";

    [ObservableProperty]
    private string _capturedByBlack = "";

    [ObservableProperty]
    private bool _isRedThinking;

    private System.Windows.Threading.DispatcherTimer? _redMoveTimer;
    private System.Windows.Threading.DispatcherTimer? _autoPlayTimer;

    [ObservableProperty]
    private bool _isAutoPlaying;

    [ObservableProperty]
    private int _autoPlaySpeed = 1000;

    public string AutoPlayButtonText => IsAutoPlaying ? "⏸ 停止" : "▶ 自动播放";

    partial void OnIsAutoPlayingChanged(bool value)
    {
        OnPropertyChanged(nameof(AutoPlayButtonText));
    }

    // ================================================================
    //  构造
    // ================================================================

    public MainViewModel(IGameService gameService)
    {
        _gameService = gameService;

        Engine.OnRedMoveCompleted += OnRedMoveCompleted;
        Engine.OnBlackMoveValidated += OnBlackMoveValidated;
        Engine.OnGameOver += OnGameOver;
        Engine.OnStateChanged += OnStateChanged;
    }

    [RelayCommand]
    public void LoadRecords()
    {
        _gameService.LoadRecords();
        _favoriteIds = _userStore.LoadFavorites();
        MeiHuaPuRecords.Clear();
        foreach (var record in _gameService.AvailableRecords)
        {
            record.Source = RecordSource.MeiHuaPu;
            record.IsFavorite = _favoriteIds.Contains(record.Id);
            MeiHuaPuRecords.Add(record);
        }
        GameStatus = MeiHuaPuRecords.Count > 0
            ? $"已加载 {MeiHuaPuRecords.Count} 局梅花谱，请选择棋局"
            : "未找到棋局数据";
    }

    // ================================================================
    //  开始
    // ================================================================

    [RelayCommand]
    public void StartReview()
    {
        if (SelectedRecord == null) return;
        StopAutoPlay();
        _gameService.StartReview(SelectedRecord);
        ClearAllState();
        UpdateProgress();
        UpdateCapturedPieces();
        RebuildMoveHistoryDisplay();
    }

    [RelayCommand]
    public void StartTraining()
    {
        if (SelectedRecord == null) return;
        _gameService.StartTraining(SelectedRecord);
        ClearAllState();
        IsGameActive = true;
        CurrentSide = "红方走棋...";
        IsRedThinking = true;
        UpdateProgress();
        UpdateCapturedPieces();
        RebuildMoveHistoryDisplay();
    }

    private void ClearAllState()
    {
        MoveHistoryDisplay.Clear();
        HintMessage = "";
        HasHint = false;
        IsHintError = false;
        CapturedByRed = "";
        CapturedByBlack = "";
        IsTrainingComplete = false;
        IsGameActive = false;
    }

    public void OnPlayerMove(int fromRow, int fromCol, int toRow, int toCol)
    {
        if (CurrentMode != AppMode.Training) return;
        if (!IsGameActive) return;
        if (Engine.Phase != GamePhase.BlackTurn) return;
        var result = Engine.TryBlackMove(fromRow, fromCol, toRow, toCol);
        if (result == MatchResult.Correct)
        {
            HintMessage = "";
            HasHint = false;
            IsHintError = false;
            UpdateProgress();
        }
    }

    private void OnRedMoveCompleted(Move move)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            AddMoveToHistory(move);
            UpdateProgress();
            UpdateCapturedPieces();
            UpdateMoveHighlight();
            CurrentSide = "黑方走棋";
        });
    }

    private void OnBlackMoveValidated(MatchResult result, List<string>? hints)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            if (result == MatchResult.Correct)
            {
                var lastMove = Engine.MoveHistory.LastOrDefault();
                if (lastMove != null) AddMoveToHistory(lastMove);
                UpdateProgress(); UpdateCapturedPieces(); UpdateMoveHighlight();
                HintMessage = ""; HasHint = false; IsHintError = false;
                CurrentSide = "红方走棋..."; IsRedThinking = true;
                ScheduleRedMove();
            }
            else if (result == MatchResult.Incorrect)
            {
                HintMessage = hints is { Count: > 0 }
                    ? "❌ 不对！\n" + string.Join("\n", hints)
                    : "❌ 走法不正确！请参照梅花谱走法";
                HasHint = true; IsHintError = true;
            }
        });
    }

    private void ScheduleRedMove()
    {
        _redMoveTimer?.Stop();
        _redMoveTimer = new System.Windows.Threading.DispatcherTimer
        { Interval = TimeSpan.FromMilliseconds(500) };
        _redMoveTimer.Tick += (s, e) =>
        {
            _redMoveTimer.Stop(); IsRedThinking = false;
            if (Engine.Phase == GamePhase.RedTurn && IsGameActive) Engine.ExecuteRedMove();
        };
        _redMoveTimer.Start();
    }

    private void OnGameOver(string message)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            if (CurrentMode == AppMode.Training) { IsGameActive = false; IsTrainingComplete = true; }
            GameStatus = message; CurrentSide = "--";
            OnPropertyChanged(nameof(CanUndo)); OnPropertyChanged(nameof(CanGoPrevious)); OnPropertyChanged(nameof(CanGoForward));
        });
    }

    private void OnStateChanged()
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            OnPropertyChanged(nameof(CanUndo)); OnPropertyChanged(nameof(CanGoPrevious)); OnPropertyChanged(nameof(CanGoForward));
            UpdateProgress();
        });
    }

    private void AddMoveToHistory(Move move)
    {
        if (move.Side == Side.Red)
        {
            var vm = new MoveViewModel { StepNumber = (MoveHistoryDisplay.Count / 2) + 1, RedNotation = move.Notation };
            MoveHistoryDisplay.Add(vm);
        }
        else
        {
            var last = MoveHistoryDisplay.LastOrDefault();
            if (last != null && !last.HasBlackMove)
            {
                last.BlackNotation = move.Notation;
                var idx = MoveHistoryDisplay.IndexOf(last);
                MoveHistoryDisplay[idx] = last;
            }
            else MoveHistoryDisplay.Add(new MoveViewModel { StepNumber = (MoveHistoryDisplay.Count / 2) + 1, BlackNotation = move.Notation });
        }
    }

    private void RebuildMoveHistoryDisplay()
    {
        MoveHistoryDisplay.Clear();
        foreach (var move in Engine.MoveHistory) AddMoveToHistory(move);
        UpdateMoveHighlight();
    }

    private void UpdateMoveHighlight()
    {
        for (int i = 0; i < MoveHistoryDisplay.Count; i++)
        {
            int s = (i + 1) * 2 - 1;
            int e = MoveHistoryDisplay[i].HasBlackMove ? s + 1 : s;
            MoveHistoryDisplay[i].IsCurrentStep = CurrentMode == AppMode.Review && Engine.NavigationStep >= s && Engine.NavigationStep <= e;
        }
    }

    private void UpdateProgress()
    {
        var (c, t) = Engine.CurrentRecord != null ? (Engine.NavigationStep, Engine.CurrentRecord.TotalSteps) : (0, 0);
        StepProgress = $"第 {c} / {t} 步";
    }

    private void UpdateCapturedPieces()
    {
        CapturedByRed = string.Join(" ", Engine.Board.AllPieces.Where(p => !p.IsAlive && p.Side == Side.Red).Select(p => p.DisplayChar));
        CapturedByBlack = string.Join(" ", Engine.Board.AllPieces.Where(p => !p.IsAlive && p.Side == Side.Black).Select(p => p.DisplayChar));
    }

    [RelayCommand] public void Undo()
    {
        if (!Engine.UndoLastPair()) return;
        var last = MoveHistoryDisplay.LastOrDefault();
        if (last != null && last.HasBlackMove) { last.BlackNotation = ""; MoveHistoryDisplay[^1] = last; }
        else if (MoveHistoryDisplay.Count > 0) MoveHistoryDisplay.RemoveAt(MoveHistoryDisplay.Count - 1);
        HintMessage = ""; HasHint = false; IsHintError = false;
        UpdateProgress(); UpdateCapturedPieces();
    }

    [RelayCommand] public void Restart() { StopAutoPlay(); if (CurrentMode == AppMode.Review) StartReview(); else StartTraining(); }

    [RelayCommand] public void ShowHint()
    {
        var hint = Engine.GetHint();
        if (hint != null) { HintMessage = $"💡 正确走法：{hint.Notation}" + (hint.Hints is { Count: > 0 } ? "\n" + string.Join("\n", hint.Hints) : ""); HasHint = true; IsHintError = false; }
        else { HintMessage = "当前无提示信息"; HasHint = true; IsHintError = false; }
    }

    [RelayCommand] public void GoToFirst() { StopAutoPlay(); Engine.GoToFirst(); RebuildMoveHistoryDisplay(); UpdateProgress(); UpdateCapturedPieces(); }
    [RelayCommand] public void GoToLast() { StopAutoPlay(); Engine.GoToLast(); RebuildMoveHistoryDisplay(); UpdateProgress(); UpdateCapturedPieces(); }
    [RelayCommand] public void PreviousStep() { StopAutoPlay(); Engine.PreviousStep(); RebuildMoveHistoryDisplay(); UpdateProgress(); UpdateCapturedPieces(); }
    [RelayCommand] public void NextStep() { StopAutoPlay(); Engine.NextStep(); RebuildMoveHistoryDisplay(); UpdateProgress(); UpdateCapturedPieces(); }

    [RelayCommand] public void ToggleAutoPlay() { if (IsAutoPlaying) StopAutoPlay(); else StartAutoPlay(); }

    private void StartAutoPlay()
    {
        if (CurrentMode != AppMode.Review) return;
        StopAutoPlay();
        if (!Engine.CanGoForward) return;
        IsAutoPlaying = true;
        _autoPlayTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(Math.Max(1, AutoPlaySpeed)) };
        _autoPlayTimer.Tick += OnAutoPlayTick;
        _autoPlayTimer.Start();
    }

    private void OnAutoPlayTick(object? sender, EventArgs e)
    {
        if (!IsAutoPlaying || !Engine.CanGoForward) { StopAutoPlay(); return; }
        Engine.NextStep(); RebuildMoveHistoryDisplay(); UpdateProgress(); UpdateCapturedPieces();
        if (!Engine.CanGoForward) StopAutoPlay();
    }

    private void StopAutoPlay()
    {
        if (_autoPlayTimer != null) { _autoPlayTimer.Stop(); _autoPlayTimer.Tick -= OnAutoPlayTick; _autoPlayTimer = null; }
        IsAutoPlaying = false;
    }
}
