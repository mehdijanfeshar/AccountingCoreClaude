using Accounting.Domain.Sales;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accounting.Infrastructure.Sales;

// ماژول فروش — نگاشت جدول‌های DDL 080 (backend/db/080_sales.sql). قرارداد پروژه: CHAR(36) ⇄ Guid، bool روی
// NUMBER(1)؛ enum بدون نوع ستون صریح (NUMBER(1) صریح اوراکل را به خواندن bool می‌کشاند).

internal static class SalesMappingExtensions
{
    public static PropertyBuilder<Guid> Id36(this PropertyBuilder<Guid> p, string column) =>
        p.HasColumnName(column).HasMaxLength(36).IsUnicode(false).IsFixedLength().HasConversion(GuidToChar36Converter.Instance);

    public static PropertyBuilder<Guid?> Id36(this PropertyBuilder<Guid?> p, string column) =>
        p.HasColumnName(column).HasMaxLength(36).IsUnicode(false).IsFixedLength().HasConversion(GuidToChar36Converter.Instance);

    public static PropertyBuilder<string> Str(this PropertyBuilder<string> p, string column, int max) =>
        p.HasColumnName(column).HasMaxLength(max).IsUnicode(false);

    public static PropertyBuilder<bool> Flag(this PropertyBuilder<bool> p, string column) =>
        p.HasColumnName(column).HasColumnType("NUMBER(1)");

    public static void Audit<T>(this EntityTypeBuilder<T> b) where T : class
    {
        b.Property<string>("CreatedBy").Str("ADDUSERID", 10).IsRequired();
        b.Property<DateTime>("CreatedAtUtc").HasColumnName("CREATEDDATE").HasPrecision(6);
    }

