using Gamestore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gamestore.DAL.Configurations;

public class PublisherConfiguration : IEntityTypeConfiguration<Publisher>
{
    public void Configure(EntityTypeBuilder<Publisher> builder)
    {
        builder.ToTable("Publishers");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.CompanyName)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(p => p.CompanyName).IsUnique();

        builder.Property(p => p.HomePage)
            .HasMaxLength(500);

        builder.Property(p => p.Description)
            .HasMaxLength(2000);
    }
}
