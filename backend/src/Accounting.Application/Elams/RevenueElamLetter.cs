using System.Globalization;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Elams;

/// <summary>
/// نامهٔ اعلامیهٔ درآمد برای سامانهٔ سبا (درآمد) (<c>Send_LetterN</c>، کلاس <c>clsLetterN</c>) — فیلدها عین
/// <c>SendElmDrmdCommandHandler</c> پروژهٔ مرجع.
/// </summary>
public sealed record RevenueElamLetter(
    string LetterSerial,
    string BranchCode,
    string SaveBranchCode,
    string LetterDate,
    string LetterFlag,
    string LetterType,
    string Price1,
    string Price2,
    string Rabet,
    string IdNo,
    string WorkshopId,
    string WorkshopName,
    string FunctionDate,
    string DebtNo,
    string ContractNo,
    string ContractDate,
    string CodeDigit);

/// <summary>ارسال نامهٔ اعلامیهٔ درآمد به سامانهٔ سبا (درآمد). پاسخ خام سرویس برمی‌گردد.</summary>
public interface IRevenueElamSender
{
    Task<string> SendAsync(RevenueElamLetter letter, CancellationToken cancellationToken = default);
}

/// <summary>
/// ساخت نامه از اعلامیه — منطق <c>SendElmDrmdCommandHandler</c> مرجع، با اصلاح دو اشکال آن:
/// مرجع <c>ElamhCase.ToString()</c>/<c>ElamhDaramadType.ToString()</c> را (که نام enum مثل «Creditor»
/// می‌دهد، نه «2») هم در رشتهٔ رقم کنترلی و هم در مقایسه با «1» به کار می‌برد — اولی در
/// <c>ElamSec2</c> روی <c>long.Parse</c> خطا می‌داد و دومی هیچ‌وقت برقرار نمی‌شد. اینجا مقدار عددی.
/// </summary>
public static class RevenueElamLetterBuilder
{
    public static RevenueElamLetter Build(TB_ELAMHEAD head, TB_ELAMDETAIL detail)
    {
        var caseNo = ((int)(head.ELAMH_CASE ?? ElamCase.Creditor)).ToString(CultureInfo.InvariantCulture);
        var yy = head.YEAR!.Substring(2, 2);
        var amount = ((long)(detail.DEBTOR ?? 0)).ToString(CultureInfo.InvariantCulture);
        var serial = head.ELAMH_SERIALNO!;
        var isPersonal = head.ELAMHDRAMAD_TYPE == DaramElamhType.PersonalDramadElam;

        var check = "1" + caseNo + yy + head.ELAMH_SENDRCVVAHED + head.VAHEDCODE + amount;

        return new RevenueElamLetter(
            LetterSerial: yy + "0" + head.VAHEDCODE + serial,
            BranchCode: head.VAHEDCODE!,
            SaveBranchCode: head.ELAMH_SENDRCVVAHED!,
            LetterDate: head.ELAMH_DATE ?? string.Empty,
            LetterFlag: "1",
            LetterType: caseNo,
            Price1: caseNo == "1" ? amount : "0",
            Price2: caseNo != "1" ? amount : "0",
            Rabet: head.ELAMH_CODE ?? string.Empty,
            IdNo: head.ELAMH_SENDRCVVAHED + "1" + serial.Substring(9, 5),
            WorkshopId: head.ELAMH_WORKSHOPCODE ?? string.Empty,
            WorkshopName: head.ELAMH_WORKSHOPNAME ?? string.Empty,
            FunctionDate: head.YEAR + head.ELAMH_LSTMON,
            DebtNo: head.ELAMH_RCVNO ?? string.Empty,
            ContractNo: isPersonal ? head.PEIMAN_NO ?? string.Empty : string.Empty,
            ContractDate: isPersonal ? head.ELAMH_RCVDT ?? string.Empty : string.Empty,
            CodeDigit: CheckDigits(check).ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>دو رقم کنترلی mod 11 با وزن‌های ۲..۷ — عین <c>ElamSec2</c> مرجع.</summary>
    public static long CheckDigits(string digits)
    {
        var rem1 = Mod11(digits);
        var rem2 = Mod11(digits + rem1.ToString(CultureInfo.InvariantCulture));
        return long.Parse(rem1.ToString(CultureInfo.InvariantCulture) + rem2.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
    }

    private static long Mod11(string p)
    {
        long sum = 0;
        long weight = 2;
        for (var i = p.Length - 1; i >= 0; i--)
        {
            sum += weight * (p[i] - '0');
            weight++;
            if (weight == 8)
                weight = 2;
        }
        var rem = sum % 11;
        return rem is 0 or 1 ? 0 : 11 - rem;
    }
}
