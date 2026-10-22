using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wordrise.Modules.ContentAuthoring.Domain;

namespace Wordrise.Modules.ContentAuthoring.Infrastructure.EntityConfigurations;

internal sealed class LessonDraftConfiguration : IEntityTypeConfiguration<LessonDraft>
{
    public void Configure(EntityTypeBuilder<LessonDraft> builder)
    {
        builder.ToTable("Drafts");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();
        builder.Property(d => d.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(d => d.Error).HasMaxLength(500);

        // The queue asks "what is waiting?"; the admin list asks "what changed last?".
        builder.HasIndex(d => d.Status);
        builder.HasIndex(d => d.UpdatedAtUtc);

        builder.OwnsOne(d => d.Brief, brief =>
        {
            brief.Property(b => b.Scenario).HasColumnName("Scenario").HasMaxLength(LessonBrief.MaxScenarioLength);
            brief.Property(b => b.GrammarStep).HasColumnName("GrammarStep");
            brief.Property(b => b.WordCount).HasColumnName("WordCount");
        });
        builder.Navigation(d => d.Brief).IsRequired();

        // Every version of the lesson and its verdicts are read together, so each list is one JSON document.
        builder.OwnsMany(d => d.Revisions, revision =>
        {
            revision.ToJson("Revisions");

            revision.OwnsOne(r => r.Content, content =>
            {
                content.OwnsMany(c => c.Words);
                content.OwnsMany(c => c.Dialogue);
                content.OwnsMany(c => c.ChatOptions);
                content.OwnsMany(c => c.ListeningOptions);
                content.OwnsOne(c => c.Grammar, grammar => grammar.OwnsMany(g => g.Examples));
            });

            revision.OwnsOne(r => r.Review, review =>
            {
                review.Property(r => r.Source).HasConversion<string>();
                review.OwnsMany(r => r.Criteria, criteria => criteria.Property(c => c.Criterion).HasConversion<string>());
                review.OwnsMany(r => r.Issues, issues => issues.Property(i => i.Severity).HasConversion<string>());
            });

            revision.Navigation(r => r.Content).IsRequired();
            revision.Navigation(r => r.Review).IsRequired();
        });
        builder.Navigation(d => d.Revisions).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(d => d.Feedback, feedback => feedback.ToJson("Feedback"));
        builder.Navigation(d => d.Feedback).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
