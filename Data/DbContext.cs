using EcommerceMiddleware.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using static EcommerceMiddleware.Models.PaymentModel;

//public class EcommerceDbContext : DbContext
public class EcommerceDbContext : IdentityDbContext<User>
{

    public EcommerceDbContext(DbContextOptions<EcommerceDbContext> options) : base(options)
    {
    }
    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Payment> Payments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrderItem>()
            .Property(o => o.Price)
            .HasColumnType("decimal(18, 2)");

        modelBuilder.Entity<Payment>()
            .Property(p => p.Amount)
            .HasColumnType("decimal(18, 2)");

        modelBuilder.Entity<Order>()
            .Property(o => o.TotalAmount)
            .HasColumnType("decimal(18, 2)");

        modelBuilder.Entity<Product>()
            .Property(p => p.Price)
            .HasColumnType("decimal(18, 2)");

        // Optionally add configurations for the new properties if needed
        modelBuilder.Entity<Product>()
            .Property(p => p.Rating)
            .HasColumnType("float"); // Or use decimal if preferred

        base.OnModelCreating(modelBuilder);
    }

}


// public DbSet<User> Users { get; set; }
// public DbSet<Product> Products { get; set; }
//// public DbSet<Cart> Carts { get; set; }
// public DbSet<Order> Orders { get; set; }
//// public DbSet<Payment> Payments { get; set; }
