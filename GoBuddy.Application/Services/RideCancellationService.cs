using GoBuddy.Application.Interfaces;
using GoBuddy.Domain.Enums;
using GoBuddy.Application.Interfaces.Repositories;

namespace GoBuddy.BusinessLayer.Services
{
    public class RideCancellationService : IRideCancellationService
    {
        private readonly IRideRequestRepository rideRequestRepository;
        private readonly IRideSessionRepository rideSessionRepository;

        public RideCancellationService(
            IRideRequestRepository rideRequestRepository,
            IRideSessionRepository rideSessionRepository)
        {
            this.rideRequestRepository = rideRequestRepository;
            this.rideSessionRepository = rideSessionRepository;
        }

        public async Task CancelRideByPassengerAsync(int rideRequestId)
        {
            var rideRequest = await rideRequestRepository.GetByIdAsync(rideRequestId);

            if (rideRequest == null)
                throw new ApplicationException("Ride request not found");

            var rideSession = await rideSessionRepository.GetByIdAsync(rideRequest.RideSessionId);

            if (rideSession == null)
                throw new ApplicationException("Ride session not found");

            if (rideSession.Status != RideSessionStatus.Active)
                throw new ApplicationException("Ride already cancelled or completed");

            if (rideRequest.Status == RideRequestStatus.Cancelled)
                throw new ApplicationException("Ride already cancelled");

            rideRequest.Cancel();

            rideSession.Cancel();

            await rideSessionRepository.SaveChangesAsync();
        }

        public async Task CancelRideByDriverAsync(int driverId)
        {
            var rideSession = await rideSessionRepository.GetByDriverIdAsync(driverId);

            if (rideSession == null)
                throw new ApplicationException("Ride session not found");

            if (rideSession.Status != RideSessionStatus.Active)
                throw new ApplicationException("Ride already cancelled or completed");

            var rideRequests = await rideRequestRepository.GetByRideSessionIdAsync(rideSession.RideSessionId);

            if (rideRequests.Any(r => r.Status == RideRequestStatus.Cancelled))
                throw new ApplicationException("Passenger already cancelled the ride");

            rideSession.Cancel();

            foreach (var request in rideRequests)
            {
                if (request.Status != RideRequestStatus.Cancelled)
                {
                    request.Cancel();
                }
            }

            await rideSessionRepository.SaveChangesAsync();
        }
    }
}