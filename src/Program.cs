using System.Text;
using AutoUpdate;

Console.OutputEncoding = Encoding.UTF8;

var version = typeof(Updater).Assembly.GetName().Version;
Console.WriteLine($"=== AutoUpdate App v{version} ===\n");

// 1. Kiểm tra bản cập nhật
Console.WriteLine("Đang kiểm tra cập nhật...");
var update = await Updater.CheckAsync("GunDx2k4/auto-update-dotnet");

if (update is not null)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"\n[★] Phát hiện phiên bản mới: v{update.Version}");
    if (!string.IsNullOrWhiteSpace(update.ReleaseNotes))
    {
        Console.WriteLine($"\n--- Nội dung cập nhật ---\n{update.ReleaseNotes}\n--------------------------");
    }
    Console.ResetColor();

    Console.Write("\nBạn có muốn cập nhật ngay không? (y/N): ");
    var key = Console.IsInputRedirected ? 'n' : Console.ReadKey().KeyChar;
    Console.WriteLine();

    if (key is 'y' or 'Y')
    {
        Console.WriteLine("Đang tải bản cập nhật...");
        await Updater.ApplyAsync(update, percent =>
        {
            Console.Write($"\rTiến độ: {percent:F1}%   ");
        });
    }
}

// 2. Logic chính của app
Console.WriteLine("\nApp đang chạy bình thường...");
Console.WriteLine("Nhấn phím bất kỳ để thoát...");
if (!Console.IsInputRedirected)
{
    Console.ReadKey();
}
