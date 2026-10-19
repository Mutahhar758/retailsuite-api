using System.Text.Json;
using System.Text.Json.Serialization;
using Retailer.Domain.Legacy;

namespace Retailer.Infrastructure.Legacy.Inventory;

public class AdditionalUnitItem
{
    [JsonPropertyName("unitId")]
    public string UnitId { get; set; } = string.Empty;

    [JsonPropertyName("unitTitle")]
    public string? UnitTitle { get; set; }

    [JsonPropertyName("shortTitle")]
    public string? ShortTitle { get; set; }

    [JsonPropertyName("factor")]
    public decimal Factor { get; set; }
}

public static class ItemUnitDisplayHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Resolves effective sub-units for an item:
    /// 1. If <paramref name="itemAdditionalUnitsJson"/> is explicitly provided (even "[]"), uses the item-level override.
    /// 2. Otherwise, inherits any active child units from <paramref name="subUnits"/> where <c>ParentUnitId == primaryUnitId</c> and <c>ConversionRatio &gt; 1</c>.
    /// </summary>
    public static List<AdditionalUnitItem> GetEffectiveSubUnits(
        string? primaryUnitId,
        string? itemAdditionalUnitsJson,
        IReadOnlyList<Unit> subUnits)
    {
        if (!string.IsNullOrWhiteSpace(itemAdditionalUnitsJson))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<List<AdditionalUnitItem>>(itemAdditionalUnitsJson, JsonOptions) ?? new();
                var result = new List<AdditionalUnitItem>();
                foreach (var entry in parsed)
                {
                    if (entry.Factor <= 1m) continue;
                    var u = subUnits.FirstOrDefault(x => x.Id == entry.UnitId);
                    result.Add(new AdditionalUnitItem
                    {
                        UnitId = entry.UnitId ?? string.Empty,
                        UnitTitle = !string.IsNullOrWhiteSpace(entry.UnitTitle) ? entry.UnitTitle : (u?.Title ?? entry.UnitId),
                        ShortTitle = !string.IsNullOrWhiteSpace(entry.ShortTitle) ? entry.ShortTitle : u?.ShortTitle,
                        Factor = entry.Factor
                    });
                }
                return result;
            }
            catch
            {
                // Fall back to parent unit inheritance if JSON is malformed
            }
        }

        if (string.IsNullOrWhiteSpace(primaryUnitId))
            return new List<AdditionalUnitItem>();

        return subUnits
            .Where(u => u.ParentUnitId == primaryUnitId && u.ConversionRatio.HasValue && u.ConversionRatio.Value > 1m)
            .OrderBy(u => u.ConversionRatio)
            .Select(u => new AdditionalUnitItem
            {
                UnitId = u.Id,
                UnitTitle = u.Title,
                ShortTitle = u.ShortTitle,
                Factor = u.ConversionRatio!.Value
            })
            .ToList();
    }

    public static string? ResolveEffectiveAdditionalUnitsJson(
        string? primaryUnitId,
        string? itemAdditionalUnitsJson,
        IReadOnlyList<Unit> subUnits)
    {
        var effective = GetEffectiveSubUnits(primaryUnitId, itemAdditionalUnitsJson, subUnits);
        if (effective.Count == 0)
            return null;
        return JsonSerializer.Serialize(effective, JsonOptions);
    }

    /// <summary>
    /// Converts any fractional base quantity (<c>qty % 1 != 0</c>, e.g. 0.050, 0.250, 1.250) into its sub-unit equivalent
    /// (e.g. 50 g, 250 g, 1250 g) when the item has an effective sub-unit with <c>Factor &gt; 1</c>.
    /// Whole quantities (e.g. 1.000, 2.000) stay in the base unit (e.g. 1 kg, 2 kg).
    /// </summary>
    public static (decimal DisplayQty, string DisplayUnit, bool Converted) FormatQty(
        decimal baseQty,
        string? primaryUnitId,
        string? fallbackUnitTitle,
        string? itemAdditionalUnitsJson,
        IReadOnlyList<Unit> subUnits)
    {
        var defaultLabel = fallbackUnitTitle?.Trim() ?? string.Empty;

        if (baseQty == 0m || baseQty % 1m == 0m)
        {
            return (baseQty, defaultLabel, false);
        }

        var effectiveSubUnits = GetEffectiveSubUnits(primaryUnitId, itemAdditionalUnitsJson, subUnits);
        if (effectiveSubUnits.Count == 0)
        {
            return (baseQty, defaultLabel, false);
        }

        // Prefer a sub-unit that produces a clean whole integer quantity (e.g. 0.050 * 1000 = 50, 1.250 * 1000 = 1250)
        AdditionalUnitItem? chosen = null;
        foreach (var sub in effectiveSubUnits)
        {
            var candidate = baseQty * sub.Factor;
            if (Math.Abs(candidate - Math.Round(candidate)) < 0.0001m)
            {
                chosen = sub;
                break;
            }
        }
        chosen ??= effectiveSubUnits[0];

        var convertedQty = Math.Round(baseQty * chosen.Factor, 4);
        var subLabel = !string.IsNullOrWhiteSpace(chosen.ShortTitle)
            ? chosen.ShortTitle.Trim()
            : (!string.IsNullOrWhiteSpace(chosen.UnitTitle) ? chosen.UnitTitle.Trim() : defaultLabel);

        return (convertedQty, subLabel, true);
    }

    public static string FormatQtyWithUnitText(
        decimal baseQty,
        string? primaryUnitId,
        string? fallbackUnitTitle,
        string? itemAdditionalUnitsJson,
        IReadOnlyList<Unit> subUnits)
    {
        if (baseQty == 0m)
            return "0";

        var (displayQty, displayUnit, _) = FormatQty(
            baseQty,
            primaryUnitId,
            fallbackUnitTitle,
            itemAdditionalUnitsJson,
            subUnits);

        var formattedNum = displayQty % 1m == 0m
            ? displayQty.ToString("N0")
            : displayQty.ToString("0.###");

        return string.IsNullOrWhiteSpace(displayUnit)
            ? formattedNum
            : $"{formattedNum} {displayUnit}";
    }
}
