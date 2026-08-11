using Gamestore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gamestore.DAL.Configurations;

public class OrderGameConfiguration : IEntityTypeConfiguration<OrderGame>
{
    public void Configure(EntityTypeBuilder<OrderGame> builder)
    {
        builder.ToTable("OrderGames");

        builder.HasKey(og => new { og.OrderId, og.ProductId });

        builder.Property(og => og.Price)
            .IsRequired();

        builder.Property(og => og.Quantity)
            .IsRequired();

        builder.Property(og => og.Discount);

        builder.HasOne(og => og.Order)
            .WithMany(o => o.OrderGames)
            .HasForeignKey(og => og.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(og => og.Game)
            .WithMany(g => g.OrderGames)
            .HasForeignKey(og => og.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
