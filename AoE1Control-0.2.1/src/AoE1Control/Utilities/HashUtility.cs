using System.Security.Cryptography;

namespace AoE1Control.Utilities;

internal static class HashUtility
{
    internal static string ComputeSha256(string path)
    {
        using FileStream stream = new(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete);

        byte[] hash = SHA256.HashData(stream);
        return Convert.ToHexStringLower(hash);
    }
}
