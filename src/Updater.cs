using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;

namespace AutoUpdate;

public record UpdateInfo(
    string Version,
    string ReleaseNotes,
    string DownloadUrl,
    long? SizeBytes = null
);

public static class Updater
{
    private static readonly HttpClient Http = new()
    {
        DefaultRequestHeaders = { { "User-Agent", "AutoUpdate" } }
    };

    /// <summary>
    /// Dọn dẹp file .old từ lần cập nhật trước (nếu có).
    /// </summary>
    public static void CleanUpOld()
    {
        string currentExe = Environment.ProcessPath ?? "";
        if (string.IsNullOrEmpty(currentExe)) return;

        string oldExe = currentExe + ".old";
        if (File.Exists(oldExe))
        {
            try { File.Delete(oldExe); } catch { }
        }
    }

    /// <summary>
    /// Kiểm tra phiên bản mới từ GitHub Release. Trả về UpdateInfo nếu có bản mới, ngược lại null.
    /// </summary>
    public static async Task<UpdateInfo?> CheckAsync(
        string repo = "GunDx2k4/auto-update-dotnet",
        Version? currentVersion = null)
    {
        CleanUpOld();

        currentVersion ??= typeof(Updater).Assembly.GetName().Version;

        using var res = await Http.GetAsync($"https://api.github.com/repos/{repo}/releases/latest");
        if (!res.IsSuccessStatusCode) return null;

        var json = await res.Content.ReadFromJsonAsync<JsonElement>();
        string tag = json.GetProperty("tag_name").GetString()?.TrimStart('v') ?? "";

        if (!Version.TryParse(tag, out var newVersion) || newVersion <= currentVersion) return null;

        string? downloadUrl = null;
        long? sizeBytes = null;

        foreach (var asset in json.GetProperty("assets").EnumerateArray())
        {
            string name = asset.GetProperty("name").GetString() ?? "";
            if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                downloadUrl = asset.GetProperty("browser_download_url").GetString();
                if (asset.TryGetProperty("size", out var sizeProp))
                {
                    sizeBytes = sizeProp.GetInt64();
                }
                break;
            }
        }

        if (string.IsNullOrEmpty(downloadUrl)) return null;

        string notes = json.TryGetProperty("body", out var b) ? b.GetString() ?? "" : "";
        return new UpdateInfo(tag, notes, downloadUrl, sizeBytes);
    }

    /// <summary>
    /// Tải file exe mới, hoán đổi file và khởi động lại ứng dụng.
    /// </summary>
    /// <param name="update">Thông tin bản cập nhật từ CheckAsync.</param>
    /// <param name="onProgress">Callback tiến độ phần trăm từ 0 đến 100.</param>
    public static async Task ApplyAsync(UpdateInfo update, Action<double>? onProgress = null)
    {
        string currentExe = Environment.ProcessPath ?? "";
        if (string.IsNullOrEmpty(currentExe) || !File.Exists(currentExe))
        {
            throw new InvalidOperationException("Không thể xác định file thực thi hiện tại.");
        }

        string oldExe = currentExe + ".old";
        string tempExe = Path.Combine(Path.GetTempPath(), $"update_{Guid.NewGuid():N}.exe");

        // Tải file về thư mục tạm có báo cáo tiến trình
        using var response = await Http.GetAsync(update.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        long totalBytes = response.Content.Headers.ContentLength ?? update.SizeBytes ?? -1;
        await using var contentStream = await response.Content.ReadAsStreamAsync();
        await using (var fileStream = new FileStream(tempExe, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true))
        {
            byte[] buffer = new byte[81920];
            long totalRead = 0;
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                totalRead += bytesRead;

                if (totalBytes > 0 && onProgress != null)
                {
                    onProgress.Invoke((double)totalRead / totalBytes * 100.0);
                }
            }
        }

        // Hoán đổi file: current -> old, temp -> current
        if (File.Exists(oldExe))
        {
            try { File.Delete(oldExe); } catch { }
        }

        File.Move(currentExe, oldExe);
        File.Move(tempExe, currentExe);

        // Khởi động lại ứng dụng
        Process.Start(new ProcessStartInfo(currentExe) { UseShellExecute = true });
        Environment.Exit(0);
    }
}
