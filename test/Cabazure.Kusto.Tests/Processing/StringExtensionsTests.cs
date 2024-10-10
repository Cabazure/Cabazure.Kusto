using StringExtensions = Cabazure.Kusto.Processing.StringExtensions;

namespace Cabazure.Kusto.Tests.Processing;

public class StringExtensionsTests
{
    [Theory]
    [InlineData("abcefghijklmnopqrstuvxyzABCEFGHIJKLMNOPQRSTUVXYZ0123456789", "abcefghijklmnopqrstuvxyzABCEFGHIJKLMNOPQRSTUVXYZ0123456789")]
    [InlineData("@£$€}][{", "")]
    [InlineData("*-+/-_æøåÆØÅ'¨´`", "")]
    [InlineData("*-+/", "")]
    public void ToAlphaNummeric_Removed_NonAlphaNummeric_Charactors_From_String(
        string input,
        string expectedResult)
        => StringExtensions
            .ToAlphaNumeric(input)
            .Should()
            .Be(expectedResult);
}
