using Accounting.Application.Elams;
using Microsoft.Extensions.Configuration;

namespace Accounting.Infrastructure.Services;

/// <summary>
/// ارسال اعلامیهٔ درآمد به وب‌سرویس SOAP سامانهٔ سبا (درآمد) (<c>CentralAccount.asmx</c>، متد
/// <c>Send_LetterN</c>) — همان Connected Service پروژهٔ مرجع (<c>Connected Services/elmDrmd</c>).
/// آدرس از کانفیگ <c>ElamDrmd:Url</c>؛ نبود ⇒ آدرس تولیدشده در Reference.cs.
///
/// <para>
/// ⚠️ مرجع (<c>Infrastructure.Service/ElamDrmdWebService.cs</c>) جز چهار فیلد اول، همهٔ فیلدهای نامه را با
/// <c>cODEDIGITField</c> پر می‌کرد (اشکال کپی‌وپیست). اینجا هر فیلد مقدار خودش را می‌گیرد.
/// </para>
/// </summary>
public sealed class ElamDrmdWebService : IRevenueElamSender
{
    private readonly IConfiguration _configuration;

    public ElamDrmdWebService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<string> SendAsync(RevenueElamLetter letter, CancellationToken cancellationToken = default)
    {
        var url = _configuration["ElamDrmd:Url"];
        var client = string.IsNullOrWhiteSpace(url)
            ? new elmDrmd.CentralAccountSoapClient(elmDrmd.CentralAccountSoapClient.EndpointConfiguration.CentralAccountSoap)
            : new elmDrmd.CentralAccountSoapClient(elmDrmd.CentralAccountSoapClient.EndpointConfiguration.CentralAccountSoap, url);

        var dto = new elmDrmd.clsLetterN
        {
            LETTER_SERIAL = letter.LetterSerial,
            BRHCODE = letter.BranchCode,
            saveBRHCODE = letter.SaveBranchCode,
            LETTER_DATE = letter.LetterDate,
            LETTER_FLAG = letter.LetterFlag,
            LETTER_TYPE = letter.LetterType,
            LETTER_PRICE1 = letter.Price1,
            LETTER_PRICE2 = letter.Price2,
            LETTER_RABET = letter.Rabet,
            IDNO = letter.IdNo,
            RWSHID = letter.WorkshopId,
            RWSHNAME = letter.WorkshopName,
            BES_FUNCTIONDATE = letter.FunctionDate,
            CWS_DBTNO = letter.DebtNo,
            BES_CNTNO = letter.ContractNo,
            BES_CNTDATE = letter.ContractDate,
            CODEDIGIT = letter.CodeDigit,
            BES_EMPZFLAG = string.Empty,
            CREATEDT = string.Empty,
            CREATEUID = string.Empty,
            LETTER_CODE1 = string.Empty,
            LETTER_CODE2 = string.Empty,
            LETTER_DEL = string.Empty,
            LETTER_LDATE = string.Empty,
            LETTER_LNO = string.Empty,
            LETTER_NAM = string.Empty,
            LETTER_NO = string.Empty,
            LETTER_OBJDATE = string.Empty,
            LETTER_PRINT = string.Empty,
            LETTER_SANADFLAG = string.Empty,
            LETTER_SODOR = string.Empty,
        };

        try
        {
            return await client.Send_LetterNAsync(dto).WaitAsync(cancellationToken);
        }
        finally
        {
            try
            {
                await client.CloseAsync();
            }
            catch
            {
                client.Abort();
            }
        }
    }
}
