namespace Accounting.Application.Common.Validation;

/// <summary>
/// اعتبار کد ملی شخص حقیقی (۱۰ رقم) و شناسهٔ ملی شخص حقوقی (۱۱ رقم) با رقم کنترل.
/// </summary>
public static class NationalIdentifier
{
    /// <summary>کد ملی: Σ رقم‌های ۱..۹ × (۱۰..۲) mod 11؛ کمتر از ۲ ⇒ رقم کنترل همان باقیمانده، وگرنه ۱۱ منهای آن.</summary>
    public static bool IsValidNationalCode(string? code)
    {
        if (code is not { Length: 10 } || !code.All(char.IsAsciiDigit) || code.Distinct().Count() == 1)
            return false;

        var sum = 0;
        for (var i = 0; i < 9; i++)
            sum += (code[i] - '0') * (10 - i);

        var r = sum % 11;
        var check = code[9] - '0';
        return r < 2 ? check == r : check == 11 - r;
    }

    private static readonly int[] LegalWeights = [29, 27, 23, 19, 17, 29, 27, 23, 19, 17];

    /// <summary>شناسهٔ ملی: (رقم دهم + ۲) به هر رقم ۱..۱۰ افزوده و در ضریب ضرب؛ mod 11 (۱۰ ⇒ ۰) = رقم یازدهم.</summary>
    public static bool IsValidLegalId(string? id)
    {
        if (id is not { Length: 11 } || !id.All(char.IsAsciiDigit) || id.Distinct().Count() == 1)
            return false;

        var d = (id[9] - '0') + 2;
        var sum = 0;
        for (var i = 0; i < 10; i++)
            sum += ((id[i] - '0') + d) * LegalWeights[i];

        var r = sum % 11;
        if (r == 10) r = 0;
        return r == id[10] - '0';
    }
}
