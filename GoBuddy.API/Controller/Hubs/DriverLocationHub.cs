using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using GoBuddy.API.SharedConstants;
using GoBuddy.API.DTOs;

namespace GoBuddy.API.Hubs
{
    public static class OnlineDriversStore
    {
        public static ConcurrentDictionary<string, DriverLocation> Drivers = new();
        public static ConcurrentDictionary<string, PendingRequest> PendingRequests = new();
        public static ConcurrentDictionary<string, ActiveRide> ActiveRides = new();
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

        public CancellationTokenSource RequestCancellationTokenSource { get; set; } = new();
    }

    public class ActiveRide
    {
        public string DriverConnectionId { get; set; } = string.Empty;
        public string PassengerConnectionId { get; set; } = string.Empty;
        public string PassengerId { get; set; } = string.Empty;
        public string PassengerPin { get; set; } = string.Empty;
        public bool PinConfirmed { get; set; } = false;
        public double TotalKm { get; set; }
    }

    public class DriverLocationHub : Hub
    {
        public async Task DriverGoOnline(double latitude, double longitude,
            string driverName, string vehicleModel, int availableSeats, decimal ratePerKm)
        {
            var connectionId = Context.ConnectionId;

            OnlineDriversStore.Drivers[connectionId] = new DriverLocation
            {
                ConnectionId = connectionId,
                DriverName = driverName,
                VehicleModel = vehicleModel,
                Latitude = latitude,
                Longitude = longitude,
                LastUpdated = DateTime.UtcNow,
                IsBusy = false,
                AvailableSeats = availableSeats,
                RatePerKm = ratePerKm
            };

            var payload = GetDriverOnlinePayload(connectionId, driverName, vehicleModel, latitude, longitude, availableSeats);

            await Clients.All.SendAsync(SignalRConstants.DriverOnline, payload);
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

            var payload = GetLocationUpdatedPayload(connectionId, latitude, longitude);

            await Clients.All.SendAsync(SignalRConstants.LocationUpdated, payload);
        }

        public async Task DriverGoOffline()
        {
            var connectionId = Context.ConnectionId;

            var hasActiveRide = OnlineDriversStore.ActiveRides.Values
                .Any(activeRide => activeRide.DriverConnectionId == connectionId);

            if (hasActiveRide)
            {
                await Clients.Caller.SendAsync(SignalRConstants.CannotGoOffline, new
                {
                    Message = "You have an active ride. Cancel the ride before going offline."
                });
                return;
            }

            await CleanupDriver(connectionId);

            await Clients.All.SendAsync(SignalRConstants.DriverOffline, new
            {
                DriverId = connectionId
            });
        }

        public async Task SendRideRequest(RideRequestDto request)
        {
            if (!OnlineDriversStore.Drivers.TryGetValue(request.DriverConnectionId, out var driver) || driver.IsBusy)
            {
                await Clients.Caller.SendAsync(SignalRConstants.RequestFailed, new
                {
                    Message = driver == null
                        ? "Driver is no longer available."
                        : "Driver is busy. Try another driver."
                });
                return;
            }

            driver.IsBusy = true;

            var requestCancellationTokenSource = new CancellationTokenSource();

            var pendingRequest = CreatePendingRequest(request, Context.ConnectionId, requestCancellationTokenSource);

            OnlineDriversStore.PendingRequests[request.DriverConnectionId] = pendingRequest;

            await Clients.Client(request.DriverConnectionId)
                .SendAsync(SignalRConstants.IncomingRideRequest, new
                {
                    PassengerId = request.PassengerId,
                    PassengerConnectionId = Context.ConnectionId,
                    PassengerName = request.PassengerName,
                    PickupLat = request.PickupLatitude,
                    PickupLng = request.PickupLongitude,
                    DropLat = request.DropLatitude,
                    DropLng = request.DropLongitude,
                    PickupName = request.PickupName,
                    DropName = request.DropName
                });

            await Clients.Caller.SendAsync(SignalRConstants.RequestSent,
                new { Message = "Request sent! Waiting for driver..." });

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(AppConstants.DriverResponseTimeoutMilliseconds,
                        requestCancellationTokenSource.Token);

                    if (OnlineDriversStore.PendingRequests.TryRemove(request.DriverConnectionId, out _))
                    {
                        if (OnlineDriversStore.Drivers.TryGetValue(request.DriverConnectionId, out var driverEntry))
                            driverEntry.IsBusy = false;

                        await Clients.Client(pendingRequest.PassengerConnectionId)
                            .SendAsync(SignalRConstants.RequestTimeout,
                                new { Message = "Driver did not respond." });

                        await Clients.Client(request.DriverConnectionId)
                            .SendAsync(SignalRConstants.RequestExpired,
                                new { Message = "Request expired." });
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
                await Clients.Caller.SendAsync(SignalRConstants.Error, new { Message = "No pending request found." });
                return;
            }

            request.RequestCancellationTokenSource.Cancel();

            if (OnlineDriversStore.Drivers.TryGetValue(driverConnectionId, out var driver))
                driver.IsBusy = true;

            double totalKm = Math.Round(
                Math.Sqrt(
                    Math.Pow(request.PickupLat - request.DropLat, AppConstants.SquarePower) +
                    Math.Pow(request.PickupLng - request.DropLng, AppConstants.SquarePower)
                ) * AppConstants.KmConversionFactor,
                AppConstants.DistanceRoundingPrecision);

            var rideId = Guid.NewGuid().ToString();

            var activeRide = CreateActiveRide(driverConnectionId, request, totalKm);

            OnlineDriversStore.ActiveRides[rideId] = activeRide;

            await Clients.Client(request.PassengerConnectionId).SendAsync(SignalRConstants.RideAccepted, new
            {
                RideId = rideId,
                DriverConnectionId = driverConnectionId,
                DriverName = driver?.DriverName ?? string.Empty,
                VehicleModel = driver?.VehicleModel ?? string.Empty,
                DriverLat = driver?.Latitude ?? 0,
                DriverLng = driver?.Longitude ?? 0
            });

            await Clients.Caller.SendAsync(SignalRConstants.RideConfirmed);
        }

