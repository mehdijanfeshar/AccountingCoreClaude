using System.Security.Cryptography;
using Accounting.Application.Common.Interfaces;
using Microsoft.AspNetCore.DataProtection;

namespace Accounting.Api.Security;

/// <summary>
/// رازهای ماندگار روی سرور (رمز اسکیمای سیستم قدیم) با ASP.NET Data Protection. کلیدها در پوشهٔ
/// <c>App_Data/keys</c> نگه داشته می‌شوند تا پس از راه‌اندازی دوبارهٔ برنامه (IIS) هم رمزگشایی شوند.
/// </summary>
public sealed class DataProtectionSecretProtector(IDataProtectionProvider provider) : ISecretProtector
{
    private readonly IDataProtector _protector = provider.CreateProtector("Accounting.LegacyImport.Secrets.v1");

    public string Protect(string plain) => _protector.Protect(plain);

    public string? Unprotect(string protectedValue)
    {
        try { return _protector.Unprotect(protectedValue); }
        catch (CryptographicException) { return null; }
    }
}
