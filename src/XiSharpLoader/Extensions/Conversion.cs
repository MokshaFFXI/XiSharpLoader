using System;
using System.Runtime.CompilerServices;
using static XiSharpLoader.Helpers.Enums;

[assembly: InternalsVisibleTo("XiSharpLoaderTests")]
namespace XiSharpLoader.Extensions
{
    internal static class Conversion
    {
        public static PolLanguage ToPolLanguage(this string input)
        {
            if (string.Equals(input, "JP", StringComparison.OrdinalIgnoreCase)
               || string.Equals(input, "0", StringComparison.OrdinalIgnoreCase))
            {
                return PolLanguage.Japanese;
            }
            else if (string.Equals(input, "EN", StringComparison.OrdinalIgnoreCase)
                || string.Equals(input, "1", StringComparison.OrdinalIgnoreCase))
            {
                return PolLanguage.English;
            }
            else if (string.Equals(input, "EU", StringComparison.OrdinalIgnoreCase)
                || string.Equals(input, "2", StringComparison.OrdinalIgnoreCase))
            {
                return PolLanguage.European;
            }
            else
            {
                return PolLanguage.English;
            }
        }
    }
}
