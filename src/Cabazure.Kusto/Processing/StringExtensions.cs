using System.Text.RegularExpressions;

namespace Cabazure.Kusto.Processing;

public static class StringExtensions
{
    private static readonly Regex AlphaNummericFilter = new(
        "[^a-zA-Z0-9]",
        RegexOptions.Compiled,
        TimeSpan.FromSeconds(1));

    public static string ToAlphaNumeric(
        this string str)
        => AlphaNummericFilter.Replace(str, string.Empty);
}
