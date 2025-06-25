using System.Diagnostics.CodeAnalysis;

namespace EmailQueue.API.Platform;

public static class StringExtensions
{
    // ReSharper disable ConvertIfStatementToReturnStatement
    [return: NotNullIfNotNull(nameof(value))]
    public static string? Truncate(this string? value, int maxLength, string suffix = "…")
    {
        if (maxLength < 0) throw new ArgumentException("maxLength must not be negative.", nameof(maxLength));
        if (value is null) return null;
        if (value.Length == 0 || maxLength == 0) return string.Empty;
        if (value.Length <= maxLength) return value;
        if (maxLength < suffix.Length) return value[..maxLength];
        return $"{value[..(maxLength - suffix.Length)]}{suffix}";
    }
}
