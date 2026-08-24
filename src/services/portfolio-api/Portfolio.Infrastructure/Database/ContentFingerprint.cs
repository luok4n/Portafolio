using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Portfolio.Infrastructure.Database;

/// <summary>
/// A stable hash of the content files, used to decide whether the database needs reseeding.
/// </summary>
/// <remarks>
/// Without it, every deployment would rewrite every row whether or not anything changed — noisy,
/// slower than it needs to be, and it makes "when did this content last actually change?"
/// unanswerable. Files are hashed in name order so the result does not depend on how the file
/// system happens to enumerate them.
/// </remarks>
internal static class ContentFingerprint
{
    public static string Compute(string directory)
    {
        var files = Directory.EnumerateFiles(directory, "*.json", SearchOption.TopDirectoryOnly)
            .OrderBy(Path.GetFileName, StringComparer.Ordinal)
            .ToList();

        using var sha = SHA256.Create();
        using var buffer = new MemoryStream();

        foreach (var file in files)
        {
            var name = Encoding.UTF8.GetBytes(Path.GetFileName(file) + "\n");
            buffer.Write(name, 0, name.Length);

            using var stream = File.OpenRead(file);
            stream.CopyTo(buffer);
            buffer.WriteByte((byte)'\n');
        }

        buffer.Position = 0;
        return Convert.ToHexString(sha.ComputeHash(buffer)).ToLower(CultureInfo.InvariantCulture);
    }
}
