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
        if (result.Error != null)
        {
            ErrorPanel.Visibility = Visibility.Visible;
            ErrorText.Text = result.Error;
            TitleText.Text = "检查更新";
        }
        else if (result.IsLatest)
        {
            LatestPanel.Visibility = Visibility.Visible;
            CurrentVersionLabel.Text = $"当前版本 v{result.CurrentVersion}";
            TitleText.Text = "检查更新";
        }
        else
        {
            UpdatePanel.Visibility = Visibility.Visible;
            DownloadBtn.Visibility = Visibility.Visible;
            OldVersionLabel.Text = $"v{result.CurrentVersion}";
            NewVersionLabel.Text = $"v{result.LatestVersion}";
            ReleaseNameLabel.Text = result.ReleaseName;
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
