using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wordrise.Modules.Learning.Domain.Lessons;

namespace Wordrise.Modules.Learning.Infrastructure.EntityConfigurations;

internal sealed class LessonConfiguration : IEntityTypeConfiguration<Lesson>
{
    public void Configure(EntityTypeBuilder<Lesson> builder)
    {
        builder.ToTable("Lessons");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedNever();
        builder.HasIndex(l => l.Number).IsUnique();
        builder.Property(l => l.TargetLanguage).HasMaxLength(8);
        builder.Property(l => l.NativeLanguage).HasMaxLength(8);
        builder.Property(l => l.Title).HasMaxLength(100);
        builder.Property(l => l.Scenario).HasMaxLength(100);
        builder.Property(l => l.GrammarPoint).HasMaxLength(100);

        // The lesson body is always read whole, so it is stored as one JSON document.
        builder.OwnsOne(l => l.Content, content =>
        {
            content.ToJson();
            content.OwnsMany(c => c.Words);
            content.OwnsMany(c => c.Dialogue);
            content.OwnsMany(c => c.ChatOptions);
            content.OwnsMany(c => c.ListeningOptions);
            content.OwnsOne(c => c.Grammar, grammar => grammar.OwnsMany(g => g.Examples));
        });
    }
}
