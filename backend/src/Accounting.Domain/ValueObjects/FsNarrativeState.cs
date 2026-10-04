namespace Accounting.Domain.ValueObjects;

/// <summary>وضعیت یک یادداشت توضیحی متنی (ح-۶، سند منبع §۹) — <c>TB_FS_NARRATIVE.STATE</c>.</summary>
public enum FsNarrativeState
{
    Draft = 1,
    InReview = 2,
    Approved = 3,
    NeedsRevision = 4,
}
