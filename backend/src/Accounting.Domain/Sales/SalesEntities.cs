namespace Accounting.Domain.Sales;

// ماژول فروش، فاز ۱ (DDL 080) — طرح: docs/sales-module.md. POCO خالص؛ قواعد در Application.

public enum CreditPolicy { Warn = 1, Block = 2, Approve = 3 }

public enum SalesItemKind { Goods = 1, Service = 2 }

public enum PersonKind { Natural = 1, Legal = 2 }

public enum SalesRole { Seller = 1, Manager = 2, Accountant = 3 }

public enum SalesDocType { ProForma = 1, Order = 2, Delivery = 3, Invoice = 4, Return = 5 }

public enum SalesDocStatus { Draft = 1, Pending = 2, Approved = 3, Posted = 4, Void = 9 }

public enum SalesType { Domestic = 1, Export = 2, Service = 3 }

/// <summary>تنظیمات فروش یک واحد (یک ردیف برای هر واحد).</summary>
public class SalesSetting
{
    public string VahedCode { get; set; } = "";
    public decimal DiscountApprovalPercent { get; set; } = 10;
    public CreditPolicy CreditPolicy { get; set; } = CreditPolicy.Approve;
    /// <summary>تفکیک وظایف: تأییدکننده ≠ ایجادکننده.</summary>
    public bool SegregationOfDuties { get; set; } = true;
    /// <summary>فاکتور کالا پیش از تحویل (درآمد تا تحویل در بدهی قراردادی).</summary>
    public bool BillBeforeDelivery { get; set; }
    public bool AllowNegativeStock { get; set; }
    /// <summary>نوع سند حسابداری (<c>TB_SYSTYPE.ID</c>) سندهای فروش.</summary>
    public Guid? SystemTypeId { get; set; }
    public string? SellerId { get; set; }
    public string? MemoryId { get; set; }
    public string CreatedBy { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
}

/// <summary>نقش حسابداری ⇒ معین (+ تفصیلی ثابت). کلیدها: <see cref="SalesAccountRoles"/>.</summary>
public class SalesAccountMap
{
    public Guid Id { get; set; }
    public string VahedCode { get; set; } = "";
    public string RoleKey { get; set; } = "";
    public Guid AccountId { get; set; }
    public Guid? TafsiliId { get; set; }
}

public static class SalesAccountRoles
{
    public const string Receivable = "ar";
    public const string Sales = "sales";
    public const string Service = "service";
    public const string Discount = "disc";
    public const string Return = "ret";
    public const string Vat = "vat";
    public const string Contract = "contract";
    public const string Cogs = "cogs";
    public const string Inventory = "inv";

    public static readonly IReadOnlyList<string> All = [Receivable, Sales, Service, Discount, Return, Vat, Contract, Cogs, Inventory];
}

/// <summary>نرخ مالیات بر ارزش افزوده از تاریخ (سراسری).</summary>
public class SalesVatRate
{
    public Guid Id { get; set; }
    /// <summary>شمسی yyyyMMdd</summary>
    public string FromDate { get; set; } = "";
    public decimal Rate { get; set; }
    public string? Note { get; set; }
    public bool IsDeleted { get; set; }
    public string CreatedBy { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
}

public class SalesItem
{
    public Guid Id { get; set; }
    public string VahedCode { get; set; } = "";
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public SalesItemKind Kind { get; set; } = SalesItemKind.Goods;
    public string? GroupName { get; set; }
    public string UnitName { get; set; } = "";
    public long Price { get; set; }
    /// <summary>بهای استاندارد واحد — مبنای بهای تمام‌شده تا ماژول انبار.</summary>
    public long StandardCost { get; set; }
    /// <summary>شناسهٔ ۱۳ رقمی کالا/خدمت سامانهٔ مودیان.</summary>
    public string? TaxCode { get; set; }
    public bool IsExempt { get; set; }
    public decimal Stock { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
    public string CreatedBy { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
}

/// <summary>پروفایل فروش روی یک تفصیلی موجود (مشتری).</summary>
public class SalesCustomer
{
    public Guid Id { get; set; }
    public string VahedCode { get; set; } = "";
    public Guid TafsiliId { get; set; }
    public PersonKind PersonKind { get; set; } = PersonKind.Legal;
    public string? NationalId { get; set; }
    public string? EconomicCode { get; set; }
    public string? PostalCode { get; set; }
    public long CreditLimit { get; set; }
    public int PayTerms { get; set; }
    public bool IsBlocked { get; set; }
    public bool IsDeleted { get; set; }
    public string CreatedBy { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
}

public class SalesUserRole
{
    public Guid Id { get; set; }
    public string VahedCode { get; set; } = "";
    public string UserId { get; set; } = "";
    public SalesRole Role { get; set; }
    public string CreatedBy { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
}

public class SalesDoc
{
    public Guid Id { get; set; }
    public string VahedCode { get; set; } = "";
    public string Year { get; set; } = "";
    public SalesDocType DocType { get; set; }
    public string? DocNo { get; set; }
    /// <summary>شمسی yyyyMMdd</summary>
    public string DocDate { get; set; } = "";
    public Guid CustomerId { get; set; }
    public Guid? SourceId { get; set; }
    public SalesDocStatus Status { get; set; } = SalesDocStatus.Draft;
    public int PayTerms { get; set; }
    public string? ValidUntil { get; set; }
    public string? DeliveryDate { get; set; }
    public SalesType SalesType { get; set; } = SalesType.Domestic;
    public string? RefNo { get; set; }
    public string? Note { get; set; }
    public string? PrintNote { get; set; }
    public long HeaderDiscount { get; set; }
    public long Gross { get; set; }
    public long Discount { get; set; }
    public long Taxable { get; set; }
    public long Vat { get; set; }
    public long Total { get; set; }
    public string? ApprovalReasons { get; set; }
    public Guid? VoucherId { get; set; }
    public string? ApprovedBy { get; set; }
    public string? PostedBy { get; set; }
    public bool IsDeleted { get; set; }
    public string CreatedBy { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    public List<SalesDocLine> Lines { get; set; } = [];
}

public class SalesDocLine
{
    public Guid Id { get; set; }
    public Guid DocId { get; set; }
    public int Radif { get; set; }
    public Guid ItemId { get; set; }
    public decimal Qty { get; set; }
    public long Price { get; set; }
    public decimal DiscountPercent { get; set; }
    public long Gross { get; set; }
    public long LineDiscount { get; set; }
    public long HeaderDiscountShare { get; set; }
    public long Taxable { get; set; }
    public decimal VatRate { get; set; }
    public long Vat { get; set; }
    public long Total { get; set; }
    /// <summary>بهای واحد در لحظهٔ قطعی‌سازی (بهای استاندارد کالا).</summary>
    public long UnitCost { get; set; }
}

public class SalesDocHistory
{
    public Guid Id { get; set; }
    public Guid DocId { get; set; }
    public string Action { get; set; } = "";
    public string CreatedBy { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
}
