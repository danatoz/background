using Background.Dal.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Background.Dal.EntityConfigurations;

public class PromptConfiguration : IEntityTypeConfiguration<Prompt>
{
    public void Configure(EntityTypeBuilder<Prompt> builder)
    {
        builder.ToTable("Prompts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Version)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Content)
            .IsRequired();

        builder.Property(x => x.SystemPrompt);

        builder.Property(x => x.ModelName)
            .HasMaxLength(100);

        builder.Property(x => x.Temperature)
            .HasPrecision(3, 2);

        builder.Property(x => x.MaxTokens);

        builder.Property(x => x.ResponseFormat)
            .HasMaxLength(20);

        builder.Property(x => x.TopP)
            .HasPrecision(3, 2);

        builder.Property(x => x.Seed);

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property(x => x.Tags)
            .HasMaxLength(500);

        builder.Property(x => x.ResponseSchema)
            .HasConversion(new ValueConverter<JsonSchema?, string?>(
                v => v.HasValue ? v.Value.Raw : null,
                v => v != null ? new JsonSchema(v) : null));

        builder.Property(x => x.Provider)
            .HasMaxLength(50)
            .IsRequired()
            .HasDefaultValue("ChatCompletion");

        builder.Property(x => x.IsActive)
            .HasDefaultValue(false);

        builder.Property(x => x.FolderFilter)
            .HasMaxLength(200);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasIndex(x => new { x.Name, x.Version })
            .IsUnique();

        builder.HasIndex(x => new { x.FolderFilter, x.IsActive });
    }
}
