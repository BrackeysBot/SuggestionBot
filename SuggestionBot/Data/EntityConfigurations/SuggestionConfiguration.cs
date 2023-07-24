using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SuggestionBot.Data.EntityConfigurations;

/// <summary>
///     Represents the configuration for the <see cref="Suggestion" /> entity.
/// </summary>
internal sealed class SuggestionConfiguration : IEntityTypeConfiguration<Suggestion>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Suggestion> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).IsRequired();
        builder.Property(e => e.AuthorId).IsRequired();
        builder.Property(e => e.Content).IsRequired();
        builder.Property(e => e.GuildId).IsRequired();
        builder.Property(e => e.MessageId).IsRequired();
        builder.Property(e => e.Status).IsRequired();
        builder.Property(e => e.Timestamp).IsRequired();
        builder.Property(e => e.ThreadId);
        builder.Property(e => e.StaffMemberId);
    }
}
