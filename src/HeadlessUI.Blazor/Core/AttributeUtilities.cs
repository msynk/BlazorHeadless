namespace HeadlessUI.Blazor;

/// <summary>
/// Provides utilities for merging HTML attributes from internal component logic
/// with user-supplied additional attributes. Handles special merging rules for
/// class (space-concatenation) and style (semicolon-concatenation).
/// </summary>
public static class AttributeUtilities
{
    public static Dictionary<string, object> Merge(
        Dictionary<string, object> baseAttributes,
        IReadOnlyDictionary<string, object>? additionalAttributes)
    {
        if (additionalAttributes is null or { Count: 0 })
            return baseAttributes;

        var merged = new Dictionary<string, object>(baseAttributes, StringComparer.OrdinalIgnoreCase);

        foreach (var (key, value) in additionalAttributes)
        {
            if (key.Equals("class", StringComparison.OrdinalIgnoreCase)
                && merged.TryGetValue("class", out var existingClass))
            {
                merged["class"] = $"{existingClass} {value}".Trim();
            }
            else if (key.Equals("style", StringComparison.OrdinalIgnoreCase)
                     && merged.TryGetValue("style", out var existingStyle))
            {
                var baseStyle = existingStyle?.ToString()?.TrimEnd(';');
                merged["style"] = $"{baseStyle}; {value}";
            }
            else
            {
                merged[key] = value;
            }
        }

        return merged;
    }

    public static void SetDataAttribute(
        Dictionary<string, object> attributes,
        string name,
        string value)
    {
        attributes[$"data-{name}"] = value;
    }

    public static void SetDataAttribute(
        Dictionary<string, object> attributes,
        string name,
        bool condition)
    {
        if (condition)
            attributes[$"data-{name}"] = "";
    }
}
