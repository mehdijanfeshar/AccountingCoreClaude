using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

// طبق تصمیم معماری Legacy-as-Domain (۲۰۲۶-۰۸-۱۷)، خودِ Entityها در پروژهٔ
// Accounting.Domain زندگی می‌کنند و اینجا فقط Persistence Mapping آن‌ها می‌ماند.
// جهت وابستگی: Infrastructure → Domain (مجاز). Domain هیچ ارجاعی به EF Core ندارد.
using Accounting.Domain.Entity;

namespace Accounting.Infrastructure.Legacy;

public partial class LegacyDbContext : DbContext
{
    public LegacyDbContext(DbContextOptions<LegacyDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<TB_ACCOUNT> TB_ACCOUNTs { get; set; }

    public virtual DbSet<TB_ACCOUNTCODE> TB_ACCOUNTCODEs { get; set; }

    public virtual DbSet<TB_ACCOUNTCODE_INTERFACE> TB_ACCOUNTCODE_INTERFACEs { get; set; }

    public virtual DbSet<TB_ACCOUNTEXCEPTION> TB_ACCOUNTEXCEPTIONs { get; set; }

    public virtual DbSet<TB_ACCOUNT_LINK_LEVEL> TB_ACCOUNT_LINK_LEVELs { get; set; }

    public virtual DbSet<TB_ACCOUNT_LINK_TAFSILGROUP> TB_ACCOUNT_LINK_TAFSILGROUPs { get; set; }

    public virtual DbSet<TB_ACCOUNT_LINK_TAFSILI> TB_ACCOUNT_LINK_TAFSILIs { get; set; }

    public virtual DbSet<TB_ACCOUNT_TYPE> TB_ACCOUNT_TYPEs { get; set; }

    public virtual DbSet<TB_ATTACH> TB_ATTACHes { get; set; }

    public virtual DbSet<TB_ATTRIBFORACCOUNTCODE> TB_ATTRIBFORACCOUNTCODEs { get; set; }

    public virtual DbSet<TB_ATTRIBSINVOUCHER> TB_ATTRIBSINVOUCHERs { get; set; }

    public virtual DbSet<TB_AUDITLOG> TB_AUDITLOGs { get; set; }

    public virtual DbSet<TB_BANKBRANCH_LIST> TB_BANKBRANCH_LISTs { get; set; }

    public virtual DbSet<TB_BANKCARTDETAIL> TB_BANKCARTDETAILs { get; set; }

    public virtual DbSet<TB_BANK_LIST> TB_BANK_LISTs { get; set; }

    public virtual DbSet<TB_BILL_LOG> TB_BILL_LOGs { get; set; }

    public virtual DbSet<TB_CHARGEANDCOST_DETAIL> TB_CHARGEANDCOST_DETAILs { get; set; }

    public virtual DbSet<TB_CHARGEANDCOST_HEAD> TB_CHARGEANDCOST_HEADs { get; set; }

    public virtual DbSet<TB_CHARGE_LINK_COST> TB_CHARGE_LINK_COSTs { get; set; }

    public virtual DbSet<TB_CHECK> TB_CHECKs { get; set; }

    public virtual DbSet<TB_CHECKBOOK> TB_CHECKBOOKs { get; set; }

    public virtual DbSet<TB_CHECK_TYPE> TB_CHECK_TYPEs { get; set; }

    public virtual DbSet<TB_CHEQUES_INCORRENT> TB_CHEQUES_INCORRENTs { get; set; }

    public virtual DbSet<TB_CITY> TB_CITies { get; set; }

    public virtual DbSet<TB_ELAMDETAIL> TB_ELAMDETAILs { get; set; }

    public virtual DbSet<TB_ELAMDETAIL_LINK_TAFSILI> TB_ELAMDETAIL_LINK_TAFSILIs { get; set; }

    public virtual DbSet<TB_ELAMHEAD> TB_ELAMHEADs { get; set; }

    public virtual DbSet<TB_EXPENCE> TB_EXPENCEs { get; set; }

    public virtual DbSet<TB_EXPENCEGROUP> TB_EXPENCEGROUPs { get; set; }

    public virtual DbSet<TB_EXPENCE_LINK_TAFSILI> TB_EXPENCE_LINK_TAFSILIs { get; set; }

    public virtual DbSet<TB_IDENTITYDETAIL> TB_IDENTITYDETAILs { get; set; }

    public virtual DbSet<TB_IDENTITYFIXITEM> TB_IDENTITYFIXITEMs { get; set; }

    public virtual DbSet<TB_IDENTITYGROUP> TB_IDENTITYGROUPs { get; set; }

    public virtual DbSet<TB_IDENTITYHEAD> TB_IDENTITYHEADs { get; set; }

    public virtual DbSet<TB_IDENTITYSUBGRP> TB_IDENTITYSUBGRPs { get; set; }

    public virtual DbSet<TB_LEVEL_TAFSIL> TB_LEVEL_TAFSILs { get; set; }

    public virtual DbSet<TB_PAYRECIVDETAIL> TB_PAYRECIVDETAILs { get; set; }

    public virtual DbSet<TB_PAYRECIVDETAIL_LINK_TAFSILI> TB_PAYRECIVDETAIL_LINK_TAFSILIs { get; set; }

    public virtual DbSet<TB_PAYRECIVHEAD> TB_PAYRECIVHEADs { get; set; }

    public virtual DbSet<TB_PERSON_ACTION> TB_PERSON_ACTIONs { get; set; }

    public virtual DbSet<TB_PREDESCRIB> TB_PREDESCRIBs { get; set; }

    public virtual DbSet<TB_PROVINCE> TB_PROVINCEs { get; set; }

    public virtual DbSet<TB_RABET> TB_RABETs { get; set; }

    public virtual DbSet<TB_RABET_CLOSING> TB_RABET_CLOSINGs { get; set; }

    public virtual DbSet<TB_RABET_TYPE> TB_RABET_TYPEs { get; set; }

    public virtual DbSet<TB_RECEIP> TB_RECEIPs { get; set; }

    public virtual DbSet<TB_REVOLVINGFUND_LINK_TAFSILI> TB_REVOLVINGFUND_LINK_TAFSILIs { get; set; }

    public virtual DbSet<TB_REVOLVING_FUND> TB_REVOLVING_FUNDs { get; set; }

    public virtual DbSet<TB_SYSTYPE> TB_SYSTYPEs { get; set; }

    public virtual DbSet<TB_TAFSILI> TB_TAFSILIs { get; set; }

    public virtual DbSet<TB_TAFSILI_UNITACCESS> TB_TAFSILI_UNITACCESSes { get; set; }

    public virtual DbSet<TB_TAFSIL_GROUP> TB_TAFSIL_GROUPs { get; set; }

    public virtual DbSet<TB_TAFSIL_LINK_TAFSILGROUP> TB_TAFSIL_LINK_TAFSILGROUPs { get; set; }

    public virtual DbSet<TB_TMP_VOUCHERHEAD> TB_TMP_VOUCHERHEADs { get; set; }

    public virtual DbSet<TB_TMP_VOUCHERSDETAIL> TB_TMP_VOUCHERSDETAILs { get; set; }

    public virtual DbSet<TB_VAHED_INFO> TB_VAHED_INFOs { get; set; }

    public virtual DbSet<TB_VAHED_TYPE> TB_VAHED_TYPEs { get; set; }

    public virtual DbSet<TB_VOUCHERDETAIL_LINK_TAFSILI> TB_VOUCHERDETAIL_LINK_TAFSILIs { get; set; }

    public virtual DbSet<TB_VOUCHERSDETAIL> TB_VOUCHERSDETAILs { get; set; }

    public virtual DbSet<TB_VOUCHERSHEAD> TB_VOUCHERSHEADs { get; set; }

    public virtual DbSet<TB_WHITEANDBLACKLIST> TB_WHITEANDBLACKLISTs { get; set; }

    public virtual DbSet<TB_WHITELIST> TB_WHITELISTs { get; set; }

    public virtual DbSet<TB_WORKSHOP> TB_WORKSHOPs { get; set; }

    public virtual DbSet<TB_WORKSHOP_LINK_TAFSILI> TB_WORKSHOP_LINK_TAFSILIs { get; set; }

    public virtual DbSet<TB_YEAR> TB_YEARs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasDefaultSchema("CENTRALACCOUNT")
            .UseCollation("USING_NLS_COMP");

        modelBuilder.Entity<TB_ACCOUNT>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK_ACCOUNT");

            entity.ToTable("TB_ACCOUNT");

            entity.HasIndex(e => new { e.ACCOUNTCODE_ID, e.VAHEDCODE }, "UK_ACCOUNT_ACCOUNTCODE").IsUnique();

            entity.Property(e => e.ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ACCOUNTCODE_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ACCOUNTHOLDER)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.ACCOUNTNUMBER)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.ACCOUNTOPENINGDATE)
                .HasMaxLength(8)
                .IsUnicode(false);
            entity.Property(e => e.ACCOUNTTYPE_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ADDUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.BANK_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.BRANCH_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.CARDNUMBER)
                .HasMaxLength(16)
                .IsUnicode(false);
            entity.Property(e => e.CHANGEUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CHECKFILE).HasColumnType("BLOB");
            entity.Property(e => e.CREATEDDATE).HasPrecision(6);
            entity.Property(e => e.FIRSTAMOUNT).HasColumnType("NUMBER(25)");
            entity.Property(e => e.ISDELETED).HasColumnType("NUMBER(1)");
            entity.Property(e => e.SHEBANUMBER)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UPDATEDDATE).HasPrecision(6);
            entity.Property(e => e.VAHEDCODE)
                .HasMaxLength(4)
                .IsUnicode(false);

            entity.HasOne(d => d.ACCOUNTCODE).WithMany(p => p.TB_ACCOUNTs)
                .HasForeignKey(d => d.ACCOUNTCODE_ID)
                .HasConstraintName("FK_ACCOUNTCODE_ACCOUNT");

            entity.HasOne(d => d.BANK).WithMany(p => p.TB_ACCOUNTs)
                .HasForeignKey(d => d.BANK_ID)
                .HasConstraintName("FK_BANK_ACCOUNT");

            entity.HasOne(d => d.BRANCH).WithMany(p => p.TB_ACCOUNTs)
                .HasForeignKey(d => d.BRANCH_ID)
                .HasConstraintName("FK_BANKBRANCH_ACCOUNT");
        });

        modelBuilder.Entity<TB_ACCOUNTCODE>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK_ACCOUNTCODE");

            entity.ToTable("TB_ACCOUNTCODE");

            entity.HasIndex(e => e.ACCCODE, "UK_ACCOUNTCODE").IsUnique();

            entity.Property(e => e.ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ACCCODE)
                .HasMaxLength(6)
                .IsUnicode(false);
            entity.Property(e => e.ACCCODENAME)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.ADDUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CHANGEUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CREATEDDATE).HasPrecision(6);
            entity.Property(e => e.IDENTYGROUPS_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ISDELETED).HasColumnType("NUMBER(1)");
            entity.Property(e => e.MOINFORCLOSE)
                .HasMaxLength(6)
                .IsUnicode(false);
            entity.Property(e => e.PARENTID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.SOURCEANDCONSUME_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.TYPEACCCODE)
                .HasComment("نوع حساب (1موقت2دائم)")
                .HasColumnType("NUMBER(1)");
            entity.Property(e => e.TYPEACTION)
                .HasComment("نوع خلاف ماهيت(کنترل نشود-اخطار دهد-ثبت نشود)")
                .HasColumnType("NUMBER(1)");
            entity.Property(e => e.TYPEACTIVITY)
                .HasComment("(1بستانکار2بدهکار3بد-بس)نوع فعاليت")
                .HasColumnType("NUMBER(1)");
            entity.Property(e => e.TYPECODE).HasColumnType("NUMBER(1)");
            entity.Property(e => e.UPDATEDDATE).HasPrecision(6);

            entity.HasOne(d => d.PARENT).WithMany(p => p.InversePARENT)
                .HasForeignKey(d => d.PARENTID)
                .HasConstraintName("FK_SELFREFRENCE");
        });

        modelBuilder.Entity<TB_ACCOUNTCODE_INTERFACE>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK_INTERFACE");

            entity.ToTable("TB_ACCOUNTCODE_INTERFACE");

            entity.Property(e => e.ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ACCOUNTCODEID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ADDUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CHANGEUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CREATEDDATE).HasPrecision(6);
            entity.Property(e => e.ISDELETED).HasColumnType("NUMBER(1)");
            entity.Property(e => e.TYPE).HasColumnType("NUMBER(1)");
            entity.Property(e => e.UPDATEDDATE).HasPrecision(6);

            entity.HasOne(d => d.ACCOUNTCODE).WithMany(p => p.TB_ACCOUNTCODE_INTERFACEs)
                .HasForeignKey(d => d.ACCOUNTCODEID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_INTRFACE_ACCCOUNTCODE");
        });

        modelBuilder.Entity<TB_ACCOUNTEXCEPTION>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK_ACCOUNTEXCEPTION");

            entity.ToTable("TB_ACCOUNTEXCEPTION");

            entity.Property(e => e.ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ACCOUNTCOE_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ADDUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CHANGEUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CREATEDDATE)
                .HasPrecision(6)
                .HasDefaultValueSql("null ");
            entity.Property(e => e.ISDELETED).HasColumnType("NUMBER(1)");
            entity.Property(e => e.UPDATEDDATE).HasPrecision(6);
            entity.Property(e => e.VAHEDTYPE_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();

            entity.HasOne(d => d.ACCOUNTCOE).WithMany(p => p.TB_ACCOUNTEXCEPTIONs)
                .HasForeignKey(d => d.ACCOUNTCOE_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EXCEPTION_ACCOUNTCODE");

            entity.HasOne(d => d.VAHEDTYPE).WithMany(p => p.TB_ACCOUNTEXCEPTIONs)
                .HasForeignKey(d => d.VAHEDTYPE_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ACCOUNTEXCEPTION_VAHEDTYPE");
        });

        modelBuilder.Entity<TB_ACCOUNT_LINK_LEVEL>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK_ACCOUNTLINKLEVEL");

            entity.ToTable("TB_ACCOUNT_LINK_LEVEL");

            entity.Property(e => e.ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ACCOUNT_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ADDUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CHANGEUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CREATEDDATE).HasPrecision(6);
            entity.Property(e => e.ISDELETED).HasColumnType("NUMBER(1)");
            entity.Property(e => e.LEVEL_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.UPDATEDDATE).HasPrecision(6);

            entity.HasOne(d => d.ACCOUNT).WithMany(p => p.TB_ACCOUNT_LINK_LEVELs)
                .HasForeignKey(d => d.ACCOUNT_ID)
                .HasConstraintName("FK_ACCOUNTCODE_LEVEL");

            entity.HasOne(d => d.LEVEL).WithMany(p => p.TB_ACCOUNT_LINK_LEVELs)
                .HasForeignKey(d => d.LEVEL_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TBLEVEL");
        });

        modelBuilder.Entity<TB_ACCOUNT_LINK_TAFSILGROUP>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK_ACCOUNTLIKETAFSILGROUP");

            entity.ToTable("TB_ACCOUNT_LINK_TAFSILGROUP");

            entity.HasIndex(e => new { e.ACCOUNT_ID, e.LEVEL_ID, e.TAFSILGROUP_ID }, "UK_ACCOUNTLINKTAFSILGROUP").IsUnique();

            entity.Property(e => e.ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ACCOUNT_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ADDUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CHANGEUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CREATEDDATE).HasPrecision(6);
            entity.Property(e => e.ISDELETED).HasColumnType("NUMBER(1)");
            entity.Property(e => e.LEVEL_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.TAFSILGROUP_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.UPDATEDDATE).HasPrecision(6);

            entity.HasOne(d => d.ACCOUNT).WithMany(p => p.TB_ACCOUNT_LINK_TAFSILGROUPs)
                .HasForeignKey(d => d.ACCOUNT_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TAFSILGOUP_ACCOUNTCODE");

            entity.HasOne(d => d.LEVEL).WithMany(p => p.TB_ACCOUNT_LINK_TAFSILGROUPs)
                .HasForeignKey(d => d.LEVEL_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TBACCOUNTTAFSILGROUP_LEVEL");

            entity.HasOne(d => d.TAFSILGROUP).WithMany(p => p.TB_ACCOUNT_LINK_TAFSILGROUPs)
                .HasForeignKey(d => d.TAFSILGROUP_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TBACCOUNTLINKE_TAFSILGROUP");
        });

        modelBuilder.Entity<TB_ACCOUNT_LINK_TAFSILI>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK_ACCOUNTLINKTAFSILI");

            entity.ToTable("TB_ACCOUNT_LINK_TAFSILI");

            entity.Property(e => e.ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ACCOUNT_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ADDUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CHANGEUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CREATEDDATE).HasPrecision(6);
            entity.Property(e => e.ISDELETED).HasColumnType("NUMBER(1)");
            entity.Property(e => e.LEVEL_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.TAFSILI_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.UPDATEDDATE).HasPrecision(6);
            entity.Property(e => e.VAHEDCODE)
                .HasMaxLength(4)
                .IsUnicode(false);

            entity.HasOne(d => d.ACCOUNT).WithMany(p => p.TB_ACCOUNT_LINK_TAFSILIs)
                .HasForeignKey(d => d.ACCOUNT_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ACCOUNTLINKTAFSILI_ACCOUNT");

            entity.HasOne(d => d.LEVEL).WithMany(p => p.TB_ACCOUNT_LINK_TAFSILIs)
                .HasForeignKey(d => d.LEVEL_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ACCOUNTLINKTAFSILI_LEVEL");

            entity.HasOne(d => d.TAFSILI).WithMany(p => p.TB_ACCOUNT_LINK_TAFSILIs)
                .HasForeignKey(d => d.TAFSILI_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ACCOUNTLINKTAFSILI_TAFSILI");
        });

        modelBuilder.Entity<TB_ACCOUNT_TYPE>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK_ACCOUNT_TYPE");

            entity.ToTable("TB_ACCOUNT_TYPE");

            entity.Property(e => e.ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.TITLE)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TB_ATTACH>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK_TBATTACH");

            entity.ToTable("TB_ATTACH");

            entity.Property(e => e.ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ADDUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.ATTACH_FILE).HasColumnType("BLOB");
            entity.Property(e => e.ATTACH_NAME).HasMaxLength(250);
            entity.Property(e => e.ATTACH_RADIF).HasPrecision(3);
            entity.Property(e => e.ATTACH_SIZE).HasPrecision(10);
            entity.Property(e => e.CHANGEUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CREATEDDATE).HasPrecision(6);
            entity.Property(e => e.ISDELETED).HasColumnType("NUMBER(1)");
            entity.Property(e => e.PAYRECEIVE_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.TAFSILI_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.TITLE).HasMaxLength(250);
            entity.Property(e => e.UPDATEDDATE).HasPrecision(6);
            entity.Property(e => e.VAHEDCODE)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.VOUCHERSHEAD_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.YEAR)
                .HasMaxLength(4)
                .IsUnicode(false)
                .IsFixedLength();

            entity.HasOne(d => d.TAFSILI).WithMany(p => p.TB_ATTACHes)
                .HasForeignKey(d => d.TAFSILI_ID)
                .HasConstraintName("FK_TBATTACH_TAFSILI");
        });

        modelBuilder.Entity<TB_ATTRIBFORACCOUNTCODE>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK_ATTRIBFORMAINCODE");

            entity.ToTable("TB_ATTRIBFORACCOUNTCODE");

            entity.HasIndex(e => new { e.ACCOUNTCODE_ID, e.VAHEDCODE, e.YEAR }, "AK_AK_ATTRIBFORMAINCO_ATTRIBFO").IsUnique();

            entity.HasIndex(e => e.ACCOUNTCODE_ID, "REFERENCE_9_FK");

            entity.Property(e => e.ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .IsFixedLength()
                .HasConversion(GuidToChar36Converter.Instance)
                .HasComment("اي دي كدهاي شناسه دار ");
            entity.Property(e => e.ACCOUNTCODE_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .IsFixedLength()
                .HasConversion(GuidToChar36Converter.Instance)
                .HasComment("اي دي كدينگ مالي ");
            entity.Property(e => e.ADDUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.ATTRIBBOXNO)
                .HasComment("مشخصه تعداد شناسه ")
                .HasColumnType("NUMBER(1)");
            entity.Property(e => e.ATTRIBSUM)
                .HasComment("جمع پذير يا جمع ناپذير ")
                .HasColumnType("NUMBER(1)");
            entity.Property(e => e.CHANGEUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CONTROLID)
                .IsRequired()
                .HasDefaultValueSql("null ")
                .HasColumnType("NUMBER(1)");
            entity.Property(e => e.CREATEDDATE).HasPrecision(6);
            entity.Property(e => e.FLAG)
                .HasComment("مشخصه نوع شناسه ")
                .HasColumnType("NUMBER(1)");
            entity.Property(e => e.ISDELETED).HasColumnType("NUMBER(1)");
            entity.Property(e => e.LENATR)
                .HasPrecision(2)
                .HasComment("مشخصه طول شناسه ");
            entity.Property(e => e.UPDATEDDATE).HasPrecision(6);
            entity.Property(e => e.VAHEDCODE)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasComment("كد واحد ");
            entity.Property(e => e.YEAR)
                .HasMaxLength(4)
                .IsUnicode(false);

            entity.HasOne(d => d.ACCOUNTCODE).WithMany(p => p.TB_ATTRIBFORACCOUNTCODEs)
                .HasForeignKey(d => d.ACCOUNTCODE_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ATTRIBFO_ACCOUNTCODE");
        });

        modelBuilder.Entity<TB_ATTRIBSINVOUCHER>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK_TB_ATTRIBSINDOCS");

            entity.ToTable("TB_ATTRIBSINVOUCHERS");

            entity.HasIndex(e => new { e.VOUCHERSDETAIL_ID, e.ATTRIBUTEVALUE }, "IDX$$_B39B0004");

            entity.HasIndex(e => new { e.VOUCHERSDETAIL_ID, e.YEAR, e.VAHEDCODE }, "IDX$$_B39B0006");

            entity.HasIndex(e => e.ATTRIBFORACCOUNTCODE_ID, "REFERENCE_10_FK");

            entity.HasIndex(e => e.VOUCHERSDETAIL_ID, "REFERENCE_8_FK");

            entity.HasIndex(e => new { e.VOUCHERSDETAIL_ID, e.ATTRIBFORACCOUNTCODE_ID, e.VAHEDCODE, e.YEAR }, "UK_TB_ATTRIBSINDOCS").IsUnique();

            entity.Property(e => e.ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .IsFixedLength()
                .HasConversion(GuidToChar36Converter.Instance)
                .HasComment("اي دي كدهاي شناسه دار ");
            entity.Property(e => e.ADDUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.ATTRIBFORACCOUNTCODE_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ATTRIBUTEVALUE)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasComment("مقدار شناسه ");
            entity.Property(e => e.CHANGEUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CREATEDDATE).HasPrecision(6);
            entity.Property(e => e.ISDELETED).HasColumnType("NUMBER(1)");
            entity.Property(e => e.UPDATEDDATE).HasPrecision(6);
            entity.Property(e => e.VAHEDCODE)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasComment("كد واحد ");
            entity.Property(e => e.VOUCHERSDETAIL_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .IsFixedLength()
                .HasConversion(GuidToChar36Converter.Instance)
                .HasComment("آي دي ريز اسناد ");
            entity.Property(e => e.YEAR)
                .HasMaxLength(4)
                .IsUnicode(false);

            entity.HasOne(d => d.ATTRIBFORACCOUNTCODE).WithMany(p => p.TB_ATTRIBSINVOUCHERs)
                .HasForeignKey(d => d.ATTRIBFORACCOUNTCODE_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ATTRIBFORACCOUNTCODE");

            entity.HasOne(d => d.VOUCHERSDETAIL).WithMany(p => p.TB_ATTRIBSINVOUCHERs)
                .HasForeignKey(d => d.VOUCHERSDETAIL_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_VOUCHERSDETAIL");
        });

        modelBuilder.Entity<TB_AUDITLOG>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("TB_AUDITLOG");

            entity.Property(e => e.ACTIONTYPE)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CHANGEDATE)
                .HasPrecision(6)
                .HasDefaultValueSql("SYSTIMESTAMP               -- تاريخ تغيير\n");
            entity.Property(e => e.COLUMNNAME)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.NEWVALUE).HasColumnType("CLOB");
            entity.Property(e => e.OLDVALUE).HasColumnType("CLOB");
            entity.Property(e => e.RECORDID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.TABLENAME)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.USERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.VAHEDCODE)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.YEAR)
                .HasMaxLength(4)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TB_BANKBRANCH_LIST>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK_BANKBRANCH");

            entity.ToTable("TB_BANKBRANCH_LIST");

            entity.Property(e => e.ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.BANK_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.BRANCHCODE)
                .HasMaxLength(5)
                .IsUnicode(false);
            entity.Property(e => e.BRANCHNAME)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.CITY_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.TAFSILI_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();

            entity.HasOne(d => d.BANK).WithMany(p => p.TB_BANKBRANCH_LISTs)
                .HasForeignKey(d => d.BANK_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BANK");
        });

        modelBuilder.Entity<TB_BANKCARTDETAIL>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK_BANKCARTDETAIL");

            entity.ToTable("TB_BANKCARTDETAIL");

            entity.HasIndex(e => new { e.BANK_ID, e.BRANCH_ID, e.ACCOUNTNUMBER, e.MONTH, e.CHEQNO, e.RECIVDATE, e.CHECKRECEIPTTYPE, e.DEBTOR, e.CREDITOR, e.VAHEDCODE, e.YEAR }, "AK_AK_BANKCARTDETAIL_BANKCART").IsUnique();

            entity.HasIndex(e => e.CHECK_ID, "REFERENCE_32_FK");

            entity.Property(e => e.ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .IsFixedLength()
                .HasConversion(GuidToChar36Converter.Instance)
                .HasComment("آي دي كارت بانك ");
            entity.Property(e => e.ACCOUNTNUMBER)
                .HasMaxLength(13)
                .IsUnicode(false)
                .HasComment("شماره جاري ");
            entity.Property(e => e.ADDUSERID)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasComment("كاربر ايجاد كننده");
            entity.Property(e => e.BANK_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .IsFixedLength()
                .HasConversion(GuidToChar36Converter.Instance)
                .HasComment("كد بانك ");
            entity.Property(e => e.BRANCH_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .IsFixedLength()
                .HasConversion(GuidToChar36Converter.Instance)
                .HasComment("كد شعبه بانك ");
            entity.Property(e => e.CHANGEUSERID)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasComment("كاربر تغيير دهنده");
            entity.Property(e => e.CHECKRECEIPTTYPE)
                .HasComment("نوع مدرك بانكي (فيش يا حواله)   ")
                .HasColumnType("NUMBER(1)");
            entity.Property(e => e.CHECK_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .IsFixedLength()
                .HasConversion(GuidToChar36Converter.Instance)
                .HasComment("آي دي چك يا اعلاميه ");
            entity.Property(e => e.CHECK_INCORRENT_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .IsFixedLength()
                .HasConversion(GuidToChar36Converter.Instance)
                .HasComment("آي دي در جريان سالهاي قبل  ");
            entity.Property(e => e.CHEQNO)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasComment("شماره چك ");
            entity.Property(e => e.CREATEDDATE)
                .HasPrecision(6)
                .HasComment("تاريخ ايجاد");
            entity.Property(e => e.CREDITOR)
                .HasComment("مبلغ بستانكاري ")
                .HasColumnType("NUMBER(25)");
            entity.Property(e => e.DEBTOR)
                .HasComment("مبلغ بدهكاري ")
                .HasColumnType("NUMBER(25)");
            entity.Property(e => e.ISDELETED).HasColumnType("NUMBER(1)");
            entity.Property(e => e.MONTH)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasComment("ماه عملكرد ");
            entity.Property(e => e.RECEIP_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .IsFixedLength()
                .HasConversion(GuidToChar36Converter.Instance)
                .HasComment("آي دي فيش يا حواله ");
            entity.Property(e => e.RECIVDATE)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasComment("تاريخ اجرا ");
            entity.Property(e => e.UPDATEDDATE)
                .HasPrecision(6)
                .HasComment("تاريخ تغيير");
            entity.Property(e => e.VAHEDCODE)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasComment("كد واحد");
            entity.Property(e => e.YEAR)
                .HasMaxLength(4)
                .IsUnicode(false)
                .IsFixedLength();

            entity.HasOne(d => d.CHECK).WithMany(p => p.TB_BANKCARTDETAILs)
                .HasForeignKey(d => d.CHECK_ID)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_BANKCART_CHECK");

            entity.HasOne(d => d.RECEIP).WithMany(p => p.TB_BANKCARTDETAILs)
                .HasForeignKey(d => d.RECEIP_ID)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_BANKCART_RECEIP");
        });

        modelBuilder.Entity<TB_BANK_LIST>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK_BANK");

            entity.ToTable("TB_BANK_LIST");

            entity.Property(e => e.ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.BANKCODE)
                .HasMaxLength(2)
                .IsUnicode(false);
            entity.Property(e => e.BANKNAME)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TB_BILL_LOG>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK_BILLLOG");

            entity.ToTable("TB_BILL_LOG");

            entity.Property(e => e.ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasDefaultValueSql("null")
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ADDUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CHANGEUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CREATEDDATE).HasPrecision(6);
            entity.Property(e => e.ISDELETED).HasColumnType("NUMBER(1)");
            entity.Property(e => e.LOG_DATE)
                .HasMaxLength(8)
                .IsUnicode(false);
            entity.Property(e => e.LOG_DESC)
                .HasMaxLength(1000)
                .IsUnicode(false);
            entity.Property(e => e.UPDATEDDATE).HasPrecision(6);
            entity.Property(e => e.VAHEDCODE)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.YEAR)
                .HasMaxLength(4)
                .IsUnicode(false)
                .IsFixedLength();
        });

        modelBuilder.Entity<TB_CHARGEANDCOST_DETAIL>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK_CHARGEANDCOSTDETAIL");

            entity.ToTable("TB_CHARGEANDCOST_DETAIL");

            entity.Property(e => e.ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ADDUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CHANGEUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CHARGEANDCOSTHEAD_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.CREATEDDATE).HasPrecision(6);
            entity.Property(e => e.CREDITOR).HasColumnType("NUMBER(25)");
            entity.Property(e => e.DEBTOR).HasColumnType("NUMBER(25)");
            entity.Property(e => e.EXPENSE_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ISDELETED).HasColumnType("NUMBER(1)");
            entity.Property(e => e.PAIDAMOUNT).HasColumnType("NUMBER(25)");
            entity.Property(e => e.PAYTO)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.REVOLVINGFUND_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.UPDATEDDATE).HasPrecision(6);
            entity.Property(e => e.VAHEDCODE)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.YEAR)
                .HasMaxLength(4)
                .IsUnicode(false);

            entity.HasOne(d => d.CHARGEANDCOSTHEAD).WithMany(p => p.TB_CHARGEANDCOST_DETAILs)
                .HasForeignKey(d => d.CHARGEANDCOSTHEAD_ID)
                .HasConstraintName("FK_CHARGEANDCOSTHEAD_DETAIL");

            entity.HasOne(d => d.EXPENSE).WithMany(p => p.TB_CHARGEANDCOST_DETAILs)
                .HasForeignKey(d => d.EXPENSE_ID)
                .HasConstraintName("FK_EXPENSE_CHARGEANDCOST");

            entity.HasOne(d => d.REVOLVINGFUND).WithMany(p => p.TB_CHARGEANDCOST_DETAILs)
                .HasForeignKey(d => d.REVOLVINGFUND_ID)
                .HasConstraintName("FK_REVOLVING_CHARGEANDCOST");
        });

        modelBuilder.Entity<TB_CHARGEANDCOST_HEAD>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK_CHARGEANDCOST_HEAD");

            entity.ToTable("TB_CHARGEANDCOST_HEAD");

            entity.Property(e => e.ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ACCOUNT_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ADDUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CHANGEUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CHARGEANDCOST_CODE)
                .HasMaxLength(5)
                .IsUnicode(false);
            entity.Property(e => e.CHARGEANDCOST_DATE)
                .HasMaxLength(8)
                .IsUnicode(false);
            entity.Property(e => e.CHARGEANDCOST_TYPE).HasColumnType("NUMBER(1)");
            entity.Property(e => e.CREATEDDATE).HasPrecision(6);
            entity.Property(e => e.DESCRIPTION)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.ISDELETED).HasColumnType("NUMBER(1)");
            entity.Property(e => e.STATUS).HasColumnType("NUMBER(1)");
            entity.Property(e => e.UPDATEDDATE).HasPrecision(6);
            entity.Property(e => e.VAHEDCODE)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.YEAR)
                .HasMaxLength(4)
                .IsUnicode(false);

            entity.HasOne(d => d.ACCOUNT).WithMany(p => p.TB_CHARGEANDCOST_HEADs)
                .HasForeignKey(d => d.ACCOUNT_ID)
                .HasConstraintName("FK_ACCOUNT_DETAIL");
        });

        modelBuilder.Entity<TB_CHARGE_LINK_COST>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK_CHARGELINKCOST");

            entity.ToTable("TB_CHARGE_LINK_COST");

            entity.Property(e => e.ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ADDUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.AMOUNT).HasColumnType("NUMBER(25)");
            entity.Property(e => e.CHANGEUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CHARGE_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.COST_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.CREATEDDATE).HasPrecision(6);
            entity.Property(e => e.ISDELETED).HasColumnType("NUMBER(1)");
            entity.Property(e => e.UPDATEDDATE).HasPrecision(6);
            entity.Property(e => e.VAHEDCODE)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.YEAR)
                .HasMaxLength(4)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TB_CHECK>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK_CHECK");

            entity.ToTable("TB_CHECK");

            entity.HasIndex(e => new { e.CHECKBOOK_ID, e.CHEQ_NO, e.VAHEDCODE }, "UK_CHECK").IsUnique();

            entity.Property(e => e.ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .IsFixedLength()
                .HasConversion(GuidToChar36Converter.Instance)
                .HasComment(" آي دي پرداختني (چك يا اعلاميه");
            entity.Property(e => e.ADDUSERID)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasComment("كاربر ايجاد كننده");
            entity.Property(e => e.CHANGEUSERID)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasComment(" کاربر تغيير دهنده");
            entity.Property(e => e.CHECKBOOK_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .IsFixedLength()
                .HasConversion(GuidToChar36Converter.Instance)
                .HasComment("آي دي دسته چك ");
            entity.Property(e => e.CHEQ_DATE)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasComment("تاريخ ايجاد ");
            entity.Property(e => e.CHEQ_NO)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasComment("شماره پرداخت ");
            entity.Property(e => e.CREATEDDATE)
                .HasPrecision(6)
                .HasComment("تاريخ ايجاد");
            entity.Property(e => e.DATE_RSID)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasComment("تاريخ اجرا ");
            entity.Property(e => e.EBTAL)
                .HasComment("ابطال ")
                .HasColumnType("NUMBER(1)");
            entity.Property(e => e.ISDELETED).HasColumnType("NUMBER(1)");
            entity.Property(e => e.PAPER_DESC)
                .HasMaxLength(800)
                .IsUnicode(false)
                .HasComment("بابت");
            entity.Property(e => e.PAYTO)
                .HasMaxLength(200)
                .IsUnicode(false)
                .HasComment("دروجه");
            entity.Property(e => e.PRINT)
                .HasComment("چاپ")
                .HasColumnType("NUMBER(1)");
            entity.Property(e => e.UPDATEDDATE)
                .HasPrecision(6)
                .HasComment("تاريخ تغيير");
            entity.Property(e => e.VAHEDCODE)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasComment("كد واحد ");

            entity.HasOne(d => d.CHECKBOOK).WithMany(p => p.TB_CHECKs)
                .HasForeignKey(d => d.CHECKBOOK_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CHECKBOOK_CHECK");
        });

        modelBuilder.Entity<TB_CHECKBOOK>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK_CHECKBOOK");

            entity.ToTable("TB_CHECKBOOK");

            entity.HasIndex(e => new { e.ACCOUNT_ID, e.FROMCHECKNUMBER, e.TOCHECKNUMBER, e.VAHEDCODE }, "UK_CHECKBOOK").IsUnique();

            entity.Property(e => e.ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ACCOUNT_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ADDUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CHANGEUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CHECKBOOK_DATE)
                .HasMaxLength(8)
                .IsUnicode(false);
            entity.Property(e => e.CHECKBOOK_TITLE)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.CHECKBOOK_TYPE).HasColumnType("NUMBER(1)");
            entity.Property(e => e.CHECKTYPE_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.CREATEDDATE).HasPrecision(6);
            entity.Property(e => e.FROMCHECKNUMBER)
                .HasMaxLength(14)
                .IsUnicode(false);
            entity.Property(e => e.ISDELETED).HasColumnType("NUMBER(1)");
            entity.Property(e => e.SERIAL)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.TOCHECKNUMBER)
                .HasMaxLength(14)
                .IsUnicode(false);
            entity.Property(e => e.UPDATEDDATE).HasPrecision(6);
            entity.Property(e => e.VAHEDCODE)
                .HasMaxLength(4)
                .IsUnicode(false);

            entity.HasOne(d => d.ACCOUNT).WithMany(p => p.TB_CHECKBOOKs)
                .HasForeignKey(d => d.ACCOUNT_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CHECKBOOK_ACCOUNT");

            entity.HasOne(d => d.CHECKTYPE).WithMany(p => p.TB_CHECKBOOKs)
                .HasForeignKey(d => d.CHECKTYPE_ID)
                .HasConstraintName("FK_CHECKTYPE");
        });

        modelBuilder.Entity<TB_CHECK_TYPE>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK_CHECKTYPE");

            entity.ToTable("TB_CHECK_TYPE");

            entity.Property(e => e.ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ADDUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CHANGEUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CHEQUE_AAMOUNT_FONT)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.CHEQUE_AAMOUNT_LEFT).HasPrecision(4);
            entity.Property(e => e.CHEQUE_AAMOUNT_TOP).HasPrecision(4);
            entity.Property(e => e.CHEQUE_AAMOUNT_WIDTH).HasPrecision(4);
            entity.Property(e => e.CHEQUE_ADATE_FONT)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.CHEQUE_ADATE_LEFT).HasPrecision(4);
            entity.Property(e => e.CHEQUE_ADATE_TOP).HasPrecision(4);
            entity.Property(e => e.CHEQUE_ADATE_WIDTH).HasPrecision(4);
            entity.Property(e => e.CHEQUE_BREAKLINE_FONT)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.CHEQUE_BREAKLINE_LEFT).HasPrecision(4);
            entity.Property(e => e.CHEQUE_BREAKLINE_TOP).HasPrecision(4);
            entity.Property(e => e.CHEQUE_BREAKLINE_WIDTH).HasPrecision(4);
            entity.Property(e => e.CHEQUE_DESCRIBE1_FONT)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.CHEQUE_DESCRIBE1_LEFT).HasPrecision(4);
            entity.Property(e => e.CHEQUE_DESCRIBE1_TOP).HasPrecision(4);
            entity.Property(e => e.CHEQUE_DESCRIBE1_WIDTH).HasPrecision(4);
            entity.Property(e => e.CHEQUE_DESCRIBE2_FONT)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.CHEQUE_DESCRIBE2_LEFT).HasPrecision(4);
            entity.Property(e => e.CHEQUE_DESCRIBE2_TOP).HasPrecision(4);
            entity.Property(e => e.CHEQUE_DESCRIBE2_WIDTH).HasPrecision(4);
            entity.Property(e => e.CHEQUE_HEIGHT).HasPrecision(3);
            entity.Property(e => e.CHEQUE_IMAGE).HasColumnType("BLOB");
            entity.Property(e => e.CHEQUE_LAMOUNT_FONT)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.CHEQUE_LAMOUNT_LEFT).HasPrecision(4);
            entity.Property(e => e.CHEQUE_LAMOUNT_TOP).HasPrecision(4);
            entity.Property(e => e.CHEQUE_LAMOUNT_WIDTH).HasPrecision(4);
            entity.Property(e => e.CHEQUE_NAMOUNT_FONT)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.CHEQUE_NAMOUNT_LEFT).HasPrecision(4);
            entity.Property(e => e.CHEQUE_NAMOUNT_TOP).HasPrecision(4);
            entity.Property(e => e.CHEQUE_NAMOUNT_WIDTH).HasPrecision(4);
            entity.Property(e => e.CHEQUE_NDATE_FONT)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.CHEQUE_NDATE_LEFT).HasPrecision(4);
            entity.Property(e => e.CHEQUE_NDATE_TOP).HasPrecision(4);
            entity.Property(e => e.CHEQUE_NDATE_WIDTH).HasPrecision(4);
            entity.Property(e => e.CHEQUE_TYPE_TITLE)
                .HasMaxLength(25)
                .IsUnicode(false);
            entity.Property(e => e.CHEQUE_WIDTH).HasPrecision(3);
            entity.Property(e => e.CREATEDDATE).HasPrecision(6);
            entity.Property(e => e.ISDELETED).HasColumnType("NUMBER(1)");
            entity.Property(e => e.PRINTER_MARGINE_LEFT).HasPrecision(3);
            entity.Property(e => e.PRINTER_MARGINE_TOP).HasPrecision(3);
            entity.Property(e => e.PRINTER_TYPE)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.UPDATEDDATE).HasPrecision(6);
            entity.Property(e => e.VAHEDCODE)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.YEAR)
                .HasMaxLength(4)
                .IsUnicode(false)
                .IsFixedLength();
        });

        modelBuilder.Entity<TB_CHEQUES_INCORRENT>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK_CHEQUES_INCORRENT");

            entity.ToTable("TB_CHEQUES_INCORRENT");

            entity.Property(e => e.ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .IsFixedLength()
                .HasConversion(GuidToChar36Converter.Instance)
                .HasComment("PK");
            entity.Property(e => e.ACCOUNTNUMBER)
                .HasMaxLength(13)
                .IsUnicode(false)
                .HasComment("شماره جاري ");
            entity.Property(e => e.ADDUSERID)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasComment("كاربر ايجاد كننده");
            entity.Property(e => e.CHANGEUSERID)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasComment("كاربر تغيير دهنده");
            entity.Property(e => e.CHECK_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.CHEQ_DATE)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasComment("تاريخ ايجاد ");
            entity.Property(e => e.CHEQ_NO)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasComment("شماره پرداخت ");
            entity.Property(e => e.CREATEDDATE)
                .HasPrecision(6)
                .HasComment("تاريخ ايجاد");
            entity.Property(e => e.CREDITOR)
                .HasComment("بستانكار")
                .HasColumnType("NUMBER(25)");
            entity.Property(e => e.DOC_DATE)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasComment("تاريخ سند");
            entity.Property(e => e.DOC_NUM)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasComment("شماره واقعي سند");
            entity.Property(e => e.ISDELETED).HasColumnType("NUMBER(1)");
            entity.Property(e => e.PAPER_DESC)
                .HasMaxLength(200)
                .IsUnicode(false)
                .HasComment("بابت");
            entity.Property(e => e.PAYTO)
                .HasMaxLength(200)
                .IsUnicode(false)
                .HasComment("دروجه");
            entity.Property(e => e.RECIVDATE)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasComment("تاريخ اجرا ");
            entity.Property(e => e.UPDATEDDATE)
                .HasPrecision(6)
                .HasComment("تاريخ تغيير");
            entity.Property(e => e.VAHEDCODE)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasComment("كد واحد");
            entity.Property(e => e.YEAR)
                .HasMaxLength(4)
                .IsUnicode(false)
                .IsFixedLength()
                .HasComment("سال استفاده از چك");
        });

        modelBuilder.Entity<TB_CITY>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK_CITY");

            entity.ToTable("TB_CITY");

            entity.HasIndex(e => e.CITYCODE, "UK_CITY").IsUnique();

            entity.Property(e => e.ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasDefaultValueSql("sys_guid() ")
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.CITYCODE)
                .HasMaxLength(5)
                .IsUnicode(false);
            entity.Property(e => e.CITYNAME)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.PROVINCE_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();

            entity.HasOne(d => d.PROVINCE).WithMany(p => p.TB_CITies)
                .HasForeignKey(d => d.PROVINCE_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CITY_PROVINCE");
        });

        modelBuilder.Entity<TB_ELAMDETAIL>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK_ELAMDETAIL");

            entity.ToTable("TB_ELAMDETAIL");

            entity.Property(e => e.ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .IsFixedLength()
                .HasConversion(GuidToChar36Converter.Instance)
                .HasComment("آي دي ريز اعلاميه ");
            entity.Property(e => e.ACCOUNTCODE_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .IsFixedLength()
                .HasConversion(GuidToChar36Converter.Instance)
                .HasComment("آي دي حساب تفضيلي");
            entity.Property(e => e.ADDUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CHANGEUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CREATEDDATE).HasPrecision(6);
            entity.Property(e => e.CREDITOR)
                .HasComment("بستانكار")
                .HasColumnType("NUMBER(25)");
            entity.Property(e => e.DEBTOR)
                .HasComment("بدهكار")
                .HasColumnType("NUMBER(25)");
            entity.Property(e => e.ELAMD_DESC)
                .HasMaxLength(800)
                .IsUnicode(false)
                .HasComment("شرح آرتيكل اعلاميه");
            entity.Property(e => e.ELAMHEAD_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .IsFixedLength()
                .HasConversion(GuidToChar36Converter.Instance)
                .HasComment("آي دي تيتر اعلاميه ");
            entity.Property(e => e.ELAM_ATRIBNO)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasComment("شماره شناسايي ");
            entity.Property(e => e.ISDELETED).HasColumnType("NUMBER(1)");
            entity.Property(e => e.UPDATEDDATE).HasPrecision(6);
            entity.Property(e => e.VAHEDCODE)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasComment("كد واحد ");
            entity.Property(e => e.YEAR)
                .HasMaxLength(4)
                .IsUnicode(false)
                .IsFixedLength();

            entity.HasOne(d => d.ACCOUNTCODE).WithMany(p => p.TB_ELAMDETAILs)
                .HasForeignKey(d => d.ACCOUNTCODE_ID)
                .HasConstraintName("FK_ELAMDETAIL_ACCOUNTCODE");

            entity.HasOne(d => d.ELAMHEAD).WithMany(p => p.TB_ELAMDETAILs)
                .HasForeignKey(d => d.ELAMHEAD_ID)
                .HasConstraintName("FK_ELAM_DETAIL_HEAD");
        });

        modelBuilder.Entity<TB_ELAMDETAIL_LINK_TAFSILI>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK_ELAMDETAIL_LINK");

            entity.ToTable("TB_ELAMDETAIL_LINK_TAFSILI");

            entity.HasIndex(e => e.TAFSILI_ID, "IDX_ELAMLINKI_TAFSILI_ID");

            entity.Property(e => e.ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ADDUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CHANGEUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CREATEDDATE).HasPrecision(6);
            entity.Property(e => e.ELAMDETAIL_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ISDELETED).HasColumnType("NUMBER(1)");
            entity.Property(e => e.LEVEL_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.TAFSILI_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.UPDATEDDATE).HasPrecision(6);
            entity.Property(e => e.VAHEDCODE)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.YEAR)
                .HasMaxLength(4)
                .IsUnicode(false);

            entity.HasOne(d => d.ELAMDETAIL).WithMany(p => p.TB_ELAMDETAIL_LINK_TAFSILIs)
                .HasForeignKey(d => d.ELAMDETAIL_ID)
                .HasConstraintName("FK_ELAMLINK_ELAMDETAIL");

            entity.HasOne(d => d.LEVEL).WithMany(p => p.TB_ELAMDETAIL_LINK_TAFSILIs)
                .HasForeignKey(d => d.LEVEL_ID)
                .HasConstraintName("FK_ELAMLINK_LEVEL");

            entity.HasOne(d => d.TAFSILI).WithMany(p => p.TB_ELAMDETAIL_LINK_TAFSILIs)
                .HasForeignKey(d => d.TAFSILI_ID)
                .HasConstraintName("FK_ELAMLINK_TAFSILI");
        });

        modelBuilder.Entity<TB_ELAMHEAD>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK_ELAMHEAD");

            entity.ToTable("TB_ELAMHEAD");

            entity.HasIndex(e => new { e.ELAMH_SERIALNO, e.ELAMH_CODE, e.VAHEDCODE }, "AK_AK_ELAMHEAD_ELAMHEAD").IsUnique();

            entity.Property(e => e.ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .IsFixedLength()
                .HasConversion(GuidToChar36Converter.Instance)
                .HasComment("آي دي تيتر اعلاميه ");
            entity.Property(e => e.ADDUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CHANGEUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CREATEDDATE).HasPrecision(6);
            entity.Property(e => e.ELAMHDRAMAD_TYPE)
                .HasComment(" 3حق بيمه نوع اعلاميه 1ذي حسابي 2سايردرآمد")
                .HasColumnType("NUMBER(1)");
            entity.Property(e => e.ELAMH_CASE)
                .HasComment("نوع اعلاميه 1بد     2بس")
                .HasColumnType("NUMBER(1)");
            entity.Property(e => e.ELAMH_CODE)
                .HasMaxLength(6)
                .IsUnicode(false);
            entity.Property(e => e.ELAMH_DABIRDATE)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasComment("تاريخ دبيرخانه");
            entity.Property(e => e.ELAMH_DABIRNO)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasComment("شماره دبيرخانه ");
            entity.Property(e => e.ELAMH_DATE)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasComment("تاريخ اعلاميه");
            entity.Property(e => e.ELAMH_DESC)
                .HasMaxLength(300)
                .IsUnicode(false)
                .HasComment("شرح اعلاميه");
            entity.Property(e => e.ELAMH_LSTMON)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasComment("ماه عملکرد ليست");
            entity.Property(e => e.ELAMH_PRINTNO)
                .HasPrecision(5)
                .HasComment("تعداد چاپ ");
            entity.Property(e => e.ELAMH_RCVDT)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasComment("،تاريخ پيمان، تاريخ رسيد ليست");
            entity.Property(e => e.ELAMH_RCVNO)
                .HasMaxLength(14)
                .IsUnicode(false)
                .HasComment("،شماره بدهي ، شماره رسيد ليست");
            entity.Property(e => e.ELAMH_SENDRCVVAHED)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasComment("كدواحد گيرنده يا ارسال كننده ");
            entity.Property(e => e.ELAMH_SERIALNO)
                .HasMaxLength(14)
                .IsUnicode(false)
                .HasComment("شماره سريال اعلاميه  ");
            entity.Property(e => e.ELAMH_WORKSHOPCODE)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasComment("کدکارگاه");
            entity.Property(e => e.ELAMH_WORKSHOPNAME)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasComment("نام کارگاه");
            entity.Property(e => e.ELAMH_YEAR)
                .HasMaxLength(2)
                .IsUnicode(false);
            entity.Property(e => e.ELAMSENDERID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .IsFixedLength()
                .HasConversion(GuidToChar36Converter.Instance)
                .HasComment("شناسه اعلاميه صادره");
            entity.Property(e => e.ISDELETED).HasColumnType("NUMBER(1)");
            entity.Property(e => e.PAY_NO)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasComment("برگه پرداخت");
            entity.Property(e => e.PEIMAN_NO)
                .HasMaxLength(12)
                .IsUnicode(false)
                .HasComment("شماره پيمان");
            entity.Property(e => e.SERIALNO_INPUT)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasComment("سريال اعلاميه ورودي");
            entity.Property(e => e.UPDATEDDATE).HasPrecision(6);
            entity.Property(e => e.VAHEDCODE)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasComment("كد واحد ");
            entity.Property(e => e.VOUCHERSHEAD_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .IsFixedLength()
                .HasConversion(GuidToChar36Converter.Instance)
                .HasComment("آي دي سند");
            entity.Property(e => e.WEB_STAT)
                .HasPrecision(2)
                .HasComment("ارسال از طريق وب=1-ارسال شده=2");
            entity.Property(e => e.WORKSHOP_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .IsFixedLength()
                .HasConversion(GuidToChar36Converter.Instance)
                .HasComment("کارگاه");
            entity.Property(e => e.YEAR)
                .HasMaxLength(4)
                .IsUnicode(false)
                .IsFixedLength();

            entity.HasOne(d => d.VOUCHERSHEAD).WithMany(p => p.TB_ELAMHEADs)
                .HasForeignKey(d => d.VOUCHERSHEAD_ID)
                .HasConstraintName("FK_ELAM_VOUCHER");
        });

        modelBuilder.Entity<TB_EXPENCE>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK_EXPENCE");

            entity.ToTable("TB_EXPENCE");

            entity.HasIndex(e => new { e.EXPENCECODE, e.VAHEDCODE }, "UK_EXPENSE_CODE").IsUnique();

            entity.Property(e => e.ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ACCOUNTCODE_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ADDUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CHANGEUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CREATEDDATE).HasPrecision(6);
            entity.Property(e => e.DEFAULTAMOUNT).HasColumnType("NUMBER(25)");
            entity.Property(e => e.DESCRIPTION)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.EXPENCECODE)
                .HasMaxLength(2)
                .IsUnicode(false);
            entity.Property(e => e.EXPENCEGROUP_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.EXPENCENAME)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.ISDELETED).HasColumnType("NUMBER(1)");
            entity.Property(e => e.UPDATEDDATE).HasPrecision(6);
            entity.Property(e => e.VAHEDCODE)
                .HasMaxLength(4)
                .IsUnicode(false);

            entity.HasOne(d => d.ACCOUNTCODE).WithMany(p => p.TB_EXPENCEs)
                .HasForeignKey(d => d.ACCOUNTCODE_ID)
                .HasConstraintName("FK_EXPENSE_ACCOUNTCODE");

            entity.HasOne(d => d.EXPENCEGROUP).WithMany(p => p.TB_EXPENCEs)
                .HasForeignKey(d => d.EXPENCEGROUP_ID)
                .HasConstraintName("FK_EXPENCEGROUP");
        });

        modelBuilder.Entity<TB_EXPENCEGROUP>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK_EXPENCEGROUP");

            entity.ToTable("TB_EXPENCEGROUP");

            entity.HasIndex(e => new { e.EXPENCEGROUPCODE, e.VAHEDCODE }, "UK_EXPENCEGROUP").IsUnique();

            entity.Property(e => e.ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.DESCRIPTION)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.EXPENCEGROUPCODE)
                .HasMaxLength(2)
                .IsUnicode(false);
            entity.Property(e => e.EXPENCEGROUPNAME)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.VAHEDCODE)
                .HasMaxLength(4)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TB_EXPENCE_LINK_TAFSILI>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK_EXPENCELINKTAFSILI");

            entity.ToTable("TB_EXPENCE_LINK_TAFSILI");

            entity.Property(e => e.ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ADDUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CHANGEUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CREATEDDATE).HasPrecision(6);
            entity.Property(e => e.EXPENSE_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ISDELETED).HasColumnType("NUMBER(1)");
            entity.Property(e => e.LEVEL_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.TAFSILI_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.UPDATEDDATE).HasPrecision(6);
            entity.Property(e => e.VAHEDCODE)
                .HasMaxLength(4)
                .IsUnicode(false);

            entity.HasOne(d => d.EXPENSE).WithMany(p => p.TB_EXPENCE_LINK_TAFSILIs)
                .HasForeignKey(d => d.EXPENSE_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EXPENCELINKTAFSILI_EXPENCCE");

            entity.HasOne(d => d.LEVEL).WithMany(p => p.TB_EXPENCE_LINK_TAFSILIs)
                .HasForeignKey(d => d.LEVEL_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EXPENCELINKTAFSILI_LEVEL");

            entity.HasOne(d => d.TAFSILI).WithMany(p => p.TB_EXPENCE_LINK_TAFSILIs)
                .HasForeignKey(d => d.TAFSILI_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EXPENCELINKTAFSILI_TAFSILI");
        });

        modelBuilder.Entity<TB_IDENTITYDETAIL>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK_IDENTYDETAILS");

            entity.ToTable("TB_IDENTITYDETAILS");

            entity.HasIndex(e => new { e.IDENTITYSUBGRPS_ID, e.IDENTITYHEAD_ID, e.VOUCHERSDETAIL_ID, e.VAHEDCODE, e.YEAR }, "AK_AK_IDENTYDETAILS_IDENTYDE").IsUnique();

            entity.HasIndex(e => e.VOUCHERSDETAIL_ID, "REFERENCE_21_FK");

            entity.HasIndex(e => e.IDENTITYSUBGRPS_ID, "REFERENCE_22_FK");

            entity.HasIndex(e => e.IDENTITYHEAD_ID, "REFERENCE_30_FK");

            entity.Property(e => e.ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .IsFixedLength()
                .HasConversion(GuidToChar36Converter.Instance)
                .HasComment("آي دي مقادير متغير شناسنامه ");
            entity.Property(e => e.ADDUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CHANGEUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CREATEDDATE).HasPrecision(6);
            entity.Property(e => e.DETAIL_VALUE)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasComment("مقدار شناسنامه ");
            entity.Property(e => e.IDENTITYHEAD_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.IDENTITYSUBGRPS_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .IsFixedLength()
                .HasConversion(GuidToChar36Converter.Instance)
                .HasComment("آي دي زير گروه شناسنامه ");
            entity.Property(e => e.ISDELETED).HasColumnType("NUMBER(1)");
            entity.Property(e => e.UPDATEDDATE).HasPrecision(6);
            entity.Property(e => e.VAHEDCODE)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasComment("كد واحد");
            entity.Property(e => e.VOUCHERSDETAIL_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .IsFixedLength()
                .HasConversion(GuidToChar36Converter.Instance)
                .HasComment("آي دي آرتيكل اسناد ");
            entity.Property(e => e.YEAR)
                .HasMaxLength(4)
                .IsUnicode(false);

            entity.HasOne(d => d.IDENTITYHEAD).WithMany(p => p.TB_IDENTITYDETAILs)
                .HasForeignKey(d => d.IDENTITYHEAD_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_IDENTITYHEAD_DETAIL");

            entity.HasOne(d => d.IDENTITYSUBGRPS).WithMany(p => p.TB_IDENTITYDETAILs)
                .HasForeignKey(d => d.IDENTITYSUBGRPS_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_IDENTYDE_IDENTYSU");

            entity.HasOne(d => d.VOUCHERSDETAIL).WithMany(p => p.TB_IDENTITYDETAILs)
                .HasForeignKey(d => d.VOUCHERSDETAIL_ID)
                .HasConstraintName("FK_IDENTYDE_VOUCHERSDETAIL");
        });

        modelBuilder.Entity<TB_IDENTITYFIXITEM>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK_IDENTYFIXITEMS");

            entity.ToTable("TB_IDENTITYFIXITEMS");

            entity.HasIndex(e => new { e.IDENTITYHEAD_ID, e.IDENTITYSUBGRPS_ID, e.VAHEDCODE, e.YEAR }, "AK_AK_IDENTYFIXITEMS_IDENTYFI").IsUnique();

            entity.HasIndex(e => e.IDENTITYHEAD_ID, "REFERENCE_19_FK");

            entity.HasIndex(e => e.IDENTITYSUBGRPS_ID, "REFERENCE_20_FK");

            entity.Property(e => e.ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .IsFixedLength()
                .HasConversion(GuidToChar36Converter.Instance)
                .HasComment("آي دي مقادير ثابت شناسنامه ");
            entity.Property(e => e.ADDUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CHANGEUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CREATEDDATE).HasPrecision(6);
            entity.Property(e => e.FIXITEMS_VALUE)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasComment("مقدار شناسنامه ");
            entity.Property(e => e.IDENTITYHEAD_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.IDENTITYSUBGRPS_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .IsFixedLength()
                .HasConversion(GuidToChar36Converter.Instance)
                .HasComment("آي دي زير گروه شناسنامه ");
            entity.Property(e => e.ISDELETED).HasColumnType("NUMBER(1)");
            entity.Property(e => e.UPDATEDDATE).HasPrecision(6);
            entity.Property(e => e.VAHEDCODE)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasComment("كد واحد ");
            entity.Property(e => e.YEAR)
                .HasMaxLength(4)
                .IsUnicode(false);

            entity.HasOne(d => d.IDENTITYHEAD).WithMany(p => p.TB_IDENTITYFIXITEMs)
                .HasForeignKey(d => d.IDENTITYHEAD_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FIXITEM_IDENTITYHEAD");

            entity.HasOne(d => d.IDENTITYSUBGRPS).WithMany(p => p.TB_IDENTITYFIXITEMs)
                .HasForeignKey(d => d.IDENTITYSUBGRPS_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FIXITEM_IDENTITYSUBGROUP");
        });

        modelBuilder.Entity<TB_IDENTITYGROUP>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK_IDENTYGROUPS");

            entity.ToTable("TB_IDENTITYGROUPS");

            entity.HasIndex(e => new { e.IDENTITYGROUPS_CODE, e.VAHEDCODE }, "UK_IDENTITYGROUPCODE").IsUnique();

            entity.Property(e => e.ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .IsFixedLength()
                .HasConversion(GuidToChar36Converter.Instance)
                .HasComment("آي دي گروه اصلي شناسنامه ");
            entity.Property(e => e.ADDUSERID)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasComment("كاربر ايجاد كننده");
            entity.Property(e => e.CHANGEUSERID)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasComment("كاربر تغيير دهنده");
            entity.Property(e => e.CREATEDDATE)
                .HasPrecision(6)
                .HasComment("تاريخ ايجاد");
            entity.Property(e => e.IDENTITYGROUPS_CODE)
                .HasMaxLength(3)
                .IsUnicode(false);
            entity.Property(e => e.IDENTITYGROUPS_DESC)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasComment("شرح گروه اصلي ");
            entity.Property(e => e.ISDELETED).HasColumnType("NUMBER(1)");
            entity.Property(e => e.TAFSILI_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.UPDATEDDATE)
                .HasPrecision(6)
                .HasComment("تاريخ تغيير");
            entity.Property(e => e.VAHEDCODE)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasComment("كد واحد ");

            entity.HasOne(d => d.TAFSILI).WithMany(p => p.TB_IDENTITYGROUPs)
                .HasForeignKey(d => d.TAFSILI_ID)
                .HasConstraintName("FK_IDENTITY_TAFSILI");
        });

        modelBuilder.Entity<TB_IDENTITYHEAD>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK_IDENTYHEAD");

            entity.ToTable("TB_IDENTITYHEAD");

            entity.HasIndex(e => new { e.IDENTITYGROUPS_ID, e.SERIAL, e.VAHEDCODE, e.YEAR }, "AK_AK_IDENTYHEAD_IDENTYHE").IsUnique();

            entity.HasIndex(e => e.IDENTITYGROUPS_ID, "REFERENCE_18_FK");

            entity.Property(e => e.ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ADDUSERID)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasComment("كاربر ايجاد كننده");
            entity.Property(e => e.CHANGEUSERID)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasComment("كاربر تغيير دهنده");
            entity.Property(e => e.CREATEDDATE)
                .HasPrecision(6)
                .HasComment("تاريخ ايجاد");
            entity.Property(e => e.IDENTITYGROUPS_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ISDELETED).HasColumnType("NUMBER(1)");
            entity.Property(e => e.SERIAL)
                .HasPrecision(6)
                .HasComment("سريال هر موضوع شناسنامه");
            entity.Property(e => e.UPDATEDDATE)
                .HasPrecision(6)
                .HasComment("تاريخ تغيير");
            entity.Property(e => e.VAHEDCODE)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.YEAR)
                .HasMaxLength(4)
                .IsUnicode(false);

            entity.HasOne(d => d.IDENTITYGROUPS).WithMany(p => p.TB_IDENTITYHEADs)
                .HasForeignKey(d => d.IDENTITYGROUPS_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_IDENTYHE_IDENTYGR");
        });

        modelBuilder.Entity<TB_IDENTITYSUBGRP>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK_IDENTYSUBGRPS");

            entity.ToTable("TB_IDENTITYSUBGRPS");

            entity.HasIndex(e => new { e.VAHEDCODE, e.YEAR, e.IDENTYSUBGROUPS_CODE }, "AK_AK_IDENTYSUBGRPS_IDENTYSU").IsUnique();

            entity.HasIndex(e => e.IDENTYGROUPS_ID, "REFERENCE_17_FK");

            entity.Property(e => e.ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .IsFixedLength()
                .HasConversion(GuidToChar36Converter.Instance)
                .HasComment("آي دي زير گروه شناسنامه ");
            entity.Property(e => e.ADDUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CHANGEUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CREATEDDATE).HasPrecision(6);
            entity.Property(e => e.FIXED)
                .HasComment("ثابت يا متغير بودن ")
                .HasColumnType("NUMBER(1)");
            entity.Property(e => e.IDENTYGROUPS_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .IsFixedLength()
                .HasConversion(GuidToChar36Converter.Instance)
                .HasComment("آي دي گروه اصلي شناسنامه ");
            entity.Property(e => e.IDENTYSUBGROUPS_CODE)
                .HasMaxLength(2)
                .IsUnicode(false);
            entity.Property(e => e.ISDELETED).HasColumnType("NUMBER(1)");
            entity.Property(e => e.SUBGRPS_DESC)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasComment("شرح");
            entity.Property(e => e.SUBGRPS_LEN)
                .HasPrecision(2)
                .HasComment("طول ");
            entity.Property(e => e.SUBGRPS_TYPE)
                .HasComment("نوع : حروف, اعداد, يا هردو ")
                .HasColumnType("NUMBER(1)");
            entity.Property(e => e.SUMFLAG)
                .HasComment("جمع پذير يا ناپذير بودن ")
                .HasColumnType("NUMBER(1)");
            entity.Property(e => e.UPDATEDDATE).HasPrecision(6);
            entity.Property(e => e.VAHEDCODE)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasComment("كد واحد ");
            entity.Property(e => e.YEAR)
                .HasMaxLength(4)
                .IsUnicode(false);

            entity.HasOne(d => d.IDENTYGROUPS).WithMany(p => p.TB_IDENTITYSUBGRPs)
                .HasForeignKey(d => d.IDENTYGROUPS_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_IDENTYSU_IDENTYGR");
        });

        modelBuilder.Entity<TB_LEVEL_TAFSIL>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK_LEVEL");

            entity.ToTable("TB_LEVEL_TAFSIL");

            entity.Property(e => e.ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ADDUSERID)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasComment("كاربر ايجاد كننده");
            entity.Property(e => e.CHANGEUSERID)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasComment(" کاربر تغيير دهنده");
            entity.Property(e => e.CREATEDDATE)
                .HasPrecision(6)
                .HasComment("تاريخ ايجاد");
            entity.Property(e => e.ISDELETED).HasColumnType("NUMBER(1)");
            entity.Property(e => e.LEVEL_CODE)
                .HasMaxLength(2)
                .IsUnicode(false);
            entity.Property(e => e.LEVEL_NAME)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UPDATEDDATE)
                .HasPrecision(6)
                .HasComment("تاريخ تغيير");
        });

        modelBuilder.Entity<TB_PAYRECIVDETAIL>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK_PAYRECIVDETAIL");

            entity.ToTable("TB_PAYRECIVDETAIL");

            entity.Property(e => e.ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ACCOUNTCODE_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ADDUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.ARTICLEDESCRIPTION)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.CHANGEUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CHECK_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.CREATEDDATE).HasPrecision(6);
            entity.Property(e => e.CREDITOR).HasColumnType("NUMBER(25)");
            entity.Property(e => e.DEBTOR).HasColumnType("NUMBER(25)");
            entity.Property(e => e.ISDELETED).HasColumnType("NUMBER(1)");
            entity.Property(e => e.PAYRECIVHEAD_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.RADIF).HasPrecision(10);
            entity.Property(e => e.RECEIPT_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .IsFixedLength()
                .HasConversion(GuidToChar36Converter.Instance)
                .HasComment("شماره فيش");
            entity.Property(e => e.UPDATEDDATE).HasPrecision(6);
            entity.Property(e => e.VAHEDCODE)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.YEAR)
                .HasMaxLength(4)
                .IsUnicode(false)
                .IsFixedLength();

            entity.HasOne(d => d.PAYRECIVHEAD).WithMany(p => p.TB_PAYRECIVDETAILs)
                .HasForeignKey(d => d.PAYRECIVHEAD_ID)
                .HasConstraintName("FK_PAYRECIVHEAD_DETAIL");

            entity.HasOne(d => d.RECEIPT).WithMany(p => p.TB_PAYRECIVDETAILs)
                .HasForeignKey(d => d.RECEIPT_ID)
                .HasConstraintName("FK_PAYRECIVEDETAIL_RECIPT");
        });

        modelBuilder.Entity<TB_PAYRECIVDETAIL_LINK_TAFSILI>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK_PAYRECIVDETAILLINKTAFSILI");

            entity.ToTable("TB_PAYRECIVDETAIL_LINK_TAFSILI");

            entity.Property(e => e.ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ADDUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CHANGEUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CREATEDDATE).HasPrecision(6);
            entity.Property(e => e.ISDELETED).HasColumnType("NUMBER(1)");
            entity.Property(e => e.LEVEL_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.PAYRECIVDETAIL_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.TAFSILI_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.UPDATEDDATE).HasPrecision(6);
            entity.Property(e => e.VAHEDCODE)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.YEAR)
                .HasMaxLength(4)
                .IsUnicode(false);

            entity.HasOne(d => d.LEVEL).WithMany(p => p.TB_PAYRECIVDETAIL_LINK_TAFSILIs)
                .HasForeignKey(d => d.LEVEL_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PAYRECIVDETAILLINKTAF_LEV");

            entity.HasOne(d => d.PAYRECIVDETAIL).WithMany(p => p.TB_PAYRECIVDETAIL_LINK_TAFSILIs)
                .HasForeignKey(d => d.PAYRECIVDETAIL_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PRECIVDETAILLINKTAF_PAYREC");

            entity.HasOne(d => d.TAFSILI).WithMany(p => p.TB_PAYRECIVDETAIL_LINK_TAFSILIs)
                .HasForeignKey(d => d.TAFSILI_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PAYRECIVDETAILLINKTAF_TAF");
        });

        modelBuilder.Entity<TB_PAYRECIVHEAD>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK_PAYRECIVHEAD");

            entity.ToTable("TB_PAYRECIVHEAD");

            entity.Property(e => e.ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ADDUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CHANGEUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CREATEDDATE).HasPrecision(6);
            entity.Property(e => e.ISDELETED).HasColumnType("NUMBER(1)");
            entity.Property(e => e.PAYRECIVCODE)
                .HasMaxLength(5)
                .IsUnicode(false);
            entity.Property(e => e.PAYRECIVDATE)
                .HasMaxLength(8)
                .IsUnicode(false);
            entity.Property(e => e.PAYRECIVDESCRIPTION)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasDefaultValueSql("'-' ");
            entity.Property(e => e.PAYRECIVTYPE).HasColumnType("NUMBER(1)");
            entity.Property(e => e.UPDATEDDATE).HasPrecision(6);
            entity.Property(e => e.VAHEDCODE)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.VOUCHERSHEAD_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.YEAR)
                .HasMaxLength(4)
                .IsUnicode(false)
                .IsFixedLength();

            entity.HasOne(d => d.VOUCHERSHEAD).WithMany(p => p.TB_PAYRECIVHEADs)
                .HasForeignKey(d => d.VOUCHERSHEAD_ID)
                .HasConstraintName("FK_PAYRECIV_VOCHERHEAD");
        });

        modelBuilder.Entity<TB_PERSON_ACTION>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK_PERSONACTION");

            entity.ToTable("TB_PERSON_ACTION");

            entity.HasIndex(e => new { e.USERID, e.FROMDATE, e.TODATE }, "UK_PERSON_ACTION").IsUnique();

            entity.Property(e => e.ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ADDUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CHANGEUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CREATEDDATE).HasPrecision(6);
            entity.Property(e => e.FROMDATE)
                .HasMaxLength(8)
                .IsUnicode(false);
            entity.Property(e => e.ISDELETED).HasColumnType("NUMBER(1)");
            entity.Property(e => e.OPERATORROLE).HasColumnType("NUMBER(1)");
            entity.Property(e => e.STATUS).HasColumnType("NUMBER(1)");
            entity.Property(e => e.TODATE)
                .HasMaxLength(8)
                .IsUnicode(false);
            entity.Property(e => e.UPDATEDDATE).HasPrecision(6);
            entity.Property(e => e.USERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.USERNAME)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.VAHEDCODE)
                .HasMaxLength(4)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TB_PREDESCRIB>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK_PREDESCRIBE");

            entity.ToTable("TB_PREDESCRIBS");

            entity.Property(e => e.ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ACCOUNTID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ADDUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.DESCRIP)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.FLAGVOUCHER)
                .HasComment("head=0 Detail=1")
                .HasColumnType("NUMBER(1)");
            entity.Property(e => e.VAHEDCODE)
                .HasMaxLength(4)
                .IsUnicode(false);

            entity.HasOne(d => d.ACCOUNT).WithMany(p => p.TB_PREDESCRIBs)
                .HasForeignKey(d => d.ACCOUNTID)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ACCOUNT");
        });

        modelBuilder.Entity<TB_PROVINCE>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK_PROVINCE");

            entity.ToTable("TB_PROVINCE");

            entity.HasIndex(e => e.PROVINCECODE, "UK_PROVINCE").IsUnique();

            entity.Property(e => e.ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasDefaultValueSql("sys_guid() ")
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.PROVINCECODE)
                .HasMaxLength(5)
                .IsUnicode(false);
            entity.Property(e => e.PROVINCENAME)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.STATUS).HasColumnType("NUMBER(1)");
        });

        modelBuilder.Entity<TB_RABET>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK_RABET");

            entity.ToTable("TB_RABET");

            entity.HasIndex(e => new { e.ACCOUNTCODE_ID, e.RABETTYPE_ID }, "UK_RABET").IsUnique();

            entity.Property(e => e.ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasDefaultValueSql("sys_guid() ")
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ACCOUNTCODE_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ADDUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CHANGEUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CREATEDDATE).HasPrecision(6);
            entity.Property(e => e.ISDELETED).HasColumnType("NUMBER(1)");
            entity.Property(e => e.RABETTYPE_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.UPDATEDDATE).HasPrecision(6);

            entity.HasOne(d => d.ACCOUNTCODE).WithMany(p => p.TB_RABETs)
                .HasForeignKey(d => d.ACCOUNTCODE_ID)
                .HasConstraintName("FK_RABET_ACCOUNTCODE");

            entity.HasOne(d => d.RABETTYPE).WithMany(p => p.TB_RABETs)
                .HasForeignKey(d => d.RABETTYPE_ID)
                .HasConstraintName("FK_RABET_TYPE");
        });

        modelBuilder.Entity<TB_RABET_CLOSING>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK_RABET_CLOSING");

            entity.ToTable("TB_RABET_CLOSING");

            entity.HasIndex(e => new { e.ACCOUNTCODE_ID, e.ACCOUNTCODE_RABET_ID, e.YEAR, e.VAHEDTYPE_ID }, "UK_RABET_CLOSING").IsUnique();

            entity.Property(e => e.ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasDefaultValueSql("sys_guid() ")
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ACCOUNTCODE_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ACCOUNTCODE_RABET_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ADDUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CHANGEUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CREATEDDATE).HasPrecision(6);
            entity.Property(e => e.ISDELETED).HasColumnType("NUMBER(1)");
            entity.Property(e => e.TITLE)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.TYPEACCOUNTCODE).HasColumnType("NUMBER(1)");
            entity.Property(e => e.UPDATEDDATE).HasPrecision(6);
            entity.Property(e => e.VAHEDTYPE_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.YEAR)
                .HasMaxLength(4)
                .IsUnicode(false)
                .IsFixedLength();

            entity.HasOne(d => d.ACCOUNTCODE).WithMany(p => p.TB_RABET_CLOSINGACCOUNTCODEs)
                .HasForeignKey(d => d.ACCOUNTCODE_ID)
                .HasConstraintName("FK_RABET_CLOSING_ACCOUNTCODE");

            entity.HasOne(d => d.ACCOUNTCODE_RABET).WithMany(p => p.TB_RABET_CLOSINGACCOUNTCODE_RABETs)
                .HasForeignKey(d => d.ACCOUNTCODE_RABET_ID)
                .HasConstraintName("FK_ACCOUNTCODE_RABET");

            entity.HasOne(d => d.VAHEDTYPE).WithMany(p => p.TB_RABET_CLOSINGs)
                .HasForeignKey(d => d.VAHEDTYPE_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RABET_CLOSING_VAHEDTYPE");
        });

        modelBuilder.Entity<TB_RABET_TYPE>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK_RABET_TYPY");

            entity.ToTable("TB_RABET_TYPE");

            entity.Property(e => e.ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.RABETCODE)
                .HasMaxLength(2)
                .IsUnicode(false);
            entity.Property(e => e.TITLE)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TB_RECEIP>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK_RECEIP");

            entity.ToTable("TB_RECEIP");

            entity.Property(e => e.ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ADDUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CHANGEUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CREATEDDATE).HasPrecision(6);
            entity.Property(e => e.DATE_RSID)
                .HasMaxLength(8)
                .IsUnicode(false);
            entity.Property(e => e.ISDELETED).HasColumnType("NUMBER(1)");
            entity.Property(e => e.RECEIPT_DATE)
                .HasMaxLength(8)
                .IsUnicode(false);
            entity.Property(e => e.RECEIPT_KIND).HasColumnType("NUMBER(1)");
            entity.Property(e => e.RECEIPT_NO)
                .HasMaxLength(8)
                .IsUnicode(false);
            entity.Property(e => e.UPDATEDDATE).HasPrecision(6);
            entity.Property(e => e.VAHEDCODE)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.YEAR)
                .HasMaxLength(4)
                .IsUnicode(false)
                .IsFixedLength();
        });

        modelBuilder.Entity<TB_REVOLVINGFUND_LINK_TAFSILI>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK_REVOLVINGLINKTAF");

            entity.ToTable("TB_REVOLVINGFUND_LINK_TAFSILI");

            entity.Property(e => e.ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ADDUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CHANGEUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CREATEDDATE).HasPrecision(6);
            entity.Property(e => e.ISDELETED).HasColumnType("NUMBER(1)");
            entity.Property(e => e.LEVEL_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.REVOLVINGFUND_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.TAFSILI_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.UPDATEDDATE).HasPrecision(6);
            entity.Property(e => e.VAHEDCODE)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.YEAR)
                .HasMaxLength(4)
                .IsUnicode(false);

            entity.HasOne(d => d.LEVEL).WithMany(p => p.TB_REVOLVINGFUND_LINK_TAFSILIs)
                .HasForeignKey(d => d.LEVEL_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_REVOLVINGLINKTAF_LEV");

            entity.HasOne(d => d.REVOLVINGFUND).WithMany(p => p.TB_REVOLVINGFUND_LINK_TAFSILIs)
                .HasForeignKey(d => d.REVOLVINGFUND_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_REVOLVINGLINKTAF_REV");

            entity.HasOne(d => d.TAFSILI).WithMany(p => p.TB_REVOLVINGFUND_LINK_TAFSILIs)
                .HasForeignKey(d => d.TAFSILI_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_REVOLVINGLINKTAF_TAF");
        });

        modelBuilder.Entity<TB_REVOLVING_FUND>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK_REVOLVING");

            entity.ToTable("TB_REVOLVING_FUND");

            entity.HasIndex(e => new { e.CODE, e.VAHEDCODE, e.YEAR }, "UK_REVOLVING_CODE").IsUnique();

            entity.Property(e => e.ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ACCOUNTCODE_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ADDUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CHANGEUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CODE)
                .HasMaxLength(2)
                .IsUnicode(false);
            entity.Property(e => e.CREATEDDATE).HasPrecision(6);
            entity.Property(e => e.DEFAULTAMOUNT).HasColumnType("NUMBER(25)");
            entity.Property(e => e.DESCRIPTION)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.ISDELETED).HasColumnType("NUMBER(1)");
            entity.Property(e => e.NAME)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.UPDATEDDATE).HasPrecision(6);
            entity.Property(e => e.VAHEDCODE)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.YEAR)
                .HasMaxLength(4)
                .IsUnicode(false);

            entity.HasOne(d => d.ACCOUNTCODE).WithMany(p => p.TB_REVOLVING_FUNDs)
                .HasForeignKey(d => d.ACCOUNTCODE_ID)
                .HasConstraintName("FK_ACCOUNTCODE_REVOLVING");
        });

        modelBuilder.Entity<TB_SYSTYPE>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK_ID");

            entity.ToTable("TB_SYSTYPE");

            entity.Property(e => e.ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.SYS_COD)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.SYS_NAME)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TB_TAFSILI>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK_TAFSILI");

            entity.ToTable("TB_TAFSILI");

            entity.HasIndex(e => e.VAHEDCODE, "IDX_VAHEDCODE");

            entity.HasIndex(e => e.TAFSILI_CODE, "UK_TASILI").IsUnique();

            entity.Property(e => e.ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ADDUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CHANGEUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CREATEDDATE).HasPrecision(6);
            entity.Property(e => e.ISACTIVE)
                .HasDefaultValueSql("1")
                .HasColumnType("NUMBER(1)");
            entity.Property(e => e.ISDELETED).HasColumnType("NUMBER(1)");
            entity.Property(e => e.OWNER)
                .HasDefaultValueSql("1")
                .HasComment("2=setad 1=vahed")
                .HasColumnType("NUMBER(1)");
            entity.Property(e => e.PERSONTYPE)
                .HasDefaultValueSql("0 ")
                .HasColumnType("NUMBER(1)");
            entity.Property(e => e.TAFSILI_CODE)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.TAFSILI_NAME)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.TAFSIL_DESC)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.UPDATEDDATE).HasPrecision(6);
            entity.Property(e => e.VAHEDCODE)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.VAHEDTYPE).HasColumnType("NUMBER(1)");

            entity.HasOne(d => d.VAHEDCODENavigation).WithMany(p => p.TB_TAFSILIs)
                .HasPrincipalKey(p => p.VAHEDCODE)
                .HasForeignKey(d => d.VAHEDCODE)
                .HasConstraintName("FK_VAHEDCODE");
        });

        modelBuilder.Entity<TB_TAFSILI_UNITACCESS>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK_TAFSILIUNITACCESS");

            entity.ToTable("TB_TAFSILI_UNITACCESS");

            entity.Property(e => e.ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ADDUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CHANGEUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CREATEDDATE).HasPrecision(6);
            entity.Property(e => e.ISDELETED).HasColumnType("NUMBER(1)");
            entity.Property(e => e.TAFSILI_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.UPDATEDDATE).HasPrecision(6);
            entity.Property(e => e.VAHEDCODE)
                .HasMaxLength(4)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TB_TAFSIL_GROUP>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK_TAFSILGROUP");

            entity.ToTable("TB_TAFSIL_GROUP");

            entity.HasIndex(e => new { e.TAFSILGROUP_CODE, e.ISDELETED }, "UK_TBTAFSILGROUP").IsUnique();

            entity.Property(e => e.ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ADDUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CHANGEUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CREATEDDATE).HasPrecision(6);
            entity.Property(e => e.ISDELETED).HasColumnType("NUMBER(1)");
            entity.Property(e => e.PERSONTYPE).HasColumnType("NUMBER(1)");
            entity.Property(e => e.TAFSILGROUP_CODE)
                .HasMaxLength(3)
                .IsUnicode(false);
            entity.Property(e => e.TAFSILGROUP_NAME)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.UPDATEDDATE).HasPrecision(6);
        });

        modelBuilder.Entity<TB_TAFSIL_LINK_TAFSILGROUP>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK_TAFSILLINKTAFSILGROUP");

            entity.ToTable("TB_TAFSIL_LINK_TAFSILGROUP");

            entity.Property(e => e.ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ADDUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CHANGEUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CREATEDDATE).HasPrecision(6);
            entity.Property(e => e.ISDELETED).HasColumnType("NUMBER(1)");
            entity.Property(e => e.TAFSILGROUP_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.TAFSIL_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.UPDATEDDATE).HasPrecision(6);
            entity.Property(e => e.VAHEDCODE)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.VAHEDTYPE).HasColumnType("NUMBER(1)");

            entity.HasOne(d => d.TAFSILGROUP).WithMany(p => p.TB_TAFSIL_LINK_TAFSILGROUPs)
                .HasForeignKey(d => d.TAFSILGROUP_ID)
                .HasConstraintName("FK_TAFSILGROUP");
        });

        modelBuilder.Entity<TB_TMP_VOUCHERHEAD>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK_TMPVOCHERHEAD");

            entity.ToTable("TB_TMP_VOUCHERHEAD");

            entity.Property(e => e.ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ADDUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CHANGEUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CREATEDDATE).HasPrecision(6);
            entity.Property(e => e.DATE_DOC)
                .HasMaxLength(8)
                .IsUnicode(false);
            entity.Property(e => e.HEAD_DESC)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.ISDELETED).HasColumnType("NUMBER(1)");
            entity.Property(e => e.SOURCEID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.SYS_TYPE)
                .HasMaxLength(1)
                .IsUnicode(false);
            entity.Property(e => e.UPDATEDDATE).HasPrecision(6);
            entity.Property(e => e.VAHEDCODE)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.VOUCHERSHEAD_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.YEAR)
                .HasMaxLength(4)
                .IsUnicode(false)
                .IsFixedLength();

            entity.HasOne(d => d.VOUCHERSHEAD).WithMany(p => p.TB_TMP_VOUCHERHEADs)
                .HasForeignKey(d => d.VOUCHERSHEAD_ID)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_TMP_VOCHERHEAD");
        });

        modelBuilder.Entity<TB_TMP_VOUCHERSDETAIL>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK_TMPVOUCHERDETAIL");

            entity.ToTable("TB_TMP_VOUCHERSDETAIL");

            entity.Property(e => e.ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ADDUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CHANGEUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CHECK_DATE)
                .HasMaxLength(8)
                .IsUnicode(false);
            entity.Property(e => e.CREATEDDATE).HasPrecision(6);
            entity.Property(e => e.CREDITOR)
                .HasDefaultValueSql("0 ")
                .HasColumnType("NUMBER(25)");
            entity.Property(e => e.DEBTOR)
                .HasDefaultValueSql("0 ")
                .HasColumnType("NUMBER(25)");
            entity.Property(e => e.DETAIL_DESC)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.ISDELETED).HasColumnType("NUMBER(1)");
            entity.Property(e => e.MOINCODE)
                .HasMaxLength(6)
                .IsUnicode(false);
            entity.Property(e => e.RADIF).HasPrecision(10);
            entity.Property(e => e.TAFSILI_CODE1)
                .HasMaxLength(11)
                .IsUnicode(false);
            entity.Property(e => e.TAFSILI_CODE2)
                .HasMaxLength(11)
                .IsUnicode(false);
            entity.Property(e => e.TAFSILI_CODE3)
                .HasMaxLength(11)
                .IsUnicode(false);
            entity.Property(e => e.TAFSILI_CODE4)
                .HasMaxLength(11)
                .IsUnicode(false);
            entity.Property(e => e.TAFSILI_CODE5)
                .HasMaxLength(11)
                .IsUnicode(false);
            entity.Property(e => e.TAFSILI_CODE6)
                .HasMaxLength(11)
                .IsUnicode(false);
            entity.Property(e => e.TAFSILI_CODE7)
                .HasMaxLength(11)
                .IsUnicode(false);
            entity.Property(e => e.TMPVOUCHERHEAD_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.UPDATEDDATE).HasPrecision(6);
            entity.Property(e => e.VAHEDCODE)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.VALUE)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.YEAR)
                .HasMaxLength(4)
                .IsUnicode(false)
                .IsFixedLength();

            entity.HasOne(d => d.TMPVOUCHERHEAD).WithMany(p => p.TB_TMP_VOUCHERSDETAILs)
                .HasForeignKey(d => d.TMPVOUCHERHEAD_ID)
                .HasConstraintName("FK_TMPVOUCHERHEAD");
        });

        modelBuilder.Entity<TB_VAHED_INFO>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK_VAHEDINFO");

            entity.ToTable("TB_VAHED_INFO");

            entity.HasIndex(e => e.VAHEDCODE, "UK_VAHEDINFO").IsUnique();

            entity.Property(e => e.ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasDefaultValueSql("sys_guid() ")
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.CITY_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.PARENT_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.VAHEDCODE)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.VAHEDNAME)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.VAHEDTYPE_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();

            entity.HasOne(d => d.VAHEDTYPE).WithMany(p => p.TB_VAHED_INFOs)
                .HasForeignKey(d => d.VAHEDTYPE_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_VAHEDINFO_TYPE");
        });

        modelBuilder.Entity<TB_VAHED_TYPE>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK_VAHEDTYP");

            entity.ToTable("TB_VAHED_TYPE");

            entity.HasIndex(e => e.TYPECODE, "UK_VAHEDTYPE").IsUnique();

            entity.Property(e => e.ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasDefaultValueSql("sys_guid() ")
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.PARENTTYPECODE)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.TYPECODE)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.TYPENAME)
                .HasMaxLength(200)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TB_VOUCHERDETAIL_LINK_TAFSILI>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK_VOUCHERDETAILLINKTAF");

            entity.ToTable("TB_VOUCHERDETAIL_LINK_TAFSILI");

            entity.HasIndex(e => e.TAFSILI_ID, "IDX_LINK_TAFSILI_TAFSILI_ID");

            entity.HasIndex(e => e.VOUCHERSDETAIL_ID, "IDX_LINK_TAF_VOUCHERSDETAIL_ID");

            entity.Property(e => e.ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ADDUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CHANGEUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CREATEDDATE).HasPrecision(6);
            entity.Property(e => e.ISDELETED).HasColumnType("NUMBER(1)");
            entity.Property(e => e.LEVEL_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.TAFSILI_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.UPDATEDDATE).HasPrecision(6);
            entity.Property(e => e.VAHEDCODE)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.VOUCHERSDETAIL_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.YEAR)
                .HasMaxLength(4)
                .IsUnicode(false);

            entity.HasOne(d => d.VOUCHERSDETAIL).WithMany(p => p.TB_VOUCHERDETAIL_LINK_TAFSILIs)
                .HasForeignKey(d => d.VOUCHERSDETAIL_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_VOUCHERDETAILLINKTAF_VOUCH");
        });

        modelBuilder.Entity<TB_VOUCHERSDETAIL>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK_VOUCHERDETAIL");

            entity.ToTable("TB_VOUCHERSDETAIL");

            entity.HasIndex(e => e.ACCOUNT_ID, "IDX_VDETAIL_ACC_VHEAD_ISDEL");

            entity.HasIndex(e => e.VOUCHERSHEAD_ID, "IDX_VOUCHERSDETAIL_HEADID");

            entity.HasIndex(e => new { e.YEAR, e.VAHEDCODE }, "IDX_YEAR_VAHED");

            entity.Property(e => e.ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ACCOUNT_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ADDUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CHANGEUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CHECK_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.CREATEDDATE).HasPrecision(6);
            entity.Property(e => e.CREDITOR).HasColumnType("NUMBER(25)");
            entity.Property(e => e.DEBTOR).HasColumnType("NUMBER(25)");
            entity.Property(e => e.DESCRIPTION)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.ETEBAR_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ISDELETED).HasColumnType("NUMBER(1)");
            entity.Property(e => e.LOWLEVELCODE_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.RADIF).HasPrecision(10);
            entity.Property(e => e.RECEIP_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.UPDATEDDATE).HasPrecision(6);
            entity.Property(e => e.VAHEDCODE)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.VOUCHERSHEAD_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.YEAR)
                .HasMaxLength(4)
                .IsUnicode(false)
                .IsFixedLength();

            entity.HasOne(d => d.ACCOUNT).WithMany(p => p.TB_VOUCHERSDETAILs)
                .HasForeignKey(d => d.ACCOUNT_ID)
                .HasConstraintName("FK_VOUCHERDETAIL_ACCOUNCODE");

            entity.HasOne(d => d.RECEIP).WithMany(p => p.TB_VOUCHERSDETAILs)
                .HasForeignKey(d => d.RECEIP_ID)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_VOUCHERDETAIL_RECEIP");

            entity.HasOne(d => d.VOUCHERSHEAD).WithMany(p => p.TB_VOUCHERSDETAILs)
                .HasForeignKey(d => d.VOUCHERSHEAD_ID)
                .HasConstraintName("FK_VOUCHERHEAD");
        });

        modelBuilder.Entity<TB_VOUCHERSHEAD>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK_DOCSHEAD");

            entity.ToTable("TB_VOUCHERSHEAD");

            entity.HasIndex(e => e.SYSTEM_TYPE, "IDX_SYS_VOUCHER");

            entity.HasIndex(e => new { e.DOC_NUM, e.YEAR, e.VAHEDCODE }, "UK_VOUCHERHEAD_NUMBER").IsUnique();

            entity.Property(e => e.ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .IsFixedLength()
                .HasConversion(GuidToChar36Converter.Instance)
                .HasComment("آي دي سند");
            entity.Property(e => e.ADDUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.APENDIX)
                .HasMaxLength(800)
                .IsUnicode(false)
                .HasComment("پيوست");
            entity.Property(e => e.ATF_NUM)
                .HasMaxLength(15)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.ATTACHFILE).HasColumnType("BLOB");
            entity.Property(e => e.ATTACHFILE_NAME)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.CHANGEUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CREATEDDATE).HasPrecision(6);
            entity.Property(e => e.DATE_DOC)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasComment("تاريخ سند");
            entity.Property(e => e.DOCLIFE)
                .HasDefaultValueSql("0 ")
                .HasComment("وضعيت سند")
                .HasColumnType("NUMBER(1)");
            entity.Property(e => e.DOC_NUM)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasComment("شماره واقعي سند");
            entity.Property(e => e.FLAG_STATE)
                .HasComment("سند آيا اختتاميه ميباشد")
                .HasColumnType("NUMBER");
            entity.Property(e => e.GLOBALNUMBER)
                .HasMaxLength(11)
                .IsUnicode(false)
                .ValueGeneratedOnAdd();
            entity.Property(e => e.HEAD_DESC)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasDefaultValueSql("'-' ")
                .HasComment("شرح سند");
            entity.Property(e => e.ISAUTOMATIC)
                .HasComment("0دستي و 1 مکانيزه")
                .HasColumnType("NUMBER(1)");
            entity.Property(e => e.ISDELETED).HasColumnType("NUMBER(1)");
            entity.Property(e => e.PARENTHEAD_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.SNDVAHEDCODE)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasComment("واحد گيرنده");
            entity.Property(e => e.SYSTEM_TYPE)
                .HasMaxLength(36)
                .IsUnicode(false)
                .IsFixedLength()
                .HasConversion(GuidToChar36Converter.Instance)
                .HasComment("نوع سيستم4=اموال و3=اعلاميه مکانيزه");
            entity.Property(e => e.UPDATEDDATE).HasPrecision(6);
            entity.Property(e => e.VAHEDCODE)
                .HasMaxLength(4)
                .IsUnicode(false)
                .HasComment("كد واحد ");
            entity.Property(e => e.YEAR)
                .HasMaxLength(4)
                .IsUnicode(false)
                .ValueGeneratedOnAdd()
                .IsFixedLength();

            entity.HasOne(d => d.SYSTEM_TYPENavigation).WithMany(p => p.TB_VOUCHERSHEADs)
                .HasForeignKey(d => d.SYSTEM_TYPE)
                .HasConstraintName("FK_TBSYSTYPE");
        });

        modelBuilder.Entity<TB_WHITEANDBLACKLIST>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK_TYPE_LINK_ACCOUNTS");

            entity.ToTable("TB_WHITEANDBLACKLIST");

            entity.HasIndex(e => new { e.ACCOUNTCODE_ID, e.VAHEDTYPE_ID, e.FROMAUTHORIZEDDATE, e.TOAUTHORIZEDDATE }, "UK_WHITEANDBLACKLIST").IsUnique();

            entity.Property(e => e.ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasDefaultValueSql("sys_guid() ")
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ACCOUNTCODE_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ADDUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CHANGEUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CREATEDDATE).HasPrecision(6);
            entity.Property(e => e.FROMAUTHORIZEDDATE)
                .HasMaxLength(8)
                .IsUnicode(false);
            entity.Property(e => e.FROMLIMITATIONDATE)
                .HasMaxLength(8)
                .IsUnicode(false);
            entity.Property(e => e.ISDELETED).HasColumnType("NUMBER(1)");
            entity.Property(e => e.STATE).HasColumnType("NUMBER(1)");
            entity.Property(e => e.TOAUTHORIZEDDATE)
                .HasMaxLength(8)
                .IsUnicode(false);
            entity.Property(e => e.TOLIMITATIONDATE)
                .HasMaxLength(8)
                .IsUnicode(false);
            entity.Property(e => e.UPDATEDDATE).HasPrecision(6);
            entity.Property(e => e.VAHEDTYPE_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();

            entity.HasOne(d => d.ACCOUNTCODE).WithMany(p => p.TB_WHITEANDBLACKLISTs)
                .HasForeignKey(d => d.ACCOUNTCODE_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ACCOUNTCODE_LINK_WHITELISTS");

            entity.HasOne(d => d.VAHEDTYPE).WithMany(p => p.TB_WHITEANDBLACKLISTs)
                .HasForeignKey(d => d.VAHEDTYPE_ID)
                .HasConstraintName("FK_VAHEDTYPE_WHITEANDBLACKLIST");
        });

        modelBuilder.Entity<TB_WHITELIST>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK_TYPE_LINK_ACCOUNT");

            entity.ToTable("TB_WHITELIST");

            entity.Property(e => e.ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasDefaultValueSql("sys_guid() ")
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ACCOUNTCODE_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ADDUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CHANGEUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CREATEDDATE).HasPrecision(6);
            entity.Property(e => e.FROMAUTHORIZEDDATE)
                .HasMaxLength(8)
                .IsUnicode(false);
            entity.Property(e => e.FROMLIMITATIONDATE)
                .HasMaxLength(8)
                .IsUnicode(false);
            entity.Property(e => e.ISDELETED).HasColumnType("NUMBER(1)");
            entity.Property(e => e.TOAUTHORIZEDDATE)
                .HasMaxLength(8)
                .IsUnicode(false);
            entity.Property(e => e.TOLIMITATIONDATE)
                .HasMaxLength(8)
                .IsUnicode(false);
            entity.Property(e => e.UPDATEDDATE).HasPrecision(6);
            entity.Property(e => e.VAHEDINFO_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.VAHEDTYPE_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();

            entity.HasOne(d => d.ACCOUNTCODE).WithMany(p => p.TB_WHITELISTs)
                .HasForeignKey(d => d.ACCOUNTCODE_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ACCOUNTCODE_LINK_WHITELIST");

            entity.HasOne(d => d.VAHEDINFO).WithMany(p => p.TB_WHITELISTs)
                .HasForeignKey(d => d.VAHEDINFO_ID)
                .HasConstraintName("FK_VAHEDINFO_LINK_WHITELIST");

            entity.HasOne(d => d.VAHEDTYPE).WithMany(p => p.TB_WHITELISTs)
                .HasForeignKey(d => d.VAHEDTYPE_ID)
                .HasConstraintName("FK_VAHEDTYPE_LINK_WHITELIST");
        });

        modelBuilder.Entity<TB_WORKSHOP>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK_WORKSHOP");

            entity.ToTable("TB_WORKSHOP");

            entity.HasIndex(e => new { e.WORKSHOPCODE, e.ISACTIVE, e.VAHEDCODE }, "UK_WORKSHOP").IsUnique();

            entity.Property(e => e.ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ACCOUNTCODE_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ADDUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.BRANCH_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.CHANGEUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CHECKFILE).HasColumnType("BLOB");
            entity.Property(e => e.CREATEDDATE).HasPrecision(6);
            entity.Property(e => e.ISACTIVE).HasColumnType("NUMBER(1)");
            entity.Property(e => e.ISDELETED).HasColumnType("NUMBER(1)");
            entity.Property(e => e.UPDATEDDATE).HasPrecision(6);
            entity.Property(e => e.VAHEDCODE)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.WORKSHOPCODE)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.WORKSHOPNAME)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.ACCOUNTCODE).WithMany(p => p.TB_WORKSHOPs)
                .HasForeignKey(d => d.ACCOUNTCODE_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WORK_ACCOUNTCODE");

            entity.HasOne(d => d.BRANCH).WithMany(p => p.TB_WORKSHOPs)
                .HasForeignKey(d => d.BRANCH_ID)
                .HasConstraintName("FK_WORK_VAHEDINFO");
        });

        modelBuilder.Entity<TB_WORKSHOP_LINK_TAFSILI>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("TB_WORKSHOP_LINK_TAFSILI");

            entity.Property(e => e.ADDUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CHANGEUSERID)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.CREATEDDATE).HasPrecision(6);
            entity.Property(e => e.ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.ISDELETED).HasColumnType("NUMBER(1)");
            entity.Property(e => e.LEVEL_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.TAFSILI_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
            entity.Property(e => e.UPDATEDDATE).HasPrecision(6);
            entity.Property(e => e.VAHEDCODE)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.WORKSHOP_ID)
                .HasMaxLength(36)
                .IsUnicode(false)
                .HasConversion(GuidToChar36Converter.Instance)
                .IsFixedLength();
        });

        modelBuilder.Entity<TB_YEAR>(entity =>
        {
            entity.HasKey(e => e.WORKING_YEAR).HasName("PK_TBYEAR");

            entity.ToTable("TB_YEAR");

            entity.Property(e => e.WORKING_YEAR).HasPrecision(4);
            entity.Property(e => e.ISCURRENT)
                .HasDefaultValueSql("0 ")
                .HasColumnType("NUMBER(1)");
            entity.Property(e => e.LAST_NUMBER).HasColumnType("NUMBER");
        });
        modelBuilder.HasSequence("VOUCHERHEAD_SEQ");

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
