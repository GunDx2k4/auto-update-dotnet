using System.Text;
using AutoUpdate;

Console.OutputEncoding = Encoding.UTF8;

var version = typeof(Updater).Assembly.GetName().Version;
Console.WriteLine($"=== AutoUpdate App v{version} ===");

await Updater.CheckAndApplyAsync("GunDx2k4/auto-update-dotnet");

Console.WriteLine("App đang chạy bình thường...");
Console.WriteLine("Nhấn phím bất kỳ để thoát...");
if (!Console.IsInputRedirected)
{
    Console.ReadKey();
}
