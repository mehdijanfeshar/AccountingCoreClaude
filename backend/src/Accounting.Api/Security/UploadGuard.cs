using System.Text;

namespace Accounting.Api.Security;

/// <summary>
/// بررسی فایل بارگذاری‌شده پیش از رسیدن به Application: اندازه، پسوند مجاز، تطبیق امضای واقعی فایل (magic bytes)
/// و پاک‌سازی نام. <c>Content-Type</c> ارسالی کلاینت قابل اعتماد نیست و اینجا استفاده نمی‌شود.
/// </summary>
public static class UploadGuard
{
    /// <summary>سقف اندازهٔ درخواست‌های بارگذاری (بایت). از سقف خود فایل کمی بیشتر است تا سربار multipart جا شود.</summary>
    public const long AttachmentRequestLimit = 11 * 1024 * 1024;

    public const long DiskFileMaxBytes = 20 * 1024 * 1024;
    public const long DiskRequestLimit = 21 * 1024 * 1024;

    private static readonly Dictionary<string, byte[][]> Signatures = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = [Encoding.ASCII.GetBytes("%PDF-")],
        [".png"] = [[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]],
        [".jpg"] = [[0xFF, 0xD8, 0xFF]],
        [".jpeg"] = [[0xFF, 0xD8, 0xFF]],
        [".gif"] = [Encoding.ASCII.GetBytes("GIF87a"), Encoding.ASCII.GetBytes("GIF89a")],
        [".docx"] = [[0x50, 0x4B, 0x03, 0x04]],
        [".xlsx"] = [[0x50, 0x4B, 0x03, 0x04]],
        [".zip"] = [[0x50, 0x4B, 0x03, 0x04]],
        [".doc"] = [[0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1]],
        [".xls"] = [[0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1]],
    };

    /// <summary>پسوندهای مجاز پیوست (تنخواه). <c>.txt</c> امضا ندارد و با بررسی «بدون بایت کنترلی» سنجیده می‌شود.</summary>
    public static readonly string[] AttachmentExtensions =
        [".pdf", ".png", ".jpg", ".jpeg", ".gif", ".doc", ".docx", ".xls", ".xlsx", ".zip", ".txt"];

    /// <summary>پیوست: پسوند مجاز + امضای مطابق پسوند. <c>null</c> یعنی سالم؛ وگرنه پیام خطا.</summary>
    public static async Task<string?> ValidateAttachmentAsync(IFormFile file, int maxBytes, CancellationToken ct)
    {
        if (file.Length > maxBytes)
        {
            return "حجم فایل بیشتر از حد مجاز است.";
        }

        var ext = Path.GetExtension(file.FileName ?? string.Empty);
        if (!AttachmentExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
        {
            return "نوع فایل مجاز نیست.";
        }

        var head = new byte[512];
        int read;
        await using (var s = file.OpenReadStream())
        {
            read = await s.ReadAsync(head.AsMemory(0, head.Length), ct);
        }

        if (ext.Equals(".txt", StringComparison.OrdinalIgnoreCase))
        {
            return head.Take(read).Any(b => b == 0) ? "محتوای فایل با پسوند آن نمی‌خواند." : null;
        }

        var ok = Signatures.TryGetValue(ext, out var sigs)
            && sigs.Any(sig => read >= sig.Length && head.AsSpan(0, sig.Length).SequenceEqual(sig));
        if (!ok)
        {
            return "محتوای فایل با پسوند آن نمی‌خواند.";
        }

        return await IsStructurallyIntactAsync(file, ext, ct) ? null : "فایل خراب یا ناقص است.";
    }

    private const int MaxZipEntries = 2000;
    private const long MaxZipUncompressedBytes = 200L * 1024 * 1024;

    /// <summary>سالم‌بودن ساختار: پایان‌نشانهٔ PDF/PNG/JPEG/GIF، باز‌شدن zip/OOXML (با سقف برای zip bomb).</summary>
    private static async Task<bool> IsStructurallyIntactAsync(IFormFile file, string ext, CancellationToken ct)
    {
        await using var s = file.OpenReadStream();

        if (ext is ".docx" or ".xlsx" or ".zip")
        {
            return IsValidZip(s, ooxml: ext != ".zip");
        }

        if (ext is ".doc" or ".xls")
        {
            return file.Length >= 512; // سرآیند OLE سالم دست‌کم یک سکتور ۵۱۲ بایتی است.
        }

        // انتهای فایل (برخی فایل‌ها پس از نشانهٔ پایان بایت‌های اضافه دارند، پس ۱ کیلوبایت آخر بررسی می‌شود).
        var tailLen = (int)Math.Min(file.Length, 1024);
        var tail = new byte[tailLen];
        s.Seek(file.Length - tailLen, SeekOrigin.Begin);
        await s.ReadExactlyAsync(tail.AsMemory(), ct);

        return ext switch
        {
            ".pdf" => tail.AsSpan().IndexOf("%%EOF"u8) >= 0,
            ".png" => tail.AsSpan().IndexOf(new byte[] { 0x49, 0x45, 0x4E, 0x44 }) >= 0, // IEND
            ".jpg" or ".jpeg" => tail.AsSpan().IndexOf(new byte[] { 0xFF, 0xD9 }) >= 0,
            ".gif" => tail.AsSpan().IndexOf((byte)0x3B) >= 0,
            _ => true,
        };
    }

    private static bool IsValidZip(Stream stream, bool ooxml)
    {
        try
        {
            using var zip = new System.IO.Compression.ZipArchive(stream, System.IO.Compression.ZipArchiveMode.Read, leaveOpen: true);
            if (zip.Entries.Count == 0 || zip.Entries.Count > MaxZipEntries)
            {
                return false;
            }

            long total = 0;
            foreach (var e in zip.Entries)
            {
                // مسیر خروجی از پوشه بیرون می‌زند (zip-slip) یا حجم بازشده غیرعادی است (zip bomb).
                if (e.FullName.Contains("..", StringComparison.Ordinal) || Path.IsPathRooted(e.FullName))
                {
                    return false;
                }

                total += e.Length;
                if (total > MaxZipUncompressedBytes)
                {
                    return false;
                }
            }

            return !ooxml || zip.GetEntry("[Content_Types].xml") is not null;
        }
        catch (InvalidDataException)
        {
            return false;
        }
    }

    /// <summary>فایل دیسکت بانک (قالب متنی): فقط سقف اندازه؛ فایل باینری (بایت صفر) رد می‌شود.</summary>
    public static async Task<string?> ValidateDiskFileAsync(IFormFile file, CancellationToken ct)
    {
        if (file.Length > DiskFileMaxBytes)
        {
            return "حجم فایل بیشتر از حد مجاز است.";
        }

        var head = new byte[4096];
        int read;
        await using (var s = file.OpenReadStream())
        {
            read = await s.ReadAsync(head.AsMemory(0, head.Length), ct);
        }

        return head.Take(read).Any(b => b == 0) ? "فایل دیسکت باید متنی باشد." : null;
    }

    /// <summary>نام فایل بدون مسیر و نویسهٔ کنترلی/غیرمجاز، حداکثر ۲۵۵ نویسه.</summary>
    public static string SafeFileName(string? name)
    {
        var leaf = Path.GetFileName((name ?? string.Empty).Replace('\\', '/'));
        var cleaned = new string(leaf.Where(c => !char.IsControl(c) && Array.IndexOf(Path.GetInvalidFileNameChars(), c) < 0).ToArray()).Trim();
        if (cleaned.Length == 0)
        {
            cleaned = "file";
        }

        return cleaned.Length > 255 ? cleaned[^255..] : cleaned;
    }
}
