using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TgLlmBot.DataAccess.Models;

namespace TgLlmBot.DataAccess.EntityTypeConfigurations;

public class DbPersonalChatSystemPromptEntityTypeConfiguration : IEntityTypeConfiguration<DbPersonalChatSystemPrompt>
{
    public void Configure(EntityTypeBuilder<DbPersonalChatSystemPrompt> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.HasKey(x => new
        {
            x.ChatId,
            x.UserId
        });
        builder.Property(x => x.Prompt).HasMaxLength(4096);
    }
}
