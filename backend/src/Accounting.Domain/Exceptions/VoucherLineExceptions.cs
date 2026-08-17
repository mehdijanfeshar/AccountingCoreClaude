namespace Accounting.Domain.Exceptions;

/// <summary>
/// ردیف سند هم‌زمان بدهکار و بستانکار دارد، یا هر دو صفر است (قانون ۶).
/// </summary>
public sealed class InvalidVoucherLineAmountException : DomainException
{
    public InvalidVoucherLineAmountException(string message) : base(message)
    {
    }
}

/// <summary>
/// حداقل یک نوع تفصیلی الزامی (IsRequiredAtPosting/Required) برای این معین در ردیف سند مقداردهی نشده است.
/// </summary>
public sealed class RequiredDetailValueMissingException : DomainException
{
    public int SubsidiaryAccountId { get; }
    public IReadOnlyCollection<int> MissingDetailAccountTypeIds { get; }

    public RequiredDetailValueMissingException(int subsidiaryAccountId, IReadOnlyCollection<int> missingDetailAccountTypeIds)
        : base(BuildMessage(subsidiaryAccountId, missingDetailAccountTypeIds))
    {
        SubsidiaryAccountId = subsidiaryAccountId;
        MissingDetailAccountTypeIds = missingDetailAccountTypeIds;
    }

    private static string BuildMessage(int subsidiaryAccountId, IReadOnlyCollection<int> missingDetailAccountTypeIds)
        => $"برای معین با شناسهٔ {subsidiaryAccountId}، مقدار تفصیلی الزامی برای نوع(های) [{string.Join(", ", missingDetailAccountTypeIds)}] ثبت نشده است.";
}

/// <summary>
/// برای این معین، نوع تفصیلی داده‌شده اصلاً لینک نشده یا صراحتاً غیرمجاز است (لینک‌نشده = غیرمجاز؛ قانون ۳).
/// </summary>
public sealed class DetailValueNotAllowedException : DomainException
{
    public int SubsidiaryAccountId { get; }
    public int DetailAccountTypeId { get; }

    public DetailValueNotAllowedException(int subsidiaryAccountId, int detailAccountTypeId)
        : base($"نوع تفصیلی با شناسهٔ {detailAccountTypeId} برای معین با شناسهٔ {subsidiaryAccountId} مجاز نیست (لینک نشده یا غیرمجاز است).")
    {
        SubsidiaryAccountId = subsidiaryAccountId;
        DetailAccountTypeId = detailAccountTypeId;
    }
}

/// <summary>
/// برای یک نوع تفصیلی، بیش از یک مقدار در همان ردیف سند ثبت شده است (قانون ۵).
/// </summary>
public sealed class DuplicateDetailValueException : DomainException
{
    public int SubsidiaryAccountId { get; }
    public int DetailAccountTypeId { get; }

    public DuplicateDetailValueException(int subsidiaryAccountId, int detailAccountTypeId)
        : base($"نوع تفصیلی با شناسهٔ {detailAccountTypeId} بیش از یک بار برای معین با شناسهٔ {subsidiaryAccountId} در یک ردیف سند مقداردهی شده است.")
    {
        SubsidiaryAccountId = subsidiaryAccountId;
        DetailAccountTypeId = detailAccountTypeId;
    }
}
