using Microsoft.EntityFrameworkCore;
using MiniErp.Api.Domain;

namespace MiniErp.Api.Data;

public sealed class ErpDbContext(DbContextOptions<ErpDbContext> options) : DbContext(options)
{
    public DbSet<UserAccount> Users => Set<UserAccount>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();
    public DbSet<SalesOrderLine> SalesOrderLines => Set<SalesOrderLine>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserAccount>(entity =>
        {
            entity.HasIndex(user => user.Email).IsUnique();
            entity.Property(user => user.Email).HasMaxLength(180);
            entity.Property(user => user.Name).HasMaxLength(160);
            entity.Property(user => user.Role).HasConversion<string>().HasMaxLength(32);
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasIndex(customer => customer.TaxId).IsUnique();
            entity.Property(customer => customer.Name).HasMaxLength(180);
            entity.Property(customer => customer.TaxId).HasMaxLength(32);
            entity.Property(customer => customer.Segment).HasMaxLength(80);
            entity.Property(customer => customer.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(customer => customer.CreditLimit).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasIndex(product => product.Sku).IsUnique();
            entity.Property(product => product.Sku).HasMaxLength(40);
            entity.Property(product => product.Name).HasMaxLength(180);
            entity.Property(product => product.Category).HasMaxLength(80);
            entity.Property(product => product.UnitPrice).HasPrecision(18, 2);
        });

        modelBuilder.Entity<InventoryItem>(entity =>
        {
            entity.HasIndex(item => new { item.ProductId, item.Warehouse }).IsUnique();
            entity.Property(item => item.Warehouse).HasMaxLength(80);
            entity.Ignore(item => item.AvailableQuantity);
        });

        modelBuilder.Entity<SalesOrder>(entity =>
        {
            entity.HasIndex(order => order.Number).IsUnique();
            entity.Property(order => order.Number).HasMaxLength(40);
            entity.Property(order => order.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(order => order.Subtotal).HasPrecision(18, 2);
            entity.Property(order => order.Tax).HasPrecision(18, 2);
            entity.Property(order => order.Total).HasPrecision(18, 2);
            entity.HasOne(order => order.Invoice)
                .WithOne(invoice => invoice.SalesOrder)
                .HasForeignKey<Invoice>(invoice => invoice.SalesOrderId);
        });

        modelBuilder.Entity<SalesOrderLine>(entity =>
        {
            entity.Property(line => line.UnitPrice).HasPrecision(18, 2);
            entity.Property(line => line.LineTotal).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasIndex(invoice => invoice.Number).IsUnique();
            entity.Property(invoice => invoice.Number).HasMaxLength(40);
            entity.Property(invoice => invoice.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(invoice => invoice.Amount).HasPrecision(18, 2);
        });

        modelBuilder.Entity<StockMovement>(entity =>
        {
            entity.Property(movement => movement.Type).HasConversion<string>().HasMaxLength(32);
            entity.Property(movement => movement.Reference).HasMaxLength(80);
            entity.Property(movement => movement.Warehouse).HasMaxLength(80);
        });
    }
}
