using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wordrise.Modules.Learning.Domain.Vocabulary;

namespace Wordrise.Modules.Learning.Infrastructure.EntityConfigurations;

internal sealed class WordProgressConfiguration : IEntityTypeConfiguration<WordProgress>
{
    public void Configure(EntityTypeBuilder<WordProgress> builder)
    {
        builder.ToTable("WordProgress");
        builder.HasKey(w => new { w.LearnerId, w.WordId });
        builder.Property(w => w.English).HasMaxLength(100);
        builder.Property(w => w.Arabic).HasMaxLength(100);
        builder.Property(w => w.LastRating).HasConversion<string>().HasMaxLength(10);

        // "What is due today" is the hottest query of the module.
        builder.HasIndex(w => new { w.LearnerId, w.DueOn });
    }
}
