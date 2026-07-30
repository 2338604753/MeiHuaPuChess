using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json.Serialization;

namespace MeiHuaPuChess.App.Services;

/// <summary>
/// GitHub Releases 更新检查服务
/// </summary>
public class UpdateService
{
    private const string RepoOwner = "2338604753";
    private const string RepoName = "MeiHuaPuChess";

    private static readonly HttpClient _http = new()
    {
        DefaultRequestHeaders = { { "User-Agent", "MeiHuaPuChess" } },
        Timeout = TimeSpan.FromSeconds(10)
    };

    /// <summary>
    /// 当前应用版本
    /// </summary>
    public static string CurrentVersion =>
        Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";

    /// <summary>
    /// 检查 GitHub 最新 Release
    /// </summary>
    public async Task<UpdateCheckResult> CheckAsync()
    {
        try
        {
            var url = $"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases/latest";
            var release = await _http.GetFromJsonAsync<GitHubRelease>(url);

            if (release is null)
                return new UpdateCheckResult { IsLatest = true, Error = "无法获取更新信息" };

            var latestVersion = release.TagName.TrimStart('v');
            var isLatest = CompareVersions(CurrentVersion, latestVersion) >= 0;

            return new UpdateCheckResult
            {
                IsLatest = isLatest,
                CurrentVersion = CurrentVersion,
                LatestVersion = latestVersion,
                ReleaseName = release.Name ?? release.TagName,
                ReleaseNotes = release.Body ?? "",
                DownloadUrl = release.HtmlUrl
            };
        }
        catch (Exception ex)
        {
            return new UpdateCheckResult
            {
                IsLatest = true,
                Error = $"检查更新失败：{ex.Message}"
            };
        }
    }

    /// <summary>
    /// 比较版本号，返回 1(v1>v2) / 0(相等) / -1(v1<v2)
    /// </summary>
    private static int CompareVersions(string v1, string v2)
    {
        var parts1 = v1.Split('.', '+', '-')[0].Split('.');
        var parts2 = v2.Split('.', '+', '-')[0].Split('.');
        for (int i = 0; i < Math.Max(parts1.Length, parts2.Length); i++)
        {
            int n1 = i < parts1.Length && int.TryParse(parts1[i], out var x) ? x : 0;
            int n2 = i < parts2.Length && int.TryParse(parts2[i], out var y) ? y : 0;
            if (n1 != n2) return n1.CompareTo(n2);
        }
        return 0;
    }
}

public class UpdateCheckResult
{
    public bool IsLatest { get; set; }
    public string CurrentVersion { get; set; } = "";
    public string LatestVersion { get; set; } = "";
    public string ReleaseName { get; set; } = "";
    public string ReleaseNotes { get; set; } = "";
    public string DownloadUrl { get; set; } = "";
    public string? Error { get; set; }
}

public class GitHubRelease
{
    [JsonPropertyName("tag_name")]
    public string TagName { get; set; } = "";

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("body")]
    public string? Body { get; set; }

    [JsonPropertyName("html_url")]
    public string HtmlUrl { get; set; } = "";
}
