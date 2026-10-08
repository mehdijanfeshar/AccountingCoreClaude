namespace Accounting.Domain.ValueObjects;

/// <summary>
/// <c>TB_RABET_CLOSING.TYPEACCOUNTCODE</c> (<c>NUMBER(1)</c>): whether the closing interface is set on a
/// کل (and so reaches every معین under it) or on a single معین. Values are the reference project's
/// <c>TypeAccountCode</c> enum (<c>D:\CentralAccount\Tamin.Core\Entities\Rabetss\TypeAccountCode.cs</c>);
/// live data holds 2. It used to be mapped as <c>bool?</c>, which read 2 as true and would have
/// written 1, a value the reference never uses.
/// </summary>
public enum RabetAccountLevel
{
    /// <summary>کد کل.</summary>
    Kol = 2,

    /// <summary>کد معین.</summary>
    Moein = 3,
}
