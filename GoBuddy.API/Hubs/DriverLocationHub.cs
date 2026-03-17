using GoBuddy.API.SharedConstants;
using GoBuddy.Application.Interfaces;
using GoBuddy.Domain.Entities;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Threading;

namespace GoBuddy.API.Hubs
{
    public static class OnlineDriversStore
    {
        public static ConcurrentDictionary<string, DriverLocation> Drivers = new();
        public static ConcurrentDictionary<string, PendingRequest> PendingRequests = new();
        public static ConcurrentDictionary<int, ActiveRide> ActiveRides = new();
    }

    public class DriverLocation
    {
        public string ConnectionId { get; set; } = string.Empty;
        public string DriverName { get; set; } = string.Empty;
        public string VehicleModel { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime LastUpdated { get; set; }
        public bool IsBusy { get; set; } = false;
        public int AvailableSeats { get; set; } = 1;
        public decimal RatePerKm { get; set; }
    }

    public class PendingRequest
    {
        public string PassengerConnectionId { get; set; } = string.Empty;
        public string PassengerId { get; set; } = string.Empty;
        public string PassengerName { get; set; } = string.Empty;
        public string PassengerPin { get; set; } = string.Empty;
        public double PickupLat { get; set; }
        public double PickupLng { get; set; }
        public double DropLat { get; set; }
        public double DropLng { get; set; }
        public string PickupName { get; set; } = string.Empty;
        public string DropName { get; set; } = string.Empty;
        public CancellationTokenSource CancellationTokenSource { get; set; } = new();
    }

    public class ActiveRide
    {
        public string DriverConnectionId { get; set; } = string.Empty;
        public string PassengerConnectionId { get; set; } = string.Empty;
        public string PassengerId { get; set; } = string.Empty;
        public string DriverId { get; set; } = string.Empty;
        public string PassengerPin { get; set; } = string.Empty;
        public bool PinConfirmed { get; set; } = false;
        public double TotalKm { get; set; }
        public int PinAttempts { get; set; } = 0;
        public double PickupLat { get; set; }
        public double PickupLng { get; set; }
        public double DropLat { get; set; }
        public double DropLng { get; set; }
        public string PickupName { get; set; } = string.Empty;
        public string DropName { get; set; } = string.Empty;
    }

    public class DriverLocationHub : Hub
    {
        private readonly IServiceScopeFactory serviceScopeFactory;
        private static int rideIdCounter = 0;

        public DriverLocationHub(IServiceScopeFactory scopeFactory)
        {
            serviceScopeFactory = scopeFactory;
        }

        public async Task DriverGoOnline(double latitude, double longitude,
            string driverName, string vehicleModel, int availableSeats, decimal ratePerKm)
        {
            var connectionId = Context.ConnectionId;

            OnlineDriversStore.Drivers[connectionId] = CreateDriverLocation(
                connectionId, driverName, vehicleModel, latitude, longitude, availableSeats, ratePerKm
            );

            await Clients.All.SendAsync("DriverOnline",
                CreateDriverOnlinePayload(connectionId, driverName, vehicleModel, latitude, longitude, availableSeats));
        }

        public async Task UpdateLocation(double latitude, double longitude)
        {
            var connectionId = Context.ConnectionId;

            if (OnlineDriversStore.Drivers.TryGetValue(connectionId, out var driver))
            {
                driver.Latitude = latitude;
                driver.Longitude = longitude;
                driver.LastUpdated = DateTime.UtcNow;
            }

            await Clients.All.SendAsync("LocationUpdated",
                CreateLocationPayload(connectionId, latitude, longitude));
        }

        public async Task DriverGoOffline()
        {
            var connectionId = Context.ConnectionId;

            var hasActiveRide = OnlineDriversStore.ActiveRides.Values
                .Any(ride => ride.DriverConnectionId == connectionId);

            if (hasActiveRide)
            {
                await Clients.Caller.SendAsync("CannotGoOffline",
                    CreateMessagePayload("You have an active ride. Cancel the ride before going offline."));
                return;
            }

            await CleanupDriver(connectionId);
            await Clients.All.SendAsync("DriverOffline", CreateDriverIdPayload(connectionId));
        }

