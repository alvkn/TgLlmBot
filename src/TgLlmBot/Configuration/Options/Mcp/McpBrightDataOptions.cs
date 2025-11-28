using System.ComponentModel.DataAnnotations;

namespace TgLlmBot.Configuration.Options.Mcp;

public class McpBrightDataOptions
{
    [Required]
    [MaxLength(1000)]
    public string ApiKey { get; set; } = default!;
}
