using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wordrise.Modules.Learning.Domain.Learners;

namespace Wordrise.Modules.Learning.Infrastructure.EntityConfigurations;

internal sealed class LearnerConfiguration : IEntityTypeConfiguration<Learner>
{
    public void Configure(EntityTypeBuilder<Learner> builder)
    {
        builder.ToTable("Learners");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedNever();

        builder.OwnsMany(l => l.CompletedLessons, completion =>
        {
            completion.ToTable("LessonCompletions");
            completion.WithOwner().HasForeignKey("LearnerId");
            completion.HasKey("LearnerId", nameof(LessonCompletion.LessonId));
        });
        builder.Navigation(l => l.CompletedLessons).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
