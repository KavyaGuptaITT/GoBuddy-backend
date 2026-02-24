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

        modelBuilder.Entity<RideSession>()
            .HasOne(session => session.Driver)
            .WithMany(user => user.RideSessions)
            .HasForeignKey(session => session.FK_Driver_ID)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RideRequest>()
            .HasOne(request => request.Passenger)
            .WithMany(user => user.RideRequests)
            .HasForeignKey(request => request.FK_User_ID)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RideRequest>()
            .HasOne(request => request.RideSession)
            .WithMany(session => session.RideRequests)
            .HasForeignKey(request => request.FK_RideSession_ID)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RideSession>()
            .HasIndex(rideSession => new { rideSession.CurrentLatitude, rideSession.CurrentLongitude });
    }
}