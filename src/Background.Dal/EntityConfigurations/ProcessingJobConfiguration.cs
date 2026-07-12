using Background.Dal.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Background.Dal.EntityConfigurations;

public class ProcessingJobConfiguration : IEntityTypeConfiguration<ProcessingJob>
{
    public void Configure(EntityTypeBuilder<ProcessingJob> builder)
    {
        builder.ToTable("ProcessingJobs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.Status)
            .IsRequired();

        builder.Property(x => x.LastStep)
            .HasMaxLength(50);

        builder.Property(x => x.ArtifactPrefix)
            .HasMaxLength(500);

        builder.Property(x => x.PipelineVersion)
            .HasMaxLength(20);

        builder.Property(x => x.RetryCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.NextRetryAt);

        builder.Property(x => x.LockedUntil);

        builder.Property(x => x.WorkerId)
            .HasMaxLength(100);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.StartedAt);

        builder.Property(x => x.CompletedAt);

        builder.Property(x => x.LastError)
            .HasMaxLength(4000);

        builder.Property(x => x.PromptId);

        builder.HasOne(x => x.Prompt)
            .WithMany()
            .HasForeignKey(x => x.PromptId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.EmailMetadata)
            .WithOne(x => x.Job)
            .HasForeignKey<EmailMetadata>(x => x.Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.NextRetryAt);
        builder.HasIndex(x => x.LockedUntil);
        builder.HasIndex(x => x.WorkerId);
    }
}
