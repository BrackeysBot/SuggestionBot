using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace SuggestionBot.Data.EntityConfigurations;

/// <summary>
///     Represents the configuration for the <see cref="BlockedUser" /> entity.
/// </summary>
internal sealed class BlockedUserConfiguration : IEntityTypeConfiguration<BlockedUser>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<BlockedUser> builder)
    {
        builder.ToTable("BlockedUsers");
        builder.HasKey(e => new { e.GuildId, e.UserId });

        builder.Property(e => e.GuildId).IsRequired();
        builder.Property(e => e.UserId).IsRequired();
        builder.Property(e => e.Reason);
        builder.Property(e => e.StaffMemberId).IsRequired();
        builder.Property(e => e.Timestamp).HasConversion<DateTimeOffsetToBytesConverter>().IsRequired();
    }
}
