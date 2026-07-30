using System.Diagnostics;
using System.Windows;
using MeiHuaPuChess.App.Services;

namespace MeiHuaPuChess.App.Views;

public partial class UpdateDialog : Window
{
    private string _downloadUrl = "";

    public UpdateDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 显示检查结果
    /// </summary>
    public void ShowResult(UpdateCheckResult result)
    {
        // 隐藏加载状态
        LoadingPanel.Visibility = Visibility.Collapsed;

        if (result.Error != null)
        {
            ErrorPanel.Visibility = Visibility.Visible;
            ErrorText.Text = result.Error;
            TitleText.Text = "检查失败";
        }
        else if (result.IsLatest)
        {
            LatestPanel.Visibility = Visibility.Visible;
            LatestVersionLabel.Text = $"v{result.CurrentVersion}（最新）";
            TitleText.Text = "检查更新";
        }
        else
        {
            UpdatePanel.Visibility = Visibility.Visible;
            DownloadBtn.Visibility = Visibility.Visible;
            CloseBtn.Content = "稍后再说";
            OldVersionLabel.Text = $"v{result.CurrentVersion}";
            NewVersionLabel.Text = $"v{result.LatestVersion}";
            ReleaseNoteLabel.Text = result.ReleaseName;
            TitleText.Text = "发现新版本";
            _downloadUrl = result.DownloadUrl;
        }
    }

    private void OnDownloadClick(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_downloadUrl))
        {
            Process.Start(new ProcessStartInfo(_downloadUrl) { UseShellExecute = true });
        }
        Close();
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
