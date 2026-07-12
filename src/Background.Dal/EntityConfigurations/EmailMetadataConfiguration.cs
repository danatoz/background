using Background.Dal.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Background.Dal.EntityConfigurations;

public class EmailMetadataConfiguration : IEntityTypeConfiguration<EmailMetadata>
{
    public void Configure(EntityTypeBuilder<EmailMetadata> builder)
    {
        builder.ToTable("EmailMetadata");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.SenderName)
            .HasMaxLength(500);

        builder.Property(x => x.SenderAddress)
            .HasMaxLength(500);

        builder.Property(x => x.Folder)
            .HasMaxLength(500);

        builder.Property(x => x.BodyS3Key)
            .HasMaxLength(1000);

        builder.Property(x => x.AttachmentsJson)
            .HasColumnType("jsonb");

        builder.HasOne(x => x.Job)
            .WithOne(x => x.EmailMetadata)
            .HasForeignKey<EmailMetadata>(x => x.Id)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
