using System.Security.Cryptography;

namespace ImmersingLinker.Core.Services.Storage;

internal static class StorageKeyHelper
{
    private const string KeyFileName = ".storage-key";

    public static (byte[] key, byte[] iv) LoadOrGenerateKey(string dataDir)
    {
        var keyPath = Path.Combine(dataDir, KeyFileName);

        if (File.Exists(keyPath))
        {
            var raw = File.ReadAllBytes(keyPath);
            return (raw[..32], raw[32..48]);
        }

        using var aes = Aes.Create();
        aes.GenerateKey();
        aes.GenerateIV();
        var combined = aes.Key.Concat(aes.IV).ToArray();
        File.WriteAllBytes(keyPath, combined);
        return (aes.Key, aes.IV);
    }

    public static byte[] Encrypt(byte[] plaintext, byte[] key, byte[] iv)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        using var ms = new MemoryStream();
        using var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write);
        cs.Write(plaintext);
        cs.FlushFinalBlock();
        return ms.ToArray();
    }

    public static byte[] Decrypt(byte[] ciphertext, byte[] key, byte[] iv)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        using var ms = new MemoryStream(ciphertext);
        using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
        using var result = new MemoryStream();
        cs.CopyTo(result);
        return result.ToArray();
    }
}
