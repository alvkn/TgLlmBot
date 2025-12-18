using System.Diagnostics.CodeAnalysis;

namespace TgLlmBot.Utils;

public static class SurrogatePairSanitizer
{
    [return: NotNullIfNotNull(nameof(input))]
    public static string? SanitizeInvalidUtf16(string? input)
    {
        if (input is null)
        {
            return null;
        }

        var sb = new System.Text.StringBuilder(input.Length);

        for (var i = 0; i < input.Length; i++)
        {
            var ch = input[i];

            if (char.IsHighSurrogate(ch))
            {
                if (i + 1 < input.Length && char.IsLowSurrogate(input[i + 1]))
                {
                    sb.Append(ch);
                    sb.Append(input[++i]);
                }
                else
                {
                    sb.Append('\uFFFD');
                }
            }
            else if (char.IsLowSurrogate(ch))
            {
                sb.Append('\uFFFD');
            }
            else
            {
                sb.Append(ch);
            }
        }

        return sb.ToString();
    }
}
