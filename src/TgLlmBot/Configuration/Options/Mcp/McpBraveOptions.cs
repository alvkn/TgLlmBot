using System.ComponentModel.DataAnnotations;

namespace TgLlmBot.Configuration.Options.Mcp;

public class McpBraveOptions
{
    [Required]
    [MaxLength(1000)]
    public string ApiKey { get; set; } = default!;
}
