using GoBuddy.Application.Interfaces;
using GoBuddy.Domain.Entities;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Security.Claims;
using GoBuddy.API.SharedConstants;

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
        public string ConnectionId { get; set; } = string.Empty;
        public string DriverUserId { get; set; } = string.Empty;
        public string DriverName { get; set; } = string.Empty;
        public string VehicleModel { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime LastUpdated { get; set; }
        public int AvailableSeats { get; set; }
        public decimal RatePerKm { get; set; }
        public string Phone { get; set; } = string.Empty;
        public string VehicleNo { get; set; } = string.Empty;
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
        public CancellationTokenSource CancellationSource { get; set; } = new();
    }

    public class ActiveRide
    {
        public string DriverConnectionId { get; set; } = string.Empty;
        public string PassengerConnectionId { get; set; } = string.Empty;
        public string PassengerId { get; set; } = string.Empty;
        public string DriverId { get; set; } = string.Empty;
        public int RideSessionId { get; set; }
        public string PassengerPin { get; set; } = string.Empty;
        public bool PinConfirmed { get; set; }
        public int PinAttempts { get; set; }
        public double PickupLat { get; set; }
        public double PickupLng { get; set; }
        public double DropLat { get; set; }
        public double DropLng { get; set; }
        public string PickupName { get; set; } = string.Empty;
        public string DropName { get; set; } = string.Empty;
        public bool DroppedOff { get; set; }
        public double FixedDistanceKm { get; set; }
    }

    public class DriverLocationHub : Hub
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public DriverLocationHub(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task DriverGoOnline(double latitude, double longitude, string driverName, string vehicleModel, int availableSeats, decimal ratePerKm, string phone, string vehicleNo)
        {
            var currentConnectionId = Context.ConnectionId;
            var currentDriverUserId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            using var dependencyScope = _scopeFactory.CreateScope();
            var vehicleRepository = dependencyScope.ServiceProvider.GetRequiredService<IVehicleRepository>();
            int dbAvailableSeats = 0;

            if (!string.IsNullOrEmpty(currentDriverUserId) && int.TryParse(currentDriverUserId, out int parsedDriverId))
            {
                var targetVehicle = await vehicleRepository.GetByDriverIdAsync(parsedDriverId);
                if (targetVehicle != null) dbAvailableSeats = targetVehicle.AvailableSeats;
            }

            OnlineDriversStore.Drivers[currentConnectionId] = new DriverLocation
            {
                ConnectionId = currentConnectionId,
                DriverUserId = currentDriverUserId,
                DriverName = driverName,
                VehicleModel = vehicleModel,
                Latitude = latitude,
                Longitude = longitude,
                LastUpdated = DateTime.UtcNow,
                AvailableSeats = dbAvailableSeats,
                RatePerKm = ratePerKm,
                Phone = phone,
                VehicleNo = vehicleNo
            };

            await Clients.All.SendAsync(SignalRConstants.DriverOnline, new
            {
                DriverId = currentConnectionId,
                DriverName = driverName,
                VehicleModel = vehicleModel,
                Latitude = latitude,
                Longitude = longitude,
                AvailableSeats = dbAvailableSeats
            });
        }

        public async Task UpdateLocation(double latitude, double longitude)
        {
            var activeConnectionId = Context.ConnectionId;
            if (!OnlineDriversStore.Drivers.TryGetValue(activeConnectionId, out var activeDriver)) return;

            activeDriver.Latitude = latitude;
            activeDriver.Longitude = longitude;
            activeDriver.LastUpdated = DateTime.UtcNow;

            await Clients.All.SendAsync(SignalRConstants.LocationUpdated, new
            {
                DriverId = activeConnectionId,
                Latitude = latitude,
                Longitude = longitude,
                AvailableSeats = activeDriver.AvailableSeats
            });
        }

        public async Task DriverGoOffline()
        {
            var activeConnectionId = Context.ConnectionId;
            bool isProcessingActiveRide = OnlineDriversStore.ActiveRides.Values.Any(ride => ride.DriverConnectionId == activeConnectionId);

            if (isProcessingActiveRide)
            {
                await Clients.Caller.SendAsync(SignalRConstants.CannotGoOffline, new { Message = MessageConstants.CannotGoOffline });
                return;
            }

            await CleanupDriverSession(activeConnectionId);
            await Clients.All.SendAsync(SignalRConstants.DriverOffline, new { DriverId = activeConnectionId });
        }

        public async Task SendRideRequest(string driverConnectionId, string passengerId, string passengerName, double pickupLat, double pickupLng, double dropLat, double dropLng, string pickupName, string dropName, string passengerPin)
        {
            var callerConnectionId = Context.ConnectionId;
            if (OnlineDriversStore.BusyPassengers.ContainsKey(callerConnectionId))
            {
                await Clients.Caller.SendAsync(SignalRConstants.RequestFailed, new { Message = MessageConstants.PendingOrActiveRequestExists });
                return;
            }
            if (!OnlineDriversStore.Drivers.TryGetValue(driverConnectionId, out var targetDriver))
            {
                await Clients.Caller.SendAsync(SignalRConstants.RequestFailed, new { Message = MessageConstants.DriverUnavailable });
                return;
            }
            if (targetDriver.AvailableSeats <= 0)
            {
                await Clients.Caller.SendAsync(SignalRConstants.RequestFailed, new { Message = MessageConstants.NoSeatsAvailable });
                return;
            }

            bool isExistingPassengerBoarded = OnlineDriversStore.ActiveRides.Values.Any(ride => ride.DriverConnectionId == driverConnectionId && ride.PinConfirmed);
            bool hasRunningRide = OnlineDriversStore.ActiveRides.Values.Any(ride => ride.DriverConnectionId == driverConnectionId);

            if (hasRunningRide && !isExistingPassengerBoarded)
            {
                await Clients.Caller.SendAsync(SignalRConstants.RequestFailed, new { Message = MessageConstants.DriverBusyPickingUp });
                return;
            }

            double distanceFromDriverPos = CalculateDistanceInKm(targetDriver.Latitude, targetDriver.Longitude, pickupLat, pickupLng);
            if (distanceFromDriverPos > AppConstants.MaxCarpoolDeviationKm)
            {
                await Clients.Caller.SendAsync(SignalRConstants.RequestFailed, new { Message = MessageConstants.DriverTooFar });
                return;
            }

            if (isExistingPassengerBoarded)
            {
                var boardedPassengerRide = OnlineDriversStore.ActiveRides.Values.First(ride => ride.DriverConnectionId == driverConnectionId && ride.PinConfirmed);
                double distanceFromExistingDestination = CalculateDistanceInKm(boardedPassengerRide.DropLat, boardedPassengerRide.DropLng, dropLat, dropLng);

                if (distanceFromExistingDestination > AppConstants.MaxCarpoolDeviationKm)
                {
                    await Clients.Caller.SendAsync(SignalRConstants.RequestFailed, new { Message = MessageConstants.DestinationTooFar });
                    return;
                }
            }

            OnlineDriversStore.BusyPassengers.TryAdd(callerConnectionId, 0);
            var tokenSource = new CancellationTokenSource();

            var newPendingRequest = new PendingRequest
            {
                PassengerConnectionId = callerConnectionId,
                PassengerId = passengerId,
                PassengerName = passengerName,
                PassengerPin = passengerPin,
                PickupLat = pickupLat,
                PickupLng = pickupLng,
                DropLat = dropLat,
                DropLng = dropLng,
                PickupName = pickupName,
                DropName = dropName,
                CancellationSource = tokenSource
            };

            var targetRequestQueue = OnlineDriversStore.PendingRequests.GetOrAdd(driverConnectionId, _ => new ConcurrentQueue<PendingRequest>());
            bool isQueueEmpty = targetRequestQueue.IsEmpty;
            targetRequestQueue.Enqueue(newPendingRequest);

            if (isQueueEmpty)
            {
                await Clients.Client(driverConnectionId).SendAsync(SignalRConstants.IncomingRideRequest, new
                {
                    PassengerId = passengerId,
                    PassengerConnectionId = callerConnectionId,
                    PassengerName = passengerName,
                    PickupLat = pickupLat,
                    PickupLng = pickupLng,
                    DropLat = dropLat,
                    DropLng = dropLng,
                    PickupName = pickupName,
                    DropName = dropName
                });

                await Clients.Caller.SendAsync(SignalRConstants.RequestSent, new { Message = MessageConstants.RequestSent });

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(AppConstants.DriverResponseTime, tokenSource.Token);
                        RemoveRequestFromQueue(driverConnectionId, callerConnectionId);
                        OnlineDriversStore.BusyPassengers.TryRemove(callerConnectionId, out _);
                        await Clients.Client(callerConnectionId).SendAsync(SignalRConstants.RequestTimeout, new { Message = MessageConstants.DriverDidNotRespond });
                        await Clients.Client(driverConnectionId).SendAsync(SignalRConstants.RequestExpired, new { Message = MessageConstants.RequestExpired });
                    }
                    catch (TaskCanceledException) { }
                });
            }
        }

        public async Task AcceptRideRequest()
        {
            var acceptingDriverConnectionId = Context.ConnectionId;
            if (!OnlineDriversStore.Drivers.TryGetValue(acceptingDriverConnectionId, out var acceptingDriver)) return;

            if (!OnlineDriversStore.PendingRequests.TryGetValue(acceptingDriverConnectionId, out var pendingQueue) || pendingQueue.IsEmpty)
            {
                await Clients.Caller.SendAsync(SignalRConstants.Error, new { Message = MessageConstants.NoPendingRequests });
                return;
            }

            if (!pendingQueue.TryDequeue(out var matchedRequest))
            {
                await Clients.Caller.SendAsync(SignalRConstants.Error, new { Message = MessageConstants.NoPendingRequests });
                return;
            }

            if (acceptingDriver.AvailableSeats > 0)
            {
                acceptingDriver.AvailableSeats--;
                if (acceptingDriver.AvailableSeats <= 0)
                    await Clients.All.SendAsync(SignalRConstants.DriverSeatsFull, new { DriverId = acceptingDriverConnectionId });
                else
                    await Clients.All.SendAsync(SignalRConstants.DriverSeatsUpdated, new { DriverId = acceptingDriverConnectionId, AvailableSeats = acceptingDriver.AvailableSeats });
            }

            OnlineDriversStore.PendingRequests[acceptingDriverConnectionId] = pendingQueue;
            matchedRequest.CancellationSource.Cancel();
            OnlineDriversStore.BusyPassengers.TryRemove(matchedRequest.PassengerConnectionId, out _);

            using var dependencyScope = _scopeFactory.CreateScope();
            var rideRepo = dependencyScope.ServiceProvider.GetRequiredService<IRideRepository>();
            var vehicleRepo = dependencyScope.ServiceProvider.GetRequiredService<IVehicleRepository>();

            int parsedDriverId = int.Parse(acceptingDriver.DriverUserId);
            int currentSessionId;
            var activeSession = await rideRepo.GetActiveSessionByDriverIdAsync(parsedDriverId);

            if (activeSession != null)
            {
                currentSessionId = activeSession.RideSessionId;
            }
            else
            {
                var driverVehicle = await vehicleRepo.GetByDriverIdAsync(parsedDriverId);
                var createdSession = new RideSession(parsedDriverId, driverVehicle!.VehicleId, matchedRequest.PickupName, matchedRequest.PickupLat, matchedRequest.PickupLng, matchedRequest.DropName, matchedRequest.DropLat, matchedRequest.DropLng);
                currentSessionId = await rideRepo.AddSessionAsync(createdSession);
            }

            var generatedRideId = Guid.NewGuid().ToString();
            OnlineDriversStore.ActiveRides[generatedRideId] = new ActiveRide
            {
                DriverConnectionId = acceptingDriverConnectionId,
                PassengerConnectionId = matchedRequest.PassengerConnectionId,
                PassengerId = matchedRequest.PassengerId,
                DriverId = acceptingDriver.DriverUserId,
                RideSessionId = currentSessionId,
                PassengerPin = matchedRequest.PassengerPin,
                PinConfirmed = false,
                PinAttempts = 0,
                PickupLat = matchedRequest.PickupLat,
                PickupLng = matchedRequest.PickupLng,
                DropLat = matchedRequest.DropLat,
                DropLng = matchedRequest.DropLng,
                PickupName = matchedRequest.PickupName,
                DropName = matchedRequest.DropName,
                FixedDistanceKm = 0
            };

            await Clients.Client(matchedRequest.PassengerConnectionId).SendAsync(SignalRConstants.RideAccepted, new
            {
                RideId = generatedRideId,
                DriverConnectionId = acceptingDriverConnectionId,
                DriverName = acceptingDriver.DriverName,
                VehicleModel = acceptingDriver.VehicleModel,
                DriverLat = acceptingDriver.Latitude,
                DriverLng = acceptingDriver.Longitude,
                RatePerKm = acceptingDriver.RatePerKm,
                Phone = acceptingDriver.Phone,
                VehicleNo = acceptingDriver.VehicleNo
            });

            await Clients.Caller.SendAsync(SignalRConstants.RideConfirmed, new
            {
                RideId = generatedRideId,
                PassengerName = matchedRequest.PassengerName,
                PassengerConnectionId = matchedRequest.PassengerConnectionId,
                PickupLat = matchedRequest.PickupLat,
                PickupLng = matchedRequest.PickupLng,
                DropLat = matchedRequest.DropLat,
                DropLng = matchedRequest.DropLng,
                PickupName = matchedRequest.PickupName,
                DropName = matchedRequest.DropName
            });

            var existingBoardedPassengers = OnlineDriversStore.ActiveRides.Values.Where(ride => ride.DriverConnectionId == acceptingDriverConnectionId && ride.PassengerConnectionId != matchedRequest.PassengerConnectionId && ride.PinConfirmed).ToList();

            foreach (var boardedPassenger in existingBoardedPassengers)
            {
                await Clients.Client(boardedPassenger.PassengerConnectionId).SendAsync(SignalRConstants.NewPassengerJoined, new
                {
                    Message = $"A new passenger ({matchedRequest.PassengerName}) is joining at {matchedRequest.PickupName}.",
                    PassengerName = matchedRequest.PassengerName,
                    PickupName = matchedRequest.PickupName,
                    PickupLat = matchedRequest.PickupLat,
                    PickupLng = matchedRequest.PickupLng,
                    DriverLat = acceptingDriver.Latitude,
                    DriverLng = acceptingDriver.Longitude
                });
            }

            if (!pendingQueue.IsEmpty && pendingQueue.TryPeek(out var upcomingRequest))
            {
                await Clients.Caller.SendAsync(SignalRConstants.IncomingRideRequest, new
                {
                    upcomingRequest.PassengerId,
                    upcomingRequest.PassengerConnectionId,
                    upcomingRequest.PassengerName,
                    upcomingRequest.PickupLat,
                    upcomingRequest.PickupLng,
                    upcomingRequest.DropLat,
                    upcomingRequest.DropLng,
                    upcomingRequest.PickupName,
                    upcomingRequest.DropName
                });
            }
        }

        public async Task RejectRideRequest()
        {
            var rejectingDriverConnectionId = Context.ConnectionId;
            if (!OnlineDriversStore.PendingRequests.TryGetValue(rejectingDriverConnectionId, out var pendingQueue) || !pendingQueue.TryDequeue(out var declinedRequest)) return;

            declinedRequest.CancellationSource.Cancel();
            OnlineDriversStore.BusyPassengers.TryRemove(declinedRequest.PassengerConnectionId, out _);
            await Clients.Client(declinedRequest.PassengerConnectionId).SendAsync(SignalRConstants.RideRejected, new { Message = MessageConstants.DriverDeclinedRequest });

            if (pendingQueue.TryPeek(out var nextQueuedReq))
            {
                await Clients.Caller.SendAsync(SignalRConstants.IncomingRideRequest, new
                {
                    nextQueuedReq.PassengerId,
                    nextQueuedReq.PassengerConnectionId,
                    nextQueuedReq.PassengerName,
                    nextQueuedReq.PickupLat,
                    nextQueuedReq.PickupLng,
                    nextQueuedReq.DropLat,
                    nextQueuedReq.DropLng,
                    nextQueuedReq.PickupName,
                    nextQueuedReq.DropName
                });
            }
        }

        public async Task ConfirmPin(string targetRideId, string inputPin)
        {
            if (!OnlineDriversStore.ActiveRides.TryGetValue(targetRideId, out var ongoingRide))
            {
                await Clients.Caller.SendAsync(SignalRConstants.PinError, new { Message = MessageConstants.RideNotFound });
                return;
            }
            if (ongoingRide.PinConfirmed)
            {
                await Clients.Caller.SendAsync(SignalRConstants.PinError, new { Message = MessageConstants.PinAlreadyConfirmed });
                return;
            }
            if (ongoingRide.PassengerPin != inputPin)
            {
                ongoingRide.PinAttempts++;
                int remainingAttempts = AppConstants.PinAttempt - ongoingRide.PinAttempts;
                if (ongoingRide.PinAttempts >= AppConstants.PinAttempt)
                {
                    await CancelRide(targetRideId, RoleConstants.SystemTooManyPins);
                    return;
                }
                await Clients.Caller.SendAsync(SignalRConstants.PinError, new { Message = $"Wrong PIN. {remainingAttempts} attempt{(remainingAttempts == 1 ? string.Empty : "s")} left." });
                return;
            }

            ongoingRide.PinConfirmed = true;
            if (!OnlineDriversStore.Drivers.TryGetValue(ongoingRide.DriverConnectionId, out var carryingDriver)) return;

            using var scope = _scopeFactory.CreateScope();
            var vehicleRepo = scope.ServiceProvider.GetRequiredService<IVehicleRepository>();
            var assignedVehicle = await vehicleRepo.GetByDriverIdAsync(int.Parse(carryingDriver.DriverUserId));

            if (assignedVehicle != null)
            {
                assignedVehicle.ReserveSeat();
                await vehicleRepo.UpdateAsync(assignedVehicle);
            }

            await Clients.Client(ongoingRide.PassengerConnectionId).SendAsync(SignalRConstants.PinConfirmed, new { Message = MessageConstants.PinConfirmedEnjoy });
            await Clients.Caller.SendAsync(SignalRConstants.PinConfirmed, new { Message = MessageConstants.PinConfirmedBoarded });

            var nearestWaitingPassenger = OnlineDriversStore.ActiveRides.Values.Where(r => r.DriverConnectionId == ongoingRide.DriverConnectionId && r.PassengerConnectionId != ongoingRide.PassengerConnectionId && !r.PinConfirmed).OrderBy(r => CalculateDistanceInKm(carryingDriver.Latitude, carryingDriver.Longitude, r.PickupLat, r.PickupLng)).FirstOrDefault();

            if (nearestWaitingPassenger != null)
            {
                await Clients.Client(ongoingRide.PassengerConnectionId).SendAsync(SignalRConstants.DriverPickingUpOther, new
                {
                    Message = $"Driver is picking up one more passenger ({nearestWaitingPassenger.PickupName}) before heading to destination.",
                    NextPickupName = nearestWaitingPassenger.PickupName,
                    NextPickupLat = nearestWaitingPassenger.PickupLat,
                    NextPickupLng = nearestWaitingPassenger.PickupLng,
                    DriverLat = carryingDriver.Latitude,
                    DriverLng = carryingDriver.Longitude
                });
            }
        }

        public async Task DriverArrivedAtPickup(string trackingRideId)
        {
            if (!OnlineDriversStore.ActiveRides.TryGetValue(trackingRideId, out var targetRide)) return;
            await Clients.Client(targetRide.PassengerConnectionId).SendAsync(SignalRConstants.DriverArrived, new { Message = MessageConstants.DriverArrivedPickup });
            await Clients.Caller.SendAsync(SignalRConstants.DriverArrived, new { Message = MessageConstants.ArrivedAskPin });
        }

        public async Task CancelRide(string rideIdToCancel, string userRole)
        {
            if (!OnlineDriversStore.ActiveRides.TryGetValue(rideIdToCancel, out var rideData))
            {
                await Clients.Caller.SendAsync(SignalRConstants.CancelError, new { Message = MessageConstants.RideNotFound });
                return;
            }
            if (rideData.PinConfirmed)
            {
                await Clients.Caller.SendAsync(SignalRConstants.CancelError, new { Message = MessageConstants.CannotCancelAfterPin });
                return;
            }

            OnlineDriversStore.ActiveRides.TryRemove(rideIdToCancel, out _);

            if (OnlineDriversStore.Drivers.TryGetValue(rideData.DriverConnectionId, out var operatingDriver))
            {
                operatingDriver.AvailableSeats++;
                await Clients.All.SendAsync(SignalRConstants.DriverSeatsUpdated, new { DriverId = rideData.DriverConnectionId, AvailableSeats = operatingDriver.AvailableSeats });
            }

            var cancellationData = new { Message = $"Ride cancelled by {userRole}.", CancelledBy = userRole, RideId = rideIdToCancel };
            await Clients.Client(rideData.DriverConnectionId).SendAsync(SignalRConstants.RideCancelled, cancellationData);
            await Clients.Client(rideData.PassengerConnectionId).SendAsync(SignalRConstants.RideCancelled, cancellationData);
        }

        public async Task RideCompleted(string completedRideId, double travelDistanceKm)
        {
            if (!OnlineDriversStore.ActiveRides.TryGetValue(completedRideId, out var finishedRide)) return;
            if (!OnlineDriversStore.Drivers.TryGetValue(finishedRide.DriverConnectionId, out var drivingUser)) return;

            OnlineDriversStore.ActiveRides.TryRemove(completedRideId, out _);
            drivingUser.AvailableSeats++;

            using var scope = _scopeFactory.CreateScope();
            var rideRepo = scope.ServiceProvider.GetRequiredService<IRideRepository>();
            var vehicleRepo = scope.ServiceProvider.GetRequiredService<IVehicleRepository>();

            int driverNumericId = int.Parse(drivingUser.DriverUserId);
            var driverVehicle = await vehicleRepo.GetByDriverIdAsync(driverNumericId);

            if (driverVehicle != null)
            {
                driverVehicle.ReleaseSeat();
                await vehicleRepo.UpdateAsync(driverVehicle);
            }

            await Clients.All.SendAsync(SignalRConstants.DriverSeatsUpdated, new { DriverId = finishedRide.DriverConnectionId, AvailableSeats = drivingUser.AvailableSeats });
            double computedCost = Math.Round(travelDistanceKm * (double)drivingUser.RatePerKm, AppConstants.CostRoundingPrecision);

            var savedRequestRecord = new RideRequest(finishedRide.RideSessionId, int.Parse(finishedRide.PassengerId), finishedRide.PickupName, finishedRide.PickupLat, finishedRide.PickupLng, finishedRide.DropName, finishedRide.DropLat, finishedRide.DropLng, travelDistanceKm, (decimal)computedCost);
            savedRequestRecord.Complete();
            await rideRepo.AddRequestAsync(savedRequestRecord);

            var connectedSession = await rideRepo.GetSessionByIdAsync(finishedRide.RideSessionId);
            if (connectedSession != null)
            {
                connectedSession.AddFare((decimal)computedCost);
                connectedSession.AddDistance(travelDistanceKm);
            }

            bool isVehicleEmpty = !OnlineDriversStore.ActiveRides.Values.Any(ride => ride.DriverConnectionId == finishedRide.DriverConnectionId);
            if (isVehicleEmpty)
            {
                connectedSession?.Complete();
                if (driverVehicle != null && drivingUser.AvailableSeats != (driverVehicle.TotalSeats - 1))
                {
                    driverVehicle.ResetAvailableSeats();
                    await vehicleRepo.UpdateAsync(driverVehicle);
                    drivingUser.AvailableSeats = driverVehicle.AvailableSeats;
                    await Clients.All.SendAsync(SignalRConstants.DriverSeatsUpdated, new { DriverId = finishedRide.DriverConnectionId, AvailableSeats = drivingUser.AvailableSeats });
                }
            }

            if (connectedSession != null) await rideRepo.UpdateSessionAsync(connectedSession);
            await Clients.Client(finishedRide.PassengerConnectionId).SendAsync(SignalRConstants.RideCompleted, new { DriverName = drivingUser.DriverName, TotalKm = travelDistanceKm, RatePerKm = drivingUser.RatePerKm, TotalCost = computedCost });

            var onboardedRidesList = OnlineDriversStore.ActiveRides.Values.Where(r => r.DriverConnectionId == finishedRide.DriverConnectionId && r.PinConfirmed).ToList();
            if (onboardedRidesList.Any())
            {
                var upcomingDropRide = onboardedRidesList.OrderBy(r => CalculateDistanceInKm(drivingUser.Latitude, drivingUser.Longitude, r.DropLat, r.DropLng)).First();
                foreach (var remainingTrip in onboardedRidesList)
                {
                    await Clients.Client(remainingTrip.PassengerConnectionId).SendAsync(SignalRConstants.NextDropUpdate, new
                    {
                        NextDropName = upcomingDropRide.DropName,
                        NextDropLat = upcomingDropRide.DropLat,
                        NextDropLng = upcomingDropRide.DropLng,
                        DriverLat = drivingUser.Latitude,
                        DriverLng = drivingUser.Longitude,
                        StopsRemaining = onboardedRidesList.Count
                    });
                }
            }
            await Clients.Caller.SendAsync(SignalRConstants.RideCompleted, new { RideId = completedRideId, TotalKm = travelDistanceKm, RatePerKm = drivingUser.RatePerKm, TotalCost = computedCost, DriverLat = drivingUser.Latitude, DriverLng = drivingUser.Longitude });
        }

        public async Task PassengerLeft(string leftPassengerId)
        {
            var storedRideEntry = OnlineDriversStore.ActiveRides.FirstOrDefault(kv => kv.Value.PassengerId == leftPassengerId);
            if (storedRideEntry.Value != null && !storedRideEntry.Value.PinConfirmed) await CancelRide(storedRideEntry.Key, RoleConstants.Passenger);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var disconnectConnectionId = Context.ConnectionId;
            var driverHostedRides = OnlineDriversStore.ActiveRides.Where(kv => kv.Value.DriverConnectionId == disconnectConnectionId).ToList();

            foreach (var rideItem in driverHostedRides)
            {
                if (!rideItem.Value.PinConfirmed)
                {
                    OnlineDriversStore.ActiveRides.TryRemove(rideItem.Key, out _);
                    await Clients.Client(rideItem.Value.PassengerConnectionId).SendAsync(SignalRConstants.RideCancelled, new { Message = MessageConstants.DriverDisconnected, CancelledBy = RoleConstants.Driver, RideId = rideItem.Key });
                }
            }

            var passengerHostedRide = OnlineDriversStore.ActiveRides.FirstOrDefault(kv => kv.Value.PassengerConnectionId == disconnectConnectionId);
            if (passengerHostedRide.Value != null && !passengerHostedRide.Value.PinConfirmed)
            {
                OnlineDriversStore.ActiveRides.TryRemove(passengerHostedRide.Key, out _);
                await Clients.Client(passengerHostedRide.Value.DriverConnectionId).SendAsync(SignalRConstants.RideCancelled, new { Message = MessageConstants.PassengerDisconnected, CancelledBy = RoleConstants.Passenger, RideId = passengerHostedRide.Key });
            }

            await CleanupDriverSession(disconnectConnectionId);
            await Clients.All.SendAsync(SignalRConstants.DriverOffline, new { DriverId = disconnectConnectionId });
            await base.OnDisconnectedAsync(exception);
        }

        private async Task CleanupDriverSession(string driverCleanupConnectionId)
        {
            if (OnlineDriversStore.PendingRequests.TryRemove(driverCleanupConnectionId, out var queuedRequests))
            {
                foreach (var singleReq in queuedRequests)
                {
                    singleReq.CancellationSource.Cancel();
                    OnlineDriversStore.BusyPassengers.TryRemove(singleReq.PassengerConnectionId, out _);
                    await Clients.Client(singleReq.PassengerConnectionId).SendAsync(SignalRConstants.RequestFailed, new { Message = MessageConstants.DriverWentOffline });
                }
            }
            OnlineDriversStore.Drivers.TryRemove(driverCleanupConnectionId, out _);
        }

        private void RemoveRequestFromQueue(string currentDriverId, string targetPassengerId)
        {
            if (!OnlineDriversStore.PendingRequests.TryGetValue(currentDriverId, out var existingQueue)) return;
            var retainedRequests = new List<PendingRequest>();
            while (existingQueue.TryDequeue(out var reqToProcess))
                if (reqToProcess.PassengerConnectionId != targetPassengerId) retainedRequests.Add(reqToProcess);
            OnlineDriversStore.PendingRequests[currentDriverId] = new ConcurrentQueue<PendingRequest>(retainedRequests);
        }

        private static double CalculateDistanceInKm(double latitude1, double longitude1, double latitude2, double longitude2)
        {
            double differenceLat = latitude2 - latitude1;
            double differenceLng = (longitude2 - longitude1) * Math.Cos(latitude1 * Math.PI / 180);
            return Math.Sqrt(differenceLat * differenceLat + differenceLng * differenceLng) * AppConstants.KmConversionFactor;
        }
    }
}