        public async Task SendRideRequest(
            string driverConnectionId,
            string passengerId,
            string passengerName,
            double pickupLat, double pickupLng,
            double dropLat, double dropLng,
            string pickupName, string dropName,
            string passengerPin)
        {
            if (!OnlineDriversStore.Drivers.TryGetValue(driverConnectionId, out var driver) || driver.IsBusy)
            {
                await Clients.Caller.SendAsync("RequestFailed",
                    CreateMessagePayload(driver == null
                        ? "Driver is no longer available."
                        : "Driver is busy. Try another driver."));
                return;
            }

            driver.IsBusy = true;

            var cancellationTokenSource = new CancellationTokenSource();

            var pendingRequest = CreatePendingRequest(
                Context.ConnectionId, passengerId, passengerName, passengerPin,
                pickupLat, pickupLng, dropLat, dropLng,
                pickupName, dropName, cancellationTokenSource
            );

            OnlineDriversStore.PendingRequests[driverConnectionId] = pendingRequest;

            await Clients.Client(driverConnectionId).SendAsync("IncomingRideRequest",
                CreateRideRequestPayload(pendingRequest));

            await Clients.Caller.SendAsync("RequestSent",
                CreateMessagePayload("Request sent! Waiting for driver..."));

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(AppConstants.RequestTimeoutMs, cancellationTokenSource.Token);

                    if (OnlineDriversStore.PendingRequests.TryRemove(driverConnectionId, out _))
                    {
                        if (OnlineDriversStore.Drivers.TryGetValue(driverConnectionId, out var driverEntry))
                            driverEntry.IsBusy = false;

                        await Clients.Client(pendingRequest.PassengerConnectionId)
                            .SendAsync("RequestTimeout", CreateMessagePayload("Driver did not respond."));

                        await Clients.Client(driverConnectionId)
                            .SendAsync("RequestExpired", CreateMessagePayload("Request expired."));
                    }
                }
                catch (TaskCanceledException) { }
            });
        }

        public async Task AcceptRideRequest()
        {
            var driverConnectionId = Context.ConnectionId;

            if (!OnlineDriversStore.PendingRequests.TryRemove(driverConnectionId, out var request))
            {
                await Clients.Caller.SendAsync("Error",
                    CreateMessagePayload("No pending request found."));
                return;
            }

            request.CancellationTokenSource.Cancel();

            if (OnlineDriversStore.Drivers.TryGetValue(driverConnectionId, out var driver))
                driver.IsBusy = true;

            var rideId = Interlocked.Increment(ref rideIdCounter);

            var driverUserId = Context.User?
                .FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

            OnlineDriversStore.ActiveRides[rideId] = CreateActiveRide(
                driverConnectionId, request, driverUserId
            );

            await Clients.Client(request.PassengerConnectionId).SendAsync("RideAccepted",
                CreateRideAcceptedPayload(rideId, driverConnectionId, driver));

            await Clients.Caller.SendAsync("RideConfirmed",
                CreateRideConfirmedPayload(rideId, request));
        }

        public async Task RideCompleted(int rideId, double totalKm)
        {
            if (!OnlineDriversStore.ActiveRides.TryGetValue(rideId, out var ride)) return;
            if (!OnlineDriversStore.Drivers.TryGetValue(ride.DriverConnectionId, out var driver)) return;

            OnlineDriversStore.ActiveRides.TryRemove(rideId, out _);
            driver.IsBusy = false;

            double totalCost = Math.Round(totalKm * (double)driver.RatePerKm, AppConstants.RoundDigits);

            using var scope = serviceScopeFactory.CreateScope();
            var rideRepository = scope.ServiceProvider.GetRequiredService<IRideRepository>();
            var vehicleRepository = scope.ServiceProvider.GetRequiredService<IVehicleRepository>();

            int driverUserId = int.Parse(ride.DriverId);

            var session = new RideSession(driverUserId);
            session.Complete();
            int sessionId = await rideRepository.CreateSessionAsync(session);

            await rideRepository.CreateRequestAsync(new RideRequest(
                sessionId,
                int.Parse(ride.PassengerId),
                ride.PickupLat, ride.PickupLng,
                ride.DropLat, ride.DropLng,
                ride.PickupName, ride.DropName,
                totalKm, (decimal)totalCost
            ));

            var vehicle = await vehicleRepository.GetByDriverIdAsync(driverUserId);

            if (vehicle != null)
            {
                vehicle.ResetAvailableSeats();
                await vehicleRepository.UpdateAsync(vehicle);
            }

            await Clients.Client(ride.PassengerConnectionId).SendAsync("RideCompleted",
                CreateRideCompletedPayload(driver, totalKm, totalCost));

            await Clients.Caller.SendAsync("RideCompleted",
                CreateRideCompletedPayload(driver, totalKm, totalCost));
        }

        private async Task CleanupDriver(string connectionId)
        {
            if (OnlineDriversStore.PendingRequests.TryRemove(connectionId, out var request))
            {
                request.CancellationTokenSource.Cancel();

                if (OnlineDriversStore.Drivers.TryGetValue(connectionId, out var driver))
                    driver.IsBusy = false;

                await Clients.Client(request.PassengerConnectionId)
                    .SendAsync("RequestFailed",
                        CreateMessagePayload("Driver went offline."));
            }

            OnlineDriversStore.Drivers.TryRemove(connectionId, out _);
        }

        private DriverLocation CreateDriverLocation(string connectionId, string driverName, string vehicleModel,
            double latitude, double longitude, int availableSeats, decimal ratePerKm)
        {
            return new DriverLocation
            {
                ConnectionId = connectionId,
                DriverName = driverName,
                VehicleModel = vehicleModel,
                Latitude = latitude,
                Longitude = longitude,
                LastUpdated = DateTime.UtcNow,
                AvailableSeats = availableSeats,
                RatePerKm = ratePerKm
            };
        }

        private PendingRequest CreatePendingRequest(string passengerConnectionId, string passengerId,
            string passengerName, string passengerPin,
            double pickupLat, double pickupLng,
            double dropLat, double dropLng,
            string pickupName, string dropName,
            CancellationTokenSource cancellationTokenSource)
        {
            return new PendingRequest
            {
                PassengerConnectionId = passengerConnectionId,
                PassengerId = passengerId,
                PassengerName = passengerName,
                PassengerPin = passengerPin,
                PickupLat = pickupLat,
                PickupLng = pickupLng,
                DropLat = dropLat,
                DropLng = dropLng,
                PickupName = pickupName,
                DropName = dropName,
                CancellationTokenSource = cancellationTokenSource
            };
        }

        private ActiveRide CreateActiveRide(string driverConnectionId, PendingRequest request, string driverId)
        {
            return new ActiveRide
            {
                DriverConnectionId = driverConnectionId,
                PassengerConnectionId = request.PassengerConnectionId,
                PassengerId = request.PassengerId,
                DriverId = driverId,
                PassengerPin = request.PassengerPin,
                PickupLat = request.PickupLat,
                PickupLng = request.PickupLng,
                DropLat = request.DropLat,
                DropLng = request.DropLng,
                PickupName = request.PickupName,
                DropName = request.DropName
            };
        }

        private object CreateDriverOnlinePayload(string driverId, string driverName, string vehicleModel,
            double latitude, double longitude, int availableSeats)
        {
            return new
            {
                DriverId = driverId,
                DriverName = driverName,
                VehicleModel = vehicleModel,
                Latitude = latitude,
                Longitude = longitude,
                AvailableSeats = availableSeats
            };
        }

        private object CreateLocationPayload(string driverId, double latitude, double longitude)
        {
            return new
            {
                DriverId = driverId,
                Latitude = latitude,
                Longitude = longitude
            };
        }

        private object CreateRideRequestPayload(PendingRequest request)
        {
            return new
            {
                request.PassengerId,
                PassengerConnectionId = request.PassengerConnectionId,
                request.PassengerName,
                request.PickupLat,
                request.PickupLng,
                request.DropLat,
                request.DropLng,
                request.PickupName,
                request.DropName
            };
        }

        private object CreateRideAcceptedPayload(int rideId, string driverConnectionId, DriverLocation? driver)
        {
            return new
            {
                RideId = rideId,
                DriverConnectionId = driverConnectionId,
                DriverName = driver?.DriverName ?? string.Empty,
                VehicleModel = driver?.VehicleModel ?? string.Empty,
                DriverLat = driver?.Latitude ?? 0,
                DriverLng = driver?.Longitude ?? 0
            };
        }

        private object CreateRideConfirmedPayload(int rideId, PendingRequest request)
        {
            return new
            {
                RideId = rideId,
                request.PassengerName,
                request.PassengerConnectionId,
                request.PickupLat,
                request.PickupLng,
                request.DropLat,
                request.DropLng,
                request.PickupName,
                request.DropName
            };
        }

        private object CreateRideCompletedPayload(DriverLocation driver, double totalKm, double totalCost)
        {
            return new
            {
                driver.DriverName,
                driver.RatePerKm,
                TotalKm = totalKm,
                TotalCost = totalCost
            };
        }

        private object CreateMessagePayload(string message)
        {
            return new { Message = message };
        }

        private object CreateDriverIdPayload(string driverId)
        {
            return new { DriverId = driverId };
        }
    }
}