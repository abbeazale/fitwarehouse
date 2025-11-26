using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<InventoryRecord> Inventory { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<InventoryRecord>(entity =>
        {
            entity.ToTable("inventory");
            
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();
            
            entity.Property(e => e.ProductName)
                .HasColumnName("product_name")
                .IsRequired()
                .HasMaxLength(128);
            
            entity.Property(e => e.Quantity)
                .HasColumnName("quantity")
                .IsRequired();
            
            entity.Property(e => e.WarehouseLocation)
                .HasColumnName("warehouse_location")
                .IsRequired()
                .HasMaxLength(64);
            
            entity.Property(e => e.SubmittedBy)
                .HasColumnName("submitted_by")
                .IsRequired()
                .HasMaxLength(64);
            
            entity.Property(e => e.ProcessedAtUtc)
                .HasColumnName("processed_at_utc")
                .IsRequired();
        });
    }
}

