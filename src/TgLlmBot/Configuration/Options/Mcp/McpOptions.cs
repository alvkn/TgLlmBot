using System.ComponentModel.DataAnnotations;

namespace TgLlmBot.Configuration.Options.Mcp;

public class McpOptions
{
    [Required]
    public McpGithubOptions Github { get; set; } = default!;

    [Required]
    public McpBraveOptions Brave { get; set; } = default!;

    [Required]
    public McpContext7Options Context7 { get; set; } = default!;

    [Required]
    public McpBrightDataOptions BrightData { get; set; } = default!;
}
