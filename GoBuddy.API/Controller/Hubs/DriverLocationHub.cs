using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using GoBuddy.Application.Interfaces;
using GoBuddy.Domain.Enums;
using GoBuddy.Domain.Entities;

namespace GoBuddy.API.Hubs
{
    public static class OnlineDriversStore
    {
        public static ConcurrentDictionary<string, DriverLocation> Drivers = new();
    }

    public class DriverLocation
    {
        public string ConnectionId { get; set; } = "";
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class DriverLocationHub : Hub
    {
        public async Task DriverGoOnline(double latitude, double longitude)
        {
            var connectionId = Context.ConnectionId;

            OnlineDriversStore.Drivers[connectionId] = new DriverLocation
            {
                ConnectionId = connectionId,
                Latitude = latitude,
                Longitude = longitude,
                LastUpdated = DateTime.UtcNow
            };

            await Clients.All.SendAsync("DriverOnline", new
            {
                DriverId = connectionId,
                Latitude = latitude,
                Longitude = longitude,
                Timestamp = DateTime.UtcNow
            });
        }

        public async Task UpdateLocation(double latitude, double longitude)
        {
            var connectionId = Context.ConnectionId;

            if (OnlineDriversStore.Drivers.ContainsKey(connectionId))
            {
                OnlineDriversStore.Drivers[connectionId].Latitude = latitude;
                OnlineDriversStore.Drivers[connectionId].Longitude = longitude;
                OnlineDriversStore.Drivers[connectionId].LastUpdated = DateTime.UtcNow;
            }

            await Clients.All.SendAsync("LocationUpdated", new
            {
                DriverId = connectionId,
                Latitude = latitude,
                Longitude = longitude,
                Timestamp = DateTime.UtcNow
            });
        }

        public async Task DriverGoOffline()
        {
            var connectionId = Context.ConnectionId;

            OnlineDriversStore.Drivers.TryRemove(connectionId, out _);

            await Clients.All.SendAsync("DriverOffline", new
            {
                DriverId = connectionId
            });
        }

        public async Task SendRideRequest(
            string driverConnectionId,
            string passengerId,
            string passengerName,
            double pickupLat,
            double pickupLng,
            double dropLat,
            double dropLng,
            string pickupName,
            string dropName)
        {
            if (!OnlineDriversStore.Drivers.ContainsKey(driverConnectionId))
            {
                await Clients.Caller.SendAsync("RequestFailed", new
                {
                    Message = "Driver is no longer available."
                });
                return;
            }

            await Clients.Client(driverConnectionId).SendAsync("IncomingRideRequest", new
            {
                PassengerId = passengerId,
                PassengerConnectionId = Context.ConnectionId,
                PassengerName = passengerName,
                PickupLat = pickupLat,
                PickupLng = pickupLng,
                DropLat = dropLat,
                DropLng = dropLng,
                PickupName = pickupName,
                DropName = dropName,
                Timestamp = DateTime.UtcNow
            });

            await Clients.Caller.SendAsync("RequestSent", new
            {
                Message = "Ride request sent! Waiting for driver response."
            });
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var connectionId = Context.ConnectionId;

            OnlineDriversStore.Drivers.TryRemove(connectionId, out _);

            await Clients.All.SendAsync("DriverOffline", new
            {
                DriverId = connectionId
            });

            await base.OnDisconnectedAsync(exception);
        }
    }
}