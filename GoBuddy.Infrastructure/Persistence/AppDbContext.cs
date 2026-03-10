using GoBuddy.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GoBuddy.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Vehicle> Vehicles { get; set; }
    public DbSet<RideSession> RideSessions { get; set; }
    public DbSet<RideRequest> RideRequests { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options)
         : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder) { 
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity => { 
            entity.HasIndex(user => user.Email).IsUnique();
            entity.HasIndex(user => user.Phone).IsUnique();
        });

        modelBuilder.Entity<Vehicle>(entity => { 
            entity.HasIndex(vehicle => vehicle.VehicleNo).IsUnique();
            entity.HasIndex(vehicle => vehicle.LicenseNo).IsUnique();
            entity.HasOne(vehicle => vehicle.Driver)
                  .WithMany(user => user.Vehicles)
                  .HasForeignKey(vehicle => vehicle.DriverId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RideSession>(entity => { 
            entity.HasOne(session => session.Driver)
                  .WithMany(user => user.RideSessions)
                  .HasForeignKey(session => session.DriverId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(rideSession => new {
                rideSession.CurrentLatitude,
                rideSession.CurrentLongitude
            });
        });

        modelBuilder.Entity<RideRequest>(entity =>
        {
            entity.HasOne(request => request.Passenger)
                  .WithMany(user => user.RideRequests)
                  .HasForeignKey(request => request.PassengerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(request => request.RideSession)
                  .WithMany(session => session.RideRequests)
                  .HasForeignKey(request => request.RideSessionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
