using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Accounting.Domain.OperationTemplates;

namespace Accounting.Application.OperationTemplates;

/// <summary>
/// تصویر قطعی تعریف الگو (G3، DDL 082): JSON با ترتیب ثابت (پارامترها و ردیف‌ها بر اساس SortOrder) و
/// هش SHA-256 آن. دو اجرای هم‌تعریف همیشه هش یکسان دارند؛ فیلدهای متغیر (شناسهٔ ردیف‌ها، زمان) در آن نیست.
/// </summary>
public static class TemplateSnapshot
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = false };

    public static (string Json, string Hash) Create(OperationTemplate t)
    {
        var shape = new
        {
            t.Id,
            t.Code,
            t.Title,
            t.Description,
            t.VoucherDescriptionPattern,
            t.SystemTypeId,
            t.Keywords,
            t.AllowedVahedTypes,
            Parameters = t.Parameters.OrderBy(p => p.SortOrder).ThenBy(p => p.Key, StringComparer.Ordinal)
                .Select(p => new { p.Key, p.Title, Type = (int)p.Type, p.DetailGroupId, p.IsRequired, p.AskPrompt, p.SortOrder }),
            Lines = t.Lines.OrderBy(l => l.SortOrder)
                .Select(l => new
                {
                    Side = (int)l.Side,
                    l.SubsidiaryAccountId,
                    l.AmountParameterKey,
                    l.Percent,
                    l.IsBalancingLine,
                    l.DescriptionPattern,
                    l.SortOrder,
                    Details = l.Details.OrderBy(d => d.Level)
                        .Select(d => new { d.Level, d.ParameterKey, d.FixedDetailId }),
                }),
        };
        var json = JsonSerializer.Serialize(shape, Options);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant();
        return (json, hash);
    }
}
