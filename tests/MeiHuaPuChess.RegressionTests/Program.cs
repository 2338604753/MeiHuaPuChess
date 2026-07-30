using System.Windows;
using System.Windows.Threading;
using MeiHuaPuChess.App;
using MeiHuaPuChess.App.ViewModels;

namespace MeiHuaPuChess.RegressionTests;

internal static class Program
{
    [STAThread]
    private static int Main()
    {
        var application = new Application();
        Exception? dispatcherException = null;
        application.DispatcherUnhandledException += (_, args) =>
        {
            dispatcherException = args.Exception;
            args.Handled = true;
        };

        var window = new MainWindow
        {
            ShowActivated = false,
            WindowState = WindowState.Minimized
        };
        window.Show();

        var viewModel = (MainViewModel)window.DataContext;
        viewModel.LoadRecords();
        viewModel.SelectedRecord = viewModel.Records[0];
        viewModel.StartReview();
        viewModel.AutoPlaySpeed = 1;
        viewModel.ToggleAutoPlay();

        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (viewModel.IsAutoPlaying && DateTime.UtcNow < deadline)
        {
            var frame = new DispatcherFrame();
            _ = new DispatcherTimer(
                TimeSpan.FromMilliseconds(10),
                DispatcherPriority.Background,
                (_, _) => frame.Continue = false,
                Dispatcher.CurrentDispatcher);
            Dispatcher.PushFrame(frame);
        }

        window.Close();

        if (dispatcherException != null)
        {
            Console.Error.WriteLine(dispatcherException);
            return 1;
        }

        var expected = viewModel.SelectedRecord.TotalSteps;
        if (viewModel.Engine.NavigationStep != expected)
        {
            Console.Error.WriteLine(
                $"Auto-play stopped at step {viewModel.Engine.NavigationStep}; expected {expected}.");
            return 1;
        }

        Console.WriteLine($"Auto-play completed all {expected} steps.");
        return 0;
    }
}
