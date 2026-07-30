using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MeiHuaPuChess.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using MeiHuaPuChess.Core.Services;
using MeiHuaPuChess.Data;

namespace MeiHuaPuChess.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();

        // 快捷键
        KeyDown += OnWindowKeyDown;

        // 设置 DI
        var services = new ServiceCollection();
        services.AddSingleton<IMeiHuaPuDataLoader, MeiHuaPuDataLoader>();
        services.AddSingleton<IGameService, GameService>();
        services.AddSingleton<MainViewModel>();
        var provider = services.BuildServiceProvider();

        _viewModel = provider.GetRequiredService<MainViewModel>();
        DataContext = _viewModel;

        // 设置棋盘引用
        ChessBoard.Engine = _viewModel.Engine;
        ChessBoard.OnPlayerMove += _viewModel.OnPlayerMove;
        ChessBoard.OnEditMove += (fR, fC, tR, tC) =>
        {
            _viewModel.OnEditMove(fR, fC, tR, tC);
            Dispatcher.Invoke(() => ChessBoard.DrawPieces());
        };

        // 编辑模式切换：同步 BoardView 状态
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.IsEditing))
            {
                ChessBoard.IsEditMode = _viewModel.IsEditing;
                ChessBoard.IsReadOnly = _viewModel.IsEditing; // 编辑时禁止正常走棋
                ChessBoard.ClearSelectionPublic();
                ChessBoard.DrawPieces();
            }
        };

        // 订阅引擎事件以刷新棋盘
        _viewModel.Engine.OnStateChanged += () =>
        {
            Dispatcher.Invoke(() =>
            {
                ChessBoard.DrawPieces();
                // 导航时清除选中状态
                ChessBoard.ClearSelectionPublic();
            });
        };
        _viewModel.Engine.OnRedMoveCompleted += (_) =>
        {
            Dispatcher.Invoke(() =>
            {
                ChessBoard.DrawPieces();
                SystemSounds.Asterisk.Play();
            });
        };
        _viewModel.Engine.OnBlackMoveValidated += (result, hints) =>
        {
            Dispatcher.Invoke(() =>
            {
                ChessBoard.DrawPieces();
                if (result == Core.Engine.MatchResult.Correct)
                    SystemSounds.Asterisk.Play();
                else
                    SystemSounds.Hand.Play();
            });
        };

        // 加载数据
        Loaded += (s, e) =>
        {
            _viewModel.LoadRecords();
            // 默认选择第一局（不自动开始）
            if (_viewModel.MeiHuaPuRecords.Count > 0)
            {
                _viewModel.SelectedRecord = _viewModel.MeiHuaPuRecords[0];
            }
        };
    }

    private void OnThemeClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string theme) return;
        var rd = (ResourceDictionary)Resources;
        rd.MergedDictionaries.Clear();
        rd.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri($"Themes/{theme}.xaml", UriKind.Relative)
        });
    }

    private async void OnCheckUpdateClick(object sender, RoutedEventArgs e)
    {
        var dlg = new Views.UpdateDialog { Owner = this };
        dlg.Show();
        var service = new Services.UpdateService();
        var result = await service.CheckAsync();
        dlg.ShowResult(result);
    }

    // ================================================================
    //  快捷键
    // ================================================================

    private void OnWindowKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Space:
                _viewModel.ToggleAutoPlayCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.Left:
                _viewModel.PreviousStepCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.Right:
                _viewModel.NextStepCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.F:
                ChessBoard.ToggleFlip();
                e.Handled = true;
                break;
            case Key.T:
                Topmost = !Topmost;
                e.Handled = true;
                break;
        }
    }

    private void OnToggleTopmostClick(object sender, RoutedEventArgs e)
    {
        Topmost = !Topmost;
    }

    private void OnFlipBoardClick(object sender, RoutedEventArgs e)
    {
        ChessBoard.ToggleFlip();
    }
}
