using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MeiHuaPuChess.Core.Engine;
using MeiHuaPuChess.Core.Enums;
using MeiHuaPuChess.Core.Models;
using MeiHuaPuChess.Core.Services;

namespace MeiHuaPuChess.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IGameService _gameService;

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

    /// <summary>切换模式</summary>
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
    //  棋局列表
    // ================================================================

    public ObservableCollection<MeiHuaPuRecord> Records { get; } = new();

    [ObservableProperty]
    private MeiHuaPuRecord? _selectedRecord;

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

    // ================================================================
    //  导航 / 操作可用性
    // ================================================================

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

    // ================================================================
    //  走棋记录
    // ================================================================

    public ObservableCollection<MoveViewModel> MoveHistoryDisplay { get; } = new();

    // ================================================================
    //  提示信息
    // ================================================================

    [ObservableProperty]
    private string _hintMessage = "";

    [ObservableProperty]
    private bool _hasHint;

    [ObservableProperty]
    private bool _isHintError; // true = 红色错误提示, false = 蓝色信息提示

    // ================================================================
    //  被吃棋子
    // ================================================================

    [ObservableProperty]
    private string _capturedByRed = "";

    [ObservableProperty]
    private string _capturedByBlack = "";

    // ================================================================
    //  红方走棋动画
    // ================================================================

    [ObservableProperty]
    private bool _isRedThinking;

    private System.Windows.Threading.DispatcherTimer? _redMoveTimer;

    // ================================================================
    //  自动播放（Review 模式）
    // ================================================================

    private System.Windows.Threading.DispatcherTimer? _autoPlayTimer;

    [ObservableProperty]
    private bool _isAutoPlaying;

    [ObservableProperty]
    private int _autoPlaySpeed = 1000; // 毫秒

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

    // ================================================================
    //  数据加载
    // ================================================================

    [RelayCommand]
    public void LoadRecords()
    {
        _gameService.LoadRecords();
        Records.Clear();
        foreach (var record in _gameService.AvailableRecords)
        {
            Records.Add(record);
        }
        GameStatus = Records.Count > 0
            ? $"已加载 {Records.Count} 局梅花谱，请选择棋局"
            : "未找到棋局数据";
    }

    // ================================================================
    //  开始（按模式）
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

    // ================================================================
    //  黑方走棋（由棋盘点击触发）
    // ================================================================

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

    // ================================================================
    //  引擎事件处理
    // ================================================================

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
                if (lastMove != null)
                {
                    AddMoveToHistory(lastMove);
                }
                UpdateProgress();
                UpdateCapturedPieces();
                UpdateMoveHighlight();
                HintMessage = "";
                HasHint = false;
                IsHintError = false;
                CurrentSide = "红方走棋...";
                IsRedThinking = true;
                ScheduleRedMove();
            }
            else if (result == MatchResult.Incorrect)
            {
                if (hints != null && hints.Count > 0)
                {
                    HintMessage = "❌ 不对！\n" + string.Join("\n", hints);
                }
                else
                {
                    HintMessage = "❌ 走法不正确！请参照梅花谱走法";
                }
                HasHint = true;
                IsHintError = true;
            }
        });
    }

    private void ScheduleRedMove()
    {
        _redMoveTimer?.Stop();
        _redMoveTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _redMoveTimer.Tick += (s, e) =>
        {
            _redMoveTimer.Stop();
            IsRedThinking = false;
            if (Engine.Phase == GamePhase.RedTurn && IsGameActive)
            {
                Engine.ExecuteRedMove();
            }
        };
        _redMoveTimer.Start();
    }

    private void OnGameOver(string message)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            if (CurrentMode == AppMode.Training)
            {
                IsGameActive = false;
                IsTrainingComplete = true;
            }
            GameStatus = message;
            CurrentSide = "--";
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanGoPrevious));
            OnPropertyChanged(nameof(CanGoForward));
        });
    }

    private void OnStateChanged()
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanGoPrevious));
            OnPropertyChanged(nameof(CanGoForward));
            UpdateProgress();
        });
    }

    // ================================================================
    //  走棋记录管理
    // ================================================================

    private void AddMoveToHistory(Move move)
    {
        if (move.Side == Side.Red)
        {
            // 新增一步：红方走法
            var vm = new MoveViewModel
            {
                StepNumber = (MoveHistoryDisplay.Count / 2) + 1,
                RedNotation = move.Notation
            };
            MoveHistoryDisplay.Add(vm);
        }
        else
        {
            // 黑方走法追加到最后一条
            var last = MoveHistoryDisplay.LastOrDefault();
            if (last != null && !last.HasBlackMove)
            {
                last.BlackNotation = move.Notation;
                // 触发 UI 更新
                var idx = MoveHistoryDisplay.IndexOf(last);
                MoveHistoryDisplay[idx] = last;
            }
            else
            {
                var vm = new MoveViewModel
                {
                    StepNumber = (MoveHistoryDisplay.Count / 2) + 1,
                    BlackNotation = move.Notation
                };
                MoveHistoryDisplay.Add(vm);
            }
        }
    }

    private void RebuildMoveHistoryDisplay()
    {
        MoveHistoryDisplay.Clear();
        foreach (var move in Engine.MoveHistory)
        {
            AddMoveToHistory(move);
        }
        UpdateMoveHighlight();
    }

    private void UpdateMoveHighlight()
    {
        // 在 Review 模式高亮当前步
        for (int i = 0; i < MoveHistoryDisplay.Count; i++)
        {
            int stepStart = (i + 1) * 2 - 1; // 这步对应的第一步（红方）
            int stepEnd = MoveHistoryDisplay[i].HasBlackMove ? stepStart + 1 : stepStart;
            MoveHistoryDisplay[i].IsCurrentStep =
                CurrentMode == AppMode.Review
                && Engine.NavigationStep >= stepStart
                && Engine.NavigationStep <= stepEnd;
        }
    }

    // ================================================================
    //  进度 / 棋子统计
    // ================================================================

    private void UpdateProgress()
    {
        var (current, total) = Engine.CurrentRecord != null
            ? (Engine.NavigationStep, Engine.CurrentRecord.TotalSteps)
            : (0, 0);
        StepProgress = $"第 {current} / {total} 步";
    }

    private void UpdateCapturedPieces()
    {
        var redCaptured = Engine.Board.AllPieces
            .Where(p => !p.IsAlive && p.Side == Side.Red)
            .Select(p => p.DisplayChar);
        var blackCaptured = Engine.Board.AllPieces
            .Where(p => !p.IsAlive && p.Side == Side.Black)
            .Select(p => p.DisplayChar);

        CapturedByRed = string.Join(" ", redCaptured);
        CapturedByBlack = string.Join(" ", blackCaptured);
    }

    // ================================================================
    //  训练操作
    // ================================================================

    [RelayCommand]
    public void Undo()
    {
        if (Engine.UndoLastPair())
        {
            // 移除最后一条黑方走法
            var last = MoveHistoryDisplay.LastOrDefault();
            if (last != null && last.HasBlackMove)
            {
                last.BlackNotation = "";
                var idx = MoveHistoryDisplay.Count - 1;
                MoveHistoryDisplay[idx] = last;
            }
            else if (MoveHistoryDisplay.Count > 0)
            {
                MoveHistoryDisplay.RemoveAt(MoveHistoryDisplay.Count - 1);
            }
            HintMessage = "";
            HasHint = false;
            IsHintError = false;
            UpdateProgress();
            UpdateCapturedPieces();
        }
    }

    [RelayCommand]
    public void Restart()
    {
        StopAutoPlay();
        if (CurrentMode == AppMode.Review)
            StartReview();
        else
            StartTraining();
    }

    [RelayCommand]
    public void ShowHint()
    {
        var hint = Engine.GetHint();
        if (hint != null)
        {
            var hintText = $"💡 正确走法：{hint.Notation}";
            if (hint.Hints != null && hint.Hints.Count > 0)
            {
                hintText += "\n" + string.Join("\n", hint.Hints);
            }
            HintMessage = hintText;
            HasHint = true;
            IsHintError = false;
        }
        else
        {
            HintMessage = "当前无提示信息";
            HasHint = true;
            IsHintError = false;
        }
    }

    // ================================================================
    //  Review 导航
    // ================================================================

    [RelayCommand]
    public void GoToFirst()
    {
        StopAutoPlay();
        Engine.GoToFirst();
        RebuildMoveHistoryDisplay();
        UpdateProgress();
        UpdateCapturedPieces();
    }

    [RelayCommand]
    public void GoToLast()
    {
        StopAutoPlay();
        Engine.GoToLast();
        RebuildMoveHistoryDisplay();
        UpdateProgress();
        UpdateCapturedPieces();
    }

    [RelayCommand]
    public void PreviousStep()
    {
        StopAutoPlay();
        Engine.PreviousStep();
        RebuildMoveHistoryDisplay();
        UpdateProgress();
        UpdateCapturedPieces();
    }

    [RelayCommand]
    public void NextStep()
    {
        StopAutoPlay();
        Engine.NextStep();
        RebuildMoveHistoryDisplay();
        UpdateProgress();
        UpdateCapturedPieces();
    }

    // ================================================================
    //  自动播放
    // ================================================================

    [RelayCommand]
    public void ToggleAutoPlay()
    {
        if (IsAutoPlaying)
            StopAutoPlay();
        else
            StartAutoPlay();
    }

    private void StartAutoPlay()
    {
        if (CurrentMode != AppMode.Review) return;

        StopAutoPlay();
        if (!Engine.CanGoForward) return;

        IsAutoPlaying = true;
        _autoPlayTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(Math.Max(1, AutoPlaySpeed))
        };
        _autoPlayTimer.Tick += OnAutoPlayTick;
        _autoPlayTimer.Start();
    }

    private void OnAutoPlayTick(object? sender, EventArgs e)
    {
        if (!IsAutoPlaying || !Engine.CanGoForward)
        {
            StopAutoPlay();
            return;
        }

        Engine.NextStep();
        RebuildMoveHistoryDisplay();
        UpdateProgress();
        UpdateCapturedPieces();

        if (!Engine.CanGoForward)
            StopAutoPlay();
    }

    private void StopAutoPlay()
    {
        if (_autoPlayTimer != null)
        {
            _autoPlayTimer.Stop();
            _autoPlayTimer.Tick -= OnAutoPlayTick;
            _autoPlayTimer = null;
        }
        IsAutoPlaying = false;
    }
}
