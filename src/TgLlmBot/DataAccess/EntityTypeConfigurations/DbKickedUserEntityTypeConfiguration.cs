using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TgLlmBot.DataAccess.Models;

namespace TgLlmBot.DataAccess.EntityTypeConfigurations;

public class DbKickedUserEntityTypeConfiguration : IEntityTypeConfiguration<DbKickedUser>
{
    public void Configure(EntityTypeBuilder<DbKickedUser> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.HasKey(x => new
        {
            x.ChatId,
            Id = x.UserId
        });
    }
}
