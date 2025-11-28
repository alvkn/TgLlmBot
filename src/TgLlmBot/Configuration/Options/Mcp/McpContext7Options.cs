using System.ComponentModel.DataAnnotations;

namespace TgLlmBot.Configuration.Options.Mcp;

public class McpContext7Options
{
    [Required]
    [MaxLength(1000)]
    public string ApiKey { get; set; } = default!;
}
