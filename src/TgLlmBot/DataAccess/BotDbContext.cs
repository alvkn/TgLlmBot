using System;
using Microsoft.EntityFrameworkCore;
using TgLlmBot.DataAccess.EntityTypeConfigurations;
using TgLlmBot.DataAccess.Models;

namespace TgLlmBot.DataAccess;

public class BotDbContext : DbContext
{
    public BotDbContext(DbContextOptions<BotDbContext> options)
        : base(options)
    {
    }

    public DbSet<DbChatMessage> ChatHistory { get; set; }

    public DbSet<DbKickedUser> KickedUsers { get; set; }

    public DbSet<DbChatSystemPrompt> ChatSystemPrompts { get; set; }
    public DbSet<DbPersonalChatSystemPrompt> PersonalChatSystemPrompts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfiguration(new DbChatMessageEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new DbKickedUserEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new DbChatSystemPromptEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new DbPersonalChatSystemPromptEntityTypeConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}