    public static void ApplySales(this ModelBuilder m)
    {
        m.Entity<SalesSetting>(b =>
        {
            b.ToTable("TB_SL_SETTING");
            b.HasKey(x => x.VahedCode).HasName("PK_SL_SETTING");
            b.Property(x => x.VahedCode).Str("VAHEDCODE", 4);
            b.Property(x => x.DiscountApprovalPercent).HasColumnName("DISC_APPROVAL_PCT").HasPrecision(5, 2);
            b.Property(x => x.CreditPolicy).HasColumnName("CREDIT_POLICY").HasConversion<int>();
            b.Property(x => x.SegregationOfDuties).Flag("SOD");
            b.Property(x => x.BillBeforeDelivery).Flag("BILL_BEFORE_DELIVERY");
            b.Property(x => x.AllowNegativeStock).Flag("ALLOW_NEGATIVE_STOCK");
            b.Property(x => x.SystemTypeId).Id36("SYSTEM_TYPE_ID");
            b.Property(x => x.SellerId).Str("SELLER_ID", 20);
            b.Property(x => x.MemoryId).Str("MEMORY_ID", 20);
            b.Audit();
        });

        m.Entity<SalesAccountMap>(b =>
        {
            b.ToTable("TB_SL_ACCOUNT_MAP");
            b.HasKey(x => x.Id).HasName("PK_SL_ACCOUNT_MAP");
            b.Property(x => x.Id).Id36("ID").ValueGeneratedNever();
            b.Property(x => x.VahedCode).Str("VAHEDCODE", 4).IsRequired();
            b.Property(x => x.RoleKey).Str("ROLE_KEY", 20).IsRequired();
            b.Property(x => x.AccountId).Id36("ACCOUNT_ID");
            b.Property(x => x.TafsiliId).Id36("TAFSILI_ID");
        });

        m.Entity<SalesVatRate>(b =>
        {
            b.ToTable("TB_SL_VAT_RATE");
            b.HasKey(x => x.Id).HasName("PK_SL_VAT_RATE");
            b.Property(x => x.Id).Id36("ID").ValueGeneratedNever();
            b.Property(x => x.FromDate).Str("FROM_DATE", 8).IsRequired();
            b.Property(x => x.Rate).HasColumnName("RATE").HasPrecision(5, 2);
            b.Property(x => x.Note).Str("NOTE", 200);
            b.Property(x => x.IsDeleted).Flag("ISDELETED");
            b.Audit();
        });

        m.Entity<SalesItem>(b =>
        {
            b.ToTable("TB_SL_ITEM");
            b.HasKey(x => x.Id).HasName("PK_SL_ITEM");
            b.Property(x => x.Id).Id36("ID").ValueGeneratedNever();
            b.Property(x => x.VahedCode).Str("VAHEDCODE", 4).IsRequired();
            b.Property(x => x.Code).Str("CODE", 30).IsRequired();
            b.Property(x => x.Name).Str("NAME", 200).IsRequired();
            b.Property(x => x.Kind).HasColumnName("KIND").HasConversion<int>();
            b.Property(x => x.GroupName).Str("GROUP_NAME", 100);
            b.Property(x => x.UnitName).Str("UNIT_NAME", 30).IsRequired();
            b.Property(x => x.Price).HasColumnName("PRICE");
            b.Property(x => x.StandardCost).HasColumnName("STD_COST");
            b.Property(x => x.TaxCode).Str("TAX_CODE", 13);
            b.Property(x => x.IsExempt).Flag("IS_EXEMPT");
            b.Property(x => x.Stock).HasColumnName("STOCK").HasPrecision(18, 3);
            b.Property(x => x.IsActive).Flag("ISACTIVE");
            b.Property(x => x.IsDeleted).Flag("ISDELETED");
            b.Audit();
        });

        m.Entity<SalesCustomer>(b =>
        {
            b.ToTable("TB_SL_CUSTOMER");
            b.HasKey(x => x.Id).HasName("PK_SL_CUSTOMER");
            b.Property(x => x.Id).Id36("ID").ValueGeneratedNever();
            b.Property(x => x.VahedCode).Str("VAHEDCODE", 4).IsRequired();
            b.Property(x => x.TafsiliId).Id36("TAFSILI_ID");
            b.Property(x => x.PersonKind).HasColumnName("PERSON_KIND").HasConversion<int>();
            b.Property(x => x.NationalId).Str("NATIONAL_ID", 11);
            b.Property(x => x.EconomicCode).Str("ECONOMIC_CODE", 14);
            b.Property(x => x.PostalCode).Str("POSTAL_CODE", 10);
            b.Property(x => x.CreditLimit).HasColumnName("CREDIT_LIMIT");
            b.Property(x => x.PayTerms).HasColumnName("PAY_TERMS");
            b.Property(x => x.IsBlocked).Flag("IS_BLOCKED");
            b.Property(x => x.IsDeleted).Flag("ISDELETED");
            b.Audit();
        });

        m.Entity<SalesUserRole>(b =>
        {
            b.ToTable("TB_SL_USER_ROLE");
            b.HasKey(x => x.Id).HasName("PK_SL_USER_ROLE");
            b.Property(x => x.Id).Id36("ID").ValueGeneratedNever();
            b.Property(x => x.VahedCode).Str("VAHEDCODE", 4).IsRequired();
            b.Property(x => x.UserId).Str("USER_ID", 10).IsRequired();
            b.Property(x => x.Role).HasColumnName("ROLE").HasConversion<int>();
            b.Audit();
        });

        m.Entity<SalesDoc>(b =>
        {
            b.ToTable("TB_SL_DOC");
            b.HasKey(x => x.Id).HasName("PK_SL_DOC");
            b.Property(x => x.Id).Id36("ID").ValueGeneratedNever();
            b.Property(x => x.VahedCode).Str("VAHEDCODE", 4).IsRequired();
            b.Property(x => x.Year).Str("YEAR", 4).IsRequired();
            b.Property(x => x.DocType).HasColumnName("DOC_TYPE").HasConversion<int>();
            b.Property(x => x.DocNo).Str("DOC_NO", 20);
            b.Property(x => x.DocDate).Str("DOC_DATE", 8).IsRequired();
            b.Property(x => x.CustomerId).Id36("CUSTOMER_ID");
            b.Property(x => x.SourceId).Id36("SOURCE_ID");
            b.Property(x => x.Status).HasColumnName("STATUS").HasConversion<int>();
            b.Property(x => x.PayTerms).HasColumnName("PAY_TERMS");
            b.Property(x => x.ValidUntil).Str("VALID_UNTIL", 8);
            b.Property(x => x.DeliveryDate).Str("DELIVERY_DATE", 8);
            b.Property(x => x.SalesType).HasColumnName("SALES_TYPE").HasConversion<int>();
            b.Property(x => x.RefNo).Str("REF_NO", 50);
            b.Property(x => x.Note).Str("NOTE", 1000);
            b.Property(x => x.PrintNote).Str("PRINT_NOTE", 1000);
            b.Property(x => x.HeaderDiscount).HasColumnName("HEADER_DISCOUNT");
            b.Property(x => x.Gross).HasColumnName("GROSS");
            b.Property(x => x.Discount).HasColumnName("DISCOUNT");
            b.Property(x => x.Taxable).HasColumnName("TAXABLE");
            b.Property(x => x.Vat).HasColumnName("VAT");
            b.Property(x => x.Total).HasColumnName("TOTAL");
            b.Property(x => x.ApprovalReasons).Str("APPROVAL_REASONS", 1000);
            b.Property(x => x.VoucherId).Id36("VOUCHER_ID");
            b.Property(x => x.ApprovedBy).Str("APPROVED_BY", 10);
            b.Property(x => x.PostedBy).Str("POSTED_BY", 10);
            b.Property(x => x.IsDeleted).Flag("ISDELETED");
            b.Property(x => x.UpdatedAtUtc).HasColumnName("UPDATEDDATE").HasPrecision(6);
            b.Audit();
            b.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.DocId).HasConstraintName("FK_SL_DOC_LINE_DOC");
        });

