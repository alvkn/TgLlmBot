namespace TgLlmBot.Services.OpenRouter.Models;

public class OpenRouterStats
{
    public OpenRouterStats(decimal usage, decimal remaining, decimal limit)
    {
        Usage = usage;
        Remaining = remaining;
        Limit = limit;
    }

    public decimal Usage { get; }
    public decimal Remaining { get; }
    public decimal Limit { get; }
}
