using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PC212242.UPractica.Clerky.Platform.Paperwork.Domain.Model.Aggregates;

namespace PC212242.UPractica.Clerky.Platform.Paperwork.Infrastructure.Persistence.EFC.Configuration;

public class ContractConfiguration : IEntityTypeConfiguration<Contract>
{
    public void Configure(EntityTypeBuilder<Contract> builder)
    {
        builder.ToTable("contracts");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.OwnsOne(c => c.DocumentIdentifier, owned =>
        {
            owned.Property(v => v.Value)
                .HasColumnName("document_identifier")
                .HasConversion(
                    value => value.ToString(),
                    value => Guid.Parse(value))
                .HasMaxLength(36)
                .IsRequired();

            owned.HasIndex(v => v.Value).IsUnique();
        });

        builder.OwnsOne(c => c.ClientId, owned =>
        {
            owned.Property(v => v.Value)
                .HasColumnName("client_id")
                .HasConversion(
                    value => value.ToString(),
                    value => Guid.Parse(value))
                .HasMaxLength(36)
                .IsRequired();
        });

        builder.OwnsOne(c => c.Period, owned =>
        {
            owned.Property(v => v.StartDate)
                .HasColumnName("start_date")
                .IsRequired();

            owned.Property(v => v.EndDate)
                .HasColumnName("end_date")
                .IsRequired();
        });

        builder.OwnsOne(c => c.TotalValue, owned =>
        {
            owned.Property(v => v.Amount)
                .HasColumnName("total_amount")
                .HasPrecision(18, 2)
                .IsRequired();

            owned.Property(v => v.Currency)
                .HasColumnName("currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Property(c => c.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(c => c.Notes)
            .HasColumnName("notes")
            .HasMaxLength(1000);

        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();
    }
}