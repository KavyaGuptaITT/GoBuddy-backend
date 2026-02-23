using GoBuddy.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GoBuddy.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Vehicle> Vehicles { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasIndex(user => user.Email)
            .IsUnique();

        modelBuilder.Entity<Vehicle>()
            .HasIndex(vehicle => vehicle.VehicleNo)
            .IsUnique();
            

        modelBuilder.Entity<Vehicle>()
            .HasOne(vehicle => vehicle.Driver)
            .WithMany(user => user.Vehicles)
            .HasForeignKey(vehicle => vehicle.FK_user_ID)
            .OnDelete(DeleteBehavior.Restrict);
    }
}