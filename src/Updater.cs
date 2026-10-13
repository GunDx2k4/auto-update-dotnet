using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;

namespace AutoUpdate;

public static class Updater
{
    private static readonly HttpClient Http = new()
    {
        DefaultRequestHeaders = { { "User-Agent", "AutoUpdate" } }
    };

    public static async Task CheckAndApplyAsync(string repo = "GunDx2k4/auto-update-dotnet")
    {
        string currentExe = Environment.ProcessPath ?? "";
        if (string.IsNullOrEmpty(currentExe) || !File.Exists(currentExe)) return;

        string oldExe = currentExe + ".old";
        if (File.Exists(oldExe))
        {
            try { File.Delete(oldExe); } catch { }
        }

        try
        {
            using var res = await Http.GetAsync($"https://api.github.com/repos/{repo}/releases/latest");
            if (!res.IsSuccessStatusCode) return;

            var json = await res.Content.ReadFromJsonAsync<JsonElement>();
            string tag = json.GetProperty("tag_name").GetString()?.TrimStart('v') ?? "";
            var currentVersion = typeof(Updater).Assembly.GetName().Version;

            if (!Version.TryParse(tag, out var newVersion) || newVersion <= currentVersion) return;

            string? downloadUrl = null;
            foreach (var asset in json.GetProperty("assets").EnumerateArray())
            {
                string name = asset.GetProperty("name").GetString() ?? "";
                if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    downloadUrl = asset.GetProperty("browser_download_url").GetString();
                    break;
                }
            }

            if (string.IsNullOrEmpty(downloadUrl)) return;

            Console.WriteLine($"[AutoUpdate] Phát hiện bản mới v{tag} (hiện tại v{currentVersion}). Đang tải...");

            string tempExe = Path.Combine(Path.GetTempPath(), $"update_{Guid.NewGuid():N}.exe");
            var bytes = await Http.GetByteArrayAsync(downloadUrl);
            await File.WriteAllBytesAsync(tempExe, bytes);

            File.Move(currentExe, oldExe);
            File.Move(tempExe, currentExe);

            Console.WriteLine("[AutoUpdate] Cập nhật thành công! Đang khởi động lại...");

            Process.Start(new ProcessStartInfo(currentExe) { UseShellExecute = true });
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AutoUpdate] Bỏ qua cập nhật (lỗi: {ex.Message})");
        }
    }
}
