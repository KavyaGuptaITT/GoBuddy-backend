using GoBuddy.Application.Interfaces;
using GoBuddy.Domain.Entities;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using GoBuddy.API.SharedConstants;

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
        public CancellationTokenSource CancellationToken { get; set; } = new();
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
        private readonly IServiceScopeFactory _scopeFactory;

        public DriverLocationHub(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }
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

            await Clients.All.SendAsync("DriverOnline", new
            {
                DriverId = connectionId,
                DriverName = driverName,
                VehicleModel = vehicleModel,
                Latitude = latitude,
                Longitude = longitude,
                AvailableSeats = availableSeats
            });
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

            await Clients.All.SendAsync("LocationUpdated", new
            {
                DriverId = connectionId,
                Latitude = latitude,
                Longitude = longitude
            });
        }

        public async Task DriverGoOffline()
        {
            var connectionId = Context.ConnectionId;

            var hasActiveRide = OnlineDriversStore.ActiveRides.Values
                .Any(ride => ride.DriverConnectionId == connectionId);

            if (hasActiveRide)
            {
                await Clients.Caller.SendAsync("CannotGoOffline", new
                {
                    Message = "You have an active ride. Cancel the ride before going offline."
                });
                return;
            }

            await CleanupDriver(connectionId);
            await Clients.All.SendAsync("DriverOffline", new { DriverId = connectionId });
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
                await Clients.Caller.SendAsync("RequestFailed", new
                {
                    Message = driver == null
                        ? "Driver is no longer available."
                        : "Driver is busy. Try another driver."
                });
                return;
            }

            driver.IsBusy = true;

            var CancellationToken = new CancellationTokenSource();
            var pendingRequest = new PendingRequest
            {
                PassengerConnectionId = Context.ConnectionId,
                PassengerId = passengerId,
                PassengerName = passengerName,
                PassengerPin = passengerPin,
                PickupLat = pickupLat,
                PickupLng = pickupLng,
                DropLat = dropLat,
                DropLng = dropLng,
                PickupName = pickupName,
                DropName = dropName,
                CancellationToken = CancellationToken
            };

            OnlineDriversStore.PendingRequests[driverConnectionId] = pendingRequest;

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
                DropName = dropName
            });

            await Clients.Caller.SendAsync("RequestSent", new { Message = "Request sent! Waiting for driver..." });

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(AppConstants.DriverResponseTime, CancellationToken.Token);

                    if (OnlineDriversStore.PendingRequests.TryRemove(driverConnectionId, out _))
                    {
                        if (OnlineDriversStore.Drivers.TryGetValue(driverConnectionId, out var driver))
                            driver.IsBusy = false;

                        await Clients.Client(pendingRequest.PassengerConnectionId)
                            .SendAsync("RequestTimeout", new { Message = "Driver did not respond." });

                        await Clients.Client(driverConnectionId)
                            .SendAsync("RequestExpired", new { Message = "Request expired." });
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
                await Clients.Caller.SendAsync("Error", new { Message = "No pending request found." });
                return;
            }

            request.CancellationToken.Cancel();

            if (OnlineDriversStore.Drivers.TryGetValue(driverConnectionId, out var driver))
                driver.IsBusy = true;

            var rideId = Guid.NewGuid().ToString();
            var driverUserId = Context.User?
                .FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
            OnlineDriversStore.ActiveRides[rideId] = new ActiveRide
            {
                DriverConnectionId = driverConnectionId,
                PassengerConnectionId = request.PassengerConnectionId,
                PassengerId = request.PassengerId,
                DriverId = driverUserId,
                PassengerPin = request.PassengerPin,
                PinConfirmed = false,
                PinAttempts = 0,
                TotalKm = 0,
                PickupLat = request.PickupLat,
                PickupLng = request.PickupLng,
                DropLat = request.DropLat,
                DropLng = request.DropLng,
                PickupName = request.PickupName,
                DropName = request.DropName
            };

            await Clients.Client(request.PassengerConnectionId).SendAsync("RideAccepted", new
            {
                RideId = rideId,
                DriverConnectionId = driverConnectionId,
                DriverName = driver?.DriverName ?? "",
                VehicleModel = driver?.VehicleModel ?? "",
                DriverLat = driver?.Latitude ?? 0,
                DriverLng = driver?.Longitude ?? 0
            });

            await Clients.Caller.SendAsync("RideConfirmed", new
            {
                RideId = rideId,
                PassengerName = request.PassengerName,
                PassengerConnectionId = request.PassengerConnectionId,
                PickupLat = request.PickupLat,
                PickupLng = request.PickupLng,
                DropLat = request.DropLat,
                DropLng = request.DropLng,
                PickupName = request.PickupName,
                DropName = request.DropName
            });
        }

        public async Task RejectRideRequest()
        {
            var driverConnectionId = Context.ConnectionId;

            if (!OnlineDriversStore.PendingRequests.TryRemove(driverConnectionId, out var request))
                return;

            request.CancellationToken.Cancel();

            if (OnlineDriversStore.Drivers.TryGetValue(driverConnectionId, out var driver))
                driver.IsBusy = false;

            await Clients.Client(request.PassengerConnectionId).SendAsync("RideRejected", new
            {
                Message = "Driver declined your request."
            });
        }

        public async Task ConfirmPin(string rideId, string enteredPin)
        {
            try
            {
                if (!OnlineDriversStore.ActiveRides.TryGetValue(rideId, out var ride))
                {
                    await Clients.Caller.SendAsync("PinError", new { Message = "Ride not found." });
                    return;
                }

                if (ride.PinConfirmed)
                {
                    await Clients.Caller.SendAsync("PinError", new { Message = "PIN already confirmed." });
                    return;
                }

                if (ride.PassengerPin != enteredPin)
                {
                    ride.PinAttempts++;
                    int attemptsLeft = AppConstants.PinAttempt - ride.PinAttempts;

                    if (ride.PinAttempts >= AppConstants.PinAttempt)
                    {
                        OnlineDriversStore.ActiveRides.TryRemove(rideId, out _);

                        if (OnlineDriversStore.Drivers.TryGetValue(ride.DriverConnectionId, out var driver))
                            driver.IsBusy = false;

                        var cancelPayload = new
                        {
                            Message = "Ride cancelled: too many incorrect PIN attempts.",
                            CancelledBy = "System"
                        };

                        await Clients.Client(ride.PassengerConnectionId)
                            .SendAsync("RideCancelled", cancelPayload);
                        await Clients.Caller.SendAsync("RideCancelled", cancelPayload);
                        return;
                    }

                    await Clients.Caller.SendAsync("PinError", new
                    {
                        Message = $"Incorrect PIN. {attemptsLeft} attempt{(attemptsLeft == 1 ? "" : "s")} left."
                    });
                    return;
                }

                ride.PinConfirmed = true;

                
                if (OnlineDriversStore.Drivers.TryGetValue(ride.DriverConnectionId, out var seatDriver))
                {
                    if (seatDriver.AvailableSeats > 0)
                        seatDriver.AvailableSeats--;

                    await Clients.All.SendAsync("SeatsUpdated", new
                    {
                        DriverId = ride.DriverConnectionId,
                        AvailableSeats = seatDriver.AvailableSeats
                    });
                }

                await Clients.Client(ride.PassengerConnectionId)
                    .SendAsync("PinConfirmed", new { Message = "PIN confirmed!" });
                await Clients.Caller.SendAsync("PinConfirmed", new { Message = "PIN confirmed!" });


            }
            catch (Exception exception)
            {
                await Clients.Caller.SendAsync("PinError", new { Message = exception.Message });
            }
        }

        public async Task CancelRide(string rideId, string cancelledBy)
        {
            if (!OnlineDriversStore.ActiveRides.TryGetValue(rideId, out var ride))
            {
                await Clients.Caller.SendAsync("CancelError", new { Message = "Ride not found." });
                return;
            }

            if (ride.PinConfirmed)
            {
                await Clients.Caller.SendAsync("CancelError", new
                {
                    Message = "Ride cannot be cancelled after PIN is confirmed."
                });
                return;
            }

            OnlineDriversStore.ActiveRides.TryRemove(rideId, out _);

            if (OnlineDriversStore.Drivers.TryGetValue(ride.DriverConnectionId, out var driver))
                driver.IsBusy = false;

            var payload = new { Message = "Ride Cancelled" };

            await Clients.Client(ride.DriverConnectionId).SendAsync("RideCancelled", payload);
            await Clients.Client(ride.PassengerConnectionId).SendAsync("RideCancelled", payload);
        }

        public async Task DriverArrivedAtPickup(string rideId)
        {
            if (!OnlineDriversStore.ActiveRides.TryGetValue(rideId, out var ride)) return;

            await Clients.Client(ride.PassengerConnectionId).SendAsync("DriverArrived", new
            {
                Message = "Driver has arrived at your pickup location!"
            });

            await Clients.Caller.SendAsync("DriverArrived", new
            {
                Message = "You have arrived at pickup. Waiting for PIN confirmation."
            });
        }

        public async Task RideCompleted(string rideId, double totalKm)
        {
            if (!OnlineDriversStore.ActiveRides.TryGetValue(rideId, out var ride)) return;
            if (!OnlineDriversStore.Drivers.TryGetValue(ride.DriverConnectionId, out var driver)) return;

            OnlineDriversStore.ActiveRides.TryRemove(rideId, out _);
            driver.IsBusy = false;

            double totalCost = Math.Round(totalKm * (double)driver.RatePerKm, 0);
            using var scope = _scopeFactory.CreateScope();
            var rideRepo = scope.ServiceProvider.GetRequiredService<IRideRepository>();
            var vehicleRepo = scope.ServiceProvider.GetRequiredService<IVehicleRepository>();

            int driverUserId = int.Parse(ride.DriverId);
            var session = new RideSession(driverUserId);
            session.Complete();
            int sessionId = await rideRepo.CreateSessionAsync(session);

            await rideRepo.CreateRequestAsync(new RideRequest(
                sessionId,
                int.Parse(ride.PassengerId),
                ride.PickupLat, ride.PickupLng,
                ride.DropLat, ride.DropLng,
                ride.PickupName, ride.DropName,
                totalKm, (decimal)totalCost
            ));

            var vehicle = await vehicleRepo.GetByDriverIdAsync(driverUserId);
            if (vehicle != null)
            {
                vehicle.ResetAvailableSeats();
                await vehicleRepo.UpdateAsync(vehicle);
            }

            await Clients.Client(ride.PassengerConnectionId).SendAsync("RideCompleted", new
            {
                DriverName = driver.DriverName,
                RatePerKm = driver.RatePerKm,
                TotalKm = totalKm,
                TotalCost = totalCost
            });

            await Clients.Caller.SendAsync("RideCompleted", new
            {
                TotalKm = totalKm,
                RatePerKm = driver.RatePerKm,
                TotalCost = totalCost
            });
        }

        public async Task PassengerLeft(string passengerId)
        {
            var rideEntry = OnlineDriversStore.ActiveRides
                .FirstOrDefault(ride => ride.Value.PassengerId == passengerId);

            if (rideEntry.Value != null && !rideEntry.Value.PinConfirmed)
                await CancelRide(rideEntry.Key, "Passenger");

            var driverEntry = OnlineDriversStore.Drivers
                .FirstOrDefault(driver => driver.Value.IsBusy);
            if (driverEntry.Value != null)
                driverEntry.Value.IsBusy = false;
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var connectionId = Context.ConnectionId;

            var rideAsDriver = OnlineDriversStore.ActiveRides
                .FirstOrDefault(ride => ride.Value.DriverConnectionId == connectionId);

            if (rideAsDriver.Value != null && !rideAsDriver.Value.PinConfirmed)
            {
                OnlineDriversStore.ActiveRides.TryRemove(rideAsDriver.Key, out _);
                if (OnlineDriversStore.Drivers.TryGetValue(connectionId, out var driver))
                    driver.IsBusy = false;
                await Clients.Client(rideAsDriver.Value.PassengerConnectionId)
                    .SendAsync("RideCancelled", new { Message = "Driver disconnected.", CancelledBy = "Driver" });
            }

            await CleanupDriver(connectionId);
            await Clients.All.SendAsync("DriverOffline", new { DriverId = connectionId });
            await base.OnDisconnectedAsync(exception);
        }

        private async Task CleanupDriver(string connectionId)
        {
            if (OnlineDriversStore.PendingRequests.TryRemove(connectionId, out var request))
            {
                request.CancellationToken.Cancel();
                if (OnlineDriversStore.Drivers.TryGetValue(connectionId, out var driver))
                    driver.IsBusy = false;
                await Clients.Client(request.PassengerConnectionId)
                    .SendAsync("RequestFailed", new { Message = "Driver went offline." });
            }

            OnlineDriversStore.Drivers.TryRemove(connectionId, out _);
        }
    }
}


