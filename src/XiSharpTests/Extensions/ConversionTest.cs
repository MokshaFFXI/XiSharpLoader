using XiSharpLoader.Extensions;
using Xunit;
using static XiSharpLoader.Helpers.Enums;

namespace XiSharpLoaderTests.Extensions
{
    public class ConversionTest
    {
        [Theory]
        [InlineData("0", PolLanguage.Japanese)]
        [InlineData(" 0", PolLanguage.Japanese)]
        [InlineData("0 ", PolLanguage.Japanese)]
        [InlineData(" 0 ", PolLanguage.Japanese)]
        [InlineData("jp", PolLanguage.Japanese)]
        [InlineData("Jp", PolLanguage.Japanese)]
        [InlineData("JP", PolLanguage.Japanese)]
        [InlineData("1", PolLanguage.English)]
        [InlineData("en", PolLanguage.English)]
        [InlineData("En", PolLanguage.English)]
        [InlineData("EN", PolLanguage.English)]
        [InlineData("2", PolLanguage.European)]
        [InlineData("eu", PolLanguage.European)]
        [InlineData("Eu", PolLanguage.European)]
        [InlineData("EU", PolLanguage.European)]
        [InlineData("something else", PolLanguage.English)]
        internal void GivenPassedPolLanguageRepresentations_WhenConverting_ThenExpectedValueReturned(string input, PolLanguage expectedOutput)
        {
            Assert.Equal(expectedOutput, input.ToPolLanguage());

        }
    }
}