        m.Entity<SalesDocLine>(b =>
        {
            b.ToTable("TB_SL_DOC_LINE");
            b.HasKey(x => x.Id).HasName("PK_SL_DOC_LINE");
            b.Property(x => x.Id).Id36("ID").ValueGeneratedNever();
            b.Property(x => x.DocId).Id36("DOC_ID");
            b.Property(x => x.Radif).HasColumnName("RADIF");
            b.Property(x => x.ItemId).Id36("ITEM_ID");
            b.Property(x => x.Qty).HasColumnName("QTY").HasPrecision(18, 3);
            b.Property(x => x.Price).HasColumnName("PRICE");
            b.Property(x => x.DiscountPercent).HasColumnName("DISC_PCT").HasPrecision(5, 2);
            b.Property(x => x.Gross).HasColumnName("GROSS");
            b.Property(x => x.LineDiscount).HasColumnName("LINE_DISC");
            b.Property(x => x.HeaderDiscountShare).HasColumnName("HEADER_DISC");
            b.Property(x => x.Taxable).HasColumnName("TAXABLE");
            b.Property(x => x.VatRate).HasColumnName("VAT_RATE").HasPrecision(5, 2);
            b.Property(x => x.Vat).HasColumnName("VAT");
            b.Property(x => x.Total).HasColumnName("TOTAL");
            b.Property(x => x.UnitCost).HasColumnName("UNIT_COST");
        });

        m.Entity<SalesDocHistory>(b =>
        {
            b.ToTable("TB_SL_DOC_HISTORY");
            b.HasKey(x => x.Id).HasName("PK_SL_DOC_HISTORY");
            b.Property(x => x.Id).Id36("ID").ValueGeneratedNever();
            b.Property(x => x.DocId).Id36("DOC_ID");
            b.Property(x => x.Action).Str("ACTION", 500).IsRequired();
            b.Audit();
        });
    }
}
