using System.Security.Cryptography;
using System.Text;

namespace Accounting.Infrastructure.LegacyImport;

/// <summary>
/// رمزگشایی نام کاربر/رمز فایل XML سرورهای سیستم قدیم، همان روش برنامهٔ قدیم (<c>Encryption.DecryptRSA</c> +
/// <c>XOREncryption</c>): base64 ← RSA با PKCS#1 v1.5 و کلید خصوصی سیستم قدیم (<see cref="LegacyImportOptions.XmlPrivateKey"/>،
/// فقط از تنظیمات/User Secrets) ← هر بایت یک نویسه ← XOR با <see cref="LegacyImportOptions.XmlXorKey"/>.
/// بدون کلید یا با کلید ناسازگار null برمی‌گرداند تا کاربر دستی وارد کند.
/// </summary>
internal sealed class LegacyXmlCredentials(LegacyImportOptions options)
{
    public bool Configured => !string.IsNullOrWhiteSpace(options.XmlPrivateKey) && !string.IsNullOrEmpty(options.XmlXorKey);

    public string? Decrypt(string? base64)
    {
        if (!Configured || string.IsNullOrWhiteSpace(base64)) return null;
        byte[] cipher;
        try { cipher = Convert.FromBase64String(base64.Trim()); }
        catch (FormatException) { return null; }

        byte[] plain;
        try
        {
            using var rsa = RSA.Create();
            var key = options.XmlPrivateKey.Trim();
            if (key.StartsWith('<')) rsa.FromXmlString(key);
            else rsa.ImportFromPem(key);
            plain = rsa.Decrypt(cipher, RSAEncryptionPadding.Pkcs1);
        }
        catch (Exception ex) when (ex is CryptographicException or ArgumentException or FormatException)
        {
            return null;
        }

        // برنامهٔ قدیم: هر بایت ← Chr(بایت)، سپس XOR نویسهٔ j با نویسهٔ (j+1) mod len کلید
        var xor = options.XmlXorKey;
        var sb = new StringBuilder(plain.Length);
        for (var j = 0; j < plain.Length; j++)
            sb.Append((char)(plain[j] ^ xor[(j + 1) % xor.Length]));
        return sb.ToString();
    }
}
