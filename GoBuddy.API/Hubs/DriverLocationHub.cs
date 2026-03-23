using GoBuddy.Application.Interfaces;
using GoBuddy.Domain.Entities;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace GoBuddy.API.Hubs
{
    public static class OnlineDriversStore
    {
        public static ConcurrentDictionary<string, DriverLocation> Drivers = new();
        public static ConcurrentDictionary<string, ConcurrentQueue<PendingRequest>> PendingRequests = new();
        public static ConcurrentDictionary<string, ActiveRide> ActiveRides = new();
        public static ConcurrentDictionary<string, byte> BusyPassengers = new();
    }

    public class DriverLocation
    {
        public string ConnectionId { get; set; } = "";
        public string DriverUserId { get; set; } = "";
        public string DriverName { get; set; } = "";
        public string VehicleModel { get; set; } = "";
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime LastUpdated { get; set; }
        public int AvailableSeats { get; set; }
        public decimal RatePerKm { get; set; }
        public string Phone { get; set; } = "";
        public string VehicleNo { get; set; } = "";
    }

    public class PendingRequest
    {
        public string PassengerConnectionId { get; set; } = "";
        public string PassengerId { get; set; } = "";
        public string PassengerName { get; set; } = "";
        public string PassengerPin { get; set; } = "";
        public double PickupLat { get; set; }
        public double PickupLng { get; set; }
        public double DropLat { get; set; }
        public double DropLng { get; set; }
        public string PickupName { get; set; } = "";
        public string DropName { get; set; } = "";
        public CancellationTokenSource CancellationSource { get; set; } = new();
    }

    public class ActiveRide
    {
        public string DriverConnectionId { get; set; } = "";
        public string PassengerConnectionId { get; set; } = "";
        public string PassengerId { get; set; } = "";
        public string DriverId { get; set; } = "";
        public int RideSessionId { get; set; }
        public string PassengerPin { get; set; } = "";
        public bool PinConfirmed { get; set; } = false;
        public int PinAttempts { get; set; } = 0;
        public double PickupLat { get; set; }
        public double PickupLng { get; set; }
        public double DropLat { get; set; }
        public double DropLng { get; set; }
        public string PickupName { get; set; } = "";
        public string DropName { get; set; } = "";
        public bool DroppedOff { get; set; } = false;
        public double FixedDistanceKm { get; set; } = 0;
    }

    public class DriverLocationHub : Hub
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public DriverLocationHub(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task DriverGoOnline(
            double latitude, double longitude,
            string driverName, string vehicleModel,
            int availableSeats, decimal ratePerKm,
            string phone, string vehicleNo)
        {
            var connectionId = Context.ConnectionId;
            var driverUserId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

            OnlineDriversStore.Drivers[connectionId] = new DriverLocation
            {
                ConnectionId = connectionId,
                DriverUserId = driverUserId,
                DriverName = driverName,
                VehicleModel = vehicleModel,
                Latitude = latitude,
                Longitude = longitude,
                LastUpdated = DateTime.UtcNow,
                AvailableSeats = availableSeats,
                RatePerKm = ratePerKm,
                Phone = phone,
                VehicleNo = vehicleNo
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
            if (!OnlineDriversStore.Drivers.TryGetValue(connectionId, out var driver)) return;

            driver.Latitude = latitude;
            driver.Longitude = longitude;
            driver.LastUpdated = DateTime.UtcNow;

            await Clients.All.SendAsync("LocationUpdated", new
            {
                DriverId = connectionId,
                Latitude = latitude,
                Longitude = longitude,
                AvailableSeats = driver.AvailableSeats
            });
        }

        public async Task DriverGoOffline()
        {
            var connectionId = Context.ConnectionId;

            bool driverHasActiveRide = OnlineDriversStore.ActiveRides.Values
                .Any(r => r.DriverConnectionId == connectionId);

            if (driverHasActiveRide)
            {
                await Clients.Caller.SendAsync("CannotGoOffline",
                    new { Message = "Complete all active rides before going offline." });
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
            var passengerConnectionId = Context.ConnectionId;

            if (OnlineDriversStore.BusyPassengers.ContainsKey(passengerConnectionId))
            {
                await Clients.Caller.SendAsync("RequestFailed",
                    new { Message = "You already have a pending or active request." });
                return;
            }

            if (!OnlineDriversStore.Drivers.TryGetValue(driverConnectionId, out var driver))
            {
                await Clients.Caller.SendAsync("RequestFailed",
                    new { Message = "Driver is no longer available." });
                return;
            }

            if (driver.AvailableSeats <= 0)
            {
                await Clients.Caller.SendAsync("RequestFailed",
                    new { Message = "No seats available with this driver." });
                return;
            }

            bool isAnyPassengerBoarded = OnlineDriversStore.ActiveRides.Values
                .Any(r => r.DriverConnectionId == driverConnectionId && r.PinConfirmed);

            bool hasAnyActiveRide = OnlineDriversStore.ActiveRides.Values
                .Any(r => r.DriverConnectionId == driverConnectionId);

            if (hasAnyActiveRide && !isAnyPassengerBoarded)
            {
                await Clients.Caller.SendAsync("RequestFailed",
                    new { Message = "Driver is busy picking up another passenger. Try again shortly." });
                return;
            }

            double distanceFromDriver = GetDistanceKm(
                driver.Latitude, driver.Longitude, pickupLat, pickupLng);
            if (distanceFromDriver > 3)
            {
                await Clients.Caller.SendAsync("RequestFailed",
                    new { Message = "You are too far from the driver (max 3 km)." });
                return;
            }

            if (isAnyPassengerBoarded)
            {
                var boardedRide = OnlineDriversStore.ActiveRides.Values
                    .First(r => r.DriverConnectionId == driverConnectionId && r.PinConfirmed);

                double distanceFromDestination = GetDistanceKm(
                    boardedRide.DropLat, boardedRide.DropLng, dropLat, dropLng);
                if (distanceFromDestination > 3)
                {
                    await Clients.Caller.SendAsync("RequestFailed",
                        new { Message = "Your destination is too far from the driver's current route." });
                    return;
                }
            }

            OnlineDriversStore.BusyPassengers.TryAdd(passengerConnectionId, 0);

            var cancellationSource = new CancellationTokenSource();
            var pendingRequest = new PendingRequest
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
                CancellationSource = cancellationSource
            };

            var requestQueue = OnlineDriversStore.PendingRequests
                .GetOrAdd(driverConnectionId, _ => new ConcurrentQueue<PendingRequest>());
            requestQueue.Enqueue(pendingRequest);

            await Clients.Client(driverConnectionId).SendAsync("IncomingRideRequest", new
            {
                PassengerId = passengerId,
                PassengerConnectionId = passengerConnectionId,
                PassengerName = passengerName,
                PickupLat = pickupLat,
                PickupLng = pickupLng,
                DropLat = dropLat,
                DropLng = dropLng,
                PickupName = pickupName,
                DropName = dropName
            });

            await Clients.Caller.SendAsync("RequestSent",
                new { Message = "Request sent! Waiting for driver response..." });

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(20000, cancellationSource.Token);
                    RemoveFromQueue(driverConnectionId, passengerConnectionId);
                    OnlineDriversStore.BusyPassengers.TryRemove(passengerConnectionId, out _);
                    await Clients.Client(passengerConnectionId)
                        .SendAsync("RequestTimeout", new { Message = "Driver did not respond in time." });
                    await Clients.Client(driverConnectionId)
                        .SendAsync("RequestExpired", new { Message = "A ride request has expired." });
                }
                catch (TaskCanceledException) { }
            });
        }

        public async Task AcceptRideRequest()
        {
            var driverConnectionId = Context.ConnectionId;

            if (!OnlineDriversStore.Drivers.TryGetValue(driverConnectionId, out var driver))
                return;

            if (!OnlineDriversStore.PendingRequests.TryGetValue(driverConnectionId, out var requestQueue)
                || requestQueue.IsEmpty)
            {
                await Clients.Caller.SendAsync("Error", new { Message = "No pending requests." });
                return;
            }

            if (!requestQueue.TryDequeue(out var acceptedRequest))
            {
                await Clients.Caller.SendAsync("Error", new { Message = "No pending requests." });
                return;
            }

            OnlineDriversStore.PendingRequests[driverConnectionId] = requestQueue;

            acceptedRequest.CancellationSource.Cancel();
            OnlineDriversStore.BusyPassengers.TryRemove(acceptedRequest.PassengerConnectionId, out _);

            using var dependencyScope = _scopeFactory.CreateScope();
            var rideRepository = dependencyScope.ServiceProvider.GetRequiredService<IRideRepository>();
            var vehicleRepository = dependencyScope.ServiceProvider.GetRequiredService<IVehicleRepository>();
            int parsedDriverUserId = int.Parse(driver.DriverUserId);
            int activeSessionId;

            var existingSession = await rideRepository.GetActiveSessionByDriverIdAsync(parsedDriverUserId);
            if (existingSession != null)
            {
                activeSessionId = existingSession.RideSessionId;
            }
            else
            {
                var vehicle = await vehicleRepository.GetByDriverIdAsync(parsedDriverUserId);
                var newSession = new RideSession(
                    parsedDriverUserId, vehicle!.VehicleId,
                    acceptedRequest.PickupName, acceptedRequest.PickupLat, acceptedRequest.PickupLng,
                    acceptedRequest.DropName, acceptedRequest.DropLat, acceptedRequest.DropLng
                );
                activeSessionId = await rideRepository.CreateSessionAsync(newSession);
            }

            var newRideId = Guid.NewGuid().ToString();
            OnlineDriversStore.ActiveRides[newRideId] = new ActiveRide
            {
                DriverConnectionId = driverConnectionId,
                PassengerConnectionId = acceptedRequest.PassengerConnectionId,
                PassengerId = acceptedRequest.PassengerId,
                DriverId = driver.DriverUserId,
                RideSessionId = activeSessionId,
                PassengerPin = acceptedRequest.PassengerPin,
                PinConfirmed = false,
                PinAttempts = 0,
                PickupLat = acceptedRequest.PickupLat,
                PickupLng = acceptedRequest.PickupLng,
                DropLat = acceptedRequest.DropLat,
                DropLng = acceptedRequest.DropLng,
                PickupName = acceptedRequest.PickupName,
                DropName = acceptedRequest.DropName,
                FixedDistanceKm = 0
            };

            await Clients.Client(acceptedRequest.PassengerConnectionId).SendAsync("RideAccepted", new
            {
                RideId = newRideId,
                DriverConnectionId = driverConnectionId,
                DriverName = driver.DriverName,
                VehicleModel = driver.VehicleModel,
                DriverLat = driver.Latitude,
                DriverLng = driver.Longitude,
                RatePerKm = driver.RatePerKm,
                Phone = driver.Phone,
                VehicleNo = driver.VehicleNo
            });

            await Clients.Caller.SendAsync("RideConfirmed", new
            {
                RideId = newRideId,
                PassengerName = acceptedRequest.PassengerName,
                PassengerConnectionId = acceptedRequest.PassengerConnectionId,
                PickupLat = acceptedRequest.PickupLat,
                PickupLng = acceptedRequest.PickupLng,
                DropLat = acceptedRequest.DropLat,
                DropLng = acceptedRequest.DropLng,
                PickupName = acceptedRequest.PickupName,
                DropName = acceptedRequest.DropName
            });

            var boardedPassengers = OnlineDriversStore.ActiveRides.Values
                .Where(r => r.DriverConnectionId == driverConnectionId
                         && r.PassengerConnectionId != acceptedRequest.PassengerConnectionId
                         && r.PinConfirmed)
                .ToList();

            foreach (var boardedRide in boardedPassengers)
            {
                await Clients.Client(boardedRide.PassengerConnectionId)
                    .SendAsync("NewPassengerJoined", new
                    {
                        Message = $"A new passenger ({acceptedRequest.PassengerName}) is joining at {acceptedRequest.PickupName}.",
                        PassengerName = acceptedRequest.PassengerName,
                        PickupName = acceptedRequest.PickupName,
                        PickupLat = acceptedRequest.PickupLat,
                        PickupLng = acceptedRequest.PickupLng,
                        DriverLat = driver.Latitude,
                        DriverLng = driver.Longitude
                    });
            }

            if (!requestQueue.IsEmpty && requestQueue.TryPeek(out var nextQueuedRequest))
            {
                await Clients.Caller.SendAsync("IncomingRideRequest", new
                {
                    nextQueuedRequest.PassengerId,
                    nextQueuedRequest.PassengerConnectionId,
                    nextQueuedRequest.PassengerName,
                    nextQueuedRequest.PickupLat,
                    nextQueuedRequest.PickupLng,
                    nextQueuedRequest.DropLat,
                    nextQueuedRequest.DropLng,
                    nextQueuedRequest.PickupName,
                    nextQueuedRequest.DropName
                });
            }
        }

        public async Task RejectRideRequest()
        {
            var driverConnectionId = Context.ConnectionId;

            if (!OnlineDriversStore.PendingRequests.TryGetValue(driverConnectionId, out var requestQueue)
                || !requestQueue.TryDequeue(out var rejectedRequest))
                return;

            rejectedRequest.CancellationSource.Cancel();
            OnlineDriversStore.BusyPassengers.TryRemove(rejectedRequest.PassengerConnectionId, out _);

            await Clients.Client(rejectedRequest.PassengerConnectionId).SendAsync("RideRejected",
                new { Message = "Driver declined your request." });

            if (requestQueue.TryPeek(out var nextQueuedRequest))
            {
                await Clients.Caller.SendAsync("IncomingRideRequest", new
                {
                    nextQueuedRequest.PassengerId,
                    nextQueuedRequest.PassengerConnectionId,
                    nextQueuedRequest.PassengerName,
                    nextQueuedRequest.PickupLat,
                    nextQueuedRequest.PickupLng,
                    nextQueuedRequest.DropLat,
                    nextQueuedRequest.DropLng,
                    nextQueuedRequest.PickupName,
                    nextQueuedRequest.DropName
                });
            }
        }

        public async Task ConfirmPin(string rideId, string enteredPin)
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
                int attemptsLeft = 3 - ride.PinAttempts;

                if (ride.PinAttempts >= 3)
                {
                    OnlineDriversStore.ActiveRides.TryRemove(rideId, out _);

                    if (OnlineDriversStore.Drivers.TryGetValue(ride.DriverConnectionId, out var driverOnFailedPin))
                    {
                        driverOnFailedPin.AvailableSeats++;
                        await Clients.All.SendAsync("DriverSeatsUpdated",
                            new { DriverId = ride.DriverConnectionId, AvailableSeats = driverOnFailedPin.AvailableSeats });
                    }

                    var cancellationPayload = new
                    {
                        Message = "Ride cancelled: too many wrong PIN attempts.",
                        CancelledBy = "System",
                        RideId = rideId
                    };
                    await Clients.Client(ride.PassengerConnectionId).SendAsync("RideCancelled", cancellationPayload);
                    await Clients.Caller.SendAsync("RideCancelled", cancellationPayload);
                    return;
                }

                await Clients.Caller.SendAsync("PinError",
                    new { Message = $"Wrong PIN. {attemptsLeft} attempt{(attemptsLeft == 1 ? "" : "s")} left." });
                return;
            }

            ride.PinConfirmed = true;

            if (!OnlineDriversStore.Drivers.TryGetValue(ride.DriverConnectionId, out var driver))
                return;

            if (driver.AvailableSeats > 0) driver.AvailableSeats--;

            if (driver.AvailableSeats <= 0)
                await Clients.All.SendAsync("DriverSeatsFull",
                    new { DriverId = ride.DriverConnectionId });
            else
                await Clients.All.SendAsync("DriverSeatsUpdated",
                    new { DriverId = ride.DriverConnectionId, AvailableSeats = driver.AvailableSeats });

            await Clients.Client(ride.PassengerConnectionId)
                .SendAsync("PinConfirmed", new { Message = "PIN confirmed! Enjoy your ride." });
            await Clients.Caller
                .SendAsync("PinConfirmed", new { Message = "PIN confirmed! Passenger boarded." });

            var nearestUnboardedPassenger = OnlineDriversStore.ActiveRides.Values
                .Where(r => r.DriverConnectionId == ride.DriverConnectionId
                         && r.PassengerConnectionId != ride.PassengerConnectionId
                         && !r.PinConfirmed)
                .OrderBy(r => GetDistanceKm(driver.Latitude, driver.Longitude, r.PickupLat, r.PickupLng))
                .FirstOrDefault();

            if (nearestUnboardedPassenger != null)
            {
                await Clients.Client(ride.PassengerConnectionId)
                    .SendAsync("DriverPickingUpOther", new
                    {
                        Message = $"Driver is picking up one more passenger ({nearestUnboardedPassenger.PickupName}) before heading to destination.",
                        NextPickupName = nearestUnboardedPassenger.PickupName,
                        DriverLat = driver.Latitude,
                        DriverLng = driver.Longitude
                    });
            }
        }

        public async Task DriverArrivedAtPickup(string rideId)
        {
            if (!OnlineDriversStore.ActiveRides.TryGetValue(rideId, out var ride)) return;

            await Clients.Client(ride.PassengerConnectionId).SendAsync("DriverArrived",
                new { Message = "Your driver has arrived at your pickup!" });
            await Clients.Caller.SendAsync("DriverArrived",
                new { Message = "Arrived at pickup. Ask passenger for PIN." });
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
                await Clients.Caller.SendAsync("CancelError",
                    new { Message = "Cannot cancel after PIN is confirmed." });
                return;
            }

            OnlineDriversStore.ActiveRides.TryRemove(rideId, out _);

            if (OnlineDriversStore.Drivers.TryGetValue(ride.DriverConnectionId, out var driver))
            {
                await Clients.All.SendAsync("DriverSeatsUpdated",
                    new { DriverId = ride.DriverConnectionId, AvailableSeats = driver.AvailableSeats });
            }

            var cancellationPayload = new
            {
                Message = $"Ride cancelled by {cancelledBy}.",
                CancelledBy = cancelledBy,
                RideId = rideId
            };
            await Clients.Client(ride.DriverConnectionId).SendAsync("RideCancelled", cancellationPayload);
            await Clients.Client(ride.PassengerConnectionId).SendAsync("RideCancelled", cancellationPayload);
        }

        public async Task RideCompleted(string rideId, double totalKm)
        {
            if (!OnlineDriversStore.ActiveRides.TryGetValue(rideId, out var ride)) return;
            if (!OnlineDriversStore.Drivers.TryGetValue(ride.DriverConnectionId, out var driver)) return;

            OnlineDriversStore.ActiveRides.TryRemove(rideId, out _);

            double totalCost = Math.Round(totalKm * (double)driver.RatePerKm, 0);

            using var dependencyScope = _scopeFactory.CreateScope();
            var rideRepository = dependencyScope.ServiceProvider.GetRequiredService<IRideRepository>();
            var vehicleRepository = dependencyScope.ServiceProvider.GetRequiredService<IVehicleRepository>();
            int parsedDriverUserId = int.Parse(driver.DriverUserId);

            var completedRideRequest = new RideRequest(
                ride.RideSessionId,
                int.Parse(ride.PassengerId),
                ride.PickupName, ride.PickupLat, ride.PickupLng,
                ride.DropName, ride.DropLat, ride.DropLng,
                totalKm, (decimal)totalCost
            );
            completedRideRequest.Complete();
            await rideRepository.CreateRequestAsync(completedRideRequest);

            var rideSession = await rideRepository.GetSessionByIdAsync(ride.RideSessionId);
            if (rideSession != null)
            {
                rideSession.AddFare((decimal)totalCost);
                rideSession.AddDistance(totalKm);
            }

            bool allPassengersDropped = !OnlineDriversStore.ActiveRides.Values
                .Any(r => r.DriverConnectionId == ride.DriverConnectionId);

            if (allPassengersDropped)
            {
                rideSession?.Complete();
                var vehicle = await vehicleRepository.GetByDriverIdAsync(parsedDriverUserId);
                if (vehicle != null)
                {
                    vehicle.ResetAvailableSeats();
                    await vehicleRepository.UpdateAsync(vehicle);
                    driver.AvailableSeats = vehicle.AvailableSeats;
                    await Clients.All.SendAsync("DriverSeatsUpdated",
                        new { DriverId = ride.DriverConnectionId, AvailableSeats = driver.AvailableSeats });
                }
            }

            if (rideSession != null) await rideRepository.UpdateSessionAsync(rideSession);

            await Clients.Client(ride.PassengerConnectionId).SendAsync("RideCompleted", new
            {
                DriverName = driver.DriverName,
                TotalKm = totalKm,
                RatePerKm = driver.RatePerKm,
                TotalCost = totalCost
            });

            var remainingActiveRides = OnlineDriversStore.ActiveRides.Values
                .Where(r => r.DriverConnectionId == ride.DriverConnectionId && r.PinConfirmed)
                .ToList();

            if (remainingActiveRides.Any())
            {
                var nextDropRide = remainingActiveRides
                    .OrderBy(r => GetDistanceKm(driver.Latitude, driver.Longitude, r.DropLat, r.DropLng))
                    .First();

                foreach (var remainingRide in remainingActiveRides)
                {
                    await Clients.Client(remainingRide.PassengerConnectionId)
                        .SendAsync("NextDropUpdate", new
                        {
                            NextDropName = nextDropRide.DropName,
                            NextDropLat = nextDropRide.DropLat,
                            NextDropLng = nextDropRide.DropLng,
                            DriverLat = driver.Latitude,
                            DriverLng = driver.Longitude,
                            StopsRemaining = remainingActiveRides.Count
                        });
                }
            }

            await Clients.Caller.SendAsync("RideCompleted", new
            {
                RideId = rideId,
                TotalKm = totalKm,
                RatePerKm = driver.RatePerKm,
                TotalCost = totalCost,
                DriverLat = driver.Latitude,
                DriverLng = driver.Longitude
            });
        }

        public async Task PassengerLeft(string passengerId)
        {
            var rideEntry = OnlineDriversStore.ActiveRides
                .FirstOrDefault(kv => kv.Value.PassengerId == passengerId);
            if (rideEntry.Value != null && !rideEntry.Value.PinConfirmed)
                await CancelRide(rideEntry.Key, "Passenger");
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var connectionId = Context.ConnectionId;

            var ridesAsDriver = OnlineDriversStore.ActiveRides
                .Where(kv => kv.Value.DriverConnectionId == connectionId).ToList();

            foreach (var rideEntry in ridesAsDriver)
            {
                if (!rideEntry.Value.PinConfirmed)
                {
                    OnlineDriversStore.ActiveRides.TryRemove(rideEntry.Key, out _);
                    await Clients.Client(rideEntry.Value.PassengerConnectionId).SendAsync("RideCancelled", new
                    {
                        Message = "Driver disconnected. Ride cancelled.",
                        CancelledBy = "Driver",
                        RideId = rideEntry.Key
                    });
                }
            }

            var rideAsPassenger = OnlineDriversStore.ActiveRides
                .FirstOrDefault(kv => kv.Value.PassengerConnectionId == connectionId);

            if (rideAsPassenger.Value != null && !rideAsPassenger.Value.PinConfirmed)
            {
                OnlineDriversStore.ActiveRides.TryRemove(rideAsPassenger.Key, out _);

                if (OnlineDriversStore.Drivers.TryGetValue(rideAsPassenger.Value.DriverConnectionId, out var driverOfDisconnectedPassenger))
                {
                    driverOfDisconnectedPassenger.AvailableSeats++;
                    await Clients.All.SendAsync("DriverSeatsUpdated",
                        new { DriverId = rideAsPassenger.Value.DriverConnectionId, AvailableSeats = driverOfDisconnectedPassenger.AvailableSeats });
                }

                await Clients.Client(rideAsPassenger.Value.DriverConnectionId).SendAsync("RideCancelled", new
                {
                    Message = "Passenger disconnected.",
                    CancelledBy = "Passenger",
                    RideId = rideAsPassenger.Key
                });
            }

            await CleanupDriver(connectionId);
            await Clients.All.SendAsync("DriverOffline", new { DriverId = connectionId });
            await base.OnDisconnectedAsync(exception);
        }

        private async Task CleanupDriver(string connectionId)
        {
            if (OnlineDriversStore.PendingRequests.TryRemove(connectionId, out var pendingQueue))
            {
                foreach (var pendingRequest in pendingQueue)
                {
                    pendingRequest.CancellationSource.Cancel();
                    OnlineDriversStore.BusyPassengers.TryRemove(pendingRequest.PassengerConnectionId, out _);
                    await Clients.Client(pendingRequest.PassengerConnectionId).SendAsync("RequestFailed",
                        new { Message = "Driver went offline." });
                }
            }
            OnlineDriversStore.Drivers.TryRemove(connectionId, out _);
        }

        private void RemoveFromQueue(string driverConnectionId, string passengerConnectionId)
        {
            if (!OnlineDriversStore.PendingRequests.TryGetValue(driverConnectionId, out var pendingQueue)) return;
            var remainingRequests = new List<PendingRequest>();
            while (pendingQueue.TryDequeue(out var request))
                if (request.PassengerConnectionId != passengerConnectionId) remainingRequests.Add(request);
            OnlineDriversStore.PendingRequests[driverConnectionId] = new ConcurrentQueue<PendingRequest>(remainingRequests);
        }

        private static double GetDistanceKm(double lat1, double lng1, double lat2, double lng2)
        {
            double latDiff = lat2 - lat1;
            double lngDiff = (lng2 - lng1) * Math.Cos(lat1 * Math.PI / 180);
            return Math.Sqrt(latDiff * latDiff + lngDiff * lngDiff) * 111.0;
        }
    }
}