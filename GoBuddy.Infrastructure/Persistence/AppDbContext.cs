using GoBuddy.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GoBuddy.Infrastructure.Persistence
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<RideSession> RideSessions { get; set; }
        public DbSet<RideRequest> RideRequests { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            var user = modelBuilder.Entity<User>();

            user.HasIndex(userEntity => userEntity.Email).IsUnique();
            user.HasIndex(userEntity => userEntity.Phone).IsUnique();


            var vehicle = modelBuilder.Entity<Vehicle>();

            vehicle.HasIndex(vehicleEntity => vehicleEntity.VehicleNo).IsUnique();
            vehicle.HasIndex(vehicleEntity => vehicleEntity.LicenseNo).IsUnique();

            vehicle.HasOne(vehicleEntity => vehicleEntity.Driver)
                   .WithMany(userEntity => userEntity.Vehicles)
                   .HasForeignKey(vehicleEntity => vehicleEntity.DriverId)
                   .OnDelete(DeleteBehavior.Restrict);


            var rideSession = modelBuilder.Entity<RideSession>();

            rideSession.HasOne(sessionEntity => sessionEntity.Driver)
                       .WithMany(userEntity => userEntity.RideSessions)
                       .HasForeignKey(sessionEntity => sessionEntity.DriverId)
                       .OnDelete(DeleteBehavior.Restrict);

            var rideRequest = modelBuilder.Entity<RideRequest>();

            rideRequest.HasOne(requestEntity => requestEntity.Passenger)
                       .WithMany(userEntity => userEntity.RideRequests)
                       .HasForeignKey(requestEntity => requestEntity.PassengerId)
                       .OnDelete(DeleteBehavior.Restrict);

            rideRequest.HasOne(requestEntity => requestEntity.RideSession)
                       .WithMany(sessionEntity => sessionEntity.RideRequests)
                       .HasForeignKey(requestEntity => requestEntity.RideSessionId)
                       .OnDelete(DeleteBehavior.Cascade);
        }
    }

}