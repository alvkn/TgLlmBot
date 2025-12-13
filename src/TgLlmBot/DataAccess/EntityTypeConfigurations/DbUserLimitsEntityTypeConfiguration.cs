using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TgLlmBot.DataAccess.Models;

namespace TgLlmBot.DataAccess.EntityTypeConfigurations;

public class DbUserLimitsEntityTypeConfiguration : IEntityTypeConfiguration<DbUserLimit>
{
    public void Configure(EntityTypeBuilder<DbUserLimit> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.HasKey(x => new
        {
            x.ChatId,
            x.UserId
        });
    }
}
