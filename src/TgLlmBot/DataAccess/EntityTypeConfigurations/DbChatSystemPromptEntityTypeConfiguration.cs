using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TgLlmBot.DataAccess.Models;

namespace TgLlmBot.DataAccess.EntityTypeConfigurations;

public class DbChatSystemPromptEntityTypeConfiguration : IEntityTypeConfiguration<DbChatSystemPrompt>
{
    public void Configure(EntityTypeBuilder<DbChatSystemPrompt> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.HasKey(x => x.ChatId);
        builder.Property(x => x.Prompt).HasMaxLength(4096);
    }
}
