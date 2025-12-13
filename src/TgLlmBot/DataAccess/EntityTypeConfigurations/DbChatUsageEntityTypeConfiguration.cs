using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TgLlmBot.DataAccess.Models;

namespace TgLlmBot.DataAccess.EntityTypeConfigurations;

public class DbChatUsageEntityTypeConfiguration : IEntityTypeConfiguration<DbChatUsage>
{
    public void Configure(EntityTypeBuilder<DbChatUsage> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.HasKey(x => new
        {
            x.Date,
            x.ChatId,
            x.UserId
        });
        builder.HasIndex(x => x.Date);
    }
}