        public async Task RejectRideRequest()
        {
            var driverConnectionId = Context.ConnectionId;

            if (!OnlineDriversStore.PendingRequests.TryRemove(driverConnectionId, out var request))
                return;

            request.RequestCancellationTokenSource.Cancel();

            if (OnlineDriversStore.Drivers.TryGetValue(driverConnectionId, out var driver))
                driver.IsBusy = false;

            await Clients.Client(request.PassengerConnectionId)
                .SendAsync(SignalRConstants.RideRejected,
                    new { Message = "Driver declined your request." });
        }

        public async Task CancelRide(string rideId, string cancelledBy)
        {
            if (!OnlineDriversStore.ActiveRides.TryGetValue(rideId, out var ride))
            {
                await Clients.Caller.SendAsync(SignalRConstants.CancelError,
                    new { Message = "Ride not found." });
                return;
            }

            if (ride.PinConfirmed)
            {
                await Clients.Caller.SendAsync(SignalRConstants.CancelError,
                    new { Message = "Ride cannot be cancelled after PIN is confirmed." });
                return;
            }

            OnlineDriversStore.ActiveRides.TryRemove(rideId, out _);

            if (OnlineDriversStore.Drivers.TryGetValue(ride.DriverConnectionId, out var driver))
                driver.IsBusy = false;

            var payload = new { Message = $"Ride cancelled by {cancelledBy}.", CancelledBy = cancelledBy };

            await Clients.Client(ride.DriverConnectionId).SendAsync(SignalRConstants.RideCancelled, payload);
            await Clients.Client(ride.PassengerConnectionId).SendAsync(SignalRConstants.RideCancelled, payload);
        }

        private async Task CleanupDriver(string connectionId)
        {
            if (OnlineDriversStore.PendingRequests.TryRemove(connectionId, out var request))
            {
                request.RequestCancellationTokenSource.Cancel();

                if (OnlineDriversStore.Drivers.TryGetValue(connectionId, out var driver))
                    driver.IsBusy = false;

                await Clients.Client(request.PassengerConnectionId)
                    .SendAsync(SignalRConstants.RequestFailed,
                        new { Message = "Driver went offline." });
            }

            OnlineDriversStore.Drivers.TryRemove(connectionId, out _);
        }

    
        private object GetDriverOnlinePayload(string connectionId, string driverName, string vehicleModel,
            double latitude, double longitude, int availableSeats)
        {
            return new
            {
                DriverId = connectionId,
                DriverName = driverName,
                VehicleModel = vehicleModel,
                Latitude = latitude,
                Longitude = longitude,
                AvailableSeats = availableSeats
            };
        }

        private object GetLocationUpdatedPayload(string connectionId, double latitude, double longitude)
        {
            return new
            {
                DriverId = connectionId,
                Latitude = latitude,
                Longitude = longitude
            };
        }

        private PendingRequest CreatePendingRequest(RideRequestDto request, string connectionId, CancellationTokenSource cts)
        {
            return new PendingRequest
            {
                PassengerConnectionId = connectionId,
                PassengerId = request.PassengerId,
                PassengerName = request.PassengerName,
                PassengerPin = request.PassengerPin,
                PickupLat = request.PickupLatitude,
                PickupLng = request.PickupLongitude,
                DropLat = request.DropLatitude,
                DropLng = request.DropLongitude,
                PickupName = request.PickupName,
                DropName = request.DropName,
                RequestCancellationTokenSource = cts
            };
        }

        private ActiveRide CreateActiveRide(string driverConnectionId, PendingRequest request, double totalKm)
        {
            return new ActiveRide
            {
                DriverConnectionId = driverConnectionId,
                PassengerConnectionId = request.PassengerConnectionId,
                PassengerId = request.PassengerId,
                PassengerPin = request.PassengerPin,
                PinConfirmed = false,
                TotalKm = totalKm
            };
        }
    }
